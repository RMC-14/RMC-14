using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed partial class PainPercentage : EntityEffectCondition
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out PainComponent? pain))
        {
            var painPercentage = pain.CurrentPainPercentage;
            if (painPercentage >= Min && painPercentage < Max)
                return true;
        }

        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-total-damage",
            ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}
