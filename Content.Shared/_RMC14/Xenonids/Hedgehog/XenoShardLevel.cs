using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[Serializable, NetSerializable]
public enum XenoShardLevel : byte
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4
}