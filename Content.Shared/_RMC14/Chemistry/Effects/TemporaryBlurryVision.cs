using Content.Shared._RMC14.Medical.TemporaryBlurryVision;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EntityEffects.Effects;

public sealed partial class TemporaryBlurryVision : EntityEffect
{
    [DataField]
    public int Blur = 3;

    [DataField]
    public float Time = 2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var scale = (args as EntityEffectReagentArgs)?.Scale ?? 1;
        var blurrySys = args.EntityManager.EntitySysManager.GetEntitySystem<TemporaryBlurryVisionSystem>();
        blurrySys.AddTemporaryBlurModificator(args.TargetEntity, TimeSpan.FromSeconds(Time * scale.Float()), Blur);
    }
}
