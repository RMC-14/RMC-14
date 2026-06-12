using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce;

public static class AnnouncementLayoutResolver
{
    public static void Apply(AnnouncementDisplayData display, AnnouncementLayoutOverride? layout)
    {
        if (layout is not { } resolved)
            return;

        var clamped = resolved.Clamp();
        display.ScreenPositionOverride = clamped.ScreenPosition;
        display.LayoutScale = clamped.Scale;
        display.ShowTitleOverride = clamped.ShowTitle;
        display.ShowSpriteOverride = display.SupportsSpriteCardOverride ? clamped.ShowSprite : null;
        display.TextColorOverride = ParseColor(clamped.TextColor);
        display.TitleColorOverride = ParseColor(clamped.TitleColor);
        display.BodyTextScaleOverride = clamped.BodyTextScale;
        display.TitleTextScaleOverride = clamped.TitleTextScale;
    }

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        return Color.TryFromHex(hex.Trim());
    }
}
