using System;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class FadeAnimation : IAnnouncementAnimation
{
    private float _timer;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        context.Output.FadeAlpha = 0f;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        const float duration = 2.0f;
        _timer += deltaTime;

        var progress = Math.Min(_timer / duration, 1.0f);
        context.Output.FadeAlpha = progress;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
