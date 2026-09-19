using System.Globalization;
using System.Numerics;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce;

public readonly record struct AnnouncementLayoutOverride(
    Vector2 ScreenPosition,
    float Scale,
    bool? ShowTitle = null,
    bool? ShowSprite = null,
    string? TextColor = null,
    string? TitleColor = null,
    float? BodyTextScale = null,
    float? TitleTextScale = null,
    string? SpriteBoxColor = null,
    string? SpriteBoxBorderColor = null,
    string? CRTGlowColor = null,
    string? BackgroundColor = null)
{
    public AnnouncementLayoutOverride Clamp()
    {
        return new AnnouncementLayoutOverride(
            new Vector2(
                Math.Clamp(ScreenPosition.X, 0f, 1f),
                Math.Clamp(ScreenPosition.Y, 0f, 1f)),
            Math.Clamp(Scale, 0.1f, 2.5f),
            ShowTitle,
            ShowSprite,
            NormalizeColor(TextColor),
            NormalizeColor(TitleColor),
            NormalizeScale(BodyTextScale),
            NormalizeScale(TitleTextScale),
            NormalizeColor(SpriteBoxColor),
            NormalizeColor(SpriteBoxBorderColor),
            NormalizeColor(CRTGlowColor),
            NormalizeColor(BackgroundColor));
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return Color.TryFromHex(trimmed) != null ? trimmed : null;
    }

    private static float? NormalizeScale(float? value)
    {
        if (value == null)
            return null;

        return Math.Clamp(value.Value, 0.1f, 2.5f);
    }
}

public static class AnnouncementLayoutOverrides
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static AnnouncementLayoutOverride? ParseSingle(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
            return null;

        if (!TryParseLayout(serialized, out var layout))
            return null;

        return layout;
    }

    public static string SerializeSingle(AnnouncementLayoutOverride? layout)
    {
        return layout is { } value
            ? SerializeLayout(value)
            : string.Empty;
    }

    public static Dictionary<string, AnnouncementLayoutOverride> Parse(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
            return new Dictionary<string, AnnouncementLayoutOverride>();

        var parsed = new Dictionary<string, AnnouncementLayoutOverride>();
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
            if (!TryParseLayout(valueText, out var layout))
                continue;

            parsed[key] = layout;
        }

        return parsed;
    }

    public static string Serialize(IReadOnlyDictionary<string, AnnouncementLayoutOverride> overrides)
    {
        if (overrides.Count == 0)
            return string.Empty;

        var entries = new List<KeyValuePair<string, AnnouncementLayoutOverride>>();
        foreach (var (key, value) in overrides)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            entries.Add(new KeyValuePair<string, AnnouncementLayoutOverride>(key, value));
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
            serialized.Append(SerializeLayout(entries[i].Value));
        }

        return serialized.ToString();
    }

    private static bool TryParseLayout(string serialized, out AnnouncementLayoutOverride layout)
    {
        layout = default;

        var parts = serialized.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 3 && parts.Length != 7 && parts.Length != 9 && parts.Length != 13)
            return false;

        if (!float.TryParse(parts[0], NumberStyles.Float, Culture, out var x) ||
            !float.TryParse(parts[1], NumberStyles.Float, Culture, out var y) ||
            !float.TryParse(parts[2], NumberStyles.Float, Culture, out var scale))
        {
            return false;
        }

        bool? showTitle = null;
        bool? showSprite = null;
        string? textColor = null;
        string? titleColor = null;
        float? bodyTextScale = null;
        float? titleTextScale = null;
        string? spriteBoxColor = null;
        string? spriteBoxBorderColor = null;
        string? crtGlowColor = null;
        string? backgroundColor = null;
        if (parts.Length >= 7)
        {
            showTitle = ParseOptionalBool(parts[3]);
            showSprite = ParseOptionalBool(parts[4]);
            textColor = ParseOptionalColor(parts[5]);
            titleColor = ParseOptionalColor(parts[6]);
        }

        if (parts.Length >= 9)
        {
            bodyTextScale = ParseOptionalScale(parts[7]);
            titleTextScale = ParseOptionalScale(parts[8]);
        }

        if (parts.Length >= 13)
        {
            spriteBoxColor = ParseOptionalColor(parts[9]);
            spriteBoxBorderColor = ParseOptionalColor(parts[10]);
            crtGlowColor = ParseOptionalColor(parts[11]);
            backgroundColor = ParseOptionalColor(parts[12]);
        }

        layout = new AnnouncementLayoutOverride(
            new Vector2(x, y),
            scale,
            showTitle,
            showSprite,
            textColor,
            titleColor,
            bodyTextScale,
            titleTextScale,
            spriteBoxColor,
            spriteBoxBorderColor,
            crtGlowColor,
            backgroundColor).Clamp();
        return true;
    }

    private static string SerializeLayout(AnnouncementLayoutOverride layout)
    {
        var clamped = layout.Clamp();
        return string.Concat(
            clamped.ScreenPosition.X.ToString("F4", Culture),
            ",",
            clamped.ScreenPosition.Y.ToString("F4", Culture),
            ",",
            clamped.Scale.ToString("F3", Culture),
            ",",
            SerializeOptionalBool(clamped.ShowTitle),
            ",",
            SerializeOptionalBool(clamped.ShowSprite),
            ",",
            clamped.TextColor ?? string.Empty,
            ",",
            clamped.TitleColor ?? string.Empty,
            ",",
            SerializeOptionalScale(clamped.BodyTextScale),
            ",",
            SerializeOptionalScale(clamped.TitleTextScale),
            ",",
            clamped.SpriteBoxColor ?? string.Empty,
            ",",
            clamped.SpriteBoxBorderColor ?? string.Empty,
            ",",
            clamped.CRTGlowColor ?? string.Empty,
            ",",
            clamped.BackgroundColor ?? string.Empty);
    }

    private static bool? ParseOptionalBool(string value)
    {
        return value.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ => null
        };
    }

    private static string SerializeOptionalBool(bool? value)
    {
        return value switch
        {
            true => "1",
            false => "0",
            null => string.Empty
        };
    }

    private static string? ParseOptionalColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return Color.TryFromHex(trimmed) != null ? trimmed : null;
    }

    private static float? ParseOptionalScale(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return float.TryParse(value.Trim(), NumberStyles.Float, Culture, out var scale)
            ? Math.Clamp(scale, 0.1f, 2.5f)
            : null;
    }

    private static string SerializeOptionalScale(float? value)
    {
        return value?.ToString("F3", Culture) ?? string.Empty;
    }

}
