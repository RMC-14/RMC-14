using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum VehicleAmmoLoaderUiKey : byte
{
    Key,
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VehicleAmmoLoaderUiEntry
{
    [DataField]
    public VehicleSlotPath SlotPath;

    [DataField]
    public string HardpointType;

    [DataField]
    public string? InstalledName;

    [DataField]
    public NetEntity? InstalledEntity;

    [DataField]
    public EntProtoId? AmmoPrototype;

    [DataField]
    public int MagazineSize;

    [DataField]
    public List<VehicleAmmoLoaderUiAmmoSlot> AmmoSlots;

    [DataField]
    public bool CanLoad;

    [DataField]
    public bool CanUnload;

    public VehicleAmmoLoaderUiEntry()
    {
        SlotPath = default;
        HardpointType = string.Empty;
        AmmoSlots = new List<VehicleAmmoLoaderUiAmmoSlot>();
    }

    public VehicleAmmoLoaderUiEntry(
        VehicleSlotPath slotPath,
        string hardpointType,
        string? installedName,
        NetEntity? installedEntity,
        EntProtoId? ammoPrototype,
        int magazineSize,
        List<VehicleAmmoLoaderUiAmmoSlot> ammoSlots,
        bool canLoad,
        bool canUnload)
    {
        SlotPath = slotPath;
        HardpointType = hardpointType;
        InstalledName = installedName;
        InstalledEntity = installedEntity;
        AmmoPrototype = ammoPrototype;
        MagazineSize = magazineSize;
        AmmoSlots = ammoSlots;
        CanLoad = canLoad;
        CanUnload = canUnload;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VehicleAmmoLoaderUiAmmoSlot
{
    [DataField]
    public int SlotIndex;

    [DataField]
    public string Label;

    [DataField]
    public int Rounds;

    [DataField]
    public int Capacity;

    [DataField]
    public bool CanLoad;

    [DataField]
    public bool CanUnload;

    [DataField]
    public bool IsReadySlot;

    public VehicleAmmoLoaderUiAmmoSlot()
    {
        Label = string.Empty;
    }

    public VehicleAmmoLoaderUiAmmoSlot(
        int slotIndex,
        string label,
        int rounds,
        int capacity,
        bool canLoad,
        bool canUnload,
        bool isReadySlot)
    {
        SlotIndex = slotIndex;
        Label = label;
        Rounds = rounds;
        Capacity = capacity;
        CanLoad = canLoad;
        CanUnload = canUnload;
        IsReadySlot = isReadySlot;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VehicleAmmoLoaderUiState
{
    [DataField]
    public List<VehicleAmmoLoaderUiEntry> Hardpoints;

    [DataField]
    public int AmmoAmount;

    [DataField]
    public int AmmoMax;

    [DataField]
    public EntProtoId? AmmoPrototype;

    public VehicleAmmoLoaderUiState()
    {
        Hardpoints = new List<VehicleAmmoLoaderUiEntry>();
    }

    public VehicleAmmoLoaderUiState(
        List<VehicleAmmoLoaderUiEntry> hardpoints,
        int ammoAmount,
        int ammoMax,
        EntProtoId? ammoPrototype)
    {
        Hardpoints = hardpoints;
        AmmoAmount = ammoAmount;
        AmmoMax = ammoMax;
        AmmoPrototype = ammoPrototype;
    }
}

[Serializable, NetSerializable]
public enum VehicleAmmoLoaderSlotAction : byte
{
    Load,
    Unload,
}

[Serializable, NetSerializable]
public sealed class VehicleAmmoLoaderSelectMessage : BoundUserInterfaceMessage
{
    public readonly VehicleSlotPath SlotPath;
    public readonly int AmmoSlot;
    public readonly VehicleAmmoLoaderSlotAction Action;

    public VehicleAmmoLoaderSelectMessage(
        VehicleSlotPath slotPath,
        int ammoSlot,
        VehicleAmmoLoaderSlotAction action)
    {
        SlotPath = slotPath;
        AmmoSlot = ammoSlot;
        Action = action;
    }
}
