using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Embryonic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Carries an infectious parasitic embryonic organism.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
            return;

        var entities = args.EntityManager;
        var target = args.TargetEntity;
        if (!entities.HasComponent<HumanoidAppearanceComponent>(target) ||
            entities.HasComponent<VictimInfectedComponent>(target) ||
            entities.HasComponent<SynthComponent>(target) ||
            !entities.HasComponent<InfectableComponent>(target))
        {
            return;
        }

        var slot = Math.Min((int) Potency % 10, 5);
        var hive = Ciphering.GetHiveBySlot(entities, slot);
        entities.System<SharedXenoParasiteSystem>().Infect(target, hive);
    }
}
