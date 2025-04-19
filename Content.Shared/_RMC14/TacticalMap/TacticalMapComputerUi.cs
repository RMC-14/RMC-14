using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TacticalMapUpdateCanvasMsg(List<TacticalMapLine> lines) : BoundUserInterfaceMessage
{
    public readonly List<TacticalMapLine> Lines = lines;
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapLine(Vector2i Start, Vector2i End, Color Color);
