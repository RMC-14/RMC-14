using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Medical.Pain;

namespace Content.Shared._RMC14.EntityEffects.Effects;

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
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out PainComponent? pain))
            painSystem.AddPainModificator(args.TargetEntity, TimeSpan.FromSeconds(StatusLifeTime * scale.Float()), Strength, PainModificatorType.PainReduction, pain);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-decrease-pain", ("chance", Probability), ("amount", (int)Strength));
    }
}
