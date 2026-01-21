using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

/// <summary>
/// Implements nutriment overdose effects from CMSS13.
/// When overdosing, removes reagent, applies slowdown, and triggers vomiting.
/// </summary>
public sealed partial class NutrimentOverdose : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Overdoses cause vomiting and slowdown. " +
               "Removes [color=red]max(volume/10, 5)u[/color] per second.";
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;

        // Dead entities don't process overdose
        if (entityManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
            mobState.CurrentState == MobState.Dead)
            return;

        var source = args.Source;
        if (source == null)
            return;

        // Calculate and remove nutriment
        const float removalDivisor = 10f;
        var minRemovalAmount = FixedPoint2.New(5);

        var nutrimentVolume = source.GetTotalPrototypeQuantity("Nutriment");
        var removalAmount = FixedPoint2.Max(nutrimentVolume / removalDivisor, minRemovalAmount) * args.Scale;
        source.RemoveReagent("Nutriment", removalAmount);

        // Check if we should apply vomiting effects (still overdosing after removal)
        var remainingVolume = source.GetTotalPrototypeQuantity("Nutriment");
        if (args.Reagent?.Overdose == null || remainingVolume < args.Reagent.Overdose)
            return;

        // Check if already vomiting - RMCVomitComponent tracks vomit cooldown
        if (entityManager.HasComponent<RMCVomitComponent>(target))
            return;

        // Apply slowdown (Superslow in CMSS13)
        var overdoseComp = entityManager.EnsureComponent<NutrimentOverdoseComponent>(target);
        overdoseComp.RemainingVolume = remainingVolume.Float();

        var stunSystem = entityManager.System<SharedStunSystem>();
        stunSystem.TrySlowdown(target, overdoseComp.SlowdownDuration, true, 0.5f, 0.5f);

        // Trigger vomit - RMCVomitSystem handles all the delays and cooldowns
        var vomitEvent = new RMCVomitEvent(target);
        entityManager.EventBus.RaiseEvent(EventSource.Local, ref vomitEvent);
    }
}
