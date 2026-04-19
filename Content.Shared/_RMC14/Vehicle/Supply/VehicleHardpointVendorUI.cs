using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle.Supply;

[Serializable, NetSerializable]
public enum VehicleHardpointVendorUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class VehicleHardpointVendorBuiState : BoundUserInterfaceState
{
    public List<VehicleSupplyEntryState> Vehicles;
    public List<VehicleSupplyEntryState> Hardpoints;
    public string SelectedVehicle;

    public VehicleHardpointVendorBuiState(
        List<VehicleSupplyEntryState> vehicles,
        List<VehicleSupplyEntryState> hardpoints,
        string selectedVehicle)
    {
        Vehicles = vehicles;
        Hardpoints = hardpoints;
        SelectedVehicle = selectedVehicle;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleHardpointVendorSelectMsg : BoundUserInterfaceMessage
{
    public string VehicleId;

    public VehicleHardpointVendorSelectMsg(string vehicleId)
    {
        VehicleId = vehicleId;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleHardpointVendorPrintMsg : BoundUserInterfaceMessage
{
    public string HardpointId;

    public VehicleHardpointVendorPrintMsg(string hardpointId)
    {
        HardpointId = hardpointId;
    }
}
