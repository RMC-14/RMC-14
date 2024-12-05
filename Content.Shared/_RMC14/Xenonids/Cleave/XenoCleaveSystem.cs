using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Shields;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Cleave;

public sealed class XenoCleaveSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly VanguardShieldSystem _vanguard = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCPullingSystem _rmcpulling = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoCleaveComponent, XenoCleaveActionEvent>(OnCleaveAction);
        SubscribeLocalEvent<XenoCleaveComponent, XenoToggleCleaveActionEvent>(OnCleaveToggleAction);

        SubscribeLocalEvent<CleaveRootedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshCleaveRooted);
        SubscribeLocalEvent<CleaveRootedComponent, ComponentRemove>(OnCleaveRootedRemoved);
    }

    private void OnCleaveAction(Entity<XenoCleaveComponent> xeno, ref XenoCleaveActionEvent args)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, args.Target))
            return;

        if (args.Handled)
            return;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var buffed = _vanguard.ShieldBuff(xeno);

        args.Handled = true;

        if (xeno.Comp.Flings)
        {
            var flingRange = buffed ? xeno.Comp.FlingDistanceBuffed : xeno.Comp.FlingDistance;
            _rmcpulling.TryStopUserPullIfPulling(xeno, args.Target);

            //From fling
            var origin = _transform.GetMapCoordinates(xeno);
            var target = _transform.GetMapCoordinates(args.Target);
            var diff = target.Position - origin.Position;
            var length = diff.Length();
            diff = diff.Normalized() * flingRange;

            if (_net.IsServer)
            {
                _throwing.TryThrow(args.Target, diff, 10);

                SpawnAttachedTo(xeno.Comp.FlingEffect, args.Target.ToCoordinates());
            }
        }
        else
        {
            var rootTime = buffed ? xeno.Comp.RootTimeBuffed : xeno.Comp.RootTime;
            var root = EnsureComp<CleaveRootedComponent>(args.Target);
            root.ExpiresAt = _timing.CurTime + rootTime;
            _speed.RefreshMovementSpeedModifiers(args.Target);

            if (_net.IsServer)
            {
                SpawnAttachedTo(xeno.Comp.RootEffect, args.Target.ToCoordinates());
                SpawnAttachedTo(buffed ? xeno.Comp.RootStatusEffectBuffed : xeno.Comp.RootStatusEffect, args.Target.ToCoordinates());
            }
        }
    }

    private void OnCleaveToggleAction(Entity<XenoCleaveComponent> xeno, ref XenoToggleCleaveActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        xeno.Comp.Flings = !xeno.Comp.Flings;
        _actions.SetToggled(args.Action, xeno.Comp.Flings);
        _popups.PopupClient(Loc.GetString(xeno.Comp.Flings ? "rmc-xeno-toggle-cleave-fling" : "rmc-xeno-toggle-cleave-root"), xeno, xeno);
        Dirty(xeno);
    }

    private void OnRefreshCleaveRooted(Entity<CleaveRootedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(0, 0);
    }

    private void OnCleaveRootedRemoved(Entity<CleaveRootedComponent> ent, ref ComponentRemove args)
    {
        if(!TerminatingOrDeleted(ent))
            _speed.RefreshMovementSpeedModifiers(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var rooted = EntityQueryEnumerator<CleaveRootedComponent>();

        while (rooted.MoveNext(out var uid, out var root))
        {
            if (root.ExpiresAt > time)
                continue;

            RemCompDeferred<CleaveRootedComponent>(uid);
        }
    }
}
