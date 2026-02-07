using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeldableSystem))]
public sealed partial class RMCBlowtorchWeldFuelComponent : Component
{
    // Multiplier applied to WeldableComponent to reduce RMC fuel cost.
    [DataField, AutoNetworkedField]
    public float WeldFuelMultiplier = 0.33f;

    // Minimum fuel cost after multiplier is applied.
    [DataField, AutoNetworkedField]
    public float MinWeldFuel = 0f;

    // Fuel consumption per second while the welder is active (idle burn).
    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelConsumption = FixedPoint2.New(0.025f);

    // Fuel cost to ignite.
    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelLitCost = FixedPoint2.New(0f);
}
