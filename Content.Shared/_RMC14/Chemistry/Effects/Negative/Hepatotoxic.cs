using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hepatotoxic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the liver. Prevents certain negative mutations from occurring in plants.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = -Potency;
        SuppressMutationSlot(args.EntityManager, plant, "Plant Cancer", suppress);
        SuppressMutationSlot(args.EntityManager, plant, "Gluttony", suppress);
    }

    // TODO RMC14: damage liver - 0.5 * potency liver damage per 2s tick
    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 2f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 5f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
