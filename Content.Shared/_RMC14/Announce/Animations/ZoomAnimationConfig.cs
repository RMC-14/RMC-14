using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce.Animations;

[Serializable, NetSerializable]
public sealed partial class ZoomAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public float StartScale { get; set; } = 0.1f;

    [DataField]
    public float Duration { get; set; } = 1.0f;
}
