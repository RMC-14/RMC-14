using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared._RMC14.Repairable;

/// <summary>
///     Handles RMC-specific blowtorch fuel consumption for welding operations.
///     Intercepts weld attempts and applies custom fuel values from RMCBlowtorchWeldFuelComponent.
/// </summary>
public sealed class RMCWeldableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Upstream evil
        SubscribeLocalEvent<WeldableComponent, WeldableAttemptEvent>(OnWeldableAttempt);
    }

    private void OnWeldableAttempt(EntityUid uid, WeldableComponent component, WeldableAttemptEvent args)
    {
        // Check if the tool being used has our RMC blowtorch fuel component
        if (!TryComp<RMCBlowtorchWeldFuelComponent>(args.Tool, out var blowtorchFuel))
            return;

        // Apply fuel multiplier with minimum cap
        var newFuel = component.Fuel * blowtorchFuel.WeldFuelMultiplier;
        component.Fuel = Math.Max(newFuel, blowtorchFuel.MinWeldFuel);
    }
}
