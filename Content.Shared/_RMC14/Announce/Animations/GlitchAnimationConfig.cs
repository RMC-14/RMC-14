namespace Content.Shared._RMC14.Announce.Animations;

public sealed partial class GlitchAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public TimeSpan PrintSpeed = TimeSpan.FromSeconds(0.03);

    [DataField]
    public float GlitchChance = 0.005f;
}
