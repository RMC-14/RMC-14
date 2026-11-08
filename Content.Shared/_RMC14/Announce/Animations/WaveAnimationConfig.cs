namespace Content.Shared._RMC14.Announce.Animations;

public sealed partial class WaveAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public TimeSpan PrintSpeed = TimeSpan.FromSeconds(0.03);

    [DataField]
    public int FadeChars = 6;
}
