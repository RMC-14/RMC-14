using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Destroy;

[ByRefEvent]
public record struct XenoDestroyLeapStartEvent(Vector2 LeapOffset);
