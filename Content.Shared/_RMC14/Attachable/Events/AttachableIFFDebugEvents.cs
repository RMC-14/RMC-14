using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Attachable.Events;

[ByRefEvent]
public readonly record struct AttachableIFFDebugSampleEvent(
    EntityUid User,
    MapCoordinates From,
    MapCoordinates To,
    bool HasHit,
    MapCoordinates Hit,
    bool BlockedByFriendly,
    bool IsServerSample);

[Serializable, NetSerializable]
public sealed class AttachableIFFDebugToggledEvent(bool enabled) : EntityEventArgs
{
    public readonly bool Enabled = enabled;
}

[Serializable, NetSerializable]
public sealed class AttachableIFFServerDebugSampleEvent(
    MapCoordinates from,
    MapCoordinates to,
    bool hasHit,
    MapCoordinates hit,
    bool blockedByFriendly) : EntityEventArgs
{
    public readonly MapCoordinates From = from;
    public readonly MapCoordinates To = to;
    public readonly bool HasHit = hasHit;
    public readonly MapCoordinates Hit = hit;
    public readonly bool BlockedByFriendly = blockedByFriendly;
}
