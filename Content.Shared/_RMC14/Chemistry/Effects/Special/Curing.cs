using Content.Shared._RMC14.Chemistry.XenoTransformation;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Curing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Binds to and neutralizes specific microbiological organisms.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
            return;

        if (ActualPotency >= 2)
            args.EntityManager.System<RMCXenoTransformationSystem>().Cure(args.TargetEntity);
    }

    // TODO RMC14: cures other diseases such as the zombie infection
}
