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
            FadeAnimationConfig c => new FadeAnimation(c),
            _ => new NoneAnimation()
        };
    }
}
