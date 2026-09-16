using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects;

public abstract partial class RMCChemicalEffect : EntityEffect
{
    [DataField]
    public float Potency;

    private float _moddedPotency;

    /// <summary>
    ///     The value that should be used in actual calculations for chemical effect
    ///     Halved since potency is halved before being used
    /// </summary>
    public float ActualPotency => (_moddedPotency != 0 ? _moddedPotency : Potency) * 0.5f;

    // Halved again since chemicals tick every second in SS14, not every 2
    public float PotencyPerSecond => ActualPotency * 0.5f;

    [DataField]
    public float NutFactor;
    [DataField]
    public float NutMetabolism;

    public float NutrimentFactor => NutFactor * NutMetabolism;


    public virtual float MetabolismRateMultiplier => 1f;

    public virtual bool ReactsOnTouch => false;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent } reagentArgs)
            return;

        var damageable = args.EntityManager.System<DamageableSystem>();
        var scale = reagentArgs.Scale;
        var boost = CalculateReagentBoost(reagentArgs);
        _moddedPotency = Potency + boost;
        var scaledPotency = PotencyPerSecond * scale;

        if (args.EntityManager.TryGetComponent<RMCPlantComponent>(args.TargetEntity, out var plant))
        {
            TickHydroTray(new Entity<RMCPlantComponent>(args.TargetEntity, plant), scaledPotency, reagentArgs);
            return;
        }

        if (IsCancelled(reagentArgs))
            return;

        Tick(damageable, scaledPotency, reagentArgs);

        var totalQuantity = FixedPoint2.Zero;
        if (reagentArgs.Source != null)
            totalQuantity = reagentArgs.Source.GetTotalPrototypeQuantity(reagent.ID);

        if (reagent.Overdose != null && totalQuantity >= reagent.Overdose && !IsRegulated(reagentArgs))
            TickOverdose(damageable, scaledPotency, reagentArgs);

        if (reagent.CriticalOverdose != null && totalQuantity >= reagent.CriticalOverdose && !IsRegulated(reagentArgs))
            TickCriticalOverdose(damageable, scaledPotency, reagentArgs);
    }

    private static float CalculateReagentBoost(EntityEffectReagentArgs args)
    {
        var boost = 0f;
        if (args.Reagent?.Metabolisms == null)
            return boost;

        foreach (var (_, entry) in args.Reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is RMCChemicalEffect rmcEffect)
                {
                    rmcEffect.ReagentBoost(args, ref boost);
                }
            }
        }
        return boost;
    }

    private static bool IsRegulated(EntityEffectReagentArgs args)
    {
        if (args.Reagent?.Metabolisms == null)
            return false;

        foreach (var (_, entry) in args.Reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is Special.Regulating)
                    return true;
            }
        }

        return false;
    }

    private static bool IsCancelled(EntityEffectReagentArgs args)
    {
        if (args.Reagent?.Metabolisms == null)
            return false;

        foreach (var (_, entry) in args.Reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is RMCChemicalEffect { } rmcEffect && rmcEffect.ShouldCancel(args))
                    return true;
            }
        }

        return false;
    }

    protected virtual bool ShouldCancel(EntityEffectReagentArgs args)
    {
        return false;
    }

    protected virtual void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
    }

    protected virtual void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected static RMCPlantTrayComponent? GetTray(IEntityManager entityManager, Entity<RMCPlantComponent> plant)
    {
        if (plant.Comp.Tray is { } tray && entityManager.TryGetComponent<RMCPlantTrayComponent>(tray, out var trayComp))
            return trayComp;

        return null;
    }

    protected static void AddYieldMod(IEntityManager entityManager, Entity<RMCPlantComponent> plant, float delta)
    {
        if (delta == 0f)
            return;

        entityManager.System<SharedRMCPlantTraySystem>().SetYieldMod((plant.Owner, plant.Comp), plant.Comp.YieldMod + delta);
    }

    protected static void SuppressMutationSlot(IEntityManager entityManager, Entity<RMCPlantComponent> plant, string slot, float value)
    {
        if (!entityManager.TryGetComponent<RMCPlantMutationComponent>(plant.Owner, out var mutation))
            return;

        if (mutation.Slots.GetValueOrDefault(slot, 0f) <= value)
            return;

        mutation.Slots[slot] = value;
        entityManager.System<SharedRMCPlantTraySystem>().DirtyMutationSlots((plant.Owner, mutation));
    }

    protected static void EnableMutationSlot(IEntityManager entityManager, Entity<RMCPlantComponent> plant, string slot, float value)
    {
        if (!entityManager.TryGetComponent<RMCPlantMutationComponent>(plant.Owner, out var mutation))
            return;

        if (mutation.Slots.GetValueOrDefault(slot, 0f) >= value)
            return;

        mutation.Slots[slot] = value;
        entityManager.System<SharedRMCPlantTraySystem>().DirtyMutationSlots((plant.Owner, mutation));
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
