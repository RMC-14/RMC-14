using System;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Announce.Core;

public sealed class AnnouncementPresetResolver
{
    private readonly IPrototypeManager _prototypes;

    public AnnouncementPresetResolver(IPrototypeManager prototypes)
    {
        _prototypes = prototypes;
    }

    public AnnouncementPresetPrototype? Resolve(string? presetId)
    {
        if (string.IsNullOrEmpty(presetId))
            return null;

        if (_prototypes.TryIndex<AnnouncementPresetPrototype>(presetId, out var prototypePreset))
        {
            return prototypePreset;
        }

        foreach (var preset in _prototypes.EnumeratePrototypes<AnnouncementPresetPrototype>())
        {
            foreach (var alias in preset.Aliases)
            {
                if (!string.Equals(alias, presetId, StringComparison.OrdinalIgnoreCase))
                    continue;

                return preset;
            }
        }

        return null;
    }
}
