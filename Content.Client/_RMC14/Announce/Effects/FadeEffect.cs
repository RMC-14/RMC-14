using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class FadeEffect : IAnnouncementVisualEffect
{
    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        foreach (var label in context.Labels)
        {
            var baseColor = label.Modulate;
            var alpha = baseColor.A * context.State.FadeAlpha;
            label.Modulate = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
        }
    }
}
