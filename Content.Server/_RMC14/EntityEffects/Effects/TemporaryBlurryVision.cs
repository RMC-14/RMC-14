using Content.Shared._RMC14.Medical.TemporaryBlurryVision;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.EntityEffects.Effects;

public sealed partial class TemporaryBlurryVision : EntityEffect
{
    [DataField]
    public float Blur = 3f;

    [DataField]
    public float Time = 2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var time = Time;

        if (args is EntityEffectReagentArgs reagentArgs) {
            time *= reagentArgs.Scale.Float();
        }

        var blurrySys = args.EntityManager.EntitySysManager.GetEntitySystem<TemporaryBlurrySystem>();
        blurrySys.TryApplyBlindness(args.TargetEntity, Blur, time);
    }
}
