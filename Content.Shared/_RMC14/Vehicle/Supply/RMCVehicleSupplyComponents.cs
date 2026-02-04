using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle.Supply;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RMCVehicleSupplyEntry
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
public sealed partial class RMCVehicleSupplyConsoleComponent : Component
{
    [DataField(required: true)]
    public List<RMCVehicleSupplyEntry> Vehicles = new();

    [DataField]
    public float LiftSearchRange = 20f;

    [DataField]
    public string SelectedVehicle = string.Empty;

    [DataField]
    public int SelectedVehicleCopyIndex;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleSupplyTechComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Unlocked = new();
}

[RegisterComponent]
public sealed partial class RMCVehicleHardpointVendorComponent : Component
{
    [DataField]
    public float ConsoleSearchRange = 20f;

    [NonSerialized]
    public readonly Dictionary<string, int> LastVehicleCounts = new();
}
