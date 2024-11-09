using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Crippling;

public sealed class XenoCripplingStrikeSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoCripplingStrikeComponent, XenoCripplingStrikeActionEvent>(OnXenoCripplingStrikeAction);

        SubscribeLocalEvent<XenoActiveCripplingStrikeComponent, MeleeHitEvent>(OnXenoCripplingStrikeHit);

        SubscribeLocalEvent<VictimCripplingStrikeSlowedComponent, DamageModifyEvent>(OnVictimCripplingModify, before: [typeof(CMArmorSystem)]);
        SubscribeLocalEvent<VictimCripplingStrikeSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnVictimCripplingRefreshSpeed);
        SubscribeLocalEvent<VictimCripplingStrikeSlowedComponent, ComponentRemove>(OnVictimCripplingRemove);
    }

    private void OnXenoCripplingStrikeAction(Entity<XenoCripplingStrikeComponent> xeno, ref XenoCripplingStrikeActionEvent args)
    {
        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;
        var active = EnsureComp<XenoActiveCripplingStrikeComponent>(xeno);
        var reset = EnsureComp<MeleeResetComponent>(xeno);
        _rmcMelee.MeleeResetInit((xeno.Owner, reset));

        active.ExpireAt = _timing.CurTime + xeno.Comp.ActiveDuration;
        active.SpeedMultiplier = xeno.Comp.SpeedMultiplier;
        active.SlowDuration = xeno.Comp.SlowDuration;
        active.DamageMult = xeno.Comp.DamageMult;

        Dirty(xeno, active);

        _popup.PopupClient(Loc.GetString("cm-xeno-crippling-strike-activate"), xeno, xeno);
    }

    private void OnXenoCripplingStrikeHit(Entity<XenoActiveCripplingStrikeComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, entity) ||
                HasComp<VictimCripplingStrikeSlowedComponent>(entity))
            {
                continue;
            }

            var victim = EnsureComp<VictimCripplingStrikeSlowedComponent>(entity);

            victim.ExpireAt = _timing.CurTime + xeno.Comp.SlowDuration;
            victim.SpeedMultiplier = xeno.Comp.SpeedMultiplier;
            victim.DamageMult = xeno.Comp.DamageMult;
            victim.WasHit = false;

            Dirty(entity, victim);

            _movementSpeed.RefreshMovementSpeedModifiers(entity);

            var message = Loc.GetString("cm-xeno-crippling-strike-hit", ("target", entity));
            

            if (_net.IsServer)
            {
                _popup.PopupEntity(message, entity, xeno);
                SpawnAttachedTo(xeno.Comp.Effect, entity.ToCoordinates());
            }

            RemCompDeferred<XenoActiveCripplingStrikeComponent>(xeno);
            break;
        }
    }

    private void OnVictimCripplingModify(Entity<VictimCripplingStrikeSlowedComponent> victim, ref DamageModifyEvent args)
    {
        if (!victim.Comp.WasHit)
        {
            args.Damage *= victim.Comp.DamageMult;
            victim.Comp.WasHit = true;
        }
    }


    private void OnVictimCripplingRefreshSpeed(Entity<VictimCripplingStrikeSlowedComponent> victim, ref RefreshMovementSpeedModifiersEvent args)
    {
        var multiplier = victim.Comp.SpeedMultiplier.Float();
        args.ModifySpeed(multiplier, multiplier);
    }

    private void OnVictimCripplingRemove(Entity<VictimCripplingStrikeSlowedComponent> victim, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(victim))
            _movementSpeed.RefreshMovementSpeedModifiers(victim);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        if (_net.IsServer)
        {
            var activeQuery = EntityQueryEnumerator<XenoActiveCripplingStrikeComponent>();
            while (activeQuery.MoveNext(out var uid, out var active))
            {
                if (active.ExpireAt > time)
                    continue;

                RemCompDeferred<XenoActiveCripplingStrikeComponent>(uid);
                RemCompDeferred<MeleeResetComponent>(uid);

                _popup.PopupEntity(Loc.GetString("cm-xeno-crippling-strike-expire"), uid, uid, PopupType.SmallCaution);
            }
        }

        var victimQuery = EntityQueryEnumerator<VictimCripplingStrikeSlowedComponent>();
        while (victimQuery.MoveNext(out var uid, out var victim))
        {
            if (victim.ExpireAt > time)
                continue;

            RemCompDeferred<VictimCripplingStrikeSlowedComponent>(uid);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
