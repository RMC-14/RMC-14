using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce.Animations;

[Serializable, NetSerializable]
public sealed partial class GlitchAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public float PrintSpeed { get; set; } = 0.03f;

    [DataField]
    public float GlitchChance { get; set; } = 0.005f;
}
