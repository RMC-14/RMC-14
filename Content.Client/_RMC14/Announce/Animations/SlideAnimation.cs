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

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationConfig.AnimationEnhancements;
        context.State.SlideTimer += deltaTime;
        var duration = enhancements?.SlideDuration ?? 1.0f;
        if (duration <= 0f)
        {
            context.State.CurrentSlideOffset = Vector2.Zero;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        var progress = Math.Min(context.State.SlideTimer / duration, 1.0f);

        var currentOffset = Vector2.Lerp(context.State.SlideStartPosition, Vector2.Zero, progress);
        context.State.CurrentSlideOffset = currentOffset;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}

