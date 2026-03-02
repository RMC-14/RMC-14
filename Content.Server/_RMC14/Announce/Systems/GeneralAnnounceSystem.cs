using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Announce.Core;
using Content.Server._RMC14.Announce.Validation;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Announce;
using Content.Shared.Database;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Log;

namespace Content.Server._RMC14.Announce;

public sealed class GeneralAnnounceSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private AnnouncementValidator _validator = default!;
    private AnnouncementPresetResolver _presetResolver = default!;
    private AnnouncementTargetFilter _targetFilter = default!;
    private readonly Dictionary<NetUserId, AnnouncementClientPreferences> _preferences = new();

    private const float PvsCleanupBufferSeconds = 2.0f;

    public override void Initialize()
    {
        base.Initialize();

        _validator = new AnnouncementValidator();
        _presetResolver = new AnnouncementPresetResolver(_prototypes);
        _targetFilter = new AnnouncementTargetFilter(EntityManager);
        SubscribeNetworkEvent<AnnouncementPreferenceNetMessage>(OnAnnouncementPreference);
    }

    public void Announce(string preset, string message, AnnouncementTarget? target = null)
    {
        var request = new AnnouncementRequest
        {
            Preset = preset,
            Message = message,
            Target = target ?? AnnouncementTarget.All
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceAdvanced(AnnouncementRequest request)
    {
        if (_net.IsClient)
            return;

        if (!ValidateRequest(request))
            return;

        if (!TryResolvePreset(request.Preset, out var preset))
            return;

        var filter = _targetFilter.Build(request.Target);
        AnnounceAdvanced(request, preset, filter);
    }

    public void AnnounceAdvanced(AnnouncementRequest request, Filter filter)
    {
        if (_net.IsClient)
            return;

        if (!ValidateRequest(request))
            return;

        if (!TryResolvePreset(request.Preset, out var preset))
            return;

        AnnounceAdvanced(request, preset, filter);
    }

    private void AnnounceAdvanced(AnnouncementRequest request, AnnouncementPresetPrototype preset, Filter filter)
    {
        if (filter.Count == 0)
            return;

        var stylizedSessions = new List<ICommonSession>();
        var defaultSessions = new List<ICommonSession>();
        var simplifiedSessions = new List<ICommonSession>();
        foreach (var session in filter.Recipients)
        {
            var preference = GetPreference(session, preset.ID);
            switch (preference)
            {
                case AnnouncementDisplayPreference.Disabled:
                    continue;
                case AnnouncementDisplayPreference.Default:
                    defaultSessions.Add(session);
                    continue;
                case AnnouncementDisplayPreference.Simplified:
                    simplifiedSessions.Add(session);
                    continue;
                default:
                    stylizedSessions.Add(session);
                    continue;
            }
        }

        if (stylizedSessions.Count == 0 && defaultSessions.Count == 0 && simplifiedSessions.Count == 0)
            return;

        var lines = BuildLines(request.Message);
        var speakerName = ResolveSpeakerName(request);
        var stylizedPreset = GetPresetForPreference(preset, AnnouncementDisplayPreference.Stylized);
        var defaultPreset = GetPresetForPreference(preset, AnnouncementDisplayPreference.Default);
        var simplifiedPreset = GetPresetForPreference(preset, AnnouncementDisplayPreference.Simplified);

        AnnouncementStyle? stylizedStyle = stylizedSessions.Count > 0
            ? MergeStyle(stylizedPreset.Style, request.StyleOverride)
            : null;
        AnnouncementStyle? defaultStyle = defaultSessions.Count > 0
            ? MergeStyle(defaultPreset.Style, request.StyleOverride)
            : null;
        AnnouncementStyle? simplifiedStyle = simplifiedSessions.Count > 0
            ? MergeStyle(simplifiedPreset.Style, request.StyleOverride)
            : null;

        var stylizedFilter = Filter.Empty().AddPlayers(stylizedSessions);
        var defaultFilter = Filter.Empty().AddPlayers(defaultSessions);
        var simplifiedFilter = Filter.Empty().AddPlayers(simplifiedSessions);

        var deliveredFilter = Filter.Empty();
        deliveredFilter.AddPlayers(stylizedSessions);
        deliveredFilter.AddPlayers(defaultSessions);
        deliveredFilter.AddPlayers(simplifiedSessions);

        var longestStyle = GetLongestStyle(stylizedStyle, defaultStyle, simplifiedStyle);
        if (longestStyle != null && deliveredFilter.Count > 0)
            EnsureSpeakerPvs(request, deliveredFilter, longestStyle);

        if (stylizedFilter.Count > 0 && stylizedStyle != null)
        {
            var clientData = BuildClientData(request, stylizedPreset, stylizedStyle, lines, speakerName);
            RaiseNetworkEvent(new AnnouncementNetMessage(clientData), stylizedFilter);
            PlayAnnouncementSound(request, stylizedPreset, stylizedFilter);
        }

        if (defaultFilter.Count > 0 && defaultStyle != null)
        {
            var clientData = BuildClientData(request, defaultPreset, defaultStyle, lines, speakerName);
            RaiseNetworkEvent(new AnnouncementNetMessage(clientData), defaultFilter);
            PlayAnnouncementSound(request, defaultPreset, defaultFilter);
        }

        if (simplifiedFilter.Count > 0 && simplifiedStyle != null)
        {
            var clientData = BuildClientData(request, simplifiedPreset, simplifiedStyle, lines, speakerName);
            RaiseNetworkEvent(new AnnouncementNetMessage(clientData), simplifiedFilter);
            PlayAnnouncementSound(request, simplifiedPreset, simplifiedFilter);
        }

        var deliveredCount = stylizedSessions.Count + defaultSessions.Count + simplifiedSessions.Count;
        LogAnnouncement(preset.ID, lines, request.Target, request.Source, deliveredCount);
    }

    public void AnnounceAsPlayer(
        EntityUid playerEntity,
        string message,
        string? presetId = null,
        AnnouncementTarget target = AnnouncementTarget.All,
        string? roleOverride = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = presetId ?? "MarineCommand",
            Target = target,
            Speaker = playerEntity,
            SpeakerNameOverride = roleOverride
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceHighCommand(string message, string? author = null, SoundSpecifier? sound = null)
    {
        var wrappedMessage = author != null
            ? $"{author}: {message}"
            : message;

        var request = new AnnouncementRequest
        {
            Message = wrappedMessage,
            Preset = "MarineCommand",
            Target = AnnouncementTarget.Marines,
            SoundOverride = sound
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceARES(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = "Ares",
            Target = AnnouncementTarget.All,
            Source = source,
            SpeakerNameOverride = "A.R.E.S.",
            SoundOverride = sound
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceCritical(string message)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = "Critical",
            Target = AnnouncementTarget.All
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceSlide(
        string message,
        SlideDirection direction = SlideDirection.Top,
        AnnouncementTarget target = AnnouncementTarget.All,
        EntityUid? source = null)
    {
        var style = new AnnouncementStyleOverride
        {
            Animation = AnnouncementAnimation.Slide,
            AnimationEnhancements = new RealisticAnimations
            {
                EnableSlide = true,
                SlideFrom = direction,
                SlideDuration = 1.0f
            }
        };

        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = "MarineCommand",
            Target = target,
            Source = source,
            StyleOverride = style
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceZoom(
        string message,
        float startScale = 0.1f,
        AnnouncementTarget target = AnnouncementTarget.All,
        EntityUid? source = null)
    {
        var style = new AnnouncementStyleOverride
        {
            Animation = AnnouncementAnimation.Zoom,
            AnimationEnhancements = new RealisticAnimations
            {
                EnableZoom = true,
                ZoomStartScale = startScale,
                ZoomDuration = 1.5f
            }
        };

        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = "MarineCommand",
            Target = target,
            Source = source,
            StyleOverride = style
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceCRT(
        EntityUid? source,
        string message,
        string presetId = "AresTerminal",
        SoundSpecifier? sound = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = presetId,
            Target = AnnouncementTarget.All,
            Speaker = source,
            Source = source,
            SpeakerNameOverride = "TERMINAL",
            SoundOverride = sound,
            ShowSprite = true,
            SpriteScale = 1.0f
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceAresTerminal(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        AnnounceCRT(source, message, "AresTerminal", sound);
    }

    public void AnnounceRetroTerminal(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        AnnounceCRT(source, message, "RetroTerminal", sound);
    }

    public void AnnounceModernTerminal(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        AnnounceCRT(source, message, "ModernTerminal", sound);
    }

    public void AnnounceCleanTerminal(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        AnnounceCRT(source, message, "CleanTerminal", sound);
    }

    public void AnnounceBounce(
        string message,
        int bounceCount = 3,
        float bounceHeight = 15f,
        AnnouncementTarget target = AnnouncementTarget.All,
        EntityUid? source = null)
    {
        var style = new AnnouncementStyleOverride
        {
            Animation = AnnouncementAnimation.Bounce,
            AnimationEnhancements = new RealisticAnimations
            {
                EnableBounce = true,
                BounceCount = bounceCount,
                BounceHeight = bounceHeight
            }
        };

        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = "MarineCommand",
            Target = target,
            Source = source,
            StyleOverride = style
        };

        AnnounceAdvanced(request);
    }

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

    private AnnouncementStyle MergeStyle(AnnouncementStyle baseStyle, AnnouncementStyleOverride? overrideStyle)
    {
        if (overrideStyle == null)
            return baseStyle;

        return baseStyle with
        {
            Animation = overrideStyle.Animation ?? baseStyle.Animation,
            AnimationEnhancements = overrideStyle.AnimationEnhancements ?? baseStyle.AnimationEnhancements,

            PrimaryColor = overrideStyle.PrimaryColor ?? baseStyle.PrimaryColor,
            TitleColor = overrideStyle.TitleColor ?? baseStyle.TitleColor,
            BackgroundColor = overrideStyle.BackgroundColor ?? baseStyle.BackgroundColor,
            BackgroundAlpha = overrideStyle.BackgroundAlpha ?? baseStyle.BackgroundAlpha,

            Position = overrideStyle.Position ?? baseStyle.Position,
            SpritePosition = overrideStyle.SpritePosition ?? baseStyle.SpritePosition,

            ShowSpeakerName = overrideStyle.ShowSpeakerName ?? baseStyle.ShowSpeakerName,
            SpeakerNameColor = overrideStyle.SpeakerNameColor ?? baseStyle.SpeakerNameColor,
            SpeakerNameFontSize = overrideStyle.SpeakerNameFontSize ?? baseStyle.SpeakerNameFontSize,
            SpeakerNamePosition = overrideStyle.SpeakerNamePosition ?? baseStyle.SpeakerNamePosition,

            SpriteScale = overrideStyle.SpriteScale ?? baseStyle.SpriteScale,
            SpriteSpacing = overrideStyle.SpriteSpacing ?? baseStyle.SpriteSpacing
        };
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

    private AnnouncementNetData BuildClientData(
        AnnouncementRequest request,
        AnnouncementPresetPrototype preset,
        AnnouncementStyle style,
        string[] lines,
        string? speakerName)
    {
        return new AnnouncementNetData
        {
            Text = lines,
            ConfigId = preset.ID,
            Priority = request.PriorityOverride ?? preset.Priority,
            CanInterrupt = request.CanInterrupt ?? preset.CanInterrupt,
            CanBeInterrupted = request.CanBeInterrupted ?? preset.CanBeInterrupted,
            Style = style,
            StyleOverride = request.StyleOverride,
            SpeakerEntity = EntityManager.GetNetEntity(request.Speaker),
            SpeakerName = speakerName,
            ShowSprite = request.ShowSprite && preset.ShowSprite,
            SpriteScale = request.SpriteScale,
            SpriteOffset = request.SpriteOffset ?? Vector2.Zero,
            TextOffset = request.TextOffset ?? preset.TextOffset,
            Title = request.Title,
            DecalRsi = request.DecalRsi ?? preset.DecalRsi,
            DecalState = request.DecalState ?? preset.DecalState,
            DecalPlacement = request.DecalPlacement ?? preset.DecalPlacement,
            DecalScale = request.DecalScale ?? preset.DecalScale,
            DecalAlpha = request.DecalAlpha ?? preset.DecalAlpha,
            DecalOffset = request.DecalOffset ?? preset.DecalOffset,
            IncognitoMask = request.IncognitoMask || preset.IncognitoMask
        };
    }

    private void EnsureSpeakerPvs(AnnouncementRequest request, Filter filter, AnnouncementStyle style)
    {
        if (!request.ShowSprite || !request.Speaker.HasValue)
            return;

        var speaker = request.Speaker.Value;
        if (!EntityManager.EntityExists(speaker))
            return;

        _pvsOverride.AddSessionOverrides(speaker, filter);

        var totalDuration = AnnouncementDurationCalculator.Calculate(style) + style.HoldDuration + PvsCleanupBufferSeconds;
        Timer.Spawn(TimeSpan.FromSeconds(totalDuration), () => RemoveSpeakerOverrides(speaker, filter));
    }

    private void RemoveSpeakerOverrides(EntityUid speaker, Filter filter)
    {
        if (!EntityManager.EntityExists(speaker))
            return;

        foreach (var session in filter.Recipients)
        {
            if (session.Status == SessionStatus.Connected)
                _pvsOverride.RemoveSessionOverride(speaker, session);
        }
    }

    private void PlayAnnouncementSound(AnnouncementRequest request, AnnouncementPresetPrototype preset, Filter filter)
    {
        var sound = request.SoundOverride ?? preset.Sound;
        if (sound == null)
            return;

        var volume = request.VolumeOverride ?? preset.SoundVolume;

        try
        {
            _audio.PlayGlobal(sound, filter, true, AudioParams.Default.WithVolume(volume));
        }
        catch (FileNotFoundException ex)
        {
            Log.Warning($"Audio file not found for announcement '{preset.ID}': {ex.Message}");
            Log.Warning($"Announcement will continue without sound. Please check audio file path: {sound}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to play announcement audio for '{preset.ID}': {ex.Message}");
            Log.Warning("Announcement will continue without sound.");
        }
    }

    private AnnouncementDisplayPreference GetPreference(ICommonSession session, string presetId)
    {
        if (_preferences.TryGetValue(session.UserId, out var preferences))
        {
            if (preferences.Overrides.TryGetValue(presetId, out var overridePreference))
                return overridePreference;

            return preferences.GlobalPreference;
        }

        return AnnouncementDisplayPreference.Default;
    }

    private AnnouncementPresetPrototype GetPresetForPreference(
        AnnouncementPresetPrototype preset,
        AnnouncementDisplayPreference preference)
    {
        var targetId = preference switch
        {
            AnnouncementDisplayPreference.Stylized => preset.StylizedVariant,
            AnnouncementDisplayPreference.Default => preset.DefaultVariant,
            AnnouncementDisplayPreference.Simplified => preset.SimplifiedVariant,
            _ => null
        };

        if (targetId != null && _prototypes.TryIndex<AnnouncementPresetPrototype>(targetId, out var variant))
            return variant;

        return preset;
    }

    private static AnnouncementStyle? GetLongestStyle(params AnnouncementStyle?[] styles)
    {
        AnnouncementStyle? longest = null;
        float longestDuration = 0f;

        foreach (var style in styles)
        {
            if (style == null)
                continue;

            var duration = AnnouncementDurationCalculator.Calculate(style) + style.HoldDuration;
            if (longest == null || duration >= longestDuration)
            {
                longest = style;
                longestDuration = duration;
            }
        }

        return longest;
    }

    private void OnAnnouncementPreference(AnnouncementPreferenceNetMessage msg, EntitySessionEventArgs args)
    {
        var sanitizedOverrides = new Dictionary<string, AnnouncementDisplayPreference>();
        foreach (var (key, value) in msg.Overrides)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            sanitizedOverrides[key] = value;
        }

        _preferences[args.SenderSession.UserId] = new AnnouncementClientPreferences(msg.Preference, sanitizedOverrides);
    }

    private sealed record AnnouncementClientPreferences(
        AnnouncementDisplayPreference GlobalPreference,
        Dictionary<string, AnnouncementDisplayPreference> Overrides);
}
