using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Strip;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Fireman;

public sealed class FiremanCarrySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private readonly List<(EntityUid Target, EntityUid Carrier)> _toReparent = new();

    private EntityQuery<FiremanCarriableComponent> _firemanQuery;
    private EntityQuery<HandsComponent> _handsQuery;

    public override void Initialize()
    {
        _firemanQuery = GetEntityQuery<FiremanCarriableComponent>();
        _handsQuery = GetEntityQuery<HandsComponent>();

        SubscribeLocalEvent<FiremanCarriableComponent, CanDragEvent>(OnCarriableCanDrag);
        SubscribeLocalEvent<FiremanCarriableComponent, CanDropDraggedEvent>(OnCarriableCanDropDragged);
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

        SubscribeLocalEvent<CanFiremanCarryComponent, CanDropTargetEvent>(OnCarrierCanDropTarget);
        SubscribeLocalEvent<CanFiremanCarryComponent, PullStartedMessage>(OnCarrierPullStarted);
        SubscribeLocalEvent<CanFiremanCarryComponent, PullStoppedMessage>(OnCarrierPullStopped);
        SubscribeLocalEvent<CanFiremanCarryComponent, PullSlowdownAttemptEvent>(OnCarrierPullSlowdownAttempt);
        SubscribeLocalEvent<CanFiremanCarryComponent, MobStateChangedEvent>(OnCarrierMobStateChanged);
        SubscribeLocalEvent<CanFiremanCarryComponent, RMCPullToggleEvent>(OnCarrierPullToggle);
        SubscribeLocalEvent<CanFiremanCarryComponent, BeforeThrowEvent>(OnCarrierBeforeThrow);

        SubscribeLocalEvent<BeingFiremanCarriedComponent, PreventCollideEvent>(OnBeingCarriedPreventCollide);
    }

    private void OnCarriableCanDrag(Entity<FiremanCarriableComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCarriableCanDropDragged(Entity<FiremanCarriableComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.User != args.Target)
            return;

        if (!TryComp(args.Target, out CanFiremanCarryComponent? carrier))
            return;

        if (!_rmcPulling.IsPulling(args.User, ent.Owner))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnCarrierCanDropTarget(Entity<CanFiremanCarryComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.User != ent.Owner)
            return;

        if (!TryComp(args.Dragged, out FiremanCarriableComponent? carriable))
            return;

        if (!_rmcPulling.IsPulling(args.User, args.Dragged))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnCarrierBeforeThrow(Entity<CanFiremanCarryComponent> carrier, ref BeforeThrowEvent args)
    {
        if (args.Cancelled)
            return;

        var actualItem = GetActualItem(args.ItemUid);
        if (!TryComp(actualItem, out FiremanCarriableComponent? carriable))
            return;

        if (!ValidateThrow(carrier, actualItem, carriable, args.ItemUid))
        {
            args.Cancelled = true;
            return;
        }

        args.ItemUid = actualItem;
    }

    private EntityUid GetActualItem(EntityUid itemUid)
    {
        if (TryComp(itemUid, out VirtualItemComponent? virtualItem))
            return virtualItem.BlockingEntity;

        return itemUid;
    }

    private bool ValidateThrow(Entity<CanFiremanCarryComponent> carrier, EntityUid target, FiremanCarriableComponent carriable, EntityUid heldItem)
    {
        if (!carriable.BeingCarried)
        {
            _popup.PopupClient(Loc.GetString("rmc-fireman-carry-need-carry-first"), carrier, carrier, PopupType.SmallCaution);
            return false;
        }

        if (!carriable.CanThrow)
        {
            _popup.PopupClient(Loc.GetString("rmc-fireman-carry-cant-throw"), carrier, carrier, PopupType.SmallCaution);
            return false;
        }

        if (!ValidateCarrierWhitelist(carrier, target, carriable))
        {
            _popup.PopupClient(Loc.GetString("rmc-fireman-carry-cant-throw"), carrier, carrier, PopupType.SmallCaution);
            return false;
        }

        if (!_handsQuery.TryComp(carrier, out var hands))
            return false;

        if (!_hands.IsHolding((carrier.Owner, hands), heldItem))
            return false;

        return true;
    }

    private void OnCarriableDragDropDragged(Entity<FiremanCarriableComponent> ent, ref DragDropDraggedEvent args)
    {
        if (!TryComp(args.User, out CanFiremanCarryComponent? carrier) || args.Target != args.User)
            return;

        if (!_rmcPulling.IsPulling(args.User, ent.Owner))
            return;

        args.Handled = true;

        if (!CanCarryEntity(args.User, ent.Owner, carrier, ent.Comp))
            return;

        if (!carrier.AggressiveGrab)
        {
            _popup.PopupClient(Loc.GetString("rmc-fireman-carry-need-aggressive-grab"), ent, args.User, PopupType.MediumCaution);
            return;
        }

        StartCarryDoAfter(args.User, ent);
    }

    private void StartCarryDoAfter(EntityUid user, Entity<FiremanCarriableComponent> target)
    {
        var delay = target.Comp.Delay * _skills.GetSkillDelayMultiplier(user, target.Comp.Skill);
        var ev = new FiremanCarryDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, target, target, user)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            ForceVisible = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        var targetName = Identity.Name(target, EntityManager, user);
        _popup.PopupClient(Loc.GetString("rmc-fireman-carry-start-loading", ("targetName", targetName)), target, user, PopupType.Medium);
    }

    private bool CanCarryEntity(EntityUid user, EntityUid target, CanFiremanCarryComponent carrier, FiremanCarriableComponent carriable)
    {
        if (!ValidateCarrierWhitelist(user, target, carriable))
            return false;

        if (carriable.CarrierWhitelist == null && !_skills.HasSkill(user, carriable.Skill, 1))
        {
            _popup.PopupClient(Loc.GetString("rmc-fireman-carry-not-trained"), target, user, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private bool ValidateCarrierWhitelist(EntityUid carrier, EntityUid target, FiremanCarriableComponent carriable)
    {
        if (carriable.CarrierWhitelist == null)
            return true;

        return _whitelist.IsValid(carriable.CarrierWhitelist, carrier);
    }

    private void OnCarriableFiremanCarryDoAfterAttempt(Entity<FiremanCarriableComponent> ent, ref DoAfterAttemptEvent<FiremanCarryDoAfterEvent> args)
    {
        if (!_rmcPulling.IsPulling(args.DoAfter.Args.User, ent.Owner))
            args.Cancel();
    }

    private void OnCarriableFiremanCarryDoAfter(Entity<FiremanCarriableComponent> ent, ref FiremanCarryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp(args.User, out CanFiremanCarryComponent? carrier))
            return;

        if (_transform.IsParentOf(Transform(ent.Owner), args.User))
            return;

        StartCarrying(args.User, carrier, ent);
        args.Handled = true;
    }

    private void StartCarrying(EntityUid user, CanFiremanCarryComponent carrier, Entity<FiremanCarriableComponent> target)
    {
        target.Comp.BeingCarried = true;
        Dirty(target);

        EnsureComp<BeingFiremanCarriedComponent>(target);

        carrier.Carrying = target;
        Dirty(user, carrier);

        if (!_timing.ApplyingState && !HasComp<MouseRotatorComponent>(user))
            RemCompDeferred<NoRotateOnMoveComponent>(user);

        _transform.SetParent(target, user);
        _transform.SetLocalPosition(target, Vector2.Zero);
        _standing.Down(target, changeCollision: true);

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

        StartBreakFreeDoAfter(ent);
    }

    private void StartBreakFreeDoAfter(Entity<FiremanCarriableComponent> ent)
    {
        var ev = new BreakFiremanCarryDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, ev, ent);
        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        ent.Comp.BreakingFree = true;

        if (!_rmcPulling.IsBeingPulled(ent.Owner, out var puller))
            return;

        ShowBreakFreeMessages(ent, puller);
    }

    private void ShowBreakFreeMessages(EntityUid target, EntityUid puller)
    {
        var selfMsg = Loc.GetString("rmc-pull-break-start-self", ("puller", puller));
        _popup.PopupClient(selfMsg, target, target, PopupType.MediumCaution);

        var others = Filter.PvsExcept(target, entityManager: EntityManager);
        foreach (var other in others.Recipients)
        {
            if (other.AttachedEntity is not { } recipient)
                continue;

            var pullerName = Identity.Name(puller, EntityManager, recipient);
            var pulledName = Identity.Name(target, EntityManager, recipient);
            var msg = Loc.GetString("rmc-pull-break-start-others", ("puller", pullerName), ("pulled", pulledName));
            _popup.PopupEntity(msg, target, recipient, PopupType.MediumCaution);
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
        ShowBreakFreeFinishMessages(ent, puller);
    }

    private void ShowBreakFreeFinishMessages(EntityUid target, EntityUid puller)
    {
        var selfMsg = Loc.GetString("rmc-pull-break-finish-self", ("puller", puller));
        _popup.PopupClient(selfMsg, target, target, PopupType.MediumCaution);

        var others = Filter.PvsExcept(target, entityManager: EntityManager);
        foreach (var other in others.Recipients)
        {
            if (other.AttachedEntity is not { } recipient)
                continue;

            var pullerName = Identity.Name(puller, EntityManager, recipient);
            var pulledName = Identity.Name(target, EntityManager, recipient);
            var msg = Loc.GetString("rmc-pull-break-finish-others", ("puller", pullerName), ("pulled", pulledName));
            _popup.PopupEntity(msg, target, recipient, PopupType.MediumCaution);
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

        if (!CanStartAggressiveGrab(ent))
            return;

        if (!TryComp(ent, out PullerComponent? puller) || puller.Pulling is not { } pulling)
            return;

        if (!ValidateAggressiveGrab(ent, pulling))
            return;

        StartAggressiveGrab(ent, pulling);
    }

    private bool CanStartAggressiveGrab(Entity<CanFiremanCarryComponent> ent)
    {
        if (ent.Comp.AggressiveGrab)
            return false;

        var grabDelay = ent.Comp.AggressiveGrabDelay * _skills.GetSkillDelayMultiplier(ent.Owner, ent.Comp.Skill);
        return _timing.CurTime >= ent.Comp.PullTime + grabDelay;
    }

    private bool ValidateAggressiveGrab(Entity<CanFiremanCarryComponent> carrier, EntityUid target)
    {
        if (HasComp<XenoComponent>(target))
        {
            if (!TryComp(target, out FiremanCarriableComponent? carriable) ||
                carriable.CarrierWhitelist == null ||
                !_whitelist.IsValid(carriable.CarrierWhitelist, carrier.Owner))
            {
                _popup.PopupClient(Loc.GetString("rmc-fireman-carry-cant-grab-aggressively"), target, carrier, PopupType.MediumCaution);
                return false;
            }
        }
        else if (TryComp(target, out FiremanCarriableComponent? carriable))
        {
            if (carriable.CarrierWhitelist != null && !_whitelist.IsValid(carriable.CarrierWhitelist, carrier.Owner))
            {
                _popup.PopupClient(Loc.GetString("rmc-fireman-carry-cant-grab-aggressively"), target, carrier, PopupType.MediumCaution);
                return false;
            }

            if (carriable.CarrierWhitelist == null && !_skills.HasSkill(carrier.Owner, carriable.Skill, 1))
            {
                _popup.PopupClient(Loc.GetString("rmc-fireman-carry-not-trained-grab"), target, carrier, PopupType.MediumCaution);
                return false;
            }
        }

        return true;
    }

    private void StartAggressiveGrab(Entity<CanFiremanCarryComponent> carrier, EntityUid target)
    {
        carrier.Comp.AggressiveGrab = true;
        Dirty(carrier);

        _actionBlocker.UpdateCanMove(target);
        _standing.Down(target, changeCollision: true);

        if (HasComp<XenoComponent>(target))
            EnsureComp<KnockedDownComponent>(target);

        _rmcPulling.PlayPullEffect(carrier, target);

        ShowAggressiveGrabMessages(carrier, target);
    }

    private void ShowAggressiveGrabMessages(EntityUid carrier, EntityUid target)
    {
        var selfMsg = Loc.GetString("rmc-pull-aggressive-self", ("pulled", target));
        _popup.PopupClient(selfMsg, target, carrier, PopupType.SmallCaution);

        var others = Filter.PvsExcept(carrier, entityManager: EntityManager);
        foreach (var other in others.Recipients)
        {
            if (other.AttachedEntity is not { } recipient)
                continue;

            var pullerName = Identity.Name(carrier, EntityManager, recipient);
            var pulledName = Identity.Name(target, EntityManager, recipient);
            var msg = Loc.GetString("rmc-pull-aggressive-others", ("puller", pullerName), ("pulled", pulledName));
            _popup.PopupEntity(msg, carrier, recipient, PopupType.SmallCaution);
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

        if (HasComp<XenoComponent>(target))
            RemCompDeferred<KnockedDownComponent>(target);

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

            if (carrying != null)
                _virtualItem.DeleteInHandsMatching(user, carrying.Value);
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
