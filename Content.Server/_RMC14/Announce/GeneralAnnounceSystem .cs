using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Areas;
using Content.Server._RMC14.Marines;
using Content.Server.Administration.Logs;
using Content.Server._RMC14.Announce.Validation;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Maths;
using Content.Server._RMC14.Announce.Core;
using System.IO;
using Robust.Server.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.Enums;

namespace Content.Server._RMC14.Announce;

public sealed class GeneralAnnounceSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private AnnouncementValidator? _validator;

    public override void Initialize()
    {
        base.Initialize();

        Log.Info("=== SS14 Announcement System Debug ===");

        try
        {
            var allKinds = _prototypes.GetPrototypeKinds().ToList();
            Log.Info($"Prototype kinds ({allKinds.Count}): {string.Join(", ", allKinds)}");

            if (allKinds.Contains("announcementPreset"))
            {
                Log.Info("✓ announcementPreset kind is registered");
            }
            else
            {
                Log.Error("✗ announcementPreset kind NOT found!");
                Log.Error("Expected: announcementPreset (from AnnouncementPresetPrototype)");
            }

            var presets = _prototypes.EnumeratePrototypes<AnnouncementPresetPrototype>().ToList();
            Log.Info($"Found {presets.Count} announcement presets:");

            foreach (var preset in presets)
            {
                Log.Info($"  - {preset.ID}: {preset.Name} (Target: {preset.Target}, Priority: {preset.Priority})");
                Log.Info($"    Aliases: [{string.Join(", ", preset.Aliases)}]");
            }

            if (presets.Count == 0)
            {
                Log.Error("No presets loaded! Check YAML file location and syntax.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error during initialization: {ex}");
        }

        try
        {
            _validator = new AnnouncementValidator();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize validator: {ex}");
        }
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

        if (_validator != null)
        {
            var validation = _validator.ValidateRequest(request);
            if (!validation.IsValid)
            {
                Log.Warning($"Invalid announcement request: {validation.GetErrorSummary()}");
                return;
            }
        }

        var preset = GetEffectivePreset(request);
        if (preset == null)
        {
            Log.Warning($"No valid preset found for announcement request with preset '{request.Preset}'");
            return;
        }

        var style = preset.Style;
        if (request.StyleOverride != null)
        {
            style = MergeStyles(style, request.StyleOverride);
        }

        var sound = request.SoundOverride ?? preset.Sound;
        var volume = request.VolumeOverride ?? preset.SoundVolume;
        var priority = request.PriorityOverride ?? preset.Priority;
        var canInterrupt = request.CanInterrupt ?? preset.CanInterrupt;
        var canBeInterrupted = request.CanBeInterrupted ?? preset.CanBeInterrupted;

        var filter = CreateFilter(request.Target, request.Source, request.TargetEntity);
        if (filter.Count == 0)
            return;

        if (request.Speaker.HasValue && EntityManager.EntityExists(request.Speaker.Value) && request.ShowSprite)
        {
            _pvsOverride.AddSessionOverrides(request.Speaker.Value, filter);

            var totalDuration = CalculateAnnouncementDuration(style) + style.HoldDuration + 2.0f;
            Timer.Spawn(TimeSpan.FromSeconds(totalDuration), () =>
            {
                if (EntityManager.EntityExists(request.Speaker.Value))
                {
                    foreach (var session in filter.Recipients)
                    {
                        if (session.Status == SessionStatus.Connected)
                        {
                            _pvsOverride.RemoveSessionOverride(request.Speaker.Value, session);
                        }
                    }
                }
            });
        }

        var speakerName = request.SpeakerNameOverride;
        if (string.IsNullOrEmpty(speakerName) && request.Speaker.HasValue && EntityManager.EntityExists(request.Speaker.Value))
        {
            speakerName = EntityManager.GetComponent<MetaDataComponent>(request.Speaker.Value).EntityName;
        }

        var lines = request.Message.Split('\n').Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

        var clientData = new AnnouncementNetData
        {
            Text = lines,
            ConfigId = request.Preset ?? "default",
            Priority = priority,
            CanInterrupt = canInterrupt,
            CanBeInterrupted = canBeInterrupted,
            Style = style,
            SpeakerEntity = EntityManager.GetNetEntity(request.Speaker),
            SpeakerName = speakerName,
            ShowSprite = request.ShowSprite,
            SpriteScale = request.SpriteScale,
            SpriteOffset = request.SpriteOffset ?? Vector2.Zero
        };

        var msg = new AnnouncementNetMessage(clientData);
        RaiseNetworkEvent(msg, filter);

        if (sound != null)
        {
            try
            {
                _audio.PlayGlobal(sound, filter, true, null);
            }
            catch (FileNotFoundException ex)
            {
                Log.Warning($"Audio file not found for announcement '{request.Preset}': {ex.Message}");
                Log.Warning($"Announcement will continue without sound. Please check audio file path: {sound}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to play announcement audio for '{request.Preset}': {ex.Message}");
                Log.Warning("Announcement will continue without sound.");
            }
        }

        LogAnnouncement(request.Preset ?? "custom", lines, request.Target, request.Source, filter.Count);
    }

    private float CalculateAnnouncementDuration(AnnouncementStyle style)
    {
        var baseDuration = style.Animation switch
        {
            AnnouncementAnimation.Typewriter => 5.0f,
            AnnouncementAnimation.Slide => style.AnimationEnhancements?.SlideDuration ?? 1.0f,
            AnnouncementAnimation.Zoom => style.AnimationEnhancements?.ZoomDuration ?? 1.0f,
            AnnouncementAnimation.Bounce => (style.AnimationEnhancements?.BounceCount ?? 3) * 0.5f,
            AnnouncementAnimation.Fade => 2.0f,
            AnnouncementAnimation.Pulse => 1.0f,
            AnnouncementAnimation.Glitch => 3.0f,
            _ => 1.0f
        };

        return baseDuration;
    }

    public AnnouncementBuilder CreateAnnouncement()
    {
        return new AnnouncementBuilder(this);
    }

    public void AnnounceAsPlayer(EntityUid playerEntity, string message, string? presetId = null,
        AnnouncementTarget target = AnnouncementTarget.All, string? roleOverride = null)
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

    public void AnnounceSlide(string message, SlideDirection direction = SlideDirection.Top,
        AnnouncementTarget target = AnnouncementTarget.All, EntityUid? source = null)
    {
        var style = new AnnouncementStyle
        {
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

    public void AnnounceZoom(string message, float startScale = 0.1f,
        AnnouncementTarget target = AnnouncementTarget.All, EntityUid? source = null)
    {
        var style = new AnnouncementStyle
        {
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

    public void AnnounceCRT(EntityUid? source, string message, string presetId = "AresTerminal", SoundSpecifier? sound = null)
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

    public void AnnounceBounce(string message, int bounceCount = 3, float bounceHeight = 15f,
        AnnouncementTarget target = AnnouncementTarget.All, EntityUid? source = null)
    {
        var style = new AnnouncementStyle
        {
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

    private AnnouncementPresetPrototype? GetEffectivePreset(AnnouncementRequest request)
    {
        if (string.IsNullOrEmpty(request.Preset))
            return null;

        if (_prototypes.TryIndex<AnnouncementPresetPrototype>(request.Preset, out var prototypePreset))
        {
            Log.Debug($"Found preset by direct ID: {request.Preset}");
            return prototypePreset;
        }

        foreach (var preset in _prototypes.EnumeratePrototypes<AnnouncementPresetPrototype>())
        {
            if (preset.Aliases.Contains(request.Preset, StringComparer.OrdinalIgnoreCase))
            {
                Log.Debug($"Found preset by alias: {request.Preset} -> {preset.ID}");
                return preset;
            }
        }

        var availablePresets = _prototypes.EnumeratePrototypes<AnnouncementPresetPrototype>().ToList();
        Log.Warning($"No preset found for '{request.Preset}'. Available: {string.Join(", ", availablePresets.Select(p => p.ID))}");

        return null;
    }

    private AnnouncementStyle MergeStyles(AnnouncementStyle baseStyle, AnnouncementStyle overrideStyle)
    {
        return new AnnouncementStyle
        {
            Animation = overrideStyle.Animation != AnnouncementAnimation.Typewriter ? overrideStyle.Animation : baseStyle.Animation,
            Position = overrideStyle.Position != AnnouncementPosition.MiddleCenter ? overrideStyle.Position : baseStyle.Position,
            ShowBackground = overrideStyle.ShowBackground != baseStyle.ShowBackground ? overrideStyle.ShowBackground : baseStyle.ShowBackground,
            BackgroundAlpha = overrideStyle.BackgroundAlpha != baseStyle.BackgroundAlpha ? overrideStyle.BackgroundAlpha : baseStyle.BackgroundAlpha,
            BackgroundColor = overrideStyle.BackgroundColor != baseStyle.BackgroundColor ? overrideStyle.BackgroundColor : baseStyle.BackgroundColor,
            PrimaryColor = overrideStyle.PrimaryColor != baseStyle.PrimaryColor ? overrideStyle.PrimaryColor : baseStyle.PrimaryColor,
            SecondaryColor = overrideStyle.SecondaryColor ?? baseStyle.SecondaryColor,
            AccentColor = overrideStyle.AccentColor ?? baseStyle.AccentColor,
            FontSize = overrideStyle.FontSize != baseStyle.FontSize ? overrideStyle.FontSize : baseStyle.FontSize,
            LineHeight = overrideStyle.LineHeight != baseStyle.LineHeight ? overrideStyle.LineHeight : baseStyle.LineHeight,
            PrintSpeed = overrideStyle.PrintSpeed != baseStyle.PrintSpeed ? overrideStyle.PrintSpeed : baseStyle.PrintSpeed,
            HoldDuration = overrideStyle.HoldDuration != baseStyle.HoldDuration ? overrideStyle.HoldDuration : baseStyle.HoldDuration,
            ShakeIntensity = overrideStyle.ShakeIntensity != baseStyle.ShakeIntensity ? overrideStyle.ShakeIntensity : baseStyle.ShakeIntensity,
            FlickerChance = overrideStyle.FlickerChance != baseStyle.FlickerChance ? overrideStyle.FlickerChance : baseStyle.FlickerChance,
            GlitchChance = overrideStyle.GlitchChance != baseStyle.GlitchChance ? overrideStyle.GlitchChance : baseStyle.GlitchChance,
            ShowSpriteBox = overrideStyle.ShowSpriteBox != baseStyle.ShowSpriteBox ? overrideStyle.ShowSpriteBox : baseStyle.ShowSpriteBox,
            SpriteBoxColor = overrideStyle.SpriteBoxColor != baseStyle.SpriteBoxColor ? overrideStyle.SpriteBoxColor : baseStyle.SpriteBoxColor,
            SpriteBoxBorderColor = overrideStyle.SpriteBoxBorderColor != baseStyle.SpriteBoxBorderColor ? overrideStyle.SpriteBoxBorderColor : baseStyle.SpriteBoxBorderColor,
            SpriteBoxBorderThickness = overrideStyle.SpriteBoxBorderThickness != baseStyle.SpriteBoxBorderThickness ? overrideStyle.SpriteBoxBorderThickness : baseStyle.SpriteBoxBorderThickness,
            SpriteBoxPadding = overrideStyle.SpriteBoxPadding != baseStyle.SpriteBoxPadding ? overrideStyle.SpriteBoxPadding : baseStyle.SpriteBoxPadding,
            SpriteGlow = overrideStyle.SpriteGlow != baseStyle.SpriteGlow ? overrideStyle.SpriteGlow : baseStyle.SpriteGlow,
            SpriteGlowColor = overrideStyle.SpriteGlowColor != baseStyle.SpriteGlowColor ? overrideStyle.SpriteGlowColor : baseStyle.SpriteGlowColor,
            SpriteGlowIntensity = overrideStyle.SpriteGlowIntensity != baseStyle.SpriteGlowIntensity ? overrideStyle.SpriteGlowIntensity : baseStyle.SpriteGlowIntensity,
            ShowSpeakerName = overrideStyle.ShowSpeakerName != baseStyle.ShowSpeakerName ? overrideStyle.ShowSpeakerName : baseStyle.ShowSpeakerName,
            SpeakerNameColor = overrideStyle.SpeakerNameColor != baseStyle.SpeakerNameColor ? overrideStyle.SpeakerNameColor : baseStyle.SpeakerNameColor,
            SpeakerNameFontSize = overrideStyle.SpeakerNameFontSize != baseStyle.SpeakerNameFontSize ? overrideStyle.SpeakerNameFontSize : baseStyle.SpeakerNameFontSize,
            SpeakerNamePosition = overrideStyle.SpeakerNamePosition != baseStyle.SpeakerNamePosition ? overrideStyle.SpeakerNamePosition : baseStyle.SpeakerNamePosition,
            SpritePosition = overrideStyle.SpritePosition != baseStyle.SpritePosition ? overrideStyle.SpritePosition : baseStyle.SpritePosition,
            SpriteSpacing = overrideStyle.SpriteSpacing != baseStyle.SpriteSpacing ? overrideStyle.SpriteSpacing : baseStyle.SpriteSpacing,
            AnimationEnhancements = overrideStyle.AnimationEnhancements ?? baseStyle.AnimationEnhancements,
            TextEnhancements = overrideStyle.TextEnhancements ?? baseStyle.TextEnhancements,
            BackgroundStyle = overrideStyle.BackgroundStyle ?? baseStyle.BackgroundStyle,
            EnableScreenShake = overrideStyle.EnableScreenShake != baseStyle.EnableScreenShake ? overrideStyle.EnableScreenShake : baseStyle.EnableScreenShake,
            ShakeDuration = overrideStyle.ShakeDuration != baseStyle.ShakeDuration ? overrideStyle.ShakeDuration : baseStyle.ShakeDuration,
            EnableFlash = overrideStyle.EnableFlash != baseStyle.EnableFlash ? overrideStyle.EnableFlash : baseStyle.EnableFlash,
            FlashColor = overrideStyle.FlashColor != baseStyle.FlashColor ? overrideStyle.FlashColor : baseStyle.FlashColor,
            FlashDuration = overrideStyle.FlashDuration != baseStyle.FlashDuration ? overrideStyle.FlashDuration : baseStyle.FlashDuration,
            FlashCount = overrideStyle.FlashCount != baseStyle.FlashCount ? overrideStyle.FlashCount : baseStyle.FlashCount
        };
    }

    private Filter CreateFilter(AnnouncementTarget target, EntityUid? source, EntityUid? targetEntity)
    {
        var allPlayers = Filter.Broadcast();

        switch (target)
        {
            case AnnouncementTarget.Marines:
                var marineFilter = new List<ICommonSession>();
                foreach (var session in allPlayers.Recipients)
                {
                    if (session.AttachedEntity is not { } entity)
                        continue;

                    if (HasComp<MarineComponent>(entity) ||
                        HasComp<GhostComponent>(entity))
                    {
                        marineFilter.Add(session);
                    }
                }
                return Filter.Empty().AddPlayers(marineFilter);

            case AnnouncementTarget.Xenos:
                var xenoFilter = new List<ICommonSession>();
                foreach (var session in allPlayers.Recipients)
                {
                    if (session.AttachedEntity is not { } entity)
                        continue;

                    if (HasComp<XenoComponent>(entity) ||
                        HasComp<GhostComponent>(entity))
                    {
                        xenoFilter.Add(session);
                    }
                }
                return Filter.Empty().AddPlayers(xenoFilter);

            case AnnouncementTarget.All:
            default:
                return allPlayers;
        }
    }

    private void LogAnnouncement(string configId, string[] text, AnnouncementTarget target, EntityUid? source, int recipientCount)
    {
        var sourceStr = source?.ToString() ?? "System";
        var textPreview = text.Length > 0 ? text[0] : "";
        if (textPreview.Length > 50)
            textPreview = textPreview[..47] + "...";

        _adminLogs.Add(LogType.AdminMessage, LogImpact.Medium,
            $"Announcement [{configId}] from {sourceStr} to {target} ({recipientCount} recipients): {textPreview}");
    }
}
