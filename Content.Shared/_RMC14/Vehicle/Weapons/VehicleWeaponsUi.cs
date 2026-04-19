using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum VehicleWeaponsUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class VehicleWeaponsUiEntry
{
    public readonly string SlotId;
    public readonly string HardpointType;
    public readonly NetEntity? MountedEntity;
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
    public readonly float Integrity;
    public readonly float MaxIntegrity;
    public readonly bool HasIntegrity;
    public readonly float CooldownRemaining;
    public readonly float CooldownTotal;
    public readonly bool IsOnCooldown;

    public VehicleWeaponsUiEntry(
        string slotId,
        string hardpointType,
        NetEntity? mountedEntity,
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
        bool operatorIsSelf,
        float integrity,
        float maxIntegrity,
        bool hasIntegrity,
        float cooldownRemaining,
        float cooldownTotal,
        bool isOnCooldown)
    {
        SlotId = slotId;
        HardpointType = hardpointType;
        MountedEntity = mountedEntity;
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
        Integrity = integrity;
        MaxIntegrity = maxIntegrity;
        HasIntegrity = hasIntegrity;
        CooldownRemaining = cooldownRemaining;
        CooldownTotal = cooldownTotal;
        IsOnCooldown = isOnCooldown;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleWeaponsUiState : BoundUserInterfaceState
{
    public readonly NetEntity Vehicle;
    public readonly List<VehicleWeaponsUiEntry> Hardpoints;
    public readonly bool CanToggleStabilization;
    public readonly bool StabilizationEnabled;
    public readonly bool CanToggleAuto;
    public readonly bool AutoEnabled;

    public VehicleWeaponsUiState(
        NetEntity vehicle,
        List<VehicleWeaponsUiEntry> hardpoints,
        bool canToggleStabilization,
        bool stabilizationEnabled,
        bool canToggleAuto,
        bool autoEnabled)
    {
        Vehicle = vehicle;
        Hardpoints = hardpoints;
        CanToggleStabilization = canToggleStabilization;
        StabilizationEnabled = stabilizationEnabled;
        CanToggleAuto = canToggleAuto;
        AutoEnabled = autoEnabled;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleWeaponsSelectMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? MountedEntity;

    public VehicleWeaponsSelectMessage(NetEntity? mountedEntity)
    {
        MountedEntity = mountedEntity;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleWeaponsStabilizationMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public VehicleWeaponsStabilizationMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleWeaponsAutoModeMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public VehicleWeaponsAutoModeMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleWeaponsCooldownFeedbackMessage : BoundUserInterfaceMessage
{
    public readonly float RemainingSeconds;

    public VehicleWeaponsCooldownFeedbackMessage(float remainingSeconds)
    {
        RemainingSeconds = remainingSeconds;
    }
}
