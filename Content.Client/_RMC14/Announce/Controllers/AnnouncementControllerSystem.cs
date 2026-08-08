using System.Collections.Generic;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementControllerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private AnnouncementDisplayPreference _preference;
    private Dictionary<string, AnnouncementDisplayPreference> _overrides = new();
    private AnnouncementLayoutOverride? _globalLayoutOverride;
    private Dictionary<string, AnnouncementLayoutOverride> _layoutOverrides = new();
    private Dictionary<string, PresetCacheEntry> _presetCache = new();
    private AnnouncementOverlayUIController? _overlayController;

    private readonly record struct PresetCacheEntry(AnnouncementDisplayPreference? DefaultPreference, string? GroupId);

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyle, OnPreferenceChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyleOverrides, OnOverridesChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementLayout, OnGlobalLayoutChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementLayoutOverrides, OnLayoutOverridesChanged, true);
        SubscribeNetworkEvent<AnnouncementNetMessage>(OnAnnouncementMessage);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RebuildPresetCache();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<AnnouncementPresetPrototype>())
            RebuildPresetCache();
    }

    private void RebuildPresetCache()
    {
        _presetCache = new Dictionary<string, PresetCacheEntry>();
        foreach (var preset in _prototypeManager.EnumeratePrototypes<AnnouncementPresetPrototype>())
        {
            _presetCache[preset.ID] = new PresetCacheEntry(preset.DefaultPreference, preset.GroupId?.ToString());
        }
    }

    private void OnAnnouncementMessage(AnnouncementNetMessage msg, EntitySessionEventArgs args)
    {
        var preference = ResolveDisplayPreference(msg.Data.AnnouncementId);
        if (preference == AnnouncementDisplayPreference.Disabled)
            return;

        if (_uiManager.GetUIController<AnnouncementOverlayUIController>() is not { } controller)
            return;

        if (_overlayController != controller)
        {
            if (_overlayController != null)
                _overlayController.AnnouncementDone -= OnAnnouncementDone;
            _overlayController = controller;
            _overlayController.AnnouncementDone += OnAnnouncementDone;
        }

        if (AnnouncementDisplayResolver.TryResolve(_prototypeManager, _serialization, msg.Data, preference, out var resolved))
        {
            AnnouncementLayoutResolver.Apply(resolved, ResolveLayoutOverride(msg.Data.AnnouncementId));
            controller.ShowAnnouncement(resolved);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlayController != null)
        {
            _overlayController.AnnouncementDone -= OnAnnouncementDone;
            _overlayController = null;
        }
    }

    private void OnAnnouncementDone(NetEntity? speaker)
    {
        if (speaker.HasValue && _net.IsConnected)
            RaiseNetworkEvent(new AnnouncementPlaybackDoneMsg(speaker.Value));
    }

    private void OnPreferenceChanged(AnnouncementDisplayPreference preference)
    {
        _preference = preference;
    }

    private void OnOverridesChanged(string serializedOverrides)
    {
        _overrides = AnnouncementPreferenceOverrides.Parse(serializedOverrides);
    }

    private void OnGlobalLayoutChanged(string serializedLayout)
    {
        _globalLayoutOverride = AnnouncementLayoutOverrides.ParseSingle(serializedLayout);
        UpdateCurrentAnnouncementPosition();
    }

    private void OnLayoutOverridesChanged(string serializedOverrides)
    {
        _layoutOverrides = AnnouncementLayoutOverrides.Parse(serializedOverrides);
        UpdateCurrentAnnouncementPosition();
    }

    private void UpdateCurrentAnnouncementPosition()
    {
        var screen = _uiManager.ActiveScreen;
        if (screen == null)
            return;

        var widget = screen.GetWidget<AnnouncementWidget>();
        if (widget?.ActiveAnnouncement is not { } active)
            return;

        var layout = ResolveLayoutOverride(active.Data.AnnouncementId);
        active.Data.ScreenPositionOverride = layout?.Clamp().ScreenPosition;
    }

    public AnnouncementDisplayPreference ResolveDisplayPreference(ProtoId<AnnouncementPresetPrototype> announcementId)
    {
        var id = announcementId.ToString();

        if (_overrides.TryGetValue(id, out var preference))
            return preference;

        if (_presetCache.TryGetValue(id, out var entry))
        {
            if (entry.GroupId is { } groupId &&
                _overrides.TryGetValue(groupId, out var groupPreference))
                return groupPreference;

            if (entry.DefaultPreference is { } defaultPreference)
                return defaultPreference;
        }

        return _preference;
    }

    public AnnouncementLayoutOverride? ResolveLayoutOverride(ProtoId<AnnouncementPresetPrototype> announcementId)
    {
        if (_layoutOverrides.TryGetValue(announcementId.ToString(), out var overrideValue))
            return overrideValue;

        return _globalLayoutOverride;
    }

    public AnnouncementLayoutOverride? GetGlobalLayoutOverride()
    {
        return _globalLayoutOverride;
    }

    public AnnouncementLayoutOverride? GetPresetLayoutOverride(ProtoId<AnnouncementPresetPrototype> announcementId)
    {
        return _layoutOverrides.TryGetValue(announcementId.ToString(), out var overrideValue)
            ? overrideValue
            : null;
    }
}
