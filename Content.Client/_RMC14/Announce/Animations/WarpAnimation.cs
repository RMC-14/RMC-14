using System;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class WarpAnimation : IAnnouncementAnimation
{
    private float _timer;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        ResetLabelMargins(context);
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        const float waveSpeed = 2.5f;
        const float amplitude = 3.5f;
        const float linePhase = 0.6f;

        _timer += deltaTime * waveSpeed;
        var t = _timer;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            var lineIndex = i - context.TitleOffset;
            var phase = t + lineIndex * linePhase;
            var x = MathF.Sin(phase) * amplitude;
            var y = MathF.Cos(phase * 0.8f) * (amplitude * 0.6f);
            context.Labels[i].Margin = new Thickness(x, y, 0, 0);
        }

        return AnnouncementAnimationStatus.Hold;
    }

    private static void ResetLabelMargins(AnnouncementAnimationContext context)
    {
        for (var i = 0; i < context.Labels.Length; i++)
        {
            context.Labels[i].Margin = new Thickness(0);
        }
    }
}
