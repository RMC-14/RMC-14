using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum RMCVehicleAmmoLoaderUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCVehicleAmmoLoaderUiEntry
{
    public readonly string SlotId;
    public readonly string HardpointType;
    public readonly string? InstalledName;
    public readonly NetEntity? InstalledEntity;
    public readonly int ChamberedRounds;
    public readonly int MagazineSize;
    public readonly int StoredMagazines;
    public readonly int MaxStoredMagazines;
    public readonly bool CanLoad;

    public RMCVehicleAmmoLoaderUiEntry(
        string slotId,
        string hardpointType,
        string? installedName,
        NetEntity? installedEntity,
        int chamberedRounds,
        int magazineSize,
        int storedMagazines,
        int maxStoredMagazines,
        bool canLoad)
    {
        SlotId = slotId;
        HardpointType = hardpointType;
        InstalledName = installedName;
        InstalledEntity = installedEntity;
        ChamberedRounds = chamberedRounds;
        MagazineSize = magazineSize;
        StoredMagazines = storedMagazines;
        MaxStoredMagazines = maxStoredMagazines;
        CanLoad = canLoad;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleAmmoLoaderUiState : BoundUserInterfaceState
{
    public readonly List<RMCVehicleAmmoLoaderUiEntry> Hardpoints;
    public readonly int AmmoAmount;
    public readonly int AmmoMax;

    public RMCVehicleAmmoLoaderUiState(List<RMCVehicleAmmoLoaderUiEntry> hardpoints, int ammoAmount, int ammoMax)
    {
        Hardpoints = hardpoints;
        AmmoAmount = ammoAmount;
        AmmoMax = ammoMax;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleAmmoLoaderSelectMessage : BoundUserInterfaceMessage
{
    public readonly string SlotId;

    public RMCVehicleAmmoLoaderSelectMessage(string slotId)
    {
        SlotId = slotId;
    }
}
