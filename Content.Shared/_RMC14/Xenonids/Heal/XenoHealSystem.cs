using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Strain;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Heal;

public sealed class XenoHealSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoStrainSystem _xenoStrain = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly XenoEnergySystem _xenoEnergy = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageTypePrototype> BluntGroup = "Blunt";

    private readonly HashSet<Entity<XenoComponent>> _xenos = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoHealComponent, XenoHealActionEvent>(OnXenoHealAction);
        SubscribeLocalEvent<XenoComponent, XenoApplySalveActionEvent>(OnXenoApplySalveAction);
    }

    private void OnXenoHealAction(Entity<XenoHealComponent> ent, ref XenoHealActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args.Performer, args.Action))
            return;

        args.Handled = true;

        _xenos.Clear();
        _entityLookup.GetEntitiesInRange(args.Target, ent.Comp.Radius, _xenos);

        if (_xenos.Count == 0)
            return;

        var msg = "We channel our plasma to heal our sisters' wounds around this area.";
        _popup.PopupClient(msg, args.Target, ent, PopupType.Large);

        foreach (var xeno in _xenos)
        {
            if (_mobState.IsDead(xeno))
                continue;

            if (!_mobThreshold.TryGetIncapThreshold(xeno, out var threshold) ||
                threshold <= FixedPoint2.Zero)
            {
                continue;
            }

            if (!_hive.FromSameHive(ent.Owner, xeno.Owner))
                continue;

            var heal = EnsureComp<XenoBeingHealedComponent>(xeno);
            XenoHealStack healStack = new();
            healStack.HealAmount = threshold.Value * ent.Comp.Percentage /
                          (ent.Comp.Duration.TotalSeconds * 10) *
                          (ent.Comp.TimeBetweenHeals.TotalSeconds * 10);
            healStack.Charges = (int)(ent.Comp.Duration.TotalSeconds / ent.Comp.TimeBetweenHeals.TotalSeconds);
            healStack.TimeBetweenHeals = ent.Comp.TimeBetweenHeals;

            heal.HealStacks.Add(healStack);

            SpawnAttachedTo(ent.Comp.HealEffect, xeno.Owner.ToCoordinates());
        }
    }

    private void OnXenoApplySalveAction(EntityUid ent, XenoComponent comp, XenoApplySalveActionEvent args)
    {
        var target = args.Target;

        LocId? failureMessageId = null;

        if (!HasComp<XenoComponent>(target))
        {
            failureMessageId = "rmc-xeno-apply-salve-target-not-xeno-failure";
        }

        if (ent == target)
        {
            failureMessageId = "rmc-xeno-apply-salve-target-self-failure";
        }

        if (!_hive.FromSameHive(ent, target))
        {
            failureMessageId = "rmc-xeno-apply-salve-target-hostile-failure";
        }

        if (!_interact.InRangeUnobstructed(ent, target, args.Range))
        {
            failureMessageId = "rmc-xeno-apply-salve-target-too-far-away-failure";
        }

        if (_mobState.IsDead(target))
        {
            failureMessageId = "rmc-xeno-apply-salve-target-dead-failure";
        }

        if (TryComp(target, out DamageableComponent? damageComp) && damageComp.TotalDamage == 0)
        {
            failureMessageId = "rmc-xeno-apply-salve-target-full-health-failure";
        }

        if (failureMessageId is LocId)
        {
            _popup.PopupClient(Loc.GetString(failureMessageId, ("target_xeno", target.ToString())), ent);
            return;
        }

        var totalHealAmount = args.StandardHealAmount;
        var damageTakenModifier = args.DamageTakenModifier;
        var healedHealerOrSmallXeno = false;
        if (TryComp(target, out RMCSizeComponent? sizeComp) && sizeComp.Size == RMCSizes.Small)
        {
            totalHealAmount = args.SmallHealAmount;
            damageTakenModifier = 1;
            healedHealerOrSmallXeno = true;
        }

        if (_xenoStrain.AreSameStrain(ent, target))
        {
            damageTakenModifier = 1;
            healedHealerOrSmallXeno = true;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent, (totalHealAmount * args.PlasmaCostModifier)))
        {
            return;
        }

        var damageTaken = totalHealAmount * damageTakenModifier;

        var damageTakenSpecifier = new DamageSpecifier
        {
            DamageDict =
            {
                [BluntGroup] = damageTaken,
            },
        };
        _damageable.TryChangeDamage(ent, damageTakenSpecifier, ignoreResistances: true, interruptsDoAfters: false);
        _popup.PopupClient(Loc.GetString("rmc-xeno-apply-salve-self", ("target_xeno", target)), ent, PopupType.Medium);

        var heal = EnsureComp<XenoBeingHealedComponent>(target);
        XenoHealStack healStack = new();
        healStack.Charges = (int) (args.TotalHealDuration.TotalSeconds / args.TimeBetweenHeals.TotalSeconds);
        healStack.TimeBetweenHeals = args.TimeBetweenHeals;
        healStack.HealAmount = totalHealAmount / healStack.Charges;
        healStack.NextHealAt = _timing.CurTime + healStack.TimeBetweenHeals;
        heal.HealStacks.Add(healStack);
        heal.ParallizeHealing = true;
        SpawnAttachedTo(args.HealEffect, target.ToCoordinates());

        _audio.PlayPredicted(args.HealSound, target.ToCoordinates(), ent);

        _popup.PopupClient(Loc.GetString("rmc-xeno-apply-salve-target", ("healer_xeno", ent)), target, PopupType.SmallCaution);

        if (!healedHealerOrSmallXeno && TryComp(ent, out XenoEnergyComponent? xenoEnergyComp))
        {
            _xenoEnergy.AddEnergy((ent, xenoEnergyComp), (int)damageTaken, false);
        }
        Dirty(target, heal);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var healQuery = EntityQueryEnumerator<XenoBeingHealedComponent>();
        while (healQuery.MoveNext(out var uid, out var heal))
        {
            if (heal.HealStacks.Count == 0 || _mobState.IsDead(uid))
            {
                RemCompDeferred<XenoBeingHealedComponent>(uid);
                continue;
            }

            List<XenoHealStack> finishedStacks = new();

            foreach (var healStack in heal.HealStacks)
            {
                if (healStack.Charges <= 0)
                {
                    finishedStacks.Add(healStack);
                    continue;
                }

                if (healStack.NextHealAt > time)
                {
                    continue;
                }

                Dirty(uid, heal);

                var damage = _rmcDamageable.DistributeHealing(uid, BruteGroup, healStack.HealAmount);
                var totalHeal = damage.GetTotal();
                var leftover = healStack.HealAmount - totalHeal;
                if (leftover > FixedPoint2.Zero)
                    damage = _rmcDamageable.DistributeHealing(uid, BruteGroup, leftover, damage);

                _damageable.TryChangeDamage(uid, -damage, true);

                healStack.NextHealAt = time + healStack.TimeBetweenHeals;
                healStack.Charges = healStack.Charges - 1;

                if (!heal.ParallizeHealing)
                {
                    break;
                }
            }

            foreach (var stack in finishedStacks)
            {
                heal.HealStacks.Remove(stack);
            }
        }
    }
}
