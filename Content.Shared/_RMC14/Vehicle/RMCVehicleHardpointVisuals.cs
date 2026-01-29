using System;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public enum RMCVehicleHardpointVisuals : byte
{
    PrimaryState,
    SecondaryState,
    SupportState,
}

public static class RMCVehicleHardpointLayers
{
    public const string Primary = "primary";
    public const string Secondary = "secondary";
    public const string Support = "support";
}
