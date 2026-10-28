using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared._RMC14.Synth;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemositic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Shows parasitic behavior towards live erythrocytes in order to produce more of itself.";
    }

    protected override bool ShouldCancel(EntityEffectReagentArgs args)
    {
        return args.EntityManager.HasComponent<SynthComponent>(args.TargetEntity);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            return;

        // this feels kinda jank :clueless:
        var wellFed = args.EntityManager.TryGetComponent<HungerComponent>(args.TargetEntity, out var hunger) &&
                      args.EntityManager.System<HungerSystem>().GetHunger(hunger) >= hunger.Thresholds[HungerThreshold.Peckish];

        var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.TryModifyBloodLevelQuiet((args.TargetEntity, bloodstream), wellFed ? -potency * 3f : -potency * 0.5f);

        if (wellFed && args.Source != null && args.Reagent != null)
            args.Source.AddReagent(args.Reagent.ID, FixedPoint2.New(0.5));
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            return;

        var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.TryModifyBloodLevelQuiet((args.TargetEntity, bloodstream), -potency * 10f);

        if (args.Source != null && args.Reagent != null)
            args.Source.AddReagent(args.Reagent.ID, potency * 2f);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        args.EntityManager.EnsureComponent<RMCNervousComponent>(args.TargetEntity);
    }
}
