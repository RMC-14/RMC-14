namespace Content.Shared._RMC14.Announce;

public static class AnnouncementDurationCalculator
{
    public static float Calculate(AnnouncementStyle style)
    {
        return style.AnimationConfig.Animation switch
        {
            AnnouncementAnimation.Typewriter => 5.0f,
            AnnouncementAnimation.Slide => style.AnimationConfig.AnimationEnhancements?.SlideDuration ?? 1.0f,
            AnnouncementAnimation.Zoom => style.AnimationConfig.AnimationEnhancements?.ZoomDuration ?? 1.0f,
            AnnouncementAnimation.Bounce => (style.AnimationConfig.AnimationEnhancements?.BounceCount ?? 3) * 0.5f,
            AnnouncementAnimation.Fade => 2.0f,
            AnnouncementAnimation.Pulse => 1.0f,
            AnnouncementAnimation.Glitch => 3.0f,
            _ => 1.0f
        };
    }
}

