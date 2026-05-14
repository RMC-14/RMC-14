using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementControllerSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private AnnouncementDisplayPreference _preference;
    private Dictionary<string, AnnouncementDisplayPreference> _overrides = new();
    private AnnouncementLayoutOverride? _globalLayoutOverride;
    private Dictionary<string, AnnouncementLayoutOverride> _layoutOverrides = new();

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyle, OnPreferenceChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyleOverrides, OnOverridesChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementLayout, OnGlobalLayoutChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementLayoutOverrides, OnLayoutOverridesChanged, true);
        SubscribeNetworkEvent<AnnouncementNetMessage>(OnAnnouncementMessage);
    }

    private void OnAnnouncementMessage(AnnouncementNetMessage msg, EntitySessionEventArgs args)
    {
        var preference = ResolveDisplayPreference(msg.Data.AnnouncementId);
        if (preference == AnnouncementDisplayPreference.Disabled)
            return;

        if (_uiManager.GetUIController<AnnouncementOverlayUIController>() is { } controller &&
            AnnouncementDisplayResolver.TryResolve(_prototypeManager, msg.Data, preference, out var resolved))
        {
            AnnouncementLayoutResolver.Apply(resolved, ResolveLayoutOverride(msg.Data.AnnouncementId));
            controller.ShowAnnouncement(resolved);
        }
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
    }

    private void OnLayoutOverridesChanged(string serializedOverrides)
    {
        _layoutOverrides = AnnouncementLayoutOverrides.Parse(serializedOverrides);
    }

    public AnnouncementDisplayPreference ResolveDisplayPreference(ProtoId<AnnouncementPresetPrototype> announcementId)
    {
        if (_overrides.TryGetValue(announcementId.ToString(), out var preference))
            return preference;

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
