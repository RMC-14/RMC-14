using System;
using System.Numerics;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class BounceAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.BounceTimer = 0f;
        context.State.BouncePhase = 0;
        context.State.CurrentBounceOffset = Vector2.Zero;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationConfig.AnimationEnhancements;
        var bounceCount = enhancements?.BounceCount ?? 3;
        var bounceHeight = enhancements?.BounceHeight ?? 15f;
        var totalPhases = bounceCount * 2;

        if (context.State.BouncePhase >= totalPhases)
        {
            context.State.CurrentBounceOffset = Vector2.Zero;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        context.State.BounceTimer += deltaTime * 4f;
        var bounceProgress = context.State.BounceTimer % 1f;
        var bounceY = MathF.Sin(bounceProgress * MathF.PI) * bounceHeight * (1f - context.State.BouncePhase * 0.3f);
        context.State.CurrentBounceOffset = new Vector2(0, -bounceY);

        if (context.State.BounceTimer >= 1f)
        {
            context.State.BounceTimer = 0f;
            context.State.BouncePhase++;
        }

        return AnnouncementAnimationStatus.Running;
    }
}

