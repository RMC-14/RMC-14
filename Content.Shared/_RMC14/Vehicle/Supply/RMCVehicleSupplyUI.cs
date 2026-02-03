using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle.Supply;

[Serializable, NetSerializable]
public enum RMCVehicleSupplyUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RMCVehicleSupplyEntryState
{
    public string Id;
    public string Name;
    public int Count;

    public RMCVehicleSupplyEntryState(string id, string name, int count)
    {
        Id = id;
        Name = name;
        Count = count;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleSupplyBuiState : BoundUserInterfaceState
{
    public RMCVehicleSupplyLiftMode? LiftMode;
    public bool Busy;
    public string? ActiveVehicleId;
    public List<RMCVehicleSupplyEntryState> Available;

    public RMCVehicleSupplyBuiState(
        RMCVehicleSupplyLiftMode? liftMode,
        bool busy,
        string? activeVehicleId,
        List<RMCVehicleSupplyEntryState> available)
    {
        LiftMode = liftMode;
        Busy = busy;
        ActiveVehicleId = activeVehicleId;
        Available = available;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleSupplySelectMsg : BoundUserInterfaceMessage
{
    public string VehicleId;

    public RMCVehicleSupplySelectMsg(string vehicleId)
    {
        VehicleId = vehicleId;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleSupplyLiftMsg : BoundUserInterfaceMessage
{
    public bool Raise;

    public RMCVehicleSupplyLiftMsg(bool raise)
    {
        Raise = raise;
    }
}
