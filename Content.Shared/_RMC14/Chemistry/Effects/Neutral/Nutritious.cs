using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Nutritious : RMCChemicalEffect
{
    public override string Abbreviation => "NTR";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var updatedFactor = NutrimentFactor + Level;
        return $"Restores [color=green]{updatedFactor * Potency}[/color] nutrients to the body and satiates hunger";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var mobState = System<MobStateSystem>(args);
        if (mobState.IsDead(args.TargetEntity))
            return;

        var hunger = System<HungerSystem>(args);
        var updatedFactor = NutrimentFactor + Level;
        hunger.ModifyHunger(args.TargetEntity, updatedFactor * Potency); // Half because chemicals tick every second
    }
}
