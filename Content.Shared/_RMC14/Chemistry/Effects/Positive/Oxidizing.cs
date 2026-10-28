using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Oxidizing : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private const int UTNapthalIntensity = 30;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Increases the intensity of chemical fires, at the cost of burning fuel faster. On contact with a high enough potency, ignites the target.";
    }

    public override bool IsFireSupplement => true;

    public override void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
        stats.Intensity += 7f * potency;
        stats.Duration += -4f * potency;
        stats.IntensityMod += 0.2f * potency;
        stats.DurationMod += -0.1f * potency;
        stats.RadiusMod += -0.01f * potency;
        stats.HasFireProperty = true;
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<FlammableComponent>(args.TargetEntity, out var flammable))
            return;

        if (args.Method != ReactionMethod.Touch)
            return;

        var flammableSystem = args.EntityManager.System<SharedRMCFlammableSystem>();

        var boost = MathF.Max(flammable.FireStacks, (float) args.Quantity * ActualPotency);
        flammableSystem.AdjustStacks((args.TargetEntity, flammable), boost);

        if (ActualPotency <= 4f)
            return;

        flammableSystem.Ignite((args.TargetEntity, flammable), UTNapthalIntensity, (int) flammable.FireStacks, null);
    }
}
