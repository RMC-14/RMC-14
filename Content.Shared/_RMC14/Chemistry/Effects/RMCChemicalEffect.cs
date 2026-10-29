using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Alcohol;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
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

    public virtual bool IsFireSupplement => false;

    public virtual bool IsVolatile => false;

    public virtual void ModifyFireStats(ref RMCFireStatsBuilder stats, float potency)
    {
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent } reagentArgs)
            return;

        var damageable = args.EntityManager.System<DamageableSystem>();
        var scale = reagentArgs.Scale;
        var isPlant = args.EntityManager.TryGetComponent<RMCPlantComponent>(args.TargetEntity, out var plant);
        var reaction = IsReaction(reagentArgs);
        _moddedPotency = isPlant || reaction
            ? Potency
            : (Potency + CalculateReagentBoost(reagentArgs)) * CalculateReagentEffectiveness(reagentArgs);
        var scaledPotency = PotencyPerSecond * scale;

        if (isPlant)
        {
            TickHydroTray(new Entity<RMCPlantComponent>(args.TargetEntity, plant!), scaledPotency, reagentArgs);
            return;
        }

        if (reaction && IoCManager.Resolve<INetManager>().IsClient)
            return;

        if (!reaction && IsCancelled(reagentArgs))
            return;

        if (!reaction && args.EntityManager.System<RMCAlcoholOnsetSystem>().IsPending(args.TargetEntity, reagent.ID))
            return;

        var dead = !reaction && IsDead(reagentArgs);
        if (!dead || IsForcedWhenDead(reagentArgs))
        {
            Tick(damageable, scaledPotency, reagentArgs);

            if (reaction)
                return;

            var totalQuantity = FixedPoint2.Zero;
            if (reagentArgs.Source != null)
                totalQuantity = reagentArgs.Source.GetTotalPrototypeQuantity(reagent.ID);

            if (reagent.Overdose != null && totalQuantity > reagent.Overdose && !IsRegulated(reagentArgs))
                TickOverdose(damageable, scaledPotency, reagentArgs);

            if (reagent.CriticalOverdose != null && totalQuantity > reagent.CriticalOverdose && !IsRegulated(reagentArgs))
                TickCriticalOverdose(damageable, scaledPotency, reagentArgs);
        }

        if (dead)
        {
            if (TickDead(damageable, scaledPotency, reagentArgs))
                reagentArgs.RMCProcessedDead = true;
        }
    }

    protected static bool IsReaction(EntityEffectReagentArgs args)
    {
        return args.Method != null;
    }

    private static bool IsDead(EntityEffectReagentArgs args)
    {
        return args.EntityManager.TryGetComponent<MobStateComponent>(args.TargetEntity, out var mobState) &&
               args.EntityManager.System<MobStateSystem>().IsDead(args.TargetEntity, mobState);
    }

    private static IReadOnlyList<RMCChemicalEffect> GetSiblingProperties(EntityEffectReagentArgs args)
    {
        if (args.Reagent is not { } reagent)
            return [];

        return args.EntityManager.System<RMCReagentSystem>().GetProperties(reagent.ID);
    }

    private static bool HasSiblingProperty<T>(EntityEffectReagentArgs args) where T : RMCChemicalEffect
    {
        return args.Reagent is { } reagent && args.EntityManager.System<RMCReagentSystem>().HasProperty<T>(reagent.ID);
    }

    public static bool IsForcedWhenDead(EntityEffectReagentArgs args)
    {
        return HasSiblingProperty<Neutral.Thanatometabolizing>(args);
    }

    private static float CalculateReagentBoost(EntityEffectReagentArgs args)
    {
        var boost = 0f;
        foreach (var property in GetSiblingProperties(args))
        {
            property.ReagentBoost(args, ref boost);
        }

        return boost;
    }

    private static float CalculateReagentEffectiveness(EntityEffectReagentArgs args)
    {
        var effectiveness = 1f;
        foreach (var property in GetSiblingProperties(args))
        {
            property.ReagentEffectiveness(args, ref effectiveness);
        }

        return effectiveness;
    }

    public static bool IsMetabolismCancelled(EntityEffectReagentArgs args)
    {
        return !IsReaction(args) && IsCancelled(args);
    }

    private static bool IsRegulated(EntityEffectReagentArgs args)
    {
        return HasSiblingProperty<Special.Regulating>(args);
    }

    private static bool IsCancelled(EntityEffectReagentArgs args)
    {
        foreach (var property in GetSiblingProperties(args))
        {
            if (property.ShouldCancel(args))
                return true;
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

    protected virtual void ReagentEffectiveness(EntityEffectReagentArgs args, ref float effectiveness)
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

    protected static int RoundStochastic(float amount)
    {
        var whole = (int) MathF.Floor(amount);
        if (IoCManager.Resolve<IRobustRandom>().Prob(amount - whole))
            whole++;

        return whole;
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

    protected virtual bool TickDead(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        return false;
    }
}
