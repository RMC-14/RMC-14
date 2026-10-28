using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Flowing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "The opposite of viscous - tends to spill everywhere, expanding the radius of a chemical fire.";
    }

    public override bool IsFireSupplement => true;

    public override void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
        stats.Radius += 2f * potency;
        stats.IntensityMod += -0.05f * potency;
        stats.DurationMod += -0.05f * potency;
        stats.RadiusMod += 0.05f * potency;
        stats.HasFireProperty = true;
    }
}
