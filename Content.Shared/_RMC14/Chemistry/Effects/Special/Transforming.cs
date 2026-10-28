using Content.Shared._RMC14.Chemistry.XenoTransformation;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Transforming : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Alters the host psychologically and physically.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
            return;

        args.EntityManager.System<RMCXenoTransformationSystem>().TryContract(args.TargetEntity);
    }
}
