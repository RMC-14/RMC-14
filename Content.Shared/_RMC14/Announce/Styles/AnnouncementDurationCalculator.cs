namespace Content.Shared._RMC14.Announce;

public static class AnnouncementDurationCalculator
{
    public static float Calculate(AnnouncementStyle style, string[]? lines = null)
    {
        return style.AnimationConfig.Animation switch
        {
            AnnouncementAnimation.Typewriter => CalculateCharacterDuration(style, lines, speedMultiplier: 1.0f),
            AnnouncementAnimation.Glitch => CalculateCharacterDuration(style, lines, speedMultiplier: 0.6f),
            AnnouncementAnimation.Slide => style.AnimationConfig.AnimationEnhancements?.SlideDuration ?? 1.0f,
            AnnouncementAnimation.Zoom => style.AnimationConfig.AnimationEnhancements?.ZoomDuration ?? 1.0f,
            AnnouncementAnimation.Bounce => (style.AnimationConfig.AnimationEnhancements?.BounceCount ?? 3) * 0.5f,
            AnnouncementAnimation.Fade => 2.0f,
            AnnouncementAnimation.Pulse => 1.0f,
            _ => 0.0f
        };
    }

    private static float CalculateCharacterDuration(AnnouncementStyle style, string[]? lines, float speedMultiplier)
    {
        if (lines == null || lines.Length == 0)
            return 1.0f;

        var totalChars = 0;
        foreach (var line in lines)
            totalChars += line.Length;

        return totalChars * style.AnimationConfig.PrintSpeed * speedMultiplier + 0.5f;
    }
}

