using System;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class ZoomAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.ZoomTimer = 0f;
    }

    public bool Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationEnhancements;
        if (enhancements?.EnableZoom != true)
            return true;

        context.State.ZoomTimer += deltaTime;
        var duration = enhancements.ZoomDuration;
        var progress = Math.Min(context.State.ZoomTimer / duration, 1.0f);

        var startScale = enhancements.ZoomStartScale;
        var currentScale = startScale + (1.0f - startScale) * progress;
        context.State.ZoomCurrentScale = currentScale;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return true;
        }

        return false;
    }
}
