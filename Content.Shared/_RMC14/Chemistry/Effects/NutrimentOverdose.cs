using Content.Shared._RMC14.Slow;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class NutrimentOverdose : EntityEffect
{
    // Does NOT use RMCChemicalEffect to avoid triggering overdose on Neogenetic and Hemogenic.
    [DataField]
    public FixedPoint2 OverdoseThreshold = 60;

    [DataField]
    public TimeSpan SlowdownDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public FixedPoint2 PercentRate = 0.1;

    [DataField]
    public FixedPoint2 MinimumRate = 5;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Causes [color=yellow]vomiting[/color] and [color=red]slowdown[/color] above [color=yellow]{OverdoseThreshold}u[/color].\n" +
               $"Removes [color=green]{PercentRate * 100}%[/color] or [color=green]{MinimumRate}u[/color] of Nutriment per second while above [color=yellow]{OverdoseThreshold}u[/color]";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        if (args.EntityManager.TryGetComponent<MobStateComponent>(args.TargetEntity, out var mobState) &&
            mobState.CurrentState == MobState.Dead)
            return;

        if (reagentArgs.Source == null)
            return;

        // Check if overdosing
        var nutVolume = reagentArgs.Source.GetTotalPrototypeQuantity("Nutriment");
        if (nutVolume < OverdoseThreshold)
            return;

        // Calculate and remove nutriment
        var removalAmount = FixedPoint2.Max(nutVolume * PercentRate, MinimumRate) * reagentArgs.Scale;
        reagentArgs.Source.RemoveReagent("Nutriment", removalAmount);

        // Check if we should apply vomiting (still overdosing)
        if (nutVolume < OverdoseThreshold)
            return;

        // Check if already vomiting - RMCVomitComponent tracks vomit cooldown
        if (args.EntityManager.HasComponent<RMCVomitComponent>(args.TargetEntity))
            return;

        var rmcSuperSlow = args.EntityManager.System<RMCSlowSystem>();
        rmcSuperSlow.TrySuperSlowdown(args.TargetEntity, SlowdownDuration);

        var vomitEvent = new RMCVomitEvent(args.TargetEntity);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref vomitEvent);
    }
}
