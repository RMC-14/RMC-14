using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Empower;

public sealed class XenoEmpowerSystem : EntitySystem
{
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly XenoShieldSystem _shield = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly DamageableSystem _damagable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoEmpowerComponent, XenoEmpowerActionEvent>(OnXenoEmpowerAction);
        SubscribeLocalEvent<XenoEmpowerComponent, BeforeDamageChangedEvent>(OnXenoEmpowerBeforeDamageChanged);
        SubscribeLocalEvent<XenoEmpowerComponent, RemovedShieldEvent>(OnXenoEmpowerShieldRemoved);

        SubscribeLocalEvent<XenoSuperEmpoweredComponent, GetMeleeDamageEvent>(OnXenoSuperEmpoweredGetMeleeDamage);
        SubscribeLocalEvent<XenoSuperEmpoweredComponent, RMCGetTailStabBonusDamageEvent>(OnXenoSuperEmpoweredGetTailDamage);
        SubscribeLocalEvent<XenoSuperEmpoweredComponent, XenoLeapHitEvent>(OnXenoSuperEmpoweredLeapHit);
    }

    private void OnXenoEmpowerBeforeDamageChanged(Entity<XenoEmpowerComponent> xeno, ref BeforeDamageChangedEvent args)
    {
        if (xeno.Comp.ShieldDecayAt != null && args.Damage.GetTotal() <= 0)
            return;

        xeno.Comp.ShieldDecayAt = _timing.CurTime + xeno.Comp.ShieldDecayTime;
    }

    private void OnXenoEmpowerShieldRemoved(Entity<XenoEmpowerComponent> xeno, ref RemovedShieldEvent args)
    {
        if (args.Type != XenoShieldSystem.ShieldType.Ravager)
            return;

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-ravager-shield-end"), xeno, xeno, PopupType.SmallCaution);
    }

    private void OnXenoEmpowerAction(Entity<XenoEmpowerComponent> xeno, ref XenoEmpowerActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!xeno.Comp.ActivatedOnce)
        {
            _actions.SetUseDelay(args.Action.AsNullable(), TimeSpan.Zero);
            if (!_plasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.Cost))
                return;

            xeno.Comp.ActivatedOnce = true;
            _shield.ApplyShield(xeno, XenoShieldSystem.ShieldType.Ravager, xeno.Comp.InitialShield);
            //Reset shield time
            xeno.Comp.ShieldDecayAt = _timing.CurTime + xeno.Comp.ShieldDecayTime;
            xeno.Comp.TimeoutAt = _timing.CurTime + xeno.Comp.TimeoutDuration;
            xeno.Comp.FirstActivationAt = _timing.CurTime;

            foreach (var action in _rmcActions.GetActionsWithEvent<XenoEmpowerActionEvent>(xeno))
            {
                _actions.SetToggled(action.AsNullable(), true);
            }

            _popup.PopupPredicted(Loc.GetString("rmc-xeno-empower-start-self"), Loc.GetString("rmc-xeno-empower-start-others", ("user", xeno)),
                xeno, xeno, PopupType.MediumCaution);
        }
        else
            FullEmpower(xeno);
    }

    private void FullEmpower(Entity<XenoEmpowerComponent> xeno)
    {
        if (_net.IsClient)
            return;

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoEmpowerActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), false);
        }

        SpawnAttachedTo(xeno.Comp.EmpowerEffect, xeno.Owner.ToCoordinates());

        xeno.Comp.ActivatedOnce = false;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(xeno.Owner.ToCoordinates(), xeno.Comp.Range, _mobs);

        var hits = 0;

        foreach (var ent in _mobs)
        {
            if (!_examine.InRangeUnOccluded(xeno.Owner, ent, xeno.Comp.Range))
                continue;

            if (!_xeno.CanAbilityAttackTarget(xeno, ent) || HasComp<XenoNestedComponent>(ent))
                continue;

            hits++;

            if (_turf.GetTileRef(ent.Owner.ToCoordinates()) is { } tile)
                SpawnAtPosition(xeno.Comp.TargetEffect, _turf.GetTileCenter(tile));

            if (hits >= xeno.Comp.MaxTargets)
                break;
        }

        if (hits > 0)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-ravager-empower"), xeno, xeno, PopupType.SmallCaution);
        else
            _popup.PopupEntity(Loc.GetString("rmc-xeno-ravager-empower-fizzle"), xeno, xeno, PopupType.SmallCaution);

        _shield.ApplyShield(xeno, XenoShieldSystem.ShieldType.Ravager, hits * xeno.Comp.ShieldPerTarget);
        //Reset shield time
        xeno.Comp.ShieldDecayAt = _timing.CurTime + xeno.Comp.ShieldDecayTime;

        if (hits >= xeno.Comp.SuperThreshold)
        {
            //Apply super empowered
            _emote.TryEmoteWithChat(xeno, xeno.Comp.RoarEmote);
            _aura.GiveAura(xeno, xeno.Comp.SuperEmpowerColor, xeno.Comp.SuperEmpowerPartialDuration, outlineWidth: 4);
            var super = EnsureComp<XenoSuperEmpoweredComponent>(xeno);
            super.PartialExpireAt = _timing.CurTime + xeno.Comp.SuperEmpowerPartialDuration;
            super.EmpoweredTargets = hits;
            super.DamageIncreasePer = xeno.Comp.DamageIncreasePer;
            super.DamageTailIncreasePer = xeno.Comp.DamageTailIncreasePer;
            super.LeapDamage = xeno.Comp.LeapDamage;
        }
        else
            _emote.TryEmoteWithChat(xeno, xeno.Comp.TailEmote);

        Dirty(xeno);
        xeno.Comp.TimeoutAt = null;
        DoCooldown(xeno);
    }

    private void OnXenoSuperEmpoweredGetMeleeDamage(Entity<XenoSuperEmpoweredComponent> xeno, ref GetMeleeDamageEvent args)
    {
        args.Damage += xeno.Comp.DamageIncreasePer * xeno.Comp.EmpoweredTargets;
    }

    private void OnXenoSuperEmpoweredGetTailDamage(Entity<XenoSuperEmpoweredComponent> xeno, ref RMCGetTailStabBonusDamageEvent args)
    {
        args.Damage += xeno.Comp.DamageTailIncreasePer * xeno.Comp.EmpoweredTargets;
    }

    private void OnXenoSuperEmpoweredLeapHit(Entity<XenoSuperEmpoweredComponent> xeno, ref XenoLeapHitEvent args)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, args.Hit))
            return;

        _rmcPulling.TryStopAllPullsFromAndOn(args.Hit);

        var damage = _damagable.TryChangeDamage(args.Hit, xeno.Comp.LeapDamage, origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(args.Hit, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { args.Hit }, filter);
        }

        if (_net.IsClient)
            return;

        _stun.TryParalyze(args.Hit, xeno.Comp.StunDuration, true);

        var origin = _transform.GetMapCoordinates(xeno);
        _sizeStun.KnockBack(args.Hit, origin, xeno.Comp.FlingDistance, xeno.Comp.FlingDistance, 10, true);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var empowerQuery = EntityQueryEnumerator<XenoEmpowerComponent>();

        while (empowerQuery.MoveNext(out var uid, out var empower))
        {
            if (empower.ShieldDecayAt != null && time >= empower.ShieldDecayAt)
            {
                _shield.RemoveShield(uid, XenoShieldSystem.ShieldType.Ravager);
                empower.ShieldDecayAt = null;
            }

            if (empower.TimeoutAt == null || time < empower.TimeoutAt)
                continue;

            FullEmpower((uid, empower));
        }

        var superQuery = EntityQueryEnumerator<XenoSuperEmpoweredComponent>();

        while (superQuery.MoveNext(out var uid, out var super))
        {

            if (super.ExpiresAt != null & time >= super.ExpiresAt)
            {
                RemCompDeferred<XenoSuperEmpoweredComponent>(uid);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-ravager-super-empower-fade"), uid, uid, PopupType.SmallCaution);
                continue;
            }

            if (super.ExpiresAt != null)
                continue;

            if (time < super.PartialExpireAt)
                continue;

            _aura.GiveAura(uid, super.FadingEmpowerColor, super.ExpireTime, outlineWidth: 3);
            super.ExpiresAt = time + super.ExpireTime;
        }
    }

    private void DoCooldown(Entity<XenoEmpowerComponent> xeno)
    {
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoEmpowerActionEvent>(xeno))
        {
            var actionEnt = action.AsNullable();
            _actions.SetToggled(actionEnt, false);
            var cooldownTime = xeno.Comp.CooldownDuration - (_timing.CurTime - xeno.Comp.FirstActivationAt);
            _actions.SetUseDelay(actionEnt, cooldownTime);
            _actions.SetCooldown(actionEnt, cooldownTime);
        }
    }
}
