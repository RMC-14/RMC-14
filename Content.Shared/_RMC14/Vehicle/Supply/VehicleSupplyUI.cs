using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle.Supply;

[Serializable, NetSerializable]
public enum VehicleSupplyUIKey
{
    Key
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VehicleSupplyEntryState
{
    [DataField]
    public string Id;

    [DataField]
    public string Name;

    [DataField]
    public int Count;

    public VehicleSupplyEntryState()
    {
        Id = string.Empty;
        Name = string.Empty;
    }

    public VehicleSupplyEntryState(string id, string name, int count)
    {
        Id = id;
        Name = name;
        Count = count;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VehicleSupplyUiState
{
    [DataField]
    public VehicleSupplyLiftMode? LiftMode;

    [DataField]
    public bool Busy;

    [DataField]
    public string? ActiveVehicleId;

    [DataField]
    public string? SelectedVehicleId;

    [DataField]
    public int SelectedCopyIndex;

    [DataField]
    public VehicleSupplyPreviewState? Preview;

    [DataField]
    public List<VehicleSupplyEntryState> Available;

    public VehicleSupplyUiState()
    {
        Available = new List<VehicleSupplyEntryState>();
    }

    public VehicleSupplyUiState(
        VehicleSupplyLiftMode? liftMode,
        bool busy,
        string? activeVehicleId,
        string? selectedVehicleId,
        int selectedCopyIndex,
        VehicleSupplyPreviewState? preview,
        List<VehicleSupplyEntryState> available)
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

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VehicleSupplyPreviewState
{
    [DataField]
    public string VehicleId;

    [DataField]
    public List<VehicleHardpointLayerState> Layers;

    [DataField]
    public List<VehicleSupplyPreviewOverlay> Overlays;

    public VehicleSupplyPreviewState()
    {
        VehicleId = string.Empty;
        Layers = new List<VehicleHardpointLayerState>();
        Overlays = new List<VehicleSupplyPreviewOverlay>();
    }

    public VehicleSupplyPreviewState(
        string vehicleId,
        List<VehicleHardpointLayerState> layers,
        List<VehicleSupplyPreviewOverlay> overlays)
    {
        VehicleId = vehicleId;
        Layers = layers;
        Overlays = overlays;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VehicleSupplyPreviewOverlay
{
    [DataField]
    public string Rsi;

    [DataField]
    public string State;

    [DataField]
    public int Order;

    [DataField]
    public Vector2 BaseOffset;

    [DataField]
    public bool UseDirectional;

    [DataField]
    public Vector2 North;

    [DataField]
    public Vector2 East;

    [DataField]
    public Vector2 South;

    [DataField]
    public Vector2 West;

    public VehicleSupplyPreviewOverlay()
    {
        Rsi = string.Empty;
        State = string.Empty;
    }

    public VehicleSupplyPreviewOverlay(
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
public sealed class VehicleSupplySelectMsg : BoundUserInterfaceMessage
{
    public string VehicleId;
    public int CopyIndex;

    public VehicleSupplySelectMsg(string vehicleId, int copyIndex)
    {
        VehicleId = vehicleId;
        CopyIndex = copyIndex;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleSupplyLiftMsg : BoundUserInterfaceMessage
{
    public bool Raise;

    public VehicleSupplyLiftMsg(bool raise)
    {
        Raise = raise;
    }
}
