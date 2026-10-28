using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Pneumopeutic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Treats lung damage. Forces growth speed and lifespan mutations to occur in plants.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        EnableMutationSlot(args.EntityManager, plant, "Endurance", 1f);
        EnableMutationSlot(args.EntityManager, plant, "Production", 1f);
        EnableMutationSlot(args.EntityManager, plant, "Lifespan", 1f);
        EnableMutationSlot(args.EntityManager, plant, "Maturity", 1f);
    }

    // TODO RMC14: mob effect - heal lung organ damage, damages lungs on overdose
    // heals 0.5 * potency lung damage per 2s tick
    // overdose deals 2 * potency lung damage per 2s tick

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
