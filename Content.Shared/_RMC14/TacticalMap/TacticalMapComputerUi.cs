using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TacticalMapUpdateCanvasMsg(List<TacticalMapLine> lines, Dictionary<Vector2i, string> labels) : BoundUserInterfaceMessage
{
    public readonly List<TacticalMapLine> Lines = lines;
    public readonly Dictionary<Vector2i, string> Labels = labels;
}

[Serializable, NetSerializable]
public sealed class TacticalMapQueenEyeMoveMsg(Vector2i position) : BoundUserInterfaceMessage
{
    public readonly Vector2i Position = position;
}

[Serializable, NetSerializable]
public sealed class TacticalMapCreateLabelMsg(Vector2i position, string text) : BoundUserInterfaceMessage
{
    public readonly Vector2i Position = position;
    public readonly string Text = text;
}

[Serializable, NetSerializable]
public sealed class TacticalMapEditLabelMsg(Vector2i position, string newText) : BoundUserInterfaceMessage
{
    public readonly Vector2i Position = position;
    public readonly string NewText = newText;
}

[Serializable, NetSerializable]
public sealed class TacticalMapDeleteLabelMsg(Vector2i position) : BoundUserInterfaceMessage
{
    public readonly Vector2i Position = position;
}

[Serializable, NetSerializable]
public sealed class TacticalMapMoveLabelMsg(Vector2i oldPosition, Vector2i newPosition) : BoundUserInterfaceMessage
{
    public readonly Vector2i OldPosition = oldPosition;
    public readonly Vector2i NewPosition = newPosition;
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapLine(Vector2i Start, Vector2i End, Color Color, float Thickness = 2.0f);
