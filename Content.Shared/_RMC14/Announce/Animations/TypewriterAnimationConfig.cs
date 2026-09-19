using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce.Animations;

[Serializable, NetSerializable]
public sealed partial class TypewriterAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public float PrintSpeed { get; set; } = 0.03f;
}
