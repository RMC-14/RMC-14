using Content.Server._RMC14.Announce.Core;
using Content.Shared._RMC14.Announce;
using Content.Shared.Database;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Announce;

public sealed partial class AnnouncementOverlaySystem
{
    private void LogAnnouncement(
        string announcementId,
        string[] text,
        AnnouncementTarget target,
        EntityUid? source,
        int recipientCount)
    {
        var sourceStr = source?.ToString() ?? "System";
        var textPreview = text.Length > 0 ? text[0] : string.Empty;
        if (textPreview.Length > 50)
            textPreview = textPreview[..47] + "...";

        _adminLogs.Add(LogType.AdminMessage, LogImpact.Medium,
            $"Announcement [{announcementId}] from {sourceStr} to {target} ({recipientCount} recipients): {textPreview}");
    }

    private string? ResolveSpeakerName(AnnouncementRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Route.SpeakerNameOverride))
            return request.Route.SpeakerNameOverride;

        if (request.Route.Speaker.HasValue && Exists(request.Route.Speaker.Value))
        {
            if (TryComp(request.Route.Speaker.Value, out MetaDataComponent? meta))
                return meta.EntityName;
        }

        return null;
    }

    private bool TryGetLongestPresentation(
        AnnouncementPresetPrototype preset,
        string[] lines,
        out AnnouncementPresentation longestPresentation)
    {
        longestPresentation = default!;
        var longestDuration = float.MinValue;

        foreach (var presentation in preset.Presentations.EnumerateAvailable())
        {
            var duration = AnnouncementDurationCalculator.Calculate(presentation.Style, lines) + presentation.Style.AnimationConfig.HoldDuration;
            if (duration >= longestDuration)
            {
                longestDuration = duration;
                longestPresentation = presentation;
            }
        }

        return longestDuration > float.MinValue;
    }
}
