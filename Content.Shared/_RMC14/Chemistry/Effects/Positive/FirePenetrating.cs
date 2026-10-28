using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class FirePenetrating : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Gives the chemical an anomalous combustion chemistry, causing its flame to burn through flame-resistant material.";
    }

    public override void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
        stats.FirePenetrating = true;
    }
}
