using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum RMCVehicleWeaponsUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCVehicleWeaponsUiEntry
{
    public readonly string SlotId;
    public readonly string HardpointType;
    public readonly string? InstalledName;
    public readonly NetEntity? InstalledEntity;
    public readonly bool HasItem;
    public readonly bool Selectable;
    public readonly bool Selected;
    public readonly int AmmoCount;
    public readonly int AmmoCapacity;
    public readonly bool HasAmmo;
    public readonly int MagazineSize;
    public readonly int StoredMagazines;
    public readonly int MaxStoredMagazines;
    public readonly bool HasMagazineData;
    public readonly string? OperatorName;
    public readonly bool OperatorIsSelf;

    public RMCVehicleWeaponsUiEntry(
        string slotId,
        string hardpointType,
        string? installedName,
        NetEntity? installedEntity,
        bool hasItem,
        bool selectable,
        bool selected,
        int ammoCount,
        int ammoCapacity,
        bool hasAmmo,
        int magazineSize,
        int storedMagazines,
        int maxStoredMagazines,
        bool hasMagazineData,
        string? operatorName,
        bool operatorIsSelf)
    {
        SlotId = slotId;
        HardpointType = hardpointType;
        InstalledName = installedName;
        InstalledEntity = installedEntity;
        HasItem = hasItem;
        Selectable = selectable;
        Selected = selected;
        AmmoCount = ammoCount;
        AmmoCapacity = ammoCapacity;
        HasAmmo = hasAmmo;
        MagazineSize = magazineSize;
        StoredMagazines = storedMagazines;
        MaxStoredMagazines = maxStoredMagazines;
        HasMagazineData = hasMagazineData;
        OperatorName = operatorName;
        OperatorIsSelf = operatorIsSelf;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleWeaponsUiState : BoundUserInterfaceState
{
    public readonly List<RMCVehicleWeaponsUiEntry> Hardpoints;

    public RMCVehicleWeaponsUiState(List<RMCVehicleWeaponsUiEntry> hardpoints)
    {
        Hardpoints = hardpoints;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleWeaponsSelectMessage : BoundUserInterfaceMessage
{
    public readonly string SlotId;

    public RMCVehicleWeaponsSelectMessage(string slotId)
    {
        SlotId = slotId;
    }
}
