namespace Content.Shared._RMC14.Announce.Animations;

public sealed partial class TypewriterAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public TimeSpan PrintSpeed = TimeSpan.FromSeconds(0.03);
}
