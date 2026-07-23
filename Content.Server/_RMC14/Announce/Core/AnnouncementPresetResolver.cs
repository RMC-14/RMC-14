using System;
using System.Collections.Generic;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Announce.Core;

public sealed class AnnouncementPresetResolver
{
    private readonly IPrototypeManager _prototypes;
    private readonly Dictionary<string, AnnouncementPresetPrototype> _aliasIndex;

    public AnnouncementPresetResolver(IPrototypeManager prototypes)
    {
        _prototypes = prototypes;
        _aliasIndex = BuildAliasIndex();
    }

    public AnnouncementPresetPrototype? Resolve(ProtoId<AnnouncementPresetPrototype>? presetId)
    {
        if (presetId is not { } typedPresetId)
            return null;

        if (_prototypes.TryIndex(typedPresetId, out AnnouncementPresetPrototype? preset))
            return preset;

        return ResolveByAlias(typedPresetId.ToString());
    }

    public AnnouncementPresetPrototype? Resolve(string? presetId)
    {
        if (string.IsNullOrEmpty(presetId))
            return null;

        if (_prototypes.TryIndex<AnnouncementPresetPrototype>(presetId, out var preset))
            return preset;

        return ResolveByAlias(presetId);
    }

    private AnnouncementPresetPrototype? ResolveByAlias(string alias)
    {
        return _aliasIndex.TryGetValue(alias, out var preset) ? preset : null;
    }

    private Dictionary<string, AnnouncementPresetPrototype> BuildAliasIndex()
    {
        var index = new Dictionary<string, AnnouncementPresetPrototype>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in _prototypes.EnumeratePrototypes<AnnouncementPresetPrototype>())
        {
            foreach (var alias in preset.Aliases)
            {
                index.TryAdd(alias, preset);
            }
        }
        return index;
    }
}
