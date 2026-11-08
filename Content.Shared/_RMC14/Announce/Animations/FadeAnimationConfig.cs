namespace Content.Shared._RMC14.Announce.Animations;

public sealed partial class FadeAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);
}
