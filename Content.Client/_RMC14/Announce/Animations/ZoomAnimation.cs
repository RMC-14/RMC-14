using System;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class ZoomAnimation : IAnnouncementAnimation
{
    private float _timer;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationConfig.AnimationEnhancements;
        _timer += deltaTime;
        var duration = enhancements.ZoomDuration;
        if (duration <= 0f)
        {
            context.Output.ZoomCurrentScale = 1f;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        var progress = Math.Min(_timer / duration, 1.0f);

        var startScale = enhancements.ZoomStartScale;
        var currentScale = startScale + (1.0f - startScale) * progress;
        context.Output.ZoomCurrentScale = currentScale;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
