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

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyle, OnPreferenceChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyleOverrides, OnOverridesChanged, true);
        SubscribeNetworkEvent<AnnouncementNetMessage>(OnAnnouncementMessage);
    }

    private void OnAnnouncementMessage(AnnouncementNetMessage msg, EntitySessionEventArgs args)
    {
        var preference = GetPreference(msg.Data.AnnouncementId);
        if (preference == AnnouncementDisplayPreference.Disabled)
            return;

        if (_uiManager.GetUIController<AnnouncementOverlayUIController>() is { } controller &&
            AnnouncementDisplayResolver.TryResolve(_prototypeManager, msg.Data, preference, out var resolved))
        {
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

    private AnnouncementDisplayPreference GetPreference(ProtoId<AnnouncementPresetPrototype> announcementId)
    {
        if (_overrides.TryGetValue(announcementId.ToString(), out var preference))
            return preference;

        return _preference;
    }
}
