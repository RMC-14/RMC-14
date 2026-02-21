using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
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
    public string? SelectedVehicleId;
    public int SelectedCopyIndex;
    public RMCVehicleSupplyPreviewState? Preview;
    public List<RMCVehicleSupplyEntryState> Available;

    public RMCVehicleSupplyBuiState(
        RMCVehicleSupplyLiftMode? liftMode,
        bool busy,
        string? activeVehicleId,
        string? selectedVehicleId,
        int selectedCopyIndex,
        RMCVehicleSupplyPreviewState? preview,
        List<RMCVehicleSupplyEntryState> available)
    {
        LiftMode = liftMode;
        Busy = busy;
        ActiveVehicleId = activeVehicleId;
        SelectedVehicleId = selectedVehicleId;
        SelectedCopyIndex = selectedCopyIndex;
        Preview = preview;
        Available = available;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleSupplyPreviewState
{
    public string VehicleId;
    public int CopyIndex;
    public List<RMCVehicleHardpointLayerState> Layers;
    public List<RMCVehicleSupplyPreviewOverlay> Overlays;

    public RMCVehicleSupplyPreviewState(
        string vehicleId,
        int copyIndex,
        List<RMCVehicleHardpointLayerState> layers,
        List<RMCVehicleSupplyPreviewOverlay> overlays)
    {
        VehicleId = vehicleId;
        CopyIndex = copyIndex;
        Layers = layers;
        Overlays = overlays;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleSupplyPreviewOverlay
{
    public string Rsi;
    public string State;
    public int Order;
    public Vector2 BaseOffset;
    public bool UseDirectional;
    public Vector2 North;
    public Vector2 East;
    public Vector2 South;
    public Vector2 West;

    public RMCVehicleSupplyPreviewOverlay(
        string rsi,
        string state,
        int order,
        Vector2 baseOffset,
        bool useDirectional,
        Vector2 north,
        Vector2 east,
        Vector2 south,
        Vector2 west)
    {
        Rsi = rsi;
        State = state;
        Order = order;
        BaseOffset = baseOffset;
        UseDirectional = useDirectional;
        North = north;
        East = east;
        South = south;
        West = west;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleSupplySelectMsg : BoundUserInterfaceMessage
{
    public string VehicleId;
    public int CopyIndex;

    public RMCVehicleSupplySelectMsg(string vehicleId, int copyIndex)
    {
        VehicleId = vehicleId;
        CopyIndex = copyIndex;
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
