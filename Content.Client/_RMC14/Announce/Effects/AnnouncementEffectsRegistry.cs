using Content.Shared._RMC14.Announce;
using System.Collections.Generic;

namespace Content.Client._RMC14.Announce.Effects;

public static class AnnouncementEffectsRegistry
{
    public static IEnumerable<IAnnouncementVisualEffect> BuildEffects(AnnouncementStyle style)
    {
        if (style.SpriteConfig.SpriteGlow)
            yield return new GlowEffect();

        if (style.AnimationConfig.FlickerChance > 0)
            yield return new FlickerEffect();

        if (style.AnimationConfig.Animation == AnnouncementAnimation.Fade)
            yield return new FadeEffect();

        if (style.AnimationConfig.Animation == AnnouncementAnimation.Pulse || style.AnimationConfig.Animation == AnnouncementAnimation.Heartbeat)
            yield return new PulseEffect();

        if (style.TitleConfig.Effect.Type == AnnouncementTitleEffectType.AssaultPulse)
            yield return new TitleAssaultPulseEffect();

        if (style.TitleConfig.Effect.Type == AnnouncementTitleEffectType.AssaultScroll)
            yield return new TitleAssaultScrollEffect();
    }
}

