using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapLabelData(string Text, Color Color);
