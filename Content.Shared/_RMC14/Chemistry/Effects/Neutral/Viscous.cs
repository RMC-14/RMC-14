using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Viscous : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Thick and gooey due to high surface tension. Decreases the spread radius of a chemical fire.";
    }

    public override void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
        stats.RadiusMod += -0.025f * potency;
    }

    public override bool IsFireSupplement => true;

    // TODO RMC14: chemical grenade combustion, once chem grenades exist
}
