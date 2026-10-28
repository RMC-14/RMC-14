using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Duration : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Controls the duration of a chemical fire, using unknown means.";
    }

    public override bool IsFireSupplement => true;

    public override void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
        stats.Duration += potency;
        stats.DurationMod += 0.1f * potency;
    }
}
