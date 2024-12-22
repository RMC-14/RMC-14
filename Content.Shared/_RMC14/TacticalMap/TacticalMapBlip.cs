using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapBlip(Vector2i Indices, SpriteSpecifier.Rsi Image, Color Color, TacticalMapBlipStatus Status, SpriteSpecifier.Rsi? Background, bool HiveLeader);
