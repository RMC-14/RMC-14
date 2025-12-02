namespace Content.Shared._RMC14.Announce;

public static class AnnouncementDurationCalculator
{
    public static float Calculate(AnnouncementStyle style)
    {
        return style.Animation switch
        {
            AnnouncementAnimation.Typewriter => 5.0f,
            AnnouncementAnimation.Slide => style.AnimationEnhancements?.SlideDuration ?? 1.0f,
            AnnouncementAnimation.Zoom => style.AnimationEnhancements?.ZoomDuration ?? 1.0f,
            AnnouncementAnimation.Bounce => (style.AnimationEnhancements?.BounceCount ?? 3) * 0.5f,
            AnnouncementAnimation.Fade => 2.0f,
            AnnouncementAnimation.Pulse => 1.0f,
            AnnouncementAnimation.Glitch => 3.0f,
            _ => 1.0f
        };
    }
}
