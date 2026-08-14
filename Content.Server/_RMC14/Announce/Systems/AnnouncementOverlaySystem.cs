using System.Collections.Generic;
using Content.Server._RMC14.Announce.Core;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Announce;
using Content.Shared.Database;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Announce;

public sealed partial class AnnouncementOverlaySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private static readonly TimeSpan PvsFallbackTimeout = TimeSpan.FromMinutes(30);
    private uint _nextOverrideId = 1;
    private readonly Dictionary<(EntityUid Speaker, ICommonSession Session), int> _overrideRefs = new();
    private readonly Dictionary<uint, OverrideTracker> _pendingOverrides = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AnnouncementPlaybackDoneMsg>(OnPlaybackDone);
    }

    private void OnPlaybackDone(AnnouncementPlaybackDoneMsg msg, EntitySessionEventArgs args)
    {
        if (_net.IsClient)
            return;

        CompleteOverrideSession(msg.OverrideId, args.SenderSession);
    }

    internal void Dispatch(
        AnnouncementRequest request,
        AnnouncementPresetPrototype preset,
        Filter filter)
    {
        if (_net.IsClient || filter.Count == 0)
            return;

        var lines = AnnouncementLineHelper.NormalizeAndSplit(request.Message);
        var speakerName = ResolveSpeakerName(request);

        var overrideId = AnyPresentationShowsSprite(preset)
            ? EnsureSpeakerPvs(request, filter)
            : 0u;

        var clientData = BuildClientData(request, preset, lines, speakerName, overrideId);

        RaiseNetworkEvent(new AnnouncementNetMessage(clientData), filter);
        LogAnnouncement(preset.ID, lines, request.Route.Target, request.Route.Source, filter.Count);
    }

    private AnnouncementNetData BuildClientData(
        AnnouncementRequest request,
        AnnouncementPresetPrototype preset,
        string[] lines,
        string? speakerName,
        uint overrideId)
    {
        return new AnnouncementNetData
        {
            Text = lines,
            AnnouncementId = preset.ID,
            Priority = request.PriorityOverride ?? preset.Priority,
            CanInterrupt = request.CanInterrupt ?? preset.CanInterrupt,
            CanBeInterrupted = request.CanBeInterrupted ?? preset.CanBeInterrupted,
            SpeakerEntity = GetNetEntity(request.Route.Speaker),
            SpeakerName = speakerName,
            OverrideId = overrideId
        };
    }

    private uint EnsureSpeakerPvs(AnnouncementRequest request, Filter filter)
    {
        if (!request.Route.Speaker.HasValue)
            return 0;

        var speaker = request.Route.Speaker.Value;
        if (!Exists(speaker))
            return 0;

        var tracker = new OverrideTracker(speaker);
        foreach (var session in filter.Recipients)
        {
            if (tracker.Sessions.Add(session))
                AddOverrideRef(speaker, session);
        }

        if (tracker.Sessions.Count == 0)
            return 0;

        var overrideId = _nextOverrideId++;
        _pendingOverrides[overrideId] = tracker;

        Timer.Spawn(PvsFallbackTimeout, () => CompleteOverride(overrideId), tracker.FallbackCancellation.Token);
        return overrideId;
    }

    private void AddOverrideRef(EntityUid speaker, ICommonSession session)
    {
        var key = (speaker, session);
        if (_overrideRefs.TryGetValue(key, out var count))
        {
            _overrideRefs[key] = count + 1;
            return;
        }

        _overrideRefs[key] = 1;
        _pvsOverride.AddSessionOverride(speaker, session);
    }

    private void ReleaseOverrideRef(EntityUid speaker, ICommonSession session)
    {
        var key = (speaker, session);
        if (!_overrideRefs.TryGetValue(key, out var count))
            return;

        if (count > 1)
        {
            _overrideRefs[key] = count - 1;
            return;
        }

        _overrideRefs.Remove(key);
        if (Exists(speaker))
            _pvsOverride.RemoveSessionOverride(speaker, session);
    }

    private void CompleteOverrideSession(uint overrideId, ICommonSession session)
    {
        if (overrideId == 0 || !_pendingOverrides.TryGetValue(overrideId, out var tracker))
            return;

        if (!tracker.Sessions.Remove(session))
            return;

        ReleaseOverrideRef(tracker.Speaker, session);

        if (tracker.Sessions.Count == 0)
        {
            _pendingOverrides.Remove(overrideId);
            tracker.FallbackCancellation.Cancel();
            tracker.FallbackCancellation.Dispose();
        }
    }

    private void CompleteOverride(uint overrideId)
    {
        if (!_pendingOverrides.Remove(overrideId, out var tracker))
            return;

        tracker.FallbackCancellation.Dispose();
        foreach (var session in tracker.Sessions)
        {
            ReleaseOverrideRef(tracker.Speaker, session);
        }
    }

    private sealed class OverrideTracker
    {
        public readonly EntityUid Speaker;
        public readonly HashSet<ICommonSession> Sessions = new();
        public readonly System.Threading.CancellationTokenSource FallbackCancellation = new();

        public OverrideTracker(EntityUid speaker)
        {
            Speaker = speaker;
        }
    }
}

