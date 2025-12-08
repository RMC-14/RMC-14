using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Intravenous : RMCChemicalEffect
{
    public override string Abbreviation => "INV";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var metabolism = Math.Abs(Level - 1) > 0.001f
            ? $"\nThe chemical lasts {Level} times less time in the bloodstream."
            : string.Empty;

        return $"Boosts other chemical effects by [color=green]{Level}[/color].\n" +
               $"Cannot be ingested." +
               metabolism;
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        boost += Level;
    }

    public override bool CanBeIngested()
    {
        return false;
    }

    public override FixedPoint2 GetMetabolismModifier()
    {
        return Level;
    }
}
