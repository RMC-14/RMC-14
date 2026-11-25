using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapStroke(Color Color, float Thickness, bool Smooth, List<Vector2> Points);
