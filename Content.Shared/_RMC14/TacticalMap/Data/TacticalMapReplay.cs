using System;
using System.Collections.Generic;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public sealed class TacticalMapReplayRequestEvent : EntityEventArgs
{
    public readonly string? MapId;

    public TacticalMapReplayRequestEvent(string? mapId)
    {
        MapId = mapId;
    }
}

[Serializable, NetSerializable]
public sealed class TacticalMapReplayDataEvent : EntityEventArgs
{
    public readonly List<TacticalMapReplayMap> Maps;

    public TacticalMapReplayDataEvent(List<TacticalMapReplayMap> maps)
    {
        Maps = maps;
    }
}

[Serializable, NetSerializable]
public readonly record struct TacticalMapReplayMap(
    NetEntity Map,
    string MapId,
    string DisplayName,
    List<string> LayerIds,
    List<TacticalMapReplayFrame> Frames);

[Serializable, NetSerializable]
public readonly record struct TacticalMapReplayFrame(
    float Time,
    bool IsKeyframe,
    List<TacticalMapReplayLayerDelta> Layers);

[Serializable, NetSerializable]
public readonly record struct TacticalMapReplayLayerDelta(
    string LayerId,
    bool LinesChanged,
    bool LinesIsFull,
    List<TacticalMapLine> Lines,
    bool LabelsChanged,
    bool LabelsIsFull,
    Dictionary<Vector2i, TacticalMapLabelData> Labels,
    List<Vector2i> RemovedLabels,
    bool BlipsChanged,
    bool BlipsIsFull,
    Dictionary<int, TacticalMapBlip> Blips,
    List<int> RemovedBlips);
