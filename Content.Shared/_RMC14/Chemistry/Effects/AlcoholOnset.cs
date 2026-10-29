using Content.Shared._RMC14.Chemistry.Alcohol;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class AlcoholOnset : EntityEffect
{
    [DataField(required: true)]
    public float BoozePower;

    [DataField]
    public float EffectStart = 150;

    [DataField]
    public FixedPoint2 OnsetMetabolism = 0.1;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Takes effect after [color=yellow]{EffectStart / BoozePower * 2:0.#}[/color] seconds in the body, " +
               $"then metabolizes an extra [color=yellow]{OnsetMetabolism}u[/color] per second.";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent, Source: { } source, Method: null } reagentArgs)
            return;

        var onset = args.EntityManager.System<RMCAlcoholOnsetSystem>();
        if (!onset.Accumulate(args.TargetEntity, reagent.ID, BoozePower * 0.5f, EffectStart))
            return;

        source.RemoveReagent(reagent.ID, OnsetMetabolism * reagentArgs.Scale);
    }
}
