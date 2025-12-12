using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Retrieve;

public sealed class XenoRetrieveSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _rmcEmote = default!;
    [Dependency] private readonly RMCSizeStunSystem _rmcSize = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoRetrieveComponent, XenoRetrieveActionEvent>(OnXenoRetrieveAction);
        SubscribeLocalEvent<XenoRetrieveComponent, XenoRetrieveDoAfterEvent>(OnXenoRetrieveDoAfter);
        SubscribeLocalEvent<XenoRetrieveComponent, EntityTerminatingEvent>(OnXenoRetrieveTerminating);
    }

    private void OnXenoRetrieveAction(Entity<XenoRetrieveComponent> xeno, ref XenoRetrieveActionEvent args)
    {
        var target = args.Target;
        if (!_hive.FromSameHive(xeno.Owner, target))
        {
            var msg = Loc.GetString("rmc-xeno-not-same-hive");
            _popup.PopupClient(msg, xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (xeno.Owner == target)
        {
            var msg = Loc.GetString("rmc-xeno-retrieve-self");
            _popup.PopupClient(msg, xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (Transform(target).Anchored)
        {
            var msg = Loc.GetString("rmc-xeno-retrieve-anchored");
            _popup.PopupClient(msg, xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (_rmcSize.TryGetSize(target, out var size) &&
            size > xeno.Comp.SizeLimit &&
            _mobState.IsAlive(target) &&
            !HasComp<XenoRestingComponent>(target) &&
            !_standing.IsDown(target))
        {
            var msg = Loc.GetString("rmc-xeno-retrieve-too-big", ("target", target));
            _popup.PopupClient(msg, xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (!_interaction.InRangeUnobstructed(xeno.Owner, target, xeno.Comp.Range, CollisionGroup.Impassable))
        {
            var msg = Loc.GetString("rmc-xeno-retrieve-blocked", ("target", target));
            _popup.PopupClient(msg, xeno, xeno, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        var ev = new XenoRetrieveDoAfterEvent(GetNetEntity(args.Action));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno, target)
        {
            BreakOnMove = true,
            DistanceThreshold = xeno.Comp.Range,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-xeno-retrieve-start-self", ("target", target));
            var othersMsg = Loc.GetString("rmc-xeno-retrieve-start-others", ("user", xeno), ("target", target));
            _popup.PopupPredicted(selfMsg, othersMsg, xeno, xeno);

            foreach (var visual in xeno.Comp.Visuals)
            {
                QueueDel(visual);
            }

            xeno.Comp.Visuals.Clear();

            foreach (var tile in _line.DrawLine(xeno.Owner.ToCoordinates(), target.ToCoordinates(), TimeSpan.Zero, xeno.Comp.Range, out _))
            {
                xeno.Comp.Visuals.Add(Spawn(xeno.Comp.Visual, tile.Coordinates));
            }

            if (xeno.Comp.Emote is { } emote)
                _rmcEmote.TryEmoteWithChat(xeno, emote);
        }
    }

    private void OnXenoRetrieveDoAfter(Entity<XenoRetrieveComponent> xeno, ref XenoRetrieveDoAfterEvent args)
    {
        foreach (var visual in xeno.Comp.Visuals)
        {
            QueueDel(visual);
        }

        xeno.Comp.Visuals.Clear();

        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!TryGetEntity(args.Action, out var action))
            return;

        if (!_interaction.InRangeUnobstructed(xeno.Owner, target, xeno.Comp.Range, CollisionGroup.Impassable))
        {
            var msg = Loc.GetString("rmc-xeno-retrieve-blocked", ("target", target));
            _popup.PopupClient(msg, xeno, xeno, PopupType.SmallCaution);
            return;
        }

        var userCoords = _transform.GetMapCoordinates(xeno);
        var targetCoords = _transform.GetMapCoordinates(target);
        if (userCoords.MapId != targetCoords.MapId)
            return;

        var direction = userCoords.Position - targetCoords.Position;
        direction += direction.Normalized();
        if (direction == Vector2.Zero)
            return;

        if (!TryComp(target, out PhysicsComponent? physics))
            return;

        if (!_rmcActions.TryUseAction(xeno, action.Value, target))
            return;

        _rmcPulling.TryStopAllPullsFromAndOn(target);

        var length = direction.Length();
        var distance = Math.Clamp(length, 0.1f, xeno.Comp.Range);
        direction *= distance / length;
        var impulse = direction.Normalized() * xeno.Comp.Force * physics.Mass;
        var retrieved = EnsureComp<XenoBeingRetrievedComponent>(target);
        retrieved.EndTime = _timing.CurTime + TimeSpan.FromSeconds(direction.Length() / xeno.Comp.Force);
        Dirty(target, retrieved);

        _physics.ApplyLinearImpulse(target, impulse, body: physics);
        _physics.SetBodyStatus(target, physics, BodyStatus.InAir);

        var selfMsg = Loc.GetString("rmc-xeno-retrieve-finish-user", ("target", target));
        var othersMsg = Loc.GetString("rmc-xeno-retrieve-finish-others", ("user", xeno), ("target", target));
        _popup.PopupPredicted(selfMsg, othersMsg, xeno, xeno);
        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
    }

    private void OnXenoRetrieveTerminating(Entity<XenoRetrieveComponent> ent, ref EntityTerminatingEvent args)
    {
        foreach (var visual in ent.Comp.Visuals)
        {
            if (!TerminatingOrDeleted(visual) && !EntityManager.IsQueuedForDeletion(visual))
                QueueDel(visual);
        }

        ent.Comp.Visuals.Clear();
    }

    private void StopRetrieve(Entity<XenoBeingRetrievedComponent> retrieved)
    {
        if (TryComp(retrieved, out PhysicsComponent? physics))
        {
            _physics.SetLinearVelocity(retrieved, Vector2.Zero, body: physics);
            _physics.SetBodyStatus(retrieved, physics, BodyStatus.OnGround);
        }

        RemCompDeferred<XenoBeingRetrievedComponent>(retrieved);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var leaping = EntityQueryEnumerator<XenoBeingRetrievedComponent>();
        while (leaping.MoveNext(out var uid, out var comp))
        {
            if (time < comp.EndTime)
                continue;

            StopRetrieve((uid, comp));
        }
    }
}
