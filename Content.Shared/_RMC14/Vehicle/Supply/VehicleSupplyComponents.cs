using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle.Supply;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class VehicleSupplyEntry
{
    [DataField]
    public string? Name;

    [DataField(required: true)]
    public EntProtoId Vehicle;

    [DataField]
    public string? Unlock;

    [DataField]
    public List<EntProtoId> Hardpoints = new();
}

[RegisterComponent]
public sealed partial class VehicleSupplyConsoleComponent : Component
{
    [DataField(required: true)]
    public List<VehicleSupplyEntry> Vehicles = new();

    [DataField]
    public float LiftSearchRange = 20f;

    [DataField]
    public string SelectedVehicle = string.Empty;

    [DataField]
    public int SelectedVehicleCopyIndex;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleSupplyTechComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Unlocked = new();
}

[RegisterComponent]
public sealed partial class VehicleHardpointVendorComponent : Component
{
    [DataField]
    public float ConsoleSearchRange = 20f;

    [NonSerialized]
    public readonly Dictionary<string, int> LastVehicleCounts = new();

    [NonSerialized]
    public readonly Dictionary<string, int> RemainingGroupAmounts = new();
}
