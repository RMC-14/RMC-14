using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class ClearOtherReagents : EntityEffect
{
    /// <summary>
    /// Only this reagent will be kept.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? Reagent = null;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-excreting", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        if (reagentArgs.Source == null)
            return;

        foreach (var quant in reagentArgs.Source.Contents.ToArray())
        {
            if (quant.Reagent.Prototype == Reagent)
                continue;

            reagentArgs.Source.RemoveReagent(quant);
        }
    }
}
