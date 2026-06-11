using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.SwiftSteps;
using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Dodge;

public abstract class SharedXenoDodgeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
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
        SubscribeLocalEvent<XenoActiveDodgeComponent, RMCGetSwiftStepsThresholdEvent>(OnActiveDodgeGetDodgeThreshold);
    }

    private void OnXenoActionDodge(Entity<XenoDodgeComponent> xeno, ref XenoDodgeActionEvent args)
    {
        if (TryComp<XenoActiveDodgeComponent>(xeno, out var dodge))
        {
            var refundedCooldown = GetCooldown(xeno, dodge, xeno.Comp.RefundMultiplier);
            StartCooldown((xeno, dodge), refundedCooldown, false);
            RemCompDeferred<XenoActiveDodgeComponent>(xeno);
            _popup.PopupClient(Loc.GetString("rmc-xeno-dodge-end-manual"), xeno, xeno, PopupType.MediumCaution);
        }
        else
        {
            var dodging = EnsureComp<XenoActiveDodgeComponent>(xeno);
            dodging.ExpiresAt = _timing.CurTime + xeno.Comp.Duration;
            dodging.CheckCrowd = xeno.Comp.CheckCrowd;
            Dirty(xeno, dodging);
            _speed.RefreshMovementSpeedModifiers(xeno);
            //Half a second cooldown to prevent double clicks - longer than lurkers
            StartCooldown((xeno, dodging), xeno.Comp.ToggleLockoutTime, true);
            _popup.PopupClient(Loc.GetString("rmc-xeno-dodge-self"), xeno, xeno, PopupType.Medium);
        }
    }

    private void OnActiveDodgeRefresh(Entity<XenoActiveDodgeComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = (1.0 + xeno.Comp.SpeedMult + (xeno.Comp.InCrowd ? xeno.Comp.CrowdSpeedAddMult : 0)).Float();
        args.ModifySpeed(modifier, modifier);
    }

    private void OnActiveDodgeGetDodgeThreshold(Entity<XenoActiveDodgeComponent> xeno, ref RMCGetSwiftStepsThresholdEvent args)
    {
        args.Threshold += xeno.Comp.SwiftStepsMod;
    }

    protected virtual void OnActiveDodgeRemove(Entity<XenoActiveDodgeComponent> xeno, ref ComponentRemove args)
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

        var dodgeQuery = EntityQueryEnumerator<XenoActiveDodgeComponent, XenoDodgeComponent>();

        while (dodgeQuery.MoveNext(out var uid, out var speed, out var dodge))
        {
            if (speed.ExpiresAt < time)
            {
                var cooldown = GetCooldown((uid, dodge), speed, dodge.RefundMultiplier);
                StartCooldown((uid, speed), dodge.Duration, false);
                RemCompDeferred<XenoActiveDodgeComponent>(uid);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-dodge-end"), uid, uid, PopupType.MediumCaution);
                continue;
            }

            if (!speed.CheckCrowd)
                continue;

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
            Dirty(uid, speed);
            _speed.RefreshMovementSpeedModifiers(uid);
        }
    }

    private TimeSpan GetCooldown(Entity<XenoDodgeComponent> xeno, XenoActiveDodgeComponent active, float refundMultiplier)
    {
        var remainingRatio = 1 - (active.ExpiresAt - _timing.CurTime) / xeno.Comp.Duration; // Current Time - StartTime / Duration
        //Should be double it's duration if it runs out naturally
        var refundedCooldown = Math.Max((int)(xeno.Comp.Duration * remainingRatio * refundMultiplier).TotalSeconds, (int)xeno.Comp.MinimumCooldown.TotalSeconds);

        return TimeSpan.FromSeconds(refundedCooldown);
    }

    private void StartCooldown(Entity<XenoActiveDodgeComponent> xeno, TimeSpan cooldownTime, bool toggledStatus)
    {
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoDodgeActionEvent>(xeno))
        {
            var actionEnt = action.AsNullable();
            _actions.SetCooldown(actionEnt, cooldownTime);
            _actions.SetToggled(actionEnt, toggledStatus);
        }
    }
}
