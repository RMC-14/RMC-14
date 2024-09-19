using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TacticalMapComputerUpdateCanvasMsg(Queue<TacticalMapLine> colors) : BoundUserInterfaceMessage
{
    public readonly Queue<TacticalMapLine> Colors = colors;
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapLine(Vector2i Start, Vector2i End, Color Color);
