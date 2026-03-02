using System;
namespace Content.Client._RMC14.Announce.Animations;

public sealed class HeartbeatAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.PulseTimer = 0f;
        context.State.PulseScale = 1f;
        context.State.PulseAlpha = 1f;
        ResetLabelMargins(context);
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        const float beatInterval = 1.2f;
        const float firstBeatOffset = 0.0f;
        const float secondBeatOffset = 0.25f;
        const float beatWidth = 0.18f;
        const float beatScaleIntensity = 0.35f;
        const float beatAlphaIntensity = 0.35f;

        context.State.PulseTimer += deltaTime;
        var cycleTime = context.State.PulseTimer % beatInterval;

        var firstBeat = MathF.Exp(-MathF.Pow((cycleTime - firstBeatOffset) / beatWidth, 2));
        var secondBeat = MathF.Exp(-MathF.Pow((cycleTime - secondBeatOffset) / beatWidth, 2));
        var pulse = MathF.Min(1f, firstBeat + secondBeat);

        context.State.PulseScale = 1.0f + pulse * beatScaleIntensity;
        context.State.PulseAlpha = 0.65f + pulse * beatAlphaIntensity;

        ApplyLineOffsets(context, cycleTime, beatInterval);

        context.SetAllLabels();
        return AnnouncementAnimationStatus.Hold;
    }

    private static void ApplyLineOffsets(AnnouncementAnimationContext context, float cycleTime, float beatInterval)
    {
        var wobble = MathF.Sin(cycleTime / beatInterval * MathF.Tau) * 1.5f;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            var lineIndex = i - context.TitleOffset;
            var phase = lineIndex * 0.8f;
            var x = MathF.Sin(cycleTime * 4f + phase) * 1.5f;
            var y = wobble;
            context.Labels[i].Margin = new Robust.Shared.Maths.Thickness(x, y, 0, 0);
        }
    }

    private static void ResetLabelMargins(AnnouncementAnimationContext context)
    {
        for (var i = 0; i < context.Labels.Length; i++)
        {
            context.Labels[i].Margin = new Thickness(0);
        }
    }
}
