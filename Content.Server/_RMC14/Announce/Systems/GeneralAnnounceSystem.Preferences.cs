using System.Collections.Generic;
using Content.Shared._RMC14.Announce;
using Robust.Server.GameStates;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Announce;

public sealed partial class GeneralAnnounceSystem
{
    private AnnouncementDisplayPreference GetPreference(ICommonSession session, string presetId)
    {
        if (_preferences.TryGetValue(session.UserId, out var preferences))
        {
            if (preferences.Overrides.TryGetValue(presetId, out var overridePreference))
                return NormalizePreference(overridePreference);

            return NormalizePreference(preferences.GlobalPreference);
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

    private void OnAnnouncementPreference(AnnouncementPreferenceNetMessage msg, EntitySessionEventArgs args)
    {
        var sanitizedOverrides = new Dictionary<string, AnnouncementDisplayPreference>();
        foreach (var (key, value) in msg.Overrides)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (_presetResolver.Resolve(key) is not { } preset)
                continue;

            sanitizedOverrides[preset.ID] = NormalizePreference(value);
        }

        _preferences[args.SenderSession.UserId] = new AnnouncementClientPreferences(
            NormalizePreference(msg.Preference),
            sanitizedOverrides);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected)
            return;

        _preferences.Remove(args.Session.UserId);
    }

    private static AnnouncementDisplayPreference NormalizePreference(AnnouncementDisplayPreference preference)
    {
        return preference switch
        {
            AnnouncementDisplayPreference.Disabled => AnnouncementDisplayPreference.Disabled,
            AnnouncementDisplayPreference.Stylized => AnnouncementDisplayPreference.Stylized,
            AnnouncementDisplayPreference.Default => AnnouncementDisplayPreference.Default,
            AnnouncementDisplayPreference.Simplified => AnnouncementDisplayPreference.Simplified,
            _ => AnnouncementDisplayPreference.Stylized
        };
    }

    private sealed record AnnouncementClientPreferences(
        AnnouncementDisplayPreference GlobalPreference,
        Dictionary<string, AnnouncementDisplayPreference> Overrides);
}
