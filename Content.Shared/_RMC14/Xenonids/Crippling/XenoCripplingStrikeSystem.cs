using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Aura;
using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.Xenonids.Crippling;

public sealed class XenoCripplingStrikeSystem : EntitySystem
{
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoCripplingStrikeComponent, XenoCripplingStrikeActionEvent>(OnXenoCripplingStrikeAction);

        SubscribeLocalEvent<XenoActiveCripplingStrikeComponent, MeleeHitEvent>(OnXenoCripplingStrikeHit);
        SubscribeLocalEvent<XenoActiveCripplingStrikeComponent, RefreshMovementSpeedModifiersEvent>(OnActiveCripplingRefreshSpeed);
        SubscribeLocalEvent<XenoActiveCripplingStrikeComponent, ComponentRemove>(OnActiveCripplingRemove);

        SubscribeLocalEvent<VictimCripplingStrikeDamageComponent, DamageModifyEvent>(OnVictimCripplingModify, before: [typeof(CMArmorSystem)]);
    }

    private void OnXenoCripplingStrikeAction(Entity<XenoCripplingStrikeComponent> xeno, ref XenoCripplingStrikeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(xeno, args.Action))
            return;

        args.Handled = true;
        var active = EnsureComp<XenoActiveCripplingStrikeComponent>(xeno);
        var reset = EnsureComp<MeleeResetComponent>(xeno);
        _rmcMelee.MeleeResetInit((xeno.Owner, reset));

        active.ExpireAt = _timing.CurTime + xeno.Comp.ActiveDuration;
        active.NextSlashBuffed = true;
        active.SlowDuration = xeno.Comp.SlowDuration;
        active.DamageMult = xeno.Comp.DamageMult;
        active.HitText = xeno.Comp.HitText;
        active.DeactivateText = xeno.Comp.DeactivateText;
        active.ExpireText = xeno.Comp.ExpireText;
        active.Speed = xeno.Comp.Speed;

        Dirty(xeno, active);

        _popup.PopupClient(Loc.GetString(xeno.Comp.ActivateText), xeno, xeno);
        _movementSpeed.RefreshMovementSpeedModifiers(xeno);

        if (xeno.Comp.AuraColor is { } color)
            _aura.GiveAura(xeno, color, xeno.Comp.ActiveDuration, 1);
    }

    private void OnXenoCripplingStrikeHit(Entity<XenoActiveCripplingStrikeComponent> xeno, ref MeleeHitEvent args)
    {
        if (!xeno.Comp.NextSlashBuffed)
            return;

        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, entity) ||
                HasComp<VictimCripplingStrikeDamageComponent>(entity))
            {
                continue;
            }

            var victim = EnsureComp<VictimCripplingStrikeDamageComponent>(entity);

            victim.DamageMult = xeno.Comp.DamageMult;

            Dirty(entity, victim);

            _slow.TrySlowdown(entity, xeno.Comp.SlowDuration, ignoreDurationModifier: true);

            var message = Loc.GetString(xeno.Comp.HitText, ("target", entity));

            if (_net.IsServer)
                _popup.PopupEntity(message, entity, xeno);

            xeno.Comp.NextSlashBuffed = false;
            break;
        }
    }

    private void OnActiveCripplingRefreshSpeed(Entity<XenoActiveCripplingStrikeComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (xeno.Comp.Speed is not { } speed)
            return;

        args.ModifySpeed(speed, speed);
    }

    private void OnActiveCripplingRemove(Entity<XenoActiveCripplingStrikeComponent> xeno, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(xeno))
            _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnVictimCripplingModify(Entity<VictimCripplingStrikeDamageComponent> victim, ref DamageModifyEvent args)
    {
        args.Damage *= victim.Comp.DamageMult;
        RemCompDeferred<VictimCripplingStrikeDamageComponent>(victim);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var activeQuery = EntityQueryEnumerator<XenoActiveCripplingStrikeComponent>();

        while (activeQuery.MoveNext(out var uid, out var active))
        {
            if (time < active.ExpireAt)
                continue;

            if (active.NextSlashBuffed)
                _popup.PopupEntity(Loc.GetString(active.ExpireText), uid, uid, PopupType.SmallCaution);
            // the deactivate text is supposed to show up together with the expire text
            // but in ss14 popups overlap making it unreadable so else if will have to do...
            else if (active.DeactivateText is { } deactivateText)
                _popup.PopupEntity(Loc.GetString(deactivateText), uid, uid, PopupType.MediumCaution);

            RemCompDeferred<XenoActiveCripplingStrikeComponent>(uid);
        }
    }
}
