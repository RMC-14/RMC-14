using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle.Supply;

[Serializable, NetSerializable]
public enum RMCVehicleHardpointVendorUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RMCVehicleHardpointVendorBuiState : BoundUserInterfaceState
{
    public List<RMCVehicleSupplyEntryState> Vehicles;
    public List<RMCVehicleSupplyEntryState> Hardpoints;
    public string SelectedVehicle;

    public RMCVehicleHardpointVendorBuiState(
        List<RMCVehicleSupplyEntryState> vehicles,
        List<RMCVehicleSupplyEntryState> hardpoints,
        string selectedVehicle)
    {
        Vehicles = vehicles;
        Hardpoints = hardpoints;
        SelectedVehicle = selectedVehicle;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleHardpointVendorSelectMsg : BoundUserInterfaceMessage
{
    public string VehicleId;

    public RMCVehicleHardpointVendorSelectMsg(string vehicleId)
    {
        VehicleId = vehicleId;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleHardpointVendorPrintMsg : BoundUserInterfaceMessage
{
    public string HardpointId;

    public RMCVehicleHardpointVendorPrintMsg(string hardpointId)
    {
        HardpointId = hardpointId;
    }
}
