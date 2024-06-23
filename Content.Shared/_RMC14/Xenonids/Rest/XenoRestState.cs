using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Rest;

[Serializable, NetSerializable]
public enum XenoRestState : byte
{
    NotResting,
    Resting
}
