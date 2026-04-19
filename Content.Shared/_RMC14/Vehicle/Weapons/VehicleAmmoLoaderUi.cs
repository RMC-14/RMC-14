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

[Serializable, NetSerializable]
public sealed class VehicleAmmoLoaderUiEntry
{
    public readonly VehicleSlotPath SlotPath;
    public readonly string HardpointType;
    public readonly string? InstalledName;
    public readonly NetEntity? InstalledEntity;
    public readonly EntProtoId? AmmoPrototype;
    public readonly int MagazineSize;
    public readonly List<VehicleAmmoLoaderUiAmmoSlot> AmmoSlots;
    public readonly bool CanLoad;
    public readonly bool CanUnload;

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

[Serializable, NetSerializable]
public sealed class VehicleAmmoLoaderUiAmmoSlot
{
    public readonly int SlotIndex;
    public readonly string Label;
    public readonly int Rounds;
    public readonly int Capacity;
    public readonly bool CanLoad;
    public readonly bool CanUnload;
    public readonly bool IsReadySlot;

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

[Serializable, NetSerializable]
public sealed class VehicleAmmoLoaderUiState : BoundUserInterfaceState
{
    public readonly List<VehicleAmmoLoaderUiEntry> Hardpoints;
    public readonly int AmmoAmount;
    public readonly int AmmoMax;
    public readonly EntProtoId? AmmoPrototype;

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
