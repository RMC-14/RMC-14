using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Sprite;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Strip;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Fireman;

public sealed class FiremanCarrySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<(EntityUid Target, EntityUid Carrier)> _toReparent = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<FiremanCarriableComponent, CanDragEvent>(OnCarriableCanDrag);
        SubscribeLocalEvent<FiremanCarriableComponent, DragDropDraggedEvent>(OnCarriableDragDropDragged, before: [typeof(SharedStrippableSystem)]);
        SubscribeLocalEvent<FiremanCarriableComponent, DoAfterAttemptEvent<FiremanCarryDoAfterEvent>>(OnCarriableFiremanCarryDoAfterAttempt);
        SubscribeLocalEvent<FiremanCarriableComponent, FiremanCarryDoAfterEvent>(OnCarriableFiremanCarryDoAfter);
        SubscribeLocalEvent<FiremanCarriableComponent, StandAttemptEvent>(OnCarriableStandAttempt);
        SubscribeLocalEvent<FiremanCarriableComponent, UpdateCanMoveEvent>(OnCarriableCanMove);
        SubscribeLocalEvent<FiremanCarriableComponent, MoveInputEvent>(OnCarriableMoveInput);
        SubscribeLocalEvent<FiremanCarriableComponent, BreakFiremanCarryDoAfterEvent>(OnCarriableBreakCarryDoAfter);
        SubscribeLocalEvent<FiremanCarriableComponent, PullStartedMessage>(OnCarriablePullStarted);
        SubscribeLocalEvent<FiremanCarriableComponent, PullStoppedMessage>(OnCarriablePullStopped);
        SubscribeLocalEvent<FiremanCarriableComponent, PullAttemptEvent>(OnCarriablePullAttempt);

        SubscribeLocalEvent<CanFiremanCarryComponent, PullStartedMessage>(OnCarrierPullStarted);
        SubscribeLocalEvent<CanFiremanCarryComponent, PullStoppedMessage>(OnCarrierPullStopped);
        SubscribeLocalEvent<CanFiremanCarryComponent, PullSlowdownAttemptEvent>(OnCarrierPullSlowdownAttempt);
        SubscribeLocalEvent<CanFiremanCarryComponent, MobStateChangedEvent>(OnCarrierMobStateChanged);
        SubscribeLocalEvent<CanFiremanCarryComponent, RMCPullToggleEvent>(OnCarrierPullToggle);

        SubscribeLocalEvent<BeingFiremanCarriedComponent, PreventCollideEvent>(OnBeingCarriedPreventCollide);
    }

    private void OnCarriableCanDrag(Entity<FiremanCarriableComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCarriableDragDropDragged(Entity<FiremanCarriableComponent> ent, ref DragDropDraggedEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out CanFiremanCarryComponent? carrier) || args.Target != user)
            return;

        if (!_rmcPulling.IsPulling(user, ent.Owner))
            return;

        args.Handled = true;
        if (!_skills.HasSkill(user, ent.Comp.Skill, 1))
        {
            _popup.PopupClient(Loc.GetString("You aren't trained to carry people!"), ent, user, PopupType.MediumCaution);
            return;
        }

        if (!carrier.AggressiveGrab)
        {
            _popup.PopupClient(Loc.GetString("You need to grab them aggressively first!"), ent, user, PopupType.MediumCaution);
            return;
        }

        var delay = ent.Comp.Delay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
        var ev = new FiremanCarryDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, ent, user)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            ForceVisible = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        var target = Identity.Name(ent, EntityManager, args.User);
        _popup.PopupClient(Loc.GetString($"You start loading {target} onto your back."), ent, user, PopupType.Medium);
    }

    private void OnCarriableFiremanCarryDoAfterAttempt(Entity<FiremanCarriableComponent> ent, ref DoAfterAttemptEvent<FiremanCarryDoAfterEvent> args)
    {
        if (!_rmcPulling.IsPulling(args.DoAfter.Args.User, ent.Owner))
            args.Cancel();
    }

    private void OnCarriableFiremanCarryDoAfter(Entity<FiremanCarriableComponent> ent, ref FiremanCarryDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp(user, out CanFiremanCarryComponent? carrier))
            return;

        if (_transform.IsParentOf(Transform(ent.Owner), user))
            return;

        ent.Comp.BeingCarried = true;
        Dirty(ent);

        EnsureComp<BeingFiremanCarriedComponent>(ent);

        carrier.Carrying = ent;
        Dirty(user, carrier);

        if (!_timing.ApplyingState && !HasComp<MouseRotatorComponent>(user))
            RemCompDeferred<NoRotateOnMoveComponent>(user);

        args.Handled = true;

        _transform.SetParent(ent, user);
        _transform.SetLocalPosition(ent, Vector2.Zero);
        _standing.Down(ent, changeCollision: true);

        _movementSpeed.RefreshMovementSpeedModifiers(user);
        _rmcSprite.SetRenderOrder(user, 1);
    }

    private void OnCarriableStandAttempt(Entity<FiremanCarriableComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.BeingCarried || IsBeingAggressivelyGrabbed(ent))
            args.Cancel();
    }

    private void OnCarriableCanMove(Entity<FiremanCarriableComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (IsBeingAggressivelyGrabbed(ent))
            args.Cancel();
    }

    private void OnCarriableMoveInput(Entity<FiremanCarriableComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement || ent.Comp.BreakingFree)
            return;

        if (!ent.Comp.BeingCarried && !IsBeingAggressivelyGrabbed(ent))
            return;

        var ev = new BreakFiremanCarryDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, ev, ent);
        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        ent.Comp.BreakingFree = true;
        if (!_rmcPulling.IsBeingPulled(ent.Owner, out var puller))
            return;

        var selfMsg = Loc.GetString("rmc-pull-break-start-self", ("puller", puller));
        _popup.PopupClient(selfMsg, ent, ent, PopupType.MediumCaution);

        var others = Filter.PvsExcept(ent, entityManager: EntityManager);
        foreach (var other in others.Recipients)
        {
            if (other.AttachedEntity is not { } recipient)
                continue;

            var pullerName = Identity.Name(puller, EntityManager, recipient);
            var pulledName = Identity.Name(ent, EntityManager, recipient);
            var msg = Loc.GetString("rmc-pull-break-start-others", ("puller", pullerName), ("pulled", pulledName));
            _popup.PopupEntity(msg, ent, recipient, PopupType.MediumCaution);
        }
    }

    private void OnCarriableBreakCarryDoAfter(Entity<FiremanCarriableComponent> ent, ref BreakFiremanCarryDoAfterEvent args)
    {
        ent.Comp.BreakingFree = false;
        Dirty(ent);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        ent.Comp.BeingCarried = false;
        RemCompDeferred<BeingFiremanCarriedComponent>(ent);

        if (!_rmcPulling.IsBeingPulled(ent.Owner, out var puller))
            return;

        StopCarry(puller, (ent, ent));
        var selfMsg = Loc.GetString("rmc-pull-break-finish-self", ("puller", puller));
        _popup.PopupClient(selfMsg, ent, ent, PopupType.MediumCaution);

        var others = Filter.PvsExcept(ent, entityManager: EntityManager);
        foreach (var other in others.Recipients)
        {
            if (other.AttachedEntity is not { } recipient)
                continue;

            var pullerName = Identity.Name(puller, EntityManager, recipient);
            var pulledName = Identity.Name(ent, EntityManager, recipient);
            var msg = Loc.GetString("rmc-pull-break-finish-others", ("puller", pullerName), ("pulled", pulledName));
            _popup.PopupEntity(msg, ent, recipient, PopupType.MediumCaution);
        }
    }

    private void OnCarriablePullStarted(Entity<FiremanCarriableComponent> ent, ref PullStartedMessage args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        if (_rmcPulling.IsBeingPulled(ent.Owner, out var puller) &&
            TryComp(puller, out CanFiremanCarryComponent? carrier))
        {
            StopPull((puller, carrier), ent);
        }
    }

    private void OnCarriablePullStopped(Entity<FiremanCarriableComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner != args.PulledUid)
            return;

        _standing.Stand(ent);
    }

    private void OnCarriablePullAttempt(Entity<FiremanCarriableComponent> ent, ref PullAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.BeingCarried || IsBeingAggressivelyGrabbed(ent))
            args.Cancelled = true;
    }

    private void OnCarrierPullStarted(Entity<CanFiremanCarryComponent> ent, ref PullStartedMessage args)
    {
        if (ent.Owner == args.PullerUid)
            StopPull(ent, args.PulledUid);
    }

    private void OnCarrierPullStopped(Entity<CanFiremanCarryComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner == args.PullerUid)
            StopPull(ent, args.PulledUid);
    }

    private void OnCarrierPullSlowdownAttempt(Entity<CanFiremanCarryComponent> ent, ref PullSlowdownAttemptEvent args)
    {
        if (ent.Comp.Carrying == args.Target)
            args.Cancelled = true;
    }

    private void OnCarrierMobStateChanged(Entity<CanFiremanCarryComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Carrying is not { } carrying)
            return;

        if (args.NewMobState == MobState.Dead)
            StopCarry((ent, ent), carrying);
    }

    private void OnCarrierPullToggle(Entity<CanFiremanCarryComponent> ent, ref RMCPullToggleEvent args)
    {
        args.Handled = true;

        var grabDelay = ent.Comp.AggressiveGrabDelay * _skills.GetSkillDelayMultiplier(ent.Owner, ent.Comp.Skill);
        if (ent.Comp.AggressiveGrab ||
            _timing.CurTime < ent.Comp.PullTime + grabDelay)
        {
            return;
        }

        ent.Comp.AggressiveGrab = true;
        Dirty(ent);

        if (!TryComp(ent, out PullerComponent? puller) ||
            puller.Pulling is not { } pulling)
        {
            return;
        }

        _actionBlocker.UpdateCanMove(pulling);
        _standing.Down(pulling, changeCollision: true);
        _rmcPulling.PlayPullEffect(ent, pulling);

        var selfMsg = Loc.GetString("rmc-pull-aggressive-self", ("pulled", pulling));
        _popup.PopupClient(selfMsg, pulling, ent, PopupType.SmallCaution);

        var others = Filter.PvsExcept(ent, entityManager: EntityManager);
        foreach (var other in others.Recipients)
        {
            if (other.AttachedEntity is not { } recipient)
                continue;

            var pullerName = Identity.Name(ent, EntityManager, recipient);
            var pulledName = Identity.Name(pulling, EntityManager, recipient);
            var msg = Loc.GetString("rmc-pull-aggressive-others", ("puller", pullerName), ("pulled", pulledName));
            _popup.PopupEntity(msg, ent, recipient, PopupType.SmallCaution);
        }
    }

    private void OnBeingCarriedPreventCollide(Entity<BeingFiremanCarriedComponent> ent, ref PreventCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void StopPull(Entity<CanFiremanCarryComponent> ent, EntityUid target)
    {
        if (_timing.ApplyingState)
            return;

        StopCarry((ent, ent), target);
        _actionBlocker.UpdateCanMove(target);

        ent.Comp.PullTime = _timing.CurTime;
        Dirty(ent);
    }

    private void StopCarry(Entity<CanFiremanCarryComponent?> user, Entity<FiremanCarriableComponent?>? targetNullable)
    {
        EntityUid? carrying = null;
        if (Resolve(user, ref user.Comp, false))
        {
            carrying = user.Comp.Carrying;
            user.Comp.Carrying = null;
            user.Comp.AggressiveGrab = false;
            Dirty(user);

            _rmcSprite.SetRenderOrder(user, 0);
        }

        if (targetNullable is not { } target)
            return;

        if (Resolve(target, ref target.Comp, false))
        {
            target.Comp.BeingCarried = false;
            Dirty(target);

            RemCompDeferred<BeingFiremanCarriedComponent>(target);

            if (carrying == target)
                _toReparent.Add((target, user));
        }

        _standing.Stand(target);
        _actionBlocker.UpdateCanMove(target);
    }

    private bool IsBeingAggressivelyGrabbed(EntityUid target)
    {
        return _rmcPulling.IsBeingPulled(target, out var user) &&
               TryComp(user, out CanFiremanCarryComponent? carrier) &&
               carrier.AggressiveGrab;
    }

    public override void Update(float frameTime)
    {
        try
        {
            foreach (var (target, carrier) in _toReparent)
            {
                if (TerminatingOrDeleted(target))
                    continue;

                if (TerminatingOrDeleted(carrier))
                {
                    var coordinates = _transform.GetMoverCoordinates(target);
                    if (TerminatingOrDeleted(coordinates.EntityId))
                        continue;

                    _transform.SetCoordinates(target, coordinates);
                    continue;
                }

                var parent = _transform.GetMoverCoordinates(target).EntityId;
                if (target == parent)
                    continue;

                _transform.SetParent(target, parent);
            }
        }
        finally
        {
            _toReparent.Clear();
        }
    }
}
