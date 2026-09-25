using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementStyle
{
    [DataField]
    private AnnouncementAnimationConfig animation = new();

    [DataField]
    private AnnouncementLayoutConfig layout = new();

    [DataField]
    private AnnouncementBackgroundConfig background = new();

    [DataField]
    private AnnouncementTextConfig text = new();

    [DataField]
    private AnnouncementSpriteConfig sprite = new();

    [DataField]
    private AnnouncementTitleConfig title = new();

    [DataField]
    private AnnouncementScalingConfig scaling = new();

    public AnnouncementAnimationConfig AnimationConfig => animation ??= new AnnouncementAnimationConfig();
    public AnnouncementLayoutConfig LayoutConfig => layout ??= new AnnouncementLayoutConfig();
    public AnnouncementBackgroundConfig BackgroundConfig => background ??= new AnnouncementBackgroundConfig();
    public AnnouncementTextConfig TextConfig => text ??= new AnnouncementTextConfig();
    public AnnouncementSpriteConfig SpriteConfig => sprite ??= new AnnouncementSpriteConfig();
    public AnnouncementTitleConfig TitleConfig => title ??= new AnnouncementTitleConfig();
    public AnnouncementScalingConfig ScalingConfig => scaling ??= new AnnouncementScalingConfig();
}
