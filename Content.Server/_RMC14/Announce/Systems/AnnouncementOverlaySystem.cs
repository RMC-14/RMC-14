using System.Linq;
using Content.Server._RMC14.Announce.Core;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Announce;
using Content.Shared.Database;
using Robust.Server.GameStates;
using Robust.Shared.Enums;
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

    private const float PvsFallbackTimeoutSeconds = 30f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AnnouncementPlaybackDoneMsg>(OnPlaybackDone);
    }

    private void OnPlaybackDone(AnnouncementPlaybackDoneMsg msg, EntitySessionEventArgs args)
    {
        if (_net.IsClient || !TryGetEntity(msg.Speaker, out var speaker))
            return;

        if (Exists(speaker))
            _pvsOverride.RemoveSessionOverride(speaker.Value, args.SenderSession);
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
        var clientData = BuildClientData(request, preset, lines, speakerName);

        if (AnyPresentationShowsSprite(preset))
            EnsureSpeakerPvs(request, filter);

        RaiseNetworkEvent(new AnnouncementNetMessage(clientData), filter);
        LogAnnouncement(preset.ID, lines, request.Route.Target, request.Route.Source, filter.Count);
    }

    private AnnouncementNetData BuildClientData(
        AnnouncementRequest request,
        AnnouncementPresetPrototype preset,
        string[] lines,
        string? speakerName)
    {
        return new AnnouncementNetData
        {
            Text = lines,
            AnnouncementId = preset.ID,
            Priority = request.PriorityOverride ?? preset.Priority,
            CanInterrupt = request.CanInterrupt ?? preset.CanInterrupt,
            CanBeInterrupted = request.CanBeInterrupted ?? preset.CanBeInterrupted,
            SpeakerEntity = GetNetEntity(request.Route.Speaker),
            SpeakerName = speakerName
        };
    }

    private void EnsureSpeakerPvs(AnnouncementRequest request, Filter filter)
    {
        if (!request.Route.Speaker.HasValue)
            return;

        var speaker = request.Route.Speaker.Value;
        if (!Exists(speaker))
            return;

        _pvsOverride.AddSessionOverrides(speaker, filter);

        var recipients = filter.Recipients.ToArray();
        Timer.Spawn(TimeSpan.FromSeconds(PvsFallbackTimeoutSeconds), () => RemoveSpeakerOverrides(speaker, recipients));
    }

    private void RemoveSpeakerOverrides(EntityUid speaker, ICommonSession[] sessions)
    {
        if (!Exists(speaker))
            return;

        foreach (var session in sessions)
        {
            if (session.Status == SessionStatus.Connected)
                _pvsOverride.RemoveSessionOverride(speaker, session);
        }
    }
}

