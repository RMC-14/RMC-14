using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.MotionDetector;

[Serializable, NetSerializable]
public readonly record struct Blip(MapCoordinates Coordinates, bool QueenEye);
