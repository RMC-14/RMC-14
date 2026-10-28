using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Oculotoxic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the eyes. Prevents potency and cosmetic mutations from occurring in plants.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = -Potency;
        SuppressMutationSlot(args.EntityManager, plant, "Potency", suppress);
        SuppressMutationSlot(args.EntityManager, plant, "Bioluminescence", suppress);
        SuppressMutationSlot(args.EntityManager, plant, "Flowers", suppress);
    }

    // TODO RMC14: damage eyes - 0.5 * potency eye damage per 2s tick
    // TODO RMC14: brain damage on critical overdose - 0.5 * potency brain damage per 2s tick

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        args.EntityManager.EnsureComponent<RMCBlindDisabilityComponent>(args.TargetEntity);
    }
}
