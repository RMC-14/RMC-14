using System;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum VehicleFrameDamageVisuals : byte
{
    IntegrityFraction,
}

public static class VehicleFrameDamageLayers
{
    public const string DamagedFrame = "damaged_frame";
}
