using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce.Animations;

[Serializable, NetSerializable]
public sealed partial class FadeAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public float Duration { get; set; } = 2.0f;
}
