using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Radius : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Controls the radius of a chemical fire, using unknown means.";
    }

    public override bool IsFireSupplement => true;

    public override void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
        stats.Radius += potency;
        stats.RadiusMod += 0.1f * potency;
    }
}
