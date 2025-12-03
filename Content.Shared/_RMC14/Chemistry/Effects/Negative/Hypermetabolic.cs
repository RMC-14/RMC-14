using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hypermetabolic : RMCChemicalEffect
{
    public override string Abbreviation => "EMB";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var multiplier = Level * 0.25;
        var metabolism = Math.Abs(multiplier - 1) > 0.001f
            ? $"\nThe chemical lasts {multiplier} times less time in the bloodstream."
            : string.Empty;

        return $"The chemical lasts {multiplier} times less time in the bloodstream.";
    }

    public override FixedPoint2 GetMetabolismModifier()
    {
        return 1 + Level * 0.25;
    }
}
