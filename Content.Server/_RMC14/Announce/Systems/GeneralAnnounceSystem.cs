using System.Collections.Generic;
using System.Numerics;
using Content.Server._RMC14.Announce.Core;
using Content.Server._RMC14.Announce.Validation;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Announce;
using Content.Shared.Database;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Announce;

public sealed partial class GeneralAnnounceSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
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
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeNetworkEvent<AnnouncementPreferenceNetMessage>(OnAnnouncementPreference);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
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
        if (_net.IsClient || !ValidateRequest(request) || !TryResolvePreset(request.Preset, out var preset))
            return;

        var filter = _targetFilter.Build(request.Target);
        AnnounceAdvanced(request, preset, filter);
    }

    public void AnnounceAdvanced(AnnouncementRequest request, Filter filter)
    {
        if (_net.IsClient || !ValidateRequest(request) || !TryResolvePreset(request.Preset, out var preset))
            return;

        AnnounceAdvanced(request, preset, filter);
    }

    private void AnnounceAdvanced(AnnouncementRequest request, AnnouncementPresetPrototype preset, Filter filter)
    {
        if (filter.Count == 0)
            return;

        var plan = BuildDeliveryPlan(request, preset, filter);
        if (plan == null)
            return;

        if (plan.LongestStyle != null && plan.DeliveredFilter.Count > 0)
            EnsureSpeakerPvs(request, plan.DeliveredFilter, plan.LongestStyle);

        foreach (var preference in DeliveryOrder)
        {
            if (!plan.Groups.TryGetValue(preference, out var group))
                continue;

            var clientData = BuildClientData(request, group.Preset, group.Style, plan.Lines, plan.SpeakerName);
            RaiseNetworkEvent(new AnnouncementNetMessage(clientData), group.Filter);
        }

        LogAnnouncement(preset.ID, plan.Lines, request.Target, request.Source, plan.DeliveredCount);
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
            SpeakerEntity = EntityManager.GetNetEntity(request.Speaker),
            SpeakerName = speakerName,
            ShowSprite = request.ShowSprite && preset.ShowSprite,
            SpriteScale = request.SpriteScale,
            SpriteOffset = request.SpriteOffset ?? Vector2.Zero,
            TextOffset = request.TextOffset ?? preset.TextOffset,
            Title = request.Title,
            Sound = request.SoundOverride ?? preset.Sound,
            SoundVolume = request.VolumeOverride ?? preset.SoundVolume,
            DecalRsi = request.DecalRsi ?? preset.DecalRsi,
            DecalState = request.DecalState ?? preset.DecalState,
            DecalPlacement = request.DecalPlacement ?? preset.DecalPlacement,
            DecalScale = request.DecalScale ?? preset.DecalScale,
            DecalAlpha = request.DecalAlpha ?? preset.DecalAlpha,
            DecalOffset = request.DecalOffset ?? preset.DecalOffset
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

        var totalDuration = AnnouncementDurationCalculator.Calculate(style) + style.AnimationConfig.HoldDuration + PvsCleanupBufferSeconds;
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

}

