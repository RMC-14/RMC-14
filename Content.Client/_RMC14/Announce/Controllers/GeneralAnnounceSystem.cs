using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.Announce;

public sealed class GeneralAnnounceSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private AnnouncementDisplayPreference _preference;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyle, OnPreferenceChanged, true);
        SubscribeNetworkEvent<AnnouncementNetMessage>(OnAnnouncementMessage);
    }

    private void OnAnnouncementMessage(AnnouncementNetMessage msg, EntitySessionEventArgs args)
    {
        if (_preference == AnnouncementDisplayPreference.Disabled)
            return;

        if (_uiManager.GetUIController<GeneralAnnounceUIController>() is { } controller)
        {
            controller.ShowAnnouncement(msg.Data);
        }
    }

    private void OnPreferenceChanged(AnnouncementDisplayPreference preference)
    {
        _preference = preference;
        RaiseNetworkEvent(new AnnouncementPreferenceNetMessage(preference));
    }
}
