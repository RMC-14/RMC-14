using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum HardpointUiKey : byte
{
    Key,
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class HardpointUiEntry
{
    [DataField]
    public string SlotId;

    [DataField]
    public string HardpointType;

    [DataField]
    public string? InstalledName;

    [DataField]
    public NetEntity? InstalledEntity;

    [DataField]
    public float Integrity;

    [DataField]
    public float MaxIntegrity;

    [DataField]
    public bool HasIntegrity;

    [DataField]
    public bool HasItem;

    [DataField]
    public bool Required;

    [DataField]
    public bool Removing;

    public HardpointUiEntry()
    {
        SlotId = string.Empty;
        HardpointType = string.Empty;
    }

    public HardpointUiEntry(
        string slotId,
        string hardpointType,
        string? installedName,
        NetEntity? installedEntity,
        float integrity,
        float maxIntegrity,
        bool hasIntegrity,
        bool hasItem,
        bool required,
        bool removing)
    {
        SlotId = slotId;
        HardpointType = hardpointType;
        InstalledName = installedName;
        InstalledEntity = installedEntity;
        Integrity = integrity;
        MaxIntegrity = maxIntegrity;
        HasIntegrity = hasIntegrity;
        HasItem = hasItem;
        Required = required;
        Removing = removing;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class HardpointUiState
{
    [DataField]
    public List<HardpointUiEntry> Hardpoints;

    [DataField]
    public float FrameIntegrity;

    [DataField]
    public float FrameMaxIntegrity;

    [DataField]
    public bool HasFrameIntegrity;

    [DataField]
    public string? Error;

    public HardpointUiState()
    {
        Hardpoints = new List<HardpointUiEntry>();
    }

    public HardpointUiState(
        List<HardpointUiEntry> hardpoints,
        float frameIntegrity,
        float frameMaxIntegrity,
        bool hasFrameIntegrity,
        string? error)
    {
        Hardpoints = hardpoints;
        FrameIntegrity = frameIntegrity;
        FrameMaxIntegrity = frameMaxIntegrity;
        HasFrameIntegrity = hasFrameIntegrity;
        Error = error;
    }
}

[Serializable, NetSerializable]
public sealed class HardpointRemoveMessage : BoundUserInterfaceMessage
{
    public readonly string SlotId;

    public HardpointRemoveMessage(string slotId)
    {
        SlotId = slotId;
    }
}
