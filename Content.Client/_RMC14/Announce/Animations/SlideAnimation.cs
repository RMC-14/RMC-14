using System.Numerics;
using Content.Shared._RMC14.Announce.Animations;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class SlideAnimation : IAnnouncementAnimation
{
    private readonly SlideAnimationConfig _config;
    private float _timer;

    public SlideAnimation(SlideAnimationConfig config) => _config = config;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        context.Output.CurrentSlideOffset = context.Output.SlideStartPosition;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        _timer += deltaTime;
        var duration = _config.Duration;
        if (duration <= 0f)
        {
            context.Output.CurrentSlideOffset = Vector2.Zero;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        var progress = Math.Min(_timer / duration, 1.0f);
        context.Output.CurrentSlideOffset = Vector2.Lerp(context.Output.SlideStartPosition, Vector2.Zero, progress);

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
