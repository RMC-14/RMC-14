using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenonids.Rest;

[Serializable, NetSerializable]
public enum XenoRestState : byte
{
    NotResting,
    Resting
}
