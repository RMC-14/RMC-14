using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[Serializable, NetSerializable]
public enum RMCPillBottleVisuals
{
    Color,
}

[Serializable, NetSerializable]
public enum RMCPillBottleColors
{
    Orange = 0,
    Blue,
    Yellow,
    LightPurple,
    LightGrey,
    White,
    LightGreen,
    Cyan,
    Pink,
    Aquamarine,
    Grey,
    Red,
    Black,
}
