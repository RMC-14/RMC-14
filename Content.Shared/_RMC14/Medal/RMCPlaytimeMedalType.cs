using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medal;

[Serializable, NetSerializable]
public enum RMCPlaytimeMedalType : byte
{
    Bronze,
    Silver,
    Gold,
    Platinum,
    Ruby,
    Emerald,
    Amethyst,
    Prismatic
}
