using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class GlowEffect : IAnnouncementVisualEffect
{
    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        var time = (float) currentTime.TotalSeconds;
        var intensity = context.Style.SpriteConfig.SpriteGlowIntensity;
        var glowColor = context.Style.SpriteConfig.SpriteGlowColor;

        foreach (var label in context.Labels)
        {
            var baseColor = label.Modulate;
            var glow = MathF.Sin(time * 3f) * 0.5f + 0.5f;
            var glowFactor = 1.0f + (glow * intensity);

            label.Modulate = new Color(
                Math.Min(baseColor.R * glowFactor * glowColor.R, 1.0f),
                Math.Min(baseColor.G * glowFactor * glowColor.G, 1.0f),
                Math.Min(baseColor.B * glowFactor * glowColor.B, 1.0f),
                baseColor.A);
        }
    }
}

