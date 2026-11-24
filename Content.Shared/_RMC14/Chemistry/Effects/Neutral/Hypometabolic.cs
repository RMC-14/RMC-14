using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hypometabolic : RMCChemicalEffect
{
    public override string Abbreviation => "OMB";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var multiplier = 1 / (1 + Level * 0.35);
        return $"The chemical lasts {multiplier} times longer in the bloodstream.";
    }

    public override FixedPoint2 GetMetabolismModifier()
    {
        // Dividing because rate *= GetMetabolismModifier in MetabolizerSystem
        // Division is the reciprocal of Multiplication
        return 1 / (1 + Level * 0.35);
    }
}
