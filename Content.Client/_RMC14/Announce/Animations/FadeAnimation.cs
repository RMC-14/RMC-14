using System;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class FadeAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.FadeTimer = 0f;
        context.State.FadeAlpha = 0f;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        const float duration = 2.0f;
        context.State.FadeTimer += deltaTime;

        var progress = Math.Min(context.State.FadeTimer / duration, 1.0f);
        context.State.FadeAlpha = progress;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
