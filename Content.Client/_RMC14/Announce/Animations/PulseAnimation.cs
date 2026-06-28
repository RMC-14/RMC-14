namespace Content.Client._RMC14.Announce.Animations;

public sealed class PulseAnimation : IAnnouncementAnimation
{
    private float _timer;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        context.Output.PulseAlpha = 1f;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        const float pulseSpeed = 4.0f;

        _timer += deltaTime * pulseSpeed;
        var pulseValue = MathF.Sin(_timer);
        context.Output.PulseAlpha = 0.7f + (pulseValue * 0.3f);

        context.SetAllLabels();
        return AnnouncementAnimationStatus.Hold;
    }
}
