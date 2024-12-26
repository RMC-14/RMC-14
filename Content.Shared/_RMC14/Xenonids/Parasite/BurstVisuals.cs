using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[Serializable, NetSerializable]
public enum BurstLayer
{
    Base
}

[Serializable, NetSerializable]
public enum BurstVisualState
{
    Bursting,
    Burst
}
