using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Medical.Pain;

namespace Content.Server._RMC14.EntityEffects.Effects;

public sealed partial class DecreasePain : EntityEffect
{
    [DataField]
    public FixedPoint2 Strength;

    [DataField]
    public float StatusLifeTime = 1f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var scale = (args as EntityEffectReagentArgs)?.Scale ?? 1;
        var painSystem = args.EntityManager.EntitySysManager.GetEntitySystem<PainSystem>();
        var modificator = new PainModificator(TimeSpan.FromSeconds(StatusLifeTime * scale.Float()), Strength, PainModificatorType.PainReduction);
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out PainComponent? pain))
            painSystem.AddPainModificator(args.TargetEntity, modificator, pain);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}
