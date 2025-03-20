using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class AddXenoEmbryo : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-plasma-egg", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        if (!entityManager.TryGetComponent<InfectableComponent>(args.TargetEntity, out var infected) ||
            infected.BeingInfected ||
            entityManager.HasComponent<VictimInfectedComponent>(args.TargetEntity))
            return;

        // TODO: Set hive.
        entityManager.AddComponent<VictimInfectedComponent>(args.TargetEntity);
    }
}
