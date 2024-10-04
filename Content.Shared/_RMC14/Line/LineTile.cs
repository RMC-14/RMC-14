using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Line;

[Serializable, NetSerializable]
public readonly record struct LineTile(MapCoordinates Coordinates, TimeSpan At);
