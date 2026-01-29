using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum RMCVehiclePortGunUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCVehiclePortGunUiState : BoundUserInterfaceState
{
    public readonly int AmmoCount;
    public readonly int AmmoCapacity;
    public readonly bool HasMagazine;

    public RMCVehiclePortGunUiState(int ammoCount, int ammoCapacity, bool hasMagazine)
    {
        AmmoCount = ammoCount;
        AmmoCapacity = ammoCapacity;
        HasMagazine = hasMagazine;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehiclePortGunEjectMessage : BoundUserInterfaceMessage
{
}
