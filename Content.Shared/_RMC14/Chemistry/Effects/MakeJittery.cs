using Content.Shared._RMC14.Jittering;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class MakeJittery : EntityEffect
{
    [DataField(required: true)]
    public float Amount;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-jittering", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var scale = (args as EntityEffectReagentArgs)?.Scale.Float() ?? 1f;
        args.EntityManager.System<RMCJitterinessSystem>().MakeJittery(args.TargetEntity, Amount * scale);
    }
}
