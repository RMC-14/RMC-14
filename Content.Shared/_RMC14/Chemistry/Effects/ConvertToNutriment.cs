using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class ConvertToNutriment : EntityEffect
{
    [DataField]
    public ProtoId<ReagentPrototype> TargetReagent = "Nutriment";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Becomes [color=green]{TargetReagent}[/color] while in the body.";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        var source = reagentArgs.Source;
        if (source == null)
            return;

        if (reagentArgs.Reagent?.ID == TargetReagent.Id)
            return;

        var currentQuantity = reagentArgs.Quantity;
        if (currentQuantity <= FixedPoint2.Zero)
            return;

        var percentageAmount = currentQuantity * 0.1f;
        var minAmount = FixedPoint2.New(5);
        var amountToConvert = FixedPoint2.Max(percentageAmount, minAmount) * reagentArgs.Scale;

        source.RemoveReagent(reagentArgs.Reagent!.ID, amountToConvert);
        source.AddReagent(TargetReagent, amountToConvert);
    }
}
