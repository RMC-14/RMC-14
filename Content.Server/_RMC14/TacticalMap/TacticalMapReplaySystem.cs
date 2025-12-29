using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.TacticalMap;

public sealed class TacticalMapReplaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminManager _admin = default!;

    private const int MaxFramesPerMap = 2000;

    private sealed class ReplayRecording
    {
        public string MapId = string.Empty;
        public string DisplayName = string.Empty;
        public readonly List<TacticalMapReplayFrame> Frames = new();
    }

    private readonly Dictionary<EntityUid, ReplayRecording> _recordings = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TacticalMapReplayRequestEvent>(OnReplayRequest);
        SubscribeLocalEvent<TacticalMapComponent, EntityTerminatingEvent>(OnMapTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _recordings.Clear());
    }

    public void RecordSnapshot(EntityUid mapId, TacticalMapComponent map)
    {
        var recording = GetOrCreateRecording(mapId, map);
        var frame = BuildFrame(map);
        if (recording.Frames.Count > 0)
        {
            var lastFrame = recording.Frames[^1];
            if (AreFramesEquivalent(lastFrame, frame))
            {
                if (Math.Abs(lastFrame.Time - frame.Time) > 0.001f)
                    recording.Frames[^1] = new TacticalMapReplayFrame(frame.Time, lastFrame.Layers);
                return;
            }
        }

        recording.Frames.Add(frame);

        if (recording.Frames.Count > MaxFramesPerMap)
        {
            var removeCount = recording.Frames.Count - MaxFramesPerMap;
            recording.Frames.RemoveRange(0, removeCount);
        }
    }

    public void SendReplay(ICommonSession session, string? mapIdFilter)
    {
        var maps = BuildReplayMaps(mapIdFilter);
        RaiseNetworkEvent(new TacticalMapReplayDataEvent(maps), session);
    }

    private void OnReplayRequest(TacticalMapReplayRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, AdminFlags.Admin))
            return;

        SendReplay(args.SenderSession, msg.MapId);
    }

    private void OnMapTerminating(Entity<TacticalMapComponent> ent, ref EntityTerminatingEvent args)
    {
        _recordings.Remove(ent.Owner);
    }

    private ReplayRecording GetOrCreateRecording(EntityUid mapId, TacticalMapComponent map)
    {
        if (!_recordings.TryGetValue(mapId, out var recording))
        {
            recording = new ReplayRecording();
            _recordings[mapId] = recording;
        }

        recording.MapId = map.MapId;
        recording.DisplayName = map.DisplayName;
        return recording;
    }

    private TacticalMapReplayFrame BuildFrame(TacticalMapComponent map)
    {
        var layers = new List<TacticalMapReplayLayerFrame>(map.Layers.Count);
        foreach (var (layerId, layer) in map.Layers.OrderBy(pair => pair.Key.Id))
        {
            var blips = layer.Blips.Count > 0
                ? layer.Blips.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray()
                : Array.Empty<TacticalMapBlip>();
            var layerFrame = new TacticalMapReplayLayerFrame(
                layerId.Id,
                new List<TacticalMapLine>(layer.Lines),
                new Dictionary<Vector2i, TacticalMapLabelData>(layer.Labels),
                blips);
            layers.Add(layerFrame);
        }

        return new TacticalMapReplayFrame(
            (float) _timing.CurTime.TotalSeconds,
            layers);
    }

    private static bool AreFramesEquivalent(TacticalMapReplayFrame a, TacticalMapReplayFrame b)
    {
        if (a.Layers.Count != b.Layers.Count)
            return false;

        for (var i = 0; i < a.Layers.Count; i++)
        {
            if (!AreLayersEquivalent(a.Layers[i], b.Layers[i]))
                return false;
        }

        return true;
    }

    private static bool AreLayersEquivalent(TacticalMapReplayLayerFrame a, TacticalMapReplayLayerFrame b)
    {
        if (!string.Equals(a.LayerId, b.LayerId, StringComparison.Ordinal))
            return false;

        if (a.Lines.Count != b.Lines.Count)
            return false;

        if (!a.Lines.SequenceEqual(b.Lines))
            return false;

        if (a.Blips.Length != b.Blips.Length)
            return false;

        if (!a.Blips.SequenceEqual(b.Blips))
            return false;

        if (a.Labels.Count != b.Labels.Count)
            return false;

        foreach (var (pos, label) in a.Labels)
        {
            if (!b.Labels.TryGetValue(pos, out var other) || !label.Equals(other))
                return false;
        }

        return true;
    }

    private List<TacticalMapReplayMap> BuildReplayMaps(string? mapIdFilter)
    {
        var maps = new List<TacticalMapReplayMap>();
        var query = EntityQueryEnumerator<TacticalMapComponent>();
        while (query.MoveNext(out var mapId, out var map))
        {
            if (!string.IsNullOrWhiteSpace(mapIdFilter) &&
                !string.Equals(map.MapId, mapIdFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var recording = GetOrCreateRecording(mapId, map);
            var frames = recording.Frames.Count > 0
                ? new List<TacticalMapReplayFrame>(recording.Frames)
                : new List<TacticalMapReplayFrame> { BuildFrame(map) };

            var layerIds = map.Layers.Keys.Select(layerId => layerId.Id).OrderBy(id => id).ToList();
            maps.Add(new TacticalMapReplayMap(GetNetEntity(mapId), map.MapId, map.DisplayName, layerIds, frames));
        }

        return maps;
    }
}
