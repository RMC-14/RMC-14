using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Intravenous : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Due to its chemical composition, this can only be administered intravenously.";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        boost += Potency;
    }

    public override float MetabolismRateMultiplier => Potency;
}
