using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce.Animations;

[Serializable, NetSerializable]
public sealed partial class BounceAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public int BounceCount { get; set; } = 3;

    [DataField]
    public float BounceHeight { get; set; } = 15f;
}
