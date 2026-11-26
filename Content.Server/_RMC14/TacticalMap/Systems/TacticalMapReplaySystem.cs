using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.TacticalMap;

public sealed class TacticalMapReplaySystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const int FramesPerChunk = 40;

    // frames store indices into the channel and icon tables to stay small
    private sealed class Recording
    {
        public string Name = string.Empty;
        public readonly List<TacticalMapReplayChannel> Channels = new();
        public readonly Dictionary<EntityUid, int> ChannelIndices = new();
        public readonly List<SpriteSpecifier.Rsi> Icons = new();
        public readonly Dictionary<SpriteSpecifier.Rsi, int> IconIndices = new();
        public readonly List<TacticalMapReplayCanvas> Canvases = new();
        public readonly List<TacticalMapReplayFrame> Frames = new();
    }

    private readonly Dictionary<EntityUid, Recording> _recordings = new();
    private readonly Dictionary<ICommonSession, TimeSpan> _nextRequest = new();
    private bool _publicAfterRound;
    private TimeSpan _sampleEvery;
    private TimeSpan _requestCooldown;
    private TimeSpan _nextSample;
    private int _nextRequestId;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapReplayPublicAfterRound, v => _publicAfterRound = v, true);
        Subs.CVar(_config, RMCCVars.RMCTacticalMapReplaySampleSeconds, v => _sampleEvery = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCTacticalMapReplayRequestCooldownSeconds, v => _requestCooldown = TimeSpan.FromSeconds(v), true);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _recordings.Clear();
        _nextRequest.Clear();
        _nextSample = TimeSpan.Zero;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        if (time < _nextSample)
            return;

        _nextSample = time + _sampleEvery;
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var mapId, out var map))
        {
            var recording = GetRecording(mapId, map);
            var blips = new List<TacticalMapReplayBlip>();
            foreach (var channelId in map.Channels.Values)
            {
                if (!TryComp(channelId, out TacticalMapChannelComponent? channel) ||
                    channel.LiveFeed is not { } feedId ||
                    !TryComp(feedId, out TacticalMapLiveFeedComponent? feed))
                {
                    continue;
                }

                var channelIndex = GetChannelIndex(recording, channelId, channel);
                foreach (var (net, blip) in feed.Blips)
                {
                    // the real blip is already recorded on its own channel
                    if (SharedTacticalMapSystem.IsRevealed(feed, net))
                        continue;

                    blips.Add(new TacticalMapReplayBlip(
                        net.Id,
                        (short) blip.Indices.X,
                        (short) blip.Indices.Y,
                        (ushort) GetIconIndex(recording, blip.Icon),
                        blip.Background is { } background
                            ? (ushort) GetIconIndex(recording, background)
                            : TacticalMapReplayBlip.NoBackground,
                        PackColor(blip.Color),
                        (byte) blip.Flags,
                        (byte) channelIndex));
                }
            }

            recording.Frames.Add(new TacticalMapReplayFrame((float) time.TotalSeconds, blips));
        }
    }

    public void RecordCanvas(
        EntityUid map,
        EntityUid channelId,
        List<TacticalMapStroke> strokes,
        Dictionary<Vector2i, TacticalMapLabelData> labels)
    {
        if (!TryComp(map, out TacticalMapComponent? mapComp) ||
            !TryComp(channelId, out TacticalMapChannelComponent? channel))
        {
            return;
        }

        var recording = GetRecording(map, mapComp);
        recording.Canvases.Add(new TacticalMapReplayCanvas(
            (float) _timing.CurTime.TotalSeconds,
            GetChannelIndex(recording, channelId, channel),
            new List<TacticalMapStroke>(strokes),
            new Dictionary<Vector2i, TacticalMapLabelData>(labels)));
    }

    public bool TryRequestReplay(ICommonSession session, string? mapName, out string? reason)
    {
        if (!CanAccessReplay(session, out reason))
            return false;

        var time = _timing.CurTime;
        if (_nextRequest.TryGetValue(session, out var next) && time < next)
        {
            reason = $"You must wait {(int) Math.Ceiling((next - time).TotalSeconds)} more seconds before requesting another replay.";
            return false;
        }

        _nextRequest[session] = time + _requestCooldown;
        SendReplay(session, mapName);
        return true;
    }

    public bool CanAccessReplay(ICommonSession session, out string? reason)
    {
        if (_admin.HasAdminFlag(session, AdminFlags.Admin))
        {
            reason = null;
            return true;
        }

        if (!_publicAfterRound)
        {
            reason = "Tactical map replays are only available to admins.";
            return false;
        }

        if (_gameTicker.RunLevel != GameRunLevel.PostRound)
        {
            reason = "Tactical map replays are only available after the round has ended.";
            return false;
        }

        reason = null;
        return true;
    }

    private void SendReplay(ICommonSession session, string? mapName)
    {
        var requestId = ++_nextRequestId;
        var maps = new List<TacticalMapReplayMap>();
        var recordings = new List<Recording>();
        foreach (var (mapId, recording) in _recordings)
        {
            if (!string.IsNullOrWhiteSpace(mapName) &&
                !string.Equals(recording.Name, mapName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            maps.Add(new TacticalMapReplayMap(
                GetNetEntity(mapId),
                recording.Name,
                new List<TacticalMapReplayChannel>(recording.Channels),
                new List<SpriteSpecifier.Rsi>(recording.Icons),
                new List<TacticalMapReplayCanvas>(recording.Canvases),
                recording.Frames.Count));
            recordings.Add(recording);
        }

        RaiseNetworkEvent(new TacticalMapReplayHeaderEvent(requestId, maps), session);

        // frames go out in chunks so a long round isnt one huge message

        for (var mapIndex = 0; mapIndex < recordings.Count; mapIndex++)
        {
            var frames = recordings[mapIndex].Frames;
            for (var start = 0; start < frames.Count; start += FramesPerChunk)
            {
                var count = Math.Min(FramesPerChunk, frames.Count - start);
                RaiseNetworkEvent(new TacticalMapReplayChunkEvent(requestId, mapIndex, frames.GetRange(start, count)), session);
            }
        }
    }

    private Recording GetRecording(EntityUid map, TacticalMapComponent mapComp)
    {
        if (!_recordings.TryGetValue(map, out var recording))
        {
            recording = new Recording();
            _recordings[map] = recording;
        }

        recording.Name = string.IsNullOrWhiteSpace(mapComp.DisplayName) ? Name(map) : mapComp.DisplayName;
        return recording;
    }

    private int GetChannelIndex(Recording recording, EntityUid channelId, TacticalMapChannelComponent channel)
    {
        if (recording.ChannelIndices.TryGetValue(channelId, out var index))
            return index;

        index = recording.Channels.Count;
        recording.Channels.Add(new TacticalMapReplayChannel(Name(channelId), channel.Color, channel.LiveFeed != null));
        recording.ChannelIndices[channelId] = index;
        return index;
    }

    private static int GetIconIndex(Recording recording, SpriteSpecifier.Rsi icon)
    {
        if (recording.IconIndices.TryGetValue(icon, out var index))
            return index;

        index = recording.Icons.Count;
        recording.Icons.Add(icon);
        recording.IconIndices[icon] = index;
        return index;
    }

    private static uint PackColor(Color color)
    {
        return SharedTacticalMapSystem.PackColor(color);
    }
}
