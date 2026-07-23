using System.Numerics;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TacticalMapUpdateCanvasMsg(List<TacticalMapLine> lines, Dictionary<Vector2i, TacticalMapLabelData> labels) : BoundUserInterfaceMessage
{
    public readonly List<TacticalMapLine> Lines = lines;
    public readonly Dictionary<Vector2i, TacticalMapLabelData> Labels = labels;
}

[Serializable, NetSerializable]
public sealed class TacticalMapSelectMapMsg(NetEntity map) : BoundUserInterfaceMessage
{
    public readonly NetEntity Map = map;
}

[Serializable, NetSerializable]
public sealed class TacticalMapSelectLayerMsg(string? layerId) : BoundUserInterfaceMessage
{
    public readonly string? LayerId = layerId;
}

[Serializable, NetSerializable]
public sealed class TacticalMapOverwatchBlipMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class TacticalMapQueenEyeMoveMsg(Vector2i position) : BoundUserInterfaceMessage
{
    public readonly Vector2i Position = position;
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapLine(Vector2 Start, Vector2 End, Color Color, float Thickness = 2.0f, bool Smooth = true);
