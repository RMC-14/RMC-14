using System;
using System.Numerics;
using Content.Shared._RMC14.Announce;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class SlideAnimation : IAnnouncementAnimation
{
    private float _timer;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        context.Output.CurrentSlideOffset = context.Output.SlideStartPosition;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationConfig.AnimationEnhancements;
        _timer += deltaTime;
        var duration = enhancements?.SlideDuration ?? 1.0f;
        if (duration <= 0f)
        {
            context.Output.CurrentSlideOffset = Vector2.Zero;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        var progress = Math.Min(_timer / duration, 1.0f);

        var currentOffset = Vector2.Lerp(context.Output.SlideStartPosition, Vector2.Zero, progress);
        context.Output.CurrentSlideOffset = currentOffset;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
