using System;
using System.Numerics;
using Content.Shared._RMC14.Announce;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class SlideAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.SlideTimer = 0f;
        context.State.CurrentSlideOffset = context.State.SlideStartPosition;
    }

    public bool Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationEnhancements;
        if (enhancements?.EnableSlide != true)
            return true;

        context.State.SlideTimer += deltaTime;
        var duration = enhancements.SlideDuration;
        var progress = Math.Min(context.State.SlideTimer / duration, 1.0f);

        var currentOffset = Vector2.Lerp(context.State.SlideStartPosition, Vector2.Zero, progress);
        context.State.CurrentSlideOffset = currentOffset;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return true;
        }

        return false;
    }
}
