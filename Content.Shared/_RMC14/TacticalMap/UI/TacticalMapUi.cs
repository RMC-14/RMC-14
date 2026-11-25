using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapUserUi
{
    Key,
}

[Serializable, NetSerializable]
public enum TacticalMapComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TacticalMapSubmitCanvasMsg(
    NetEntity channel,
    List<TacticalMapStroke> strokes,
    Dictionary<Vector2i, TacticalMapLabelData> labels) : BoundUserInterfaceMessage
{
    public readonly NetEntity Channel = channel;
    public readonly List<TacticalMapStroke> Strokes = strokes;
    public readonly Dictionary<Vector2i, TacticalMapLabelData> Labels = labels;
}

[Serializable, NetSerializable]
public sealed class TacticalMapActivateBlipMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class TacticalMapActivatePointMsg(NetEntity map, Vector2i indices) : BoundUserInterfaceMessage
{
    public readonly NetEntity Map = map;
    public readonly Vector2i Indices = indices;
}
