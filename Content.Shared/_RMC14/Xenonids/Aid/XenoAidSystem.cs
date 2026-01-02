using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Strain;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Aid;

public sealed class XenoAidSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoEnergySystem _xenoEnergy = default!;
    [Dependency] private readonly XenoStrainSystem _xenoStrain = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAidComponent, XenoAidActionEvent>(OnXenoAidAction);
    }

    private void OnXenoAidAction(Entity<XenoAidComponent> xeno, ref XenoAidActionEvent args)
    {
        var target = args.Target;

        if (!HasComp<XenoComponent>(target))
        {
            var msg = Loc.GetString("rmc-xeno-heal-sisters");
            _popup.PopupClient(msg, xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (!_hive.FromSameHive(xeno.Owner, target))
        {
            var msg = Loc.GetString("rmc-xeno-not-same-hive");
            _popup.PopupClient(msg, target, xeno, PopupType.SmallCaution);
            return;
        }

        if (xeno.Owner == target)
        {
            var msg = Loc.GetString("rmc-xeno-aid-self");
            _popup.PopupClient(msg, target, xeno, PopupType.SmallCaution);
            return;
        }

        if (_mobState.IsDead(target))
        {
            var msg = Loc.GetString("rmc-xeno-aid-on-fire");
            _popup.PopupClient(msg, target, xeno, PopupType.SmallCaution);
            return;
        }

        switch (args.aidType)
        {
            case XenoAidMode.Healing:
            {
                if (!_interaction.InRangeUnobstructed(xeno.Owner, target))
                    return;

                if (!_xeno.CanHeal(target))
                {
                    var msg = Loc.GetString("rmc-xeno-aid-on-fire");
                    _popup.PopupClient(msg, target, xeno, PopupType.SmallCaution);
                    return;
                }

                if (!_xenoEnergy.TryRemoveEnergyPopup(xeno.Owner, xeno.Comp.EnergyCost))
                    return;

                args.Handled = true;
                var heal = xeno.Comp.Heal;
                var bonusHeal = CompOrNull<XenoEnergyComponent>(xeno)?.Current * 0.5 ?? 0;
                _xenoEnergy.RemoveEnergy(xeno.Owner, (int) bonusHeal);

                if (_xenoStrain.AreSameStrain(xeno.Owner, target))
                    heal /= 2;
                else
                    heal += bonusHeal;

                var toHeal = -_rmcDamageable.DistributeTypesTotal(target, heal);
                _damageable.TryChangeDamage(target, toHeal);

                toHeal = -_rmcDamageable.DistributeTypesTotal(xeno.Owner, xeno.Comp.Heal * 0.5 + bonusHeal * 0.5);
                _damageable.TryChangeDamage(xeno, toHeal);

                var selfMsg = Loc.GetString("rmc-xeno-heal-self", ("target", target));
                _popup.PopupClient(selfMsg, target, xeno);

                var targetMsg = Loc.GetString("rmc-xeno-heal-target", ("target", xeno));
                _popup.PopupEntity(targetMsg, target, target);

                var othersMsg = Loc.GetString("rmc-xeno-heal-others", ("user", xeno), ("target", target));
                var filter = Filter.Pvs(target).RemovePlayersByAttachedEntity(xeno, target);
                _popup.PopupEntity(othersMsg, target, filter, true);

                if (xeno.Comp.HealEffect is { } effect)
                    SpawnAttachedTo(effect, target.ToCoordinates());

                ActivateCooldown(xeno);
                break;
            }
            case XenoAidMode.Ailments:
            {
                if (!_examine.InRangeUnOccluded(xeno.Owner, target, xeno.Comp.AilmentsRange))
                    return;

                if (!_xenoEnergy.TryRemoveEnergyPopup(xeno.Owner, xeno.Comp.EnergyCost))
                    return;

                foreach (var status in xeno.Comp.AilmentsRemove)
                {
                    _statusEffects.TryRemoveStatusEffect(target, status);
                }

                EntityManager.RemoveComponents(target, xeno.Comp.ComponentsRemove);

                var selfMsg = Loc.GetString("rmc-xeno-heal-ailments-self", ("target", target));
                _popup.PopupClient(selfMsg, target, xeno);

                var targetMsg = Loc.GetString("rmc-xeno-heal-ailments-target", ("target", target));
                _popup.PopupEntity(targetMsg, target, target);

                var othersMsg = Loc.GetString("rmc-xeno-heal-ailments-others", ("user", xeno), ("target", target));
                var filter = Filter.Pvs(target).RemovePlayersByAttachedEntity(xeno, target);
                _popup.PopupEntity(othersMsg, target, filter, true);

                if (xeno.Comp.AilmentsEffects is { } effect)
                    SpawnAttachedTo(effect, target.ToCoordinates());

                _jitter.DoJitter(target, xeno.Comp.AilmentsJitterDuration, true, 80, 8, true);
                ActivateCooldown(xeno);
                break;
            }
        }
    }

    private void ActivateCooldown(EntityUid user)
    {
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoAidActionEvent>(user))
        {
            _actions.StartUseDelay((action, action));
        }
    }
}
