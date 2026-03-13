using System;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class TitleAssaultPulseEffect : IAnnouncementVisualEffect
{
    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        if (!context.HasTitle || context.Labels.Count == 0)
            return;

        var titleLabel = context.Labels[0];
        var titleColor = context.Style.TitleConfig.TitleColor;

        var t = (float) currentTime.TotalSeconds;
        var pulse = 0.5f + 0.5f * MathF.Sin(t * 7.5f);
        var attack = 0.82f + pulse * 0.18f;
        var alpha = 0.78f + pulse * 0.22f;
        var yOffset = -1f - pulse;

        titleLabel.Modulate = new Color(
            titleColor.R * attack,
            titleColor.G * attack,
            titleColor.B * attack,
            alpha);
        titleLabel.Margin = new Thickness(0, yOffset, 0, 0);
    }
}
