using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class NutrimentOverdose : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Overdoses cause [color=yellow]vomiting[/color] and [color=red]slowdown[/color].\n" +
               $"Removes [color=green]max(volume/10, 5)u[/color] per second.";
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        if (entityManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
            mobState.CurrentState == MobState.Dead)
            return;

        var source = args.Source;
        if (source == null)
            return;

        // Calculate and remove nutriment
        var nutrimentVolume = source.GetTotalPrototypeQuantity("Nutriment");
        var percentageRemoval = nutrimentVolume * 0.1f;
        var minRemovalAmount = FixedPoint2.New(5);
        var removalAmount = FixedPoint2.Max(percentageRemoval, minRemovalAmount) * args.Scale;
        source.RemoveReagent("Nutriment", removalAmount);

        // Check if we should apply vomiting (still overdosing after initial removal)
        var remainingVolume = source.GetTotalPrototypeQuantity("Nutriment");
        if (args.Reagent?.Overdose == null || remainingVolume < args.Reagent.Overdose)
            return;

        // Check if already vomiting - RMCVomitComponent tracks vomit cooldown
        if (entityManager.HasComponent<RMCVomitComponent>(target))
            return;

        var overdoseComp = entityManager.EnsureComponent<NutrimentOverdoseComponent>(target);
        overdoseComp.RemainingVolume = remainingVolume.Float();

        var stunSystem = entityManager.System<SharedStunSystem>();
        stunSystem.TrySlowdown(target, overdoseComp.SlowdownDuration, true, 0.5f, 0.5f);

        // Trigger vomit - RMCVomitSystem handles all the delays and cooldowns
        var vomitEvent = new RMCVomitEvent(target);
        entityManager.EventBus.RaiseEvent(EventSource.Local, ref vomitEvent);
    }
}
