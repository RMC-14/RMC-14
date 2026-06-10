using System;
using System.Linq;
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

    private static string[] BuildLines(string message)
    {
        if (string.IsNullOrEmpty(message))
            return Array.Empty<string>();

        var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!normalized.Contains('\n') && normalized.Contains("\\n"))
            normalized = normalized.Replace("\\n", "\n");

        return normalized.Split('\n');
    }

    private string? ResolveSpeakerName(AnnouncementRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Route.SpeakerNameOverride))
            return request.Route.SpeakerNameOverride;

        if (request.Route.Speaker.HasValue && EntityManager.EntityExists(request.Route.Speaker.Value))
        {
            if (EntityManager.TryGetComponent(request.Route.Speaker.Value, out MetaDataComponent? meta))
                return meta.EntityName;
        }

        return null;
    }

    private bool TryGetLongestPresentation(
        AnnouncementPresetPrototype preset,
        out AnnouncementPresentation longestPresentation)
    {
        longestPresentation = default!;
        var longestDuration = float.MinValue;

        foreach (var presentation in preset.Presentations.EnumerateAvailable())
        {
            var duration = AnnouncementDurationCalculator.Calculate(presentation.Style) + presentation.Style.AnimationConfig.HoldDuration;
            if (duration >= longestDuration)
            {
                longestDuration = duration;
                longestPresentation = presentation;
            }
        }

        return longestDuration > float.MinValue;
    }
}
