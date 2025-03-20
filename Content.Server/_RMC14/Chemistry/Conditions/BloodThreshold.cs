using Content.Server.Body.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Conditions;
public sealed partial class BloodThreshold : EntityEffectCondition
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        var entity = args.EntityManager;
        if (!entity.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream) || bloodstream.BloodSolution is not { } blood)
            return false;

        var volume = blood.Comp.Solution.Volume;
        return volume >= Min && volume <= Max;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-blood-threshold",
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}
