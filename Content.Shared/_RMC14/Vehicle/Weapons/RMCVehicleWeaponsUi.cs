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
    public readonly float Integrity;
    public readonly float MaxIntegrity;
    public readonly bool HasIntegrity;
    public readonly float CooldownRemaining;
    public readonly float CooldownTotal;
    public readonly bool IsOnCooldown;

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
public sealed class RMCVehicleWeaponsUiState : BoundUserInterfaceState
{
    public readonly NetEntity Vehicle;
    public readonly List<RMCVehicleWeaponsUiEntry> Hardpoints;
    public readonly bool CanToggleStabilization;
    public readonly bool StabilizationEnabled;
    public readonly bool CanToggleAuto;
    public readonly bool AutoEnabled;

    public RMCVehicleWeaponsUiState(
        NetEntity vehicle,
        List<RMCVehicleWeaponsUiEntry> hardpoints,
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
public sealed class RMCVehicleWeaponsSelectMessage : BoundUserInterfaceMessage
{
    public readonly string SlotId;

    public RMCVehicleWeaponsSelectMessage(string slotId)
    {
        SlotId = slotId;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleWeaponsStabilizationMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public RMCVehicleWeaponsStabilizationMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleWeaponsAutoModeMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public RMCVehicleWeaponsAutoModeMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleWeaponsCooldownFeedbackMessage : BoundUserInterfaceMessage
{
    public readonly float RemainingSeconds;

    public RMCVehicleWeaponsCooldownFeedbackMessage(float remainingSeconds)
    {
        RemainingSeconds = remainingSeconds;
    }
}
