using System;
using System.Collections.Generic;
using Content.Shared._RMC14.Announce;

namespace Content.Client._RMC14.Announce;

public static class AnnouncementPreferenceOverrides
{
    public static Dictionary<string, AnnouncementDisplayPreference> Parse(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
            return new Dictionary<string, AnnouncementDisplayPreference>();

        var parsed = new Dictionary<string, AnnouncementDisplayPreference>();
        var entries = serialized.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            var separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1)
                continue;

            var key = entry[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var valueText = entry[(separatorIndex + 1)..].Trim();
            if (!int.TryParse(valueText, out var valueRaw))
                continue;

            if (!Enum.IsDefined(typeof(AnnouncementDisplayPreference), valueRaw))
                continue;

            parsed[key] = (AnnouncementDisplayPreference) valueRaw;
        }

        return parsed;
    }

    public static string Serialize(IReadOnlyDictionary<string, AnnouncementDisplayPreference> overrides)
    {
        if (overrides.Count == 0)
            return string.Empty;

        var entries = new List<KeyValuePair<string, AnnouncementDisplayPreference>>();
        foreach (var (key, value) in overrides)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            entries.Add(new KeyValuePair<string, AnnouncementDisplayPreference>(key, value));
        }

        if (entries.Count == 0)
            return string.Empty;

        entries.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));

        var serialized = new System.Text.StringBuilder();
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0)
                serialized.Append(';');

            serialized.Append(entries[i].Key);
            serialized.Append('=');
            serialized.Append((int) entries[i].Value);
        }

        return serialized.ToString();
    }
}
