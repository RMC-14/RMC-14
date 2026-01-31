using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class ConvertToReagent : EntityEffect
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> TargetReagent;

    [DataField]
    public FixedPoint2 PercentRate = 0.1;

    [DataField]
    public FixedPoint2 MinimumRate = 5;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Converts to {TargetReagent} at {PercentRate * 100}% or {MinimumRate}u per second while in the body";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        if (reagentArgs.Source == null)
            return;

        if (reagentArgs.Reagent?.ID == TargetReagent.Id)
            return;

        if (reagentArgs.Quantity <= FixedPoint2.Zero)
            return;

        var amountToConvert = FixedPoint2.Max(reagentArgs.Quantity * PercentRate, MinimumRate) * reagentArgs.Scale;
        reagentArgs.Source.RemoveReagent(reagentArgs.Reagent!.ID, amountToConvert);
        reagentArgs.Source.AddReagent(TargetReagent, amountToConvert);
    }
}
