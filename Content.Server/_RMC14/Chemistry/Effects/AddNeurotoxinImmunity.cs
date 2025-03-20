using Content.Shared._RMC14.Xenonids.Neurotoxin;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class AddNeurotoxinImmunity : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-neurotoxin-immunity", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.EnsureComponent<NeurotoxinImmunityComponent>(args.TargetEntity);
        args.EntityManager.RemoveComponent<NeurotoxinComponent>(args.TargetEntity);
    }
}
