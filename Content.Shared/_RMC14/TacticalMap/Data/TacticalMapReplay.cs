using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public sealed class TacticalMapReplayHeaderEvent(int requestId, List<TacticalMapReplayMap> maps) : EntityEventArgs
{
    public readonly int RequestId = requestId;
    public readonly List<TacticalMapReplayMap> Maps = maps;
}

[Serializable, NetSerializable]
public sealed class TacticalMapReplayChunkEvent(int requestId, int mapIndex, List<TacticalMapReplayFrame> frames) : EntityEventArgs
{
    public readonly int RequestId = requestId;
    public readonly int MapIndex = mapIndex;
    public readonly List<TacticalMapReplayFrame> Frames = frames;
}

[Serializable, NetSerializable]
public sealed class TacticalMapReplayMap(
    NetEntity map,
    string name,
    List<TacticalMapReplayChannel> channels,
    List<SpriteSpecifier.Rsi> icons,
    List<TacticalMapReplayCanvas> canvases,
    int frameCount)
{
    public readonly NetEntity Map = map;
    public readonly string Name = name;
    public readonly List<TacticalMapReplayChannel> Channels = channels;
    public readonly List<SpriteSpecifier.Rsi> Icons = icons;
    public readonly List<TacticalMapReplayCanvas> Canvases = canvases;
    public readonly int FrameCount = frameCount;
}

[Serializable, NetSerializable]
public readonly record struct TacticalMapReplayChannel(string Name, Color Color, bool HasBlips);

[Serializable, NetSerializable]
public readonly record struct TacticalMapReplayCanvas(
    float Time,
    int Channel,
    List<TacticalMapStroke> Strokes,
    Dictionary<Vector2i, TacticalMapLabelData> Labels);

[Serializable, NetSerializable]
public readonly record struct TacticalMapReplayFrame(float Time, List<TacticalMapReplayBlip> Blips);

[Serializable, NetSerializable]
public readonly record struct TacticalMapReplayBlip(
    int Entity,
    short X,
    short Y,
    ushort Icon,
    ushort Background,
    uint Color,
    byte Flags,
    byte Channel)
{
    public const ushort NoBackground = ushort.MaxValue;
}
