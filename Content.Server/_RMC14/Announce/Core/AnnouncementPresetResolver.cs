using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Serilog;

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
            Log.Debug($"Found preset by direct ID: {presetId}");
            return prototypePreset;
        }

        foreach (var preset in _prototypes.EnumeratePrototypes<AnnouncementPresetPrototype>())
        {
            if (preset.Aliases.Contains(presetId, StringComparer.OrdinalIgnoreCase))
            {
                Log.Debug($"Found preset by alias: {presetId} -> {preset.ID}");
                return preset;
            }
        }

        var available = _prototypes.EnumeratePrototypes<AnnouncementPresetPrototype>().ToList();
        Log.Warning($"No preset found for '{presetId}'. Available: {string.Join(", ", available.Select(p => p.ID))}");
        return null;
    }
}
