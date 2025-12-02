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
    private AnnouncementPresetResolver? _presetResolver;
    private AnnouncementTargetFilter? _targetFilter;

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
            _presetResolver = new AnnouncementPresetResolver(_prototypes);
            _targetFilter = new AnnouncementTargetFilter(EntityManager);
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

        if (_presetResolver == null || _targetFilter == null)
            return;

        var preset = _presetResolver.Resolve(request.Preset);
        if (preset == null)
        {
            Log.Warning($"No valid preset found for announcement request with preset '{request.Preset}'");
            return;
        }

        var style = preset.Style;
        if (request.StyleOverride != null)
        {
            var o = request.StyleOverride;
            style = style with
            {
                Animation = o.Animation ?? style.Animation,
                AnimationEnhancements = o.AnimationEnhancements ?? style.AnimationEnhancements,

                PrimaryColor = o.PrimaryColor ?? style.PrimaryColor,
                BackgroundColor = o.BackgroundColor ?? style.BackgroundColor,
                BackgroundAlpha = o.BackgroundAlpha ?? style.BackgroundAlpha,

                Position = o.Position ?? style.Position,
                SpritePosition = o.SpritePosition ?? style.SpritePosition,

                ShowSpeakerName = o.ShowSpeakerName ?? style.ShowSpeakerName,
                SpeakerNameColor = o.SpeakerNameColor ?? style.SpeakerNameColor,
                SpeakerNameFontSize = o.SpeakerNameFontSize ?? style.SpeakerNameFontSize,
                SpeakerNamePosition = o.SpeakerNamePosition ?? style.SpeakerNamePosition,

                SpriteScale = o.SpriteScale ?? style.SpriteScale,
                SpriteSpacing = o.SpriteSpacing ?? style.SpriteSpacing
            };
        }

        var sound = request.SoundOverride ?? preset.Sound;
        var volume = request.VolumeOverride ?? preset.SoundVolume;
        var priority = request.PriorityOverride ?? preset.Priority;
        var canInterrupt = request.CanInterrupt ?? preset.CanInterrupt;
        var canBeInterrupted = request.CanBeInterrupted ?? preset.CanBeInterrupted;

        var filter = _targetFilter.Build(request.Target);
        if (filter.Count == 0)
            return;

        if (request.Speaker.HasValue && EntityManager.EntityExists(request.Speaker.Value) && request.ShowSprite)
        {
            _pvsOverride.AddSessionOverrides(request.Speaker.Value, filter);

            var totalDuration = AnnouncementDurationCalculator.Calculate(style) + style.HoldDuration + 2.0f;
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

    public void AnnounceZoom(string message, float startScale = 0.1f,
        AnnouncementTarget target = AnnouncementTarget.All, EntityUid? source = null)
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
