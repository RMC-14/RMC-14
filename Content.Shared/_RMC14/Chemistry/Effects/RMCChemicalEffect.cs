using System.Linq;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Chemistry.Effects;

public abstract partial class RMCChemicalEffect : EntityEffect
{
    [DataField]
    public float Potency;

    public float ReagentBoost;

    public float PotencyBoost => Potency + ReagentBoost;

    /// <summary>
    ///     The value that should be used in actual calculations for chemical effect
    ///     Halved since potency is halved before being used
    /// </summary>
    public float ActualPotency => PotencyBoost * 0.5f;

    // Halved again since chemicals tick every second in SS14, not every 2
    public float PotencyPerSecond => ActualPotency * 0.5f;

    [DataField]
    public float NutrimentFactor;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent } reagentArgs)
            return;

        var damageable = args.EntityManager.System<DamageableSystem>();
        var scale = reagentArgs.Scale;

        // Calculate reagent-wide boost from all boost provider effects
        var reagentBoostValue = CalculateReagentWideBoost(reagentArgs);

        // Apply the boost to this effect's potency based on whether this effect allows self-boosting
        var allowSelfBoost = this is IReagentBooster { BoostSelf: false };
        ReagentBoost = allowSelfBoost ? reagentBoostValue : this is IReagentBooster ? 0f : reagentBoostValue;

        var scaledPotency = PotencyPerSecond * scale;
        Tick(damageable, scaledPotency, reagentArgs);

        var totalQuantity = FixedPoint2.Zero;
        if (reagentArgs.Source != null)
            totalQuantity = reagentArgs.Source.GetTotalPrototypeQuantity(reagent.ID);

        if (reagent.Overdose != null && totalQuantity >= reagent.Overdose)
            TickOverdose(damageable, scaledPotency, reagentArgs);

        if (reagent.CriticalOverdose != null && totalQuantity >= reagent.CriticalOverdose)
            TickCriticalOverdose(damageable, scaledPotency, reagentArgs);
    }

    private static float CalculateReagentWideBoost(EntityEffectReagentArgs args)
    {
        if (args.Reagent?.Metabolisms == null)
            return 0f;

        var totalBoost = 0f;

        foreach (var effect in args.Reagent.Metabolisms.Values.SelectMany(metabolism => metabolism.Effects))
        {
            if (effect is IReagentBooster boostProvider)
            {
                totalBoost += boostProvider.CalculateBoost(args);
            }
        }
        return totalBoost;
    }

    protected virtual void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected virtual void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected virtual void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }
}
