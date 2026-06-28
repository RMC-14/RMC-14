using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce.Animations;

[Serializable, NetSerializable]
public sealed partial class SlideAnimationConfig : IAnnouncementAnimationConfig
{
    [DataField]
    public float Duration { get; set; } = 1.0f;

    [DataField]
    public SlideDirection From { get; set; } = SlideDirection.Top;
}
