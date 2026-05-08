using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeldableSystem))]
public sealed partial class RMCBlowtorchWeldFuelComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WeldFuelMultiplier = 0.33f;

    [DataField, AutoNetworkedField]
    public float MinWeldFuel = 0f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelConsumption = FixedPoint2.New(0.025f);

    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelLitCost = FixedPoint2.New(0f);
}
