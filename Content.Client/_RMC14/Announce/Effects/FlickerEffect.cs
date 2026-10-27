using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class FlickerEffect : IAnnouncementVisualEffect
{
    private readonly Random _random = new();

    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        var noise = _random.NextDouble();
        foreach (var label in context.Labels)
        {
            var baseColor = label.Modulate;
            if (noise < context.Style.AnimationConfig.FlickerChance)
            {
                baseColor = new Color(
                    baseColor.R * 0.3f,
                    baseColor.G * 0.3f,
                    baseColor.B * 0.3f,
                    baseColor.A);
            }

            label.Modulate = baseColor;
        }
    }
}

