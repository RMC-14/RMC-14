using System;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class PulseAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.PulseTimer = 0f;
        context.State.PulseScale = 1f;
        context.State.PulseAlpha = 1f;
    }

    public bool Update(AnnouncementAnimationContext context, float deltaTime)
    {
        const float pulseSpeed = 2.0f;
        const float pulseIntensity = 0.3f;

        context.State.PulseTimer += deltaTime * pulseSpeed;
        var pulseValue = MathF.Sin(context.State.PulseTimer);
        context.State.PulseScale = 1.0f + (pulseValue * pulseIntensity);
        context.State.PulseAlpha = 0.7f + (pulseValue * 0.3f);

        context.SetAllLabels();
        return true;
    }
}
