using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Administration;
using Content.Shared._RMC14.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.TacticalMap;

public sealed class TacticalMapReplaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private const int MaxFramesPerMap = 2000;
    private static readonly TimeSpan KeyframeInterval = TimeSpan.FromSeconds(10);

    private sealed class ReplayRecording
    {
        public string MapId = string.Empty;
        public string DisplayName = string.Empty;
        public readonly List<TacticalMapReplayFrame> Frames = new();
        public TimeSpan LastKeyframeAt;
        public readonly Dictionary<string, LayerSnapshot> LastState = new();
    }

    private sealed class LayerSnapshot
    {
        public readonly List<TacticalMapLine> Lines = new();
        public readonly Dictionary<Vector2i, TacticalMapLabelData> Labels = new();
        public readonly Dictionary<int, TacticalMapBlip> Blips = new();
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
        var now = _timing.CurTime;
        var isInitial = recording.Frames.Count == 0;
        var allowKeyframe = isInitial || now - recording.LastKeyframeAt >= KeyframeInterval;

        if (!TryBuildFrame(map, recording, allowKeyframe, isInitial, out var frame))
        {
            if (recording.Frames.Count > 0)
            {
                var lastFrame = recording.Frames[^1];
                if (Math.Abs(lastFrame.Time - (float) now.TotalSeconds) > 0.001f)
                    recording.Frames[^1] = lastFrame with { Time = (float) now.TotalSeconds };
            }

            return;
        }

        if (frame.IsKeyframe)
            recording.LastKeyframeAt = now;

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
        if (!CanAccessReplay(args.SenderSession, out _))
            return;

        SendReplay(args.SenderSession, msg.MapId);
    }

    public bool CanAccessReplay(ICommonSession session, out string? reason)
    {
        if (_admin.HasAdminFlag(session, AdminFlags.Admin))
        {
            reason = null;
            return true;
        }

        if (!_cfg.GetCVar(RMCCVars.RMCTacticalMapReplayPublicAfterRound))
        {
            reason = "Tactical map replay access for non-admins is currently disabled.";
            return false;
        }

        if (_gameTicker.RunLevel != GameRunLevel.PostRound)
        {
            reason = "Tactical map replay is only available on round end.";
            return false;
        }

        reason = null;
        return true;
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

    private static void UpdateSnapshotLines(LayerSnapshot snapshot, bool isFull, List<TacticalMapLine> lines)
    {
        if (isFull)
        {
            snapshot.Lines.Clear();
            snapshot.Lines.AddRange(lines);
            return;
        }

        if (lines.Count > 0)
            snapshot.Lines.AddRange(lines);
    }

    private static bool IsAppendOnly(IReadOnlyList<TacticalMapLine> previous, IReadOnlyList<TacticalMapLine> current)
    {
        if (current.Count < previous.Count)
            return false;

        for (var i = 0; i < previous.Count; i++)
        {
            if (!previous[i].Equals(current[i]))
                return false;
        }

        return true;
    }

    private bool TryBuildFrame(
        TacticalMapComponent map,
        ReplayRecording recording,
        bool allowKeyframe,
        bool isInitial,
        out TacticalMapReplayFrame frame)
    {
        var layers = new List<TacticalMapReplayLayerDelta>(map.Layers.Count);
        var currentLayerIds = new HashSet<string>();
        var hasChanges = false;

        foreach (var (layerId, layer) in map.Layers.OrderBy(pair => pair.Key.Id))
        {
            var layerIdString = layerId.Id;
            currentLayerIds.Add(layerIdString);

            if (!recording.LastState.TryGetValue(layerIdString, out var snapshot))
            {
                snapshot = new LayerSnapshot();
                recording.LastState[layerIdString] = snapshot;
            }

            if (BuildLayerDelta(layerIdString, layer, snapshot, false, out var delta))
            {
                layers.Add(delta);
                hasChanges = true;
            }
        }

        var removedLayers = new List<string>();
        foreach (var layerId in recording.LastState.Keys)
        {
            if (!currentLayerIds.Contains(layerId))
                removedLayers.Add(layerId);
        }

        foreach (var layerId in removedLayers)
        {
            var delta = new TacticalMapReplayLayerDelta(
                layerId,
                true,
                true,
                new List<TacticalMapLine>(),
                true,
                true,
                new Dictionary<Vector2i, TacticalMapLabelData>(),
                new List<Vector2i>(),
                true,
                true,
                new Dictionary<int, TacticalMapBlip>(),
                new List<int>());
            layers.Add(delta);
            recording.LastState.Remove(layerId);
            hasChanges = true;
        }

        if (!hasChanges && !isInitial)
        {
            frame = default;
            return false;
        }

        var useKeyframe = isInitial || (allowKeyframe && hasChanges);
        if (useKeyframe)
        {
            layers = BuildKeyframeDeltas(map, recording);
        }

        frame = new TacticalMapReplayFrame(
            (float) _timing.CurTime.TotalSeconds,
            useKeyframe,
            layers);

        return true;
    }

    private static List<TacticalMapReplayLayerDelta> BuildKeyframeDeltas(
        TacticalMapComponent map,
        ReplayRecording recording)
    {
        var layers = new List<TacticalMapReplayLayerDelta>(map.Layers.Count);
        recording.LastState.Clear();

        foreach (var (layerId, layer) in map.Layers.OrderBy(pair => pair.Key.Id))
        {
            var layerIdString = layerId.Id;
            var snapshot = new LayerSnapshot();
            recording.LastState[layerIdString] = snapshot;

            BuildLayerDelta(layerIdString, layer, snapshot, true, out var delta);
            layers.Add(delta);
        }

        return layers;
    }

    private static bool BuildLayerDelta(
        string layerId,
        TacticalMapLayerData layer,
        LayerSnapshot snapshot,
        bool forceKeyframe,
        out TacticalMapReplayLayerDelta delta)
    {
        bool linesChanged = false;
        bool linesIsFull = false;
        List<TacticalMapLine> linesDelta = new();

        if (forceKeyframe)
        {
            linesChanged = true;
            linesIsFull = true;
            linesDelta = new List<TacticalMapLine>(layer.Lines);
            UpdateSnapshotLines(snapshot, true, linesDelta);
        }
        else if (!layer.Lines.SequenceEqual(snapshot.Lines))
        {
            linesChanged = true;
            if (IsAppendOnly(snapshot.Lines, layer.Lines))
            {
                linesIsFull = false;
                linesDelta = layer.Lines.Skip(snapshot.Lines.Count).ToList();
                UpdateSnapshotLines(snapshot, false, linesDelta);
            }
            else
            {
                linesIsFull = true;
                linesDelta = new List<TacticalMapLine>(layer.Lines);
                UpdateSnapshotLines(snapshot, true, linesDelta);
            }
        }

        bool labelsChanged = false;
        bool labelsIsFull = forceKeyframe;
        var labelUpdates = new Dictionary<Vector2i, TacticalMapLabelData>();
        var removedLabels = new List<Vector2i>();

        if (forceKeyframe)
        {
            labelsChanged = true;
            labelUpdates = new Dictionary<Vector2i, TacticalMapLabelData>(layer.Labels);
            snapshot.Labels.Clear();
            foreach (var (pos, label) in layer.Labels)
                snapshot.Labels[pos] = label;
        }
        else
        {
            foreach (var (pos, label) in layer.Labels)
            {
                if (!snapshot.Labels.TryGetValue(pos, out var existing) || !existing.Equals(label))
                    labelUpdates[pos] = label;
            }

            foreach (var pos in snapshot.Labels.Keys)
            {
                if (!layer.Labels.ContainsKey(pos))
                    removedLabels.Add(pos);
            }

            labelsChanged = labelUpdates.Count > 0 || removedLabels.Count > 0;
            if (labelsChanged)
            {
                foreach (var (pos, label) in labelUpdates)
                    snapshot.Labels[pos] = label;

                foreach (var pos in removedLabels)
                    snapshot.Labels.Remove(pos);
            }
        }

        bool blipsChanged = false;
        bool blipsIsFull = forceKeyframe;
        var blipUpdates = new Dictionary<int, TacticalMapBlip>();
        var removedBlips = new List<int>();

        if (forceKeyframe)
        {
            blipsChanged = true;
            blipUpdates = new Dictionary<int, TacticalMapBlip>(layer.Blips);
            snapshot.Blips.Clear();
            foreach (var (id, blip) in layer.Blips)
                snapshot.Blips[id] = blip;
        }
        else
        {
            foreach (var (id, blip) in layer.Blips)
            {
                if (!snapshot.Blips.TryGetValue(id, out var existing) || !existing.Equals(blip))
                    blipUpdates[id] = blip;
            }

            foreach (var id in snapshot.Blips.Keys)
            {
                if (!layer.Blips.ContainsKey(id))
                    removedBlips.Add(id);
            }

            blipsChanged = blipUpdates.Count > 0 || removedBlips.Count > 0;
            if (blipsChanged)
            {
                foreach (var (id, blip) in blipUpdates)
                    snapshot.Blips[id] = blip;

                foreach (var id in removedBlips)
                    snapshot.Blips.Remove(id);
            }
        }

        delta = new TacticalMapReplayLayerDelta(
            layerId,
            linesChanged,
            linesIsFull,
            linesDelta,
            labelsChanged,
            labelsIsFull,
            labelUpdates,
            removedLabels,
            blipsChanged,
            blipsIsFull,
            blipUpdates,
            removedBlips);

        return linesChanged || labelsChanged || blipsChanged || forceKeyframe;
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
                : new List<TacticalMapReplayFrame> { BuildKeyframeFrame(map) };

            var layerIds = map.Layers.Keys.Select(layerId => layerId.Id).OrderBy(id => id).ToList();
            maps.Add(new TacticalMapReplayMap(GetNetEntity(mapId), map.MapId, map.DisplayName, layerIds, frames));
        }

        return maps;
    }

    private TacticalMapReplayFrame BuildKeyframeFrame(TacticalMapComponent map)
    {
        var layers = new List<TacticalMapReplayLayerDelta>(map.Layers.Count);
        foreach (var (layerId, layer) in map.Layers.OrderBy(pair => pair.Key.Id))
        {
            layers.Add(new TacticalMapReplayLayerDelta(
                layerId.Id,
                true,
                true,
                new List<TacticalMapLine>(layer.Lines),
                true,
                true,
                new Dictionary<Vector2i, TacticalMapLabelData>(layer.Labels),
                new List<Vector2i>(),
                true,
                true,
                new Dictionary<int, TacticalMapBlip>(layer.Blips),
                new List<int>()));
        }

        return new TacticalMapReplayFrame(
            (float) _timing.CurTime.TotalSeconds,
            true,
            layers);
    }
}
