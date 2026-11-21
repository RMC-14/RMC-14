using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Boosting : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("rmc-reagent-effect-guidebook-boosting",
            ("boost", (Potency * 0.5f).ToString()));
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        boost += Potency * 0.5f;
    }
}
