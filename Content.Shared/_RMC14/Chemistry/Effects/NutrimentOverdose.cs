using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class NutrimentOverdose : EntityEffect
{
    // Does NOT use RMCChemicalEffect to avoid triggering overdose on other effects like Neogenetic/Hemogenic.
    [DataField]
    public FixedPoint2 OverdoseThreshold = 60;
    [DataField]
    public TimeSpan SlowdownDuration = TimeSpan.FromSeconds(2);

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Overdoses above [color=yellow]{OverdoseThreshold}u[/color] causes [color=yellow]vomiting[/color] and [color=red]slowdown[/color]. " +
               "Removes [color=green]10%[/color] or [color=green]5u[/color] per second.";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        if (entityManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
            mobState.CurrentState == MobState.Dead)
            return;

        var source = reagentArgs.Source;
        if (source == null)
            return;

        // Check if overdosing
        var nutrimentVolume = source.GetTotalPrototypeQuantity("Nutriment");
        if (nutrimentVolume < OverdoseThreshold)
            return;

        // Calculate and remove nutriment
        var removalAmount = FixedPoint2.Max(nutrimentVolume * 0.1f, 5) * reagentArgs.Scale;
        source.RemoveReagent("Nutriment", removalAmount);

        // Check if we should apply vomiting (still overdosing after removal)
        var remainingVolume = source.GetTotalPrototypeQuantity("Nutriment");
        if (remainingVolume < OverdoseThreshold)
            return;

        // Check if already vomiting - RMCVomitComponent tracks vomit cooldown
        if (entityManager.HasComponent<RMCVomitComponent>(target))
            return;

        var stunSystem = entityManager.System<SharedStunSystem>();
        stunSystem.TrySlowdown(target, SlowdownDuration, true, 0.5f, 0.5f);

        // Trigger vomit - RMCVomitSystem handles all the delays and cooldowns
        var vomitEvent = new RMCVomitEvent(target);
        entityManager.EventBus.RaiseEvent(EventSource.Local, ref vomitEvent);
    }
}
