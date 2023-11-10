using Robust.Shared.Serialization;

namespace Content.Shared.CM14.Xenos.Rest;

[Serializable, NetSerializable]
public enum XenoRestState : byte
{
    NotResting,
    Resting
}
