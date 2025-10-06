using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Dodge;

public sealed class XenoDodgeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private readonly HashSet<Entity<MobStateComponent>> _crowd = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoDodgeComponent, XenoDodgeActionEvent>(OnXenoActionDodge);

        SubscribeLocalEvent<XenoActiveDodgeComponent, RefreshMovementSpeedModifiersEvent>(OnActiveDodgeRefresh);
        SubscribeLocalEvent<XenoActiveDodgeComponent, ComponentRemove>(OnActiveDodgeRemove);
        SubscribeLocalEvent<XenoActiveDodgeComponent, AttemptMobCollideEvent>(OnActiveDodgeAttemptMobCollide);
        SubscribeLocalEvent<XenoActiveDodgeComponent, AttemptMobTargetCollideEvent>(OnActiveDodgeAttemptMobTargetCollide);
    }

    private void OnXenoActionDodge(Entity<XenoDodgeComponent> xeno, ref XenoDodgeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_plasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        EnsureComp<XenoActiveDodgeComponent>(xeno).ExpiresAt = _timing.CurTime + xeno.Comp.Duration;
        _speed.RefreshMovementSpeedModifiers(xeno);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-dodge-self"), xeno, xeno, PopupType.Medium);
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoDodgeActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), true);
        }
    }

    private void OnActiveDodgeRefresh(Entity<XenoActiveDodgeComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = (1.0 + xeno.Comp.SpeedMult + (xeno.Comp.InCrowd ? xeno.Comp.CrowdSpeedAddMult : 0)).Float();
        args.ModifySpeed(modifier, modifier);
    }

    private void OnActiveDodgeRemove(Entity<XenoActiveDodgeComponent> xeno, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(xeno))
        {
            _speed.RefreshMovementSpeedModifiers(xeno);
            foreach (var action in _rmcActions.GetActionsWithEvent<XenoDodgeActionEvent>(xeno))
            {
                _actions.SetToggled(action.AsNullable(), false);
            }
        }
    }

    private void OnActiveDodgeAttemptMobCollide(Entity<XenoActiveDodgeComponent> ent, ref AttemptMobCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnActiveDodgeAttemptMobTargetCollide(Entity<XenoActiveDodgeComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var dodgeQuery = EntityQueryEnumerator<XenoActiveDodgeComponent>();

        while (dodgeQuery.MoveNext(out var uid, out var speed))
        {
            if (speed.ExpiresAt < time)
            {
                RemCompDeferred<XenoActiveDodgeComponent>(uid);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-dodge-end"), uid, uid, PopupType.MediumCaution);
                continue;
            }

            _crowd.Clear();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, speed.CrowdRange, _crowd);

            bool crowd = false;
            foreach (var mob in _crowd)
            {
                if (_xeno.CanAbilityAttackTarget(uid, mob) && !_standing.IsDown(mob))
                {
                    crowd = true;
                    break;
                }
            }

            if (crowd == speed.InCrowd)
                continue;

            speed.InCrowd = crowd;
            _speed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
