using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server._RMC14.Marines;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.ERT;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Rules;
using Content.Shared.Buckle;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.ERT;

public sealed partial class RMCERTSystem
{

    private void FailRequest(RMCERTRequest request, string error, bool announceNoResponse = false)
    {
        var previousState = request.State;
        Log.Warning($"ERT request {request.Id} failed: {error}. " +
                    $"State={request.State}, SelectedCall={request.SelectedCall?.Id}, " +
                    $"Shuttle={FormatEntity(request.Shuttle)}, {GetShuttleDiagnostics(request, request.Shuttle)}");
        request.State = RMCERTRequestState.Failed;
        request.LastError = error;
        request.LastWarning = string.Empty;
        request.RecruitmentEndsAt = null;
        CleanupRequestContent(request, Loc.GetString("rmc-ert-cleanup-reason-failed"));
        UpdateSourceVisual(request, false);
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-failed",
            ("id", request.Id),
            ("error", error)));
        AddERTRequestLog(LogImpact.High,
            "failed",
            request,
            "system",
            extra: $"previousState={previousState}, error=\"{FormatLogValue(error)}\"");

        if (request.SelectedCall is { } callId &&
            _prototypes.TryIndex(callId, out var call) &&
            announceNoResponse)
        {
            AnnounceNoResponse(request, call);
        }

        DirtyState(request);
    }

    private void AnnounceDistressLaunched(RMCERTRequest request)
    {
        var title = Loc.GetString("rmc-ert-announcement-title-priority-alert");
        var message = Loc.GetString("rmc-ert-announcement-priority-alert",
            ("team", Loc.GetString("rmc-ert-response-team-fallback")),
            ("requester", request.RequesterName),
            ("reason", request.Reason));
        AnnounceERTToMarines(title, message, DistressBeaconSound);
    }

    private void AnnounceNoResponse(RMCERTRequest request, RMCERTCallPrototype? call)
    {
        var announcement = new RMCERTStageAnnouncement
        {
            Title = "rmc-ert-announcement-title-distress-beacon",
            Message = "rmc-ert-announcement-distress-no-response",
        };

        if (call != null)
        {
            Announce(announcement, request, call);
            return;
        }

        var title = Loc.GetString(announcement.Title!.Value);
        var message = Loc.GetString(announcement.Message!.Value,
            ("team", Loc.GetString("rmc-ert-response-team-fallback")),
            ("requester", request.RequesterName),
            ("reason", request.Reason));
        AnnounceERTToMarines(title, message);
    }

    private static RMCERTStageAnnouncement CreateDefaultDispatchAnnouncement()
    {
        return new RMCERTStageAnnouncement
        {
            Title = "rmc-ert-announcement-title-distress-beacon",
            Message = "rmc-ert-announcement-distress-dispatch",
            Sound = DistressReceivedSound,
        };
    }

    private void AnnounceDispatch(RMCERTRequest request, RMCERTCallPrototype call)
    {
        Announce(call.Announcements.Dispatch ?? CreateDefaultDispatchAnnouncement(), request, call);
    }

    private void Announce(RMCERTStageAnnouncement? announcement, RMCERTRequest request, RMCERTCallPrototype call)
    {
        if (announcement == null)
            return;

        var message = announcement.Message;
        if (message == null && announcement.Messages.Count > 0)
            message = _random.Pick(announcement.Messages);

        if (message == null)
            return;

        var text = FormatCallText(message.Value, request, call);
        var title = announcement.Title != null
            ? FormatCallText(announcement.Title.Value, request, call)
            : null;

        AnnounceERTToMarines(title, text, announcement.Sound, announcement.RawTitle);
    }

    private void AnnounceERTToMarines(string? title, string message, SoundSpecifier? sound = null, bool rawTitle = true)
    {
        title ??= Loc.GetString("rmc-announcement-author-highcommand");
        var loc = rawTitle
            ? "rmc-ert-announcement-message"
            : "rmc-announcement-message";
        var wrapped = Loc.GetString(loc, ("author", title), ("title", title), ("message", message));
        _marineAnnounce.AnnounceToMarines(wrapped, sound);
    }

    private void AddERTRequestLog(
        LogImpact impact,
        string action,
        RMCERTRequest request,
        string actor,
        RMCERTCallPrototype? call = null,
        string? extra = null)
    {
        var selected = call?.Name ?? GetSelectedCallLabel(request) ?? "none";
        var extraText = string.IsNullOrWhiteSpace(extra)
            ? string.Empty
            : $", {extra}";

        _adminLog.Add(LogType.RMCAdminCommandLogging,
            impact,
            $"ERT {action}: request={request.Id}, actor={FormatLogValue(actor)}, state={request.State}, " +
            $"source={request.Source}, requester={FormatLogValue(GetRequesterLogText(request))}, " +
            $"sourceName={FormatLogValue(request.SourceName)}, sourceEntity={FormatEntity(request.SourceEntity)}, " +
            $"call={FormatLogValue(selected)}, reason=\"{FormatLogValue(request.Reason)}\", " +
            $"shuttle={FormatEntity(request.Shuttle)}{extraText}");
    }

    private string GetRequesterLogText(RMCERTRequest request)
    {
        if (request.Requester is { Valid: true } requester && Exists(requester))
            return ToPrettyString(requester);

        return request.RequesterName;
    }

    private static string FormatLogValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "none";

        return value.Replace('\r', ' ').Replace('\n', ' ');
    }

    private string GetAdminActorText(EntityUid? admin, string? fallbackName = null)
    {
        if (admin is { Valid: true } && Exists(admin.Value))
            return ToPrettyString(admin.Value);

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName;

        return Loc.GetString("rmc-ert-admin-actor-server");
    }

    private string GetForceCallAnnouncement(RMCERTRequest request, RMCERTCallPrototype call, string adminText)
    {
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            return Loc.GetString("rmc-ert-admin-force-called-reason",
                ("admin", adminText),
                ("id", request.Id),
                ("call", call.Name),
                ("delay", (int)call.LaunchDelay),
                ("reason", request.Reason));
        }

        return Loc.GetString("rmc-ert-admin-force-called",
            ("admin", adminText),
            ("id", request.Id),
            ("call", call.Name),
            ("delay", (int)call.LaunchDelay));
    }

    private string BuildAdminRequestAnnouncement(RMCERTRequest request)
    {
        var sourceLabel = GetRequestSourceLabel(request);
        var baseText = Loc.GetString("rmc-ert-admin-request",
            ("id", request.Id),
            ("requester", request.RequesterName),
            ("source", sourceLabel),
            ("reason", request.Reason));
        if (request.AllowedCalls.Count != 1 ||
            !_prototypes.TryIndex(request.AllowedCalls[0], out var call) ||
            call.Announcements.RequestAdmin == null)
        {
            return baseText;
        }

        var extra = FormatCallText(call.Announcements.RequestAdmin.Value, request, call);
        return Loc.GetString("rmc-ert-admin-request-with-extra", ("base", baseText), ("extra", extra));
    }

    private string GetRequestSuccessText(EntityUid sourceEntity, RMCERTRequestSource source)
    {
        if (source == RMCERTRequestSource.Handheld &&
            TryComp(sourceEntity, out RMCERTDistressBeaconComponent? beacon))
        {
            return Loc.GetString("rmc-ert-success-handheld", ("recipient", beacon.Recipient));
        }

        return Loc.GetString("rmc-ert-success-console");
    }

    private string GetRequestSourceName(RMCERTRequestSource source, EntityUid? sourceEntity, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName.Trim();

        if (sourceEntity is { Valid: true } entity && Exists(entity))
            return Name(entity);

        return RMCERTLoc.GetSource(source);
    }

    private string GetRequestRequesterName(RMCERTRequestSource source, EntityUid? requester, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName.Trim();

        if (requester is { Valid: true } entity && Exists(entity))
            return Name(entity);

        return RMCERTLoc.GetSource(source);
    }

    private string GetRequestSourceLabel(RMCERTRequest request)
    {
        if (request.Source == RMCERTRequestSource.Handheld &&
            request.SourceEntity is { Valid: true } source &&
            TryComp(source, out RMCERTDistressBeaconComponent? beacon))
        {
            return beacon.RequestTitle;
        }

        return RMCERTLoc.GetSource(request.Source);
    }

    private string BuildMemberBriefing(RMCERTRequest request, RMCERTCallPrototype call)
    {
        if (call.Objectives.Count == 0 &&
            call.Features.Count == 0 &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("rmc-ert-briefing-title", ("team", call.Name)));

        if (!string.IsNullOrWhiteSpace(request.Reason))
            builder.AppendLine(Loc.GetString("rmc-ert-briefing-reason", ("reason", request.Reason)));

        if (call.Objectives.Count > 0)
        {
            builder.AppendLine(Loc.GetString("rmc-ert-briefing-objectives"));
            foreach (var objective in call.Objectives)
            {
                builder.Append(Loc.GetString("rmc-ert-briefing-bullet"));
                builder.AppendLine(FormatCallText(objective, request, call));
            }
        }

        if (call.Features.Count > 0)
        {
            builder.AppendLine(Loc.GetString("rmc-ert-briefing-features"));
            foreach (var feature in call.Features)
            {
                builder.Append(Loc.GetString("rmc-ert-briefing-bullet"));
                builder.AppendLine(FormatCallText(feature, request, call));
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatCallText(LocId text, RMCERTRequest request, RMCERTCallPrototype call)
    {
        return Robust.Shared.Localization.Loc.GetString(text,
            ("team", call.Name),
            ("requester", request.RequesterName),
            ("reason", request.Reason));
    }

    private static string GetOrganizationLabel(RMCERTCallPrototype call)
    {
        if (!string.IsNullOrWhiteSpace(call.Organization))
            return call.Organization;

        if (call.NpcFactions.Count > 0)
            return call.NpcFactions[0].Id;

        if (call.IffFaction is { } iffFaction)
            return iffFaction.Id;

        return call.Name;
    }

    private string? GetSelectedCallLabel(RMCERTRequest request)
    {
        if (request.SelectedCall is not { } callId)
            return null;

        if (_prototypes.TryIndex(callId, out var call))
            return call.Name;

        return callId.Id;
    }
}
