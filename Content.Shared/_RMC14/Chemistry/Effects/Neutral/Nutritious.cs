using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Nutritious : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var updatedNutrimentFactor = NutrimentFactor > 0 ? NutrimentFactor : ActualPotency;
        return $"Restores [color=green]{updatedNutrimentFactor * ActualPotency}[/color] nutrients to the body";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var mobStateSystem = entityManager.System<MobStateSystem>();
        var hungerSystem = entityManager.System<HungerSystem>();

        if (mobStateSystem.IsDead(target))
            return;

        if (!entityManager.TryGetComponent<HungerComponent>(target, out var hungerComponent))
            return;

        var updatedNutrimentFactor = NutrimentFactor > 0 ? NutrimentFactor : Potency;
        hungerSystem.ModifyHunger(target, updatedNutrimentFactor * ActualPotency, hungerComponent);
    }
}
