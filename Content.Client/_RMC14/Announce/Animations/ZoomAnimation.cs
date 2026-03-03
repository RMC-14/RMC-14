using System;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class ZoomAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.ZoomTimer = 0f;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationConfig.AnimationEnhancements;
        context.State.ZoomTimer += deltaTime;
        var duration = enhancements?.ZoomDuration ?? 1.0f;
        if (duration <= 0f)
        {
            context.State.ZoomCurrentScale = 1f;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        var progress = Math.Min(context.State.ZoomTimer / duration, 1.0f);

        var startScale = enhancements?.ZoomStartScale ?? 0.1f;
        var currentScale = startScale + (1.0f - startScale) * progress;
        context.State.ZoomCurrentScale = currentScale;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}

