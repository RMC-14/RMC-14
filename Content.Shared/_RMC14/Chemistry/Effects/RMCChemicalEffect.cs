using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Chemistry.Effects;

public abstract partial class RMCChemicalEffect : EntityEffect
{
    [DataField]
    public float Potency;

    /// <summary>
    ///     The value that should be used in actual calculations for chemical effect
    ///     Halved since potency is halved before being used
    /// </summary>
    public float ActualPotency => Potency * 0.5f;

    // Halved again since chemicals tick every second in SS14, not every 2
    public float PotencyPerSecond => ActualPotency * 0.5f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent } reagentArgs)
            return;

        var damageable = args.EntityManager.System<DamageableSystem>();
        var scale = reagentArgs.Scale;
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
