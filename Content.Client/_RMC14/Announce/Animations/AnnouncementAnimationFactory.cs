using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Announce.Animations;

namespace Content.Client._RMC14.Announce.Animations;

public static class AnnouncementAnimationFactory
{
    public static IAnnouncementAnimation Create(AnnouncementStyle style)
    {
        return style.AnimationConfig.Animation switch
        {
            TypewriterAnimationConfig c => new TypewriterAnimation(c),
            GlitchAnimationConfig c => new GlitchAnimation(c),
            SlideAnimationConfig c => new SlideAnimation(c),
            ZoomAnimationConfig c => new ZoomAnimation(c),
            BounceAnimationConfig c => new BounceAnimation(c),
            FadeAnimationConfig c => new FadeAnimation(c),
            PulseAnimationConfig => new PulseAnimation(),
            HeartbeatAnimationConfig => new HeartbeatAnimation(),
            WarpAnimationConfig => new WarpAnimation(),
            _ => new NoneAnimation()
        };
    }
}
