using Content.Shared._RMC14.Announce;
using System.Collections.Generic;

namespace Content.Client._RMC14.Announce.Effects;

public static class AnnouncementEffectsRegistry
{
    public static IEnumerable<IAnnouncementVisualEffect> BuildEffects(AnnouncementStyle style)
    {
        if (style.SpriteGlow)
            yield return new GlowEffect();

        if (style.FlickerChance > 0)
            yield return new FlickerEffect();

        if (style.Animation == AnnouncementAnimation.Fade)
            yield return new FadeEffect();

        if (style.Animation == AnnouncementAnimation.Pulse)
            yield return new PulseEffect();
    }
}
