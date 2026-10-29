using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared._RMC14.Chemistry.XenoTransformation;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Dizziness;
using Content.Shared._RMC14.Jittering;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Omnipotent : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private static readonly EntProtoId<StatusEffectComponent> Hallucination = "RMCStatusEffectHallucination";
    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";
    private static readonly EntProtoId DrowsinessStatus = "StatusEffectDrowsiness";
    private static readonly EntProtoId ForcedSleepingStatus = "StatusEffectForcedSleeping";
    private static readonly ProtoId<StatusEffectPrototype> JitterStatus = "Jitter";
    private static readonly ProtoId<StatusEffectPrototype> BlurStatus = "Blinded";
    private static readonly ProtoId<StatusEffectPrototype> MutedStatus = "Muted";
    private static readonly ProtoId<StatusEffectPrototype> StunStatus = "Stun";
    private static readonly ProtoId<StatusEffectPrototype> KnockedDownStatus = "KnockedDown";
    private static readonly ProtoId<StatusEffectPrototype> UnconsciousStatus = "Unconscious";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Fully revitalizes all bodily functions.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        RMCDisabilities.ClearAll(args.EntityManager, args.TargetEntity);

        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (IoCManager.Resolve<IRobustRandom>().Prob(0.5f))
            entities.System<RMCXenoTransformationSystem>().ReduceStage(target);

        entities.System<SharedRMCBloodstreamSystem>().RemoveBloodstreamToxins(target, 0.625f);

        var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
        var heal = rmcDamageable.DistributeHealingCached(target, BruteGroup, potency * 5f);
        heal = rmcDamageable.DistributeHealingCached(target, BurnGroup, potency * 5f, heal);
        heal = rmcDamageable.DistributeHealingCached(target, ToxinGroup, potency * 5f, heal);
        damageable.TryChangeDamage(target, heal, true, interruptsDoAfters: false);

        if (entities.TryGetComponent<DamageableComponent>(target, out var damageableComp))
        {
            var zero = new DamageSpecifier();
            var hasZero = false;
            if (damageableComp.Damage.DamageDict.TryGetValue(CellularType, out var cellular) && cellular > 0)
            {
                zero.DamageDict[CellularType] = -cellular;
                hasZero = true;
            }

            if (damageableComp.Damage.DamageDict.TryGetValue(AsphyxiationType, out var asphyxiation) && asphyxiation > 0)
            {
                zero.DamageDict[AsphyxiationType] = -asphyxiation;
                hasZero = true;
            }

            if (hasZero)
                damageable.TryChangeDamage(target, zero, true, interruptsDoAfters: false);
        }

        var status = entities.System<SharedStatusEffectsSystem>();
        status.TryRemoveStatusEffect(target, Hallucination);
        status.TryRemoveStatusEffect(target, ConfusedStatus);
        status.TryRemoveStatusEffect(target, DrowsinessStatus);
        status.TryRemoveStatusEffect(target, ForcedSleepingStatus);

        entities.System<RMCJitterinessSystem>().ClearJitteriness(target);
        entities.System<RMCDizzinessSystem>().ClearDizziness(target);

        var oldStatus = entities.System<StatusEffectsSystem>();
        oldStatus.TryRemoveStatusEffect(target, JitterStatus);
        oldStatus.TryRemoveStatusEffect(target, StunStatus);
        oldStatus.TryRemoveStatusEffect(target, KnockedDownStatus);
        oldStatus.TryRemoveStatusEffect(target, UnconsciousStatus);
        oldStatus.TryRemoveStatusEffect(target, BlurStatus);
        oldStatus.TryRemoveStatusEffect(target, TemporaryBlindnessSystem.BlindingStatusEffect);
        oldStatus.TryRemoveStatusEffect(target, MutedStatus);

        var stutter = entities.System<SharedStutteringSystem>();
        stutter.DoRemoveStutter(target, 0);
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();

        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
        {
            plantTray.AdjustNutritionLevel((trayUid, tray), amount * 0.5f);
            plantTray.AdjustWeedLevel((trayUid, tray), -amount * 2.5f);
            plantTray.AdjustPestLevel((trayUid, tray), -amount * 2.5f);
        }

        plantTray.AdjustHealth((plant.Owner, plant.Comp, null), amount);
        AddYieldMod(args.EntityManager, plant, amount);
        plantTray.AdjustMutationMod((plant.Owner, plant.Comp), amount);
    }

    // TODO RMC14: also heals internal organ damage - 0.5 * potency * delta_time (heals potency on every organ per 2s tick)
    // TODO RMC14: also sets brain damage to 0
    // TODO RMC14: also partially cures other diseases
}
