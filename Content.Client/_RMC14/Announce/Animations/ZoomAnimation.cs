using Content.Shared._RMC14.Announce.Animations;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class ZoomAnimation : IAnnouncementAnimation
{
    private readonly ZoomAnimationConfig _config;
    private float _timer;

    public ZoomAnimation(ZoomAnimationConfig config) => _config = config;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        _timer += deltaTime;
        var duration = _config.Duration;
        if (duration <= 0f)
        {
            context.Output.ZoomCurrentScale = 1f;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        var progress = Math.Min(_timer / duration, 1.0f);
        context.Output.ZoomCurrentScale = _config.StartScale + (1.0f - _config.StartScale) * progress;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
