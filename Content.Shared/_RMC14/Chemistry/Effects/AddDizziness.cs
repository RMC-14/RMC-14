using Content.Shared._RMC14.Dizziness;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class AddDizziness : EntityEffect
{
    [DataField(required: true)]
    public float Amount;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes [color=yellow]dizziness[/color].";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var scale = (args as EntityEffectReagentArgs)?.Scale.Float() ?? 1f;
        args.EntityManager.System<RMCDizzinessSystem>().AddDizzinessCapped(args.TargetEntity, Amount * scale, float.MaxValue);
    }
}
