using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum RMCHardpointUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCHardpointUiEntry
{
    public readonly string SlotId;
    public readonly string HardpointType;
    public readonly string? InstalledName;
    public readonly NetEntity? InstalledEntity;
    public readonly float Integrity;
    public readonly float MaxIntegrity;
    public readonly bool HasIntegrity;
    public readonly bool HasItem;
    public readonly bool Required;
    public readonly bool Removing;

    public RMCHardpointUiEntry(
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

[Serializable, NetSerializable]
public sealed class RMCHardpointBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<RMCHardpointUiEntry> Hardpoints;

    public RMCHardpointBoundUserInterfaceState(List<RMCHardpointUiEntry> hardpoints)
    {
        Hardpoints = hardpoints;
    }
}

[Serializable, NetSerializable]
public sealed class RMCHardpointRemoveMessage : BoundUserInterfaceMessage
{
    public readonly string SlotId;

    public RMCHardpointRemoveMessage(string slotId)
    {
        SlotId = slotId;
    }
}
