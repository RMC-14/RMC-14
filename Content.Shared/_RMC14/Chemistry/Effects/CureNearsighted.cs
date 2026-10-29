using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class CureNearsighted : EntityEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "After [color=yellow]40 seconds[/color] in the body, has a growing chance to cure [color=green]nearsightedness[/color].";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.System<RMCVisionDisabilitySystem>().CarrotJuiceTick(args.TargetEntity);
    }
}
