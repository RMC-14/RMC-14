using System;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum RMCVehicleFrameDamageVisuals : byte
{
    IntegrityFraction,
}

public static class RMCVehicleFrameDamageLayers
{
    public const string DamagedFrame = "damaged_frame";
}
