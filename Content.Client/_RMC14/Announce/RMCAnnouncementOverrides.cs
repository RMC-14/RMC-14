using System.Globalization;
using System.Numerics;
using System.Text;

namespace Content.Client._RMC14.Announce;

public readonly record struct RMCAnnouncementOverride(
    Vector2? Pivot = null,
    float? Scale = null,
    float? BodyScale = null,
    float? TitleScale = null,
    bool? ShowTitle = null,
    bool? ShowPortrait = null,
    bool? Show = null,
    Color? TextColor = null,
    Color? TitleColor = null,
    Color? BackgroundColor = null,
    Color? PortraitBackgroundColor = null,
    Color? PortraitBorderColor = null,
    Color? CrtGlowColor = null)
{
    public bool IsEmpty => this == default;

    public bool HasLayout => this with { Show = null } != default;

    public RMCAnnouncementOverride WithoutLayout()
    {
        return new RMCAnnouncementOverride(Show: Show);
    }

    public RMCAnnouncementOverride Over(RMCAnnouncementOverride fallback)
    {
        return new RMCAnnouncementOverride(
            Pivot ?? fallback.Pivot,
            Scale ?? fallback.Scale,
            BodyScale ?? fallback.BodyScale,
            TitleScale ?? fallback.TitleScale,
            ShowTitle ?? fallback.ShowTitle,
            ShowPortrait ?? fallback.ShowPortrait,
            Show ?? fallback.Show,
            TextColor ?? fallback.TextColor,
            TitleColor ?? fallback.TitleColor,
            BackgroundColor ?? fallback.BackgroundColor,
            PortraitBackgroundColor ?? fallback.PortraitBackgroundColor,
            PortraitBorderColor ?? fallback.PortraitBorderColor,
            CrtGlowColor ?? fallback.CrtGlowColor);
    }
}

public static class RMCAnnouncementOverrides
{
    public const string GlobalKey = "*";
    public const float MinScale = 0.1f;
    public const float MaxScale = 2.5f;

    public static RMCAnnouncementOverride Resolve(
        Dictionary<string, RMCAnnouncementOverride> overrides,
        string presetId,
        string groupId)
    {
        var global = overrides.GetValueOrDefault(GlobalKey);
        var group = overrides.GetValueOrDefault(groupId).Over(global);
        return presetId == groupId ? group : overrides.GetValueOrDefault(presetId).Over(group);
    }

    public static Dictionary<string, RMCAnnouncementOverride> Parse(string serialized)
    {
        var overrides = new Dictionary<string, RMCAnnouncementOverride>();
        foreach (var entry in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf('=');
            if (separator <= 0)
                continue;

            var id = entry[..separator].Trim();
            var fields = new Dictionary<string, string>();
            foreach (var field in entry[(separator + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var colon = field.IndexOf(':');
                if (colon > 0)
                    fields[field[..colon]] = field[(colon + 1)..];
            }

            Vector2? pivot = null;
            if (TryGetFloat(fields, "x", out var x) && TryGetFloat(fields, "y", out var y))
                pivot = Vector2.Clamp(new Vector2(x, y), Vector2.Zero, Vector2.One);

            var parsed = new RMCAnnouncementOverride(
                pivot,
                GetScale(fields, "scale"),
                GetScale(fields, "body"),
                GetScale(fields, "title"),
                GetBool(fields, "showTitle"),
                GetBool(fields, "showPortrait"),
                GetBool(fields, "show"),
                GetColor(fields, "text"),
                GetColor(fields, "titleColor"),
                GetColor(fields, "background"),
                GetColor(fields, "portraitBackground"),
                GetColor(fields, "portraitBorder"),
                GetColor(fields, "glow"));

            if (!parsed.IsEmpty)
                overrides[id] = parsed;
        }

        return overrides;
    }

    public static string Serialize(Dictionary<string, RMCAnnouncementOverride> overrides)
    {
        var builder = new StringBuilder();
        foreach (var (id, value) in overrides)
        {
            if (value.IsEmpty)
                continue;

            if (builder.Length > 0)
                builder.Append(';');

            builder.Append(id);
            builder.Append('=');

            var fields = new List<string>();
            if (value.Pivot is { } pivot)
            {
                fields.Add(Field("x", FormatFloat(pivot.X)));
                fields.Add(Field("y", FormatFloat(pivot.Y)));
            }

            AddFloat(fields, "scale", value.Scale);
            AddFloat(fields, "body", value.BodyScale);
            AddFloat(fields, "title", value.TitleScale);
            AddBool(fields, "showTitle", value.ShowTitle);
            AddBool(fields, "showPortrait", value.ShowPortrait);
            AddBool(fields, "show", value.Show);
            AddColor(fields, "text", value.TextColor);
            AddColor(fields, "titleColor", value.TitleColor);
            AddColor(fields, "background", value.BackgroundColor);
            AddColor(fields, "portraitBackground", value.PortraitBackgroundColor);
            AddColor(fields, "portraitBorder", value.PortraitBorderColor);
            AddColor(fields, "glow", value.CrtGlowColor);

            builder.Append(string.Join(',', fields));
        }

        return builder.ToString();
    }

    private static float? GetScale(Dictionary<string, string> fields, string key)
    {
        return TryGetFloat(fields, key, out var value) ? Math.Clamp(value, MinScale, MaxScale) : null;
    }

    private static bool? GetBool(Dictionary<string, string> fields, string key)
    {
        return fields.TryGetValue(key, out var text) ? text == "1" : null;
    }

    private static Color? GetColor(Dictionary<string, string> fields, string key)
    {
        return fields.TryGetValue(key, out var text) ? Color.TryFromHex(text) : null;
    }

    private static bool TryGetFloat(Dictionary<string, string> fields, string key, out float value)
    {
        value = 0;
        return fields.TryGetValue(key, out var text) &&
               float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static void AddFloat(List<string> fields, string key, float? value)
    {
        if (value is { } number)
            fields.Add(Field(key, FormatFloat(number)));
    }

    private static void AddBool(List<string> fields, string key, bool? value)
    {
        if (value is { } flag)
            fields.Add(Field(key, flag ? "1" : "0"));
    }

    private static void AddColor(List<string> fields, string key, Color? value)
    {
        if (value is { } color)
            fields.Add(Field(key, color.ToHex()));
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Field(string key, string value)
    {
        return string.Concat(key, ":", value);
    }
}
