using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Blitz;

public sealed class XenoBlitzSystem : EntitySystem
{
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly VanguardShieldSystem _vanguard = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBlitzComponent, XenoLeapActionEvent>(OnLeapBlitz, before: [typeof(XenoLeapSystem)]);
        SubscribeLocalEvent<XenoBlitzComponent, XenoBlitzEvent>(OnAttackBlitz);
    }

    private void OnLeapBlitz(Entity<XenoBlitzComponent> xeno, ref XenoLeapActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<XenoLeapingComponent>(xeno))
            return;

        if (xeno.Comp.Dashed && xeno.Comp.SlashReady)
        {
            var ev = new XenoBlitzEvent();
            RaiseLocalEvent(xeno, ref ev);
            args.Handled = true;
        }
        else if (xeno.Comp.Dashed)
        {
            //Cancells leaping when slash isn't ready yet
            args.Handled = true;
        }
        else
        {
            //Only run on the dash itself
            if (!TryComp<XenoPlasmaComponent>(xeno, out var plasma) || !_plasma.HasPlasma((xeno.Owner, plasma), xeno.Comp.PlasmaCost))
                return;
            xeno.Comp.Dashed = true;
            _actions.SetUseDelay(args.Action.Owner, xeno.Comp.BaseUseDelay);
            xeno.Comp.FirstPartActivatedAt = _timing.CurTime;
            //Don't handle - let the leap go through
            // TODO RMC14 Find a way for this to work without also changing toggle on move selection
            foreach (var action in _rmcActions.GetActionsWithEvent<XenoLeapActionEvent>(xeno))
            {
                _actions.SetToggled((action, action), true);
            }
        }

        Dirty(xeno);
    }

    private void OnAttackBlitz(Entity<XenoBlitzComponent> xeno, ref XenoBlitzEvent args)
    {
        xeno.Comp.Dashed = false;
        xeno.Comp.SlashReady = false;

        SetBlitzDelays(xeno);

        if (!_mob.IsAlive(xeno) || HasComp<StunnedComponent>(xeno))
            return;

        var ev = new XenoLeapAttemptEvent();

        RaiseLocalEvent(xeno, ref ev);

        if (ev.Cancelled)
            return;

        //Note doesn't seem to work here
        EnsureComp<XenoSweepingComponent>(xeno);

        var hits = 0;

        foreach (var hit in _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(xeno), xeno.Comp.Range))
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, hit))
                continue;

            if (!_interact.InRangeUnobstructed(xeno.Owner, hit.Owner, xeno.Comp.Range))
                continue;

            hits++;

            var myDamage = _damage.TryChangeDamage(hit, _xeno.TryApplyXenoSlashDamageMultiplier(hit, xeno.Comp.Damage), origin: xeno, tool: xeno);
            if (myDamage?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(hit, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { hit }, filter);
            }

            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.Effect, hit.Owner.ToCoordinates());
        }

        if (_net.IsServer && hits > 0)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        if (hits >= xeno.Comp.HitsToRecharge)
            _vanguard.RegenShield(xeno);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoLeapActionEvent>(xeno))
        {
            _actions.SetToggled((action, action), false);
        }

        Dirty(xeno);
    }

    private void SetBlitzDelays(Entity<XenoBlitzComponent> xeno)
    {
        EntityUid? bliz = null;

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoLeapActionEvent>(xeno))
        {
            bliz = action;
            break;
        }

        if (bliz == null)
            return;

        var blitzCooldownTime = xeno.Comp.FinishedUseDelay - (_timing.CurTime - xeno.Comp.FirstPartActivatedAt);

        if (blitzCooldownTime < TimeSpan.Zero)
            blitzCooldownTime = TimeSpan.Zero;

        _actions.SetUseDelay(bliz, blitzCooldownTime);
        _actions.SetCooldown(bliz, blitzCooldownTime);
    }

    public override void Update(float frameTime)
    {
        //Note has to run on client or the sweeping comp won't animate

        var time = _timing.CurTime;

        var blitzes = EntityQueryEnumerator<XenoBlitzComponent>();

        while (blitzes.MoveNext(out var uid, out var dash))
        {
            if (!dash.Dashed)
                continue;

            if (!HasComp<XenoLeapingComponent>(uid) && !dash.SlashReady)
            {
                dash.SlashAroundAt = time + dash.SlashDashTime;
                dash.SlashReady = true;
                Dirty(uid, dash);
                continue;
            }

            if (!dash.SlashReady || time < dash.SlashAroundAt)
                continue;

            var ev = new XenoBlitzEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }
}
