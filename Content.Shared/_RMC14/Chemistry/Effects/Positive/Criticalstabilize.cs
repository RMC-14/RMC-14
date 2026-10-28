using Content.Shared._RMC14.Chemistry.ChemEffectFlags;
using Content.Shared._RMC14.Damage;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Criticalstabilize : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private static readonly FixedPoint2 HumanMaxHealth = 100;
    private static readonly FixedPoint2 HealPerSecond = 0.5;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stabilizes critical damage and bleeding.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        if (!entities.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity))
            return;

        if (entities.TryGetComponent<DamageableComponent>(args.TargetEntity, out var damageableComp))
        {
            var prototypes = IoCManager.Resolve<IPrototypeManager>();
            var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
            var heal = new DamageSpecifier();

            if (prototypes.TryIndex(BruteGroup, out var bruteGroup) &&
                damageableComp.Damage.TryGetDamageInGroup(bruteGroup, out var brute) &&
                brute >= HumanMaxHealth)
            {
                heal = rmcDamageable.DistributeHealingCached((args.TargetEntity, damageableComp), BruteGroup, HealPerSecond, heal);
            }

            if (prototypes.TryIndex(BurnGroup, out var burnGroup) &&
                damageableComp.Damage.TryGetDamageInGroup(burnGroup, out var burn) &&
                burn >= HumanMaxHealth)
            {
                heal = rmcDamageable.DistributeHealingCached((args.TargetEntity, damageableComp), BurnGroup, HealPerSecond, heal);
            }

            if (damageableComp.Damage.DamageDict.TryGetValue(AsphyxiationType, out var oxy) && oxy > 20)
                heal.DamageDict[AsphyxiationType] = -HealPerSecond;

            if (heal.DamageDict.Count > 0)
                damageable.TryChangeDamage(args.TargetEntity, heal, true, interruptsDoAfters: false);
        }

        if (entities.HasComponent<BloodstreamComponent>(args.TargetEntity))
            entities.System<RMCChemEffectFlagsSystem>().TrySetFlag(args.TargetEntity, RMCChemEffectFlagsSystem.NoBleedingFlag);
    }
}
