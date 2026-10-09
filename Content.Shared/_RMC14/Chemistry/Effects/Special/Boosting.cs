using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Boosting : RMCChemicalEffect
{
    public override string Abbreviation => "BST";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Boosts the potency of all other properties in this chemical by [color=yellow]{Level * 0.5f}[/color]";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        boost += Level * 0.5f;
    }
}
