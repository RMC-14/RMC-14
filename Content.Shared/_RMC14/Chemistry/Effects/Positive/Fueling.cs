using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Fueling : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Can be burned as fuel, expanding the burn time of a chemical fire. On contact, makes a target more flammable.";
    }

    public override bool IsFireSupplement => true;

    public override void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
        stats.Intensity += -3f * potency;
        stats.Duration += 7f * potency;
        stats.IntensityMod += -0.1f * potency;
        stats.DurationMod += 0.2f * potency;
        stats.RadiusMod += 0.01f * potency;
        stats.HasFireProperty = true;
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method != ReactionMethod.Touch)
            return;

        if (!args.EntityManager.TryGetComponent<FlammableComponent>(args.TargetEntity, out var flammable))
            return;

        var flammableSystem = args.EntityManager.System<SharedRMCFlammableSystem>();
        flammableSystem.AdjustStacks((args.TargetEntity, flammable), (float) args.Quantity * ActualPotency * 0.5f);
    }
}
