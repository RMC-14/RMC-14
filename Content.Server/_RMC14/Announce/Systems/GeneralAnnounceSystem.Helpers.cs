using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Announce;
using Content.Shared.Database;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Announce;

public sealed partial class GeneralAnnounceSystem
{
    private static readonly AnnouncementDisplayPreference[] DeliveryOrder =
    [
        AnnouncementDisplayPreference.Stylized,
        AnnouncementDisplayPreference.Default,
        AnnouncementDisplayPreference.Simplified
    ];

    private void LogAnnouncement(
        string configId,
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
            $"Announcement [{configId}] from {sourceStr} to {target} ({recipientCount} recipients): {textPreview}");
    }

    private bool ValidateRequest(AnnouncementRequest request)
    {
        var validation = _validator.ValidateRequest(request);
        if (validation.IsValid)
            return true;

        Log.Warning($"Invalid announcement request: {validation.GetErrorSummary()}");
        return false;
    }

    private bool TryResolvePreset(string? presetId, out AnnouncementPresetPrototype preset)
    {
        var resolved = _presetResolver.Resolve(presetId);
        if (resolved != null)
        {
            preset = resolved;
            return true;
        }

        preset = default!;
        Log.Warning($"No valid preset found for announcement request with preset '{presetId}'");
        return false;
    }

    private static string[] BuildLines(string message)
    {
        if (string.IsNullOrEmpty(message))
            return Array.Empty<string>();

        var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!normalized.Contains('\n') && normalized.Contains("\\n"))
            normalized = normalized.Replace("\\n", "\n");

        return normalized
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private string? ResolveSpeakerName(AnnouncementRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SpeakerNameOverride))
            return request.SpeakerNameOverride;

        if (request.Speaker.HasValue && EntityManager.EntityExists(request.Speaker.Value))
        {
            if (EntityManager.TryGetComponent(request.Speaker.Value, out MetaDataComponent? meta))
                return meta.EntityName;
        }

        return null;
    }

    private AnnouncementDeliveryPlan? BuildDeliveryPlan(
        AnnouncementRequest request,
        AnnouncementPresetPrototype preset,
        Filter filter)
    {
        var buckets = new Dictionary<AnnouncementDisplayPreference, List<ICommonSession>>();

        foreach (var session in filter.Recipients)
        {
            var preference = GetPreference(session, preset.ID);
            if (preference == AnnouncementDisplayPreference.Disabled)
                continue;

            if (!buckets.TryGetValue(preference, out var sessions))
            {
                sessions = new List<ICommonSession>();
                buckets[preference] = sessions;
            }

            sessions.Add(session);
        }

        if (buckets.Count == 0)
            return null;

        var plan = new AnnouncementDeliveryPlan
        {
            Lines = BuildLines(request.Message),
            SpeakerName = ResolveSpeakerName(request)
        };

        var longestDuration = 0f;

        foreach (var preference in DeliveryOrder)
        {
            if (!buckets.TryGetValue(preference, out var sessions) || sessions.Count == 0)
                continue;

            var preferencePreset = GetPresetForPreference(preset, preference);
            var style = AnnouncementStyleMerger.Merge(preferencePreset.Style, request.StyleOverride);
            var preferenceFilter = Filter.Empty().AddPlayers(sessions);

            plan.Groups[preference] = new AnnouncementDeliveryGroup
            {
                Preset = preferencePreset,
                Style = style,
                Filter = preferenceFilter
            };

            plan.DeliveredFilter.AddPlayers(sessions);
            plan.DeliveredCount += sessions.Count;

            var duration = AnnouncementDurationCalculator.Calculate(style) + style.AnimationConfig.HoldDuration;
            if (plan.LongestStyle == null || duration >= longestDuration)
            {
                longestDuration = duration;
                plan.LongestStyle = style;
            }
        }

        return plan;
    }

    private sealed class AnnouncementDeliveryPlan
    {
        public string[] Lines { get; set; } = Array.Empty<string>();
        public string? SpeakerName { get; set; }
        public Filter DeliveredFilter { get; } = Filter.Empty();
        public int DeliveredCount { get; set; }
        public AnnouncementStyle? LongestStyle { get; set; }
        public Dictionary<AnnouncementDisplayPreference, AnnouncementDeliveryGroup> Groups { get; } = new();
    }

    private sealed class AnnouncementDeliveryGroup
    {
        public required AnnouncementPresetPrototype Preset { get; init; }
        public required AnnouncementStyle Style { get; init; }
        public required Filter Filter { get; init; }
    }
}
