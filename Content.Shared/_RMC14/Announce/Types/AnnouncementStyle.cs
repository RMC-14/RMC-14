using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementStyle : ISerializationHooks, IRobustCloneable<AnnouncementStyle>
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

    public AnnouncementAnimationConfig AnimationConfig => animation;
    public AnnouncementLayoutConfig LayoutConfig => layout;
    public AnnouncementBackgroundConfig BackgroundConfig => background;
    public AnnouncementTextConfig TextConfig => text;
    public AnnouncementSpriteConfig SpriteConfig => sprite;
    public AnnouncementTitleConfig TitleConfig => title;
    public AnnouncementScalingConfig ScalingConfig => scaling;

    public AnnouncementStyle Clone()
    {
        return new AnnouncementStyle
        {
            animation = animation.Clone(),
            layout = layout.Clone(),
            background = background.Clone(),
            text = text.Clone(),
            sprite = sprite.Clone(),
            title = title.Clone(),
            scaling = scaling.Clone(),
        };
    }

    public void ValidateAndNormalize()
    {
        animation ??= new AnnouncementAnimationConfig();
        layout ??= new AnnouncementLayoutConfig();
        background ??= new AnnouncementBackgroundConfig();
        text ??= new AnnouncementTextConfig();
        sprite ??= new AnnouncementSpriteConfig();
        title ??= new AnnouncementTitleConfig();
        scaling ??= new AnnouncementScalingConfig();

        animation.ValidateAndNormalize();
        layout.ValidateAndNormalize();
        background.ValidateAndNormalize();
        text.ValidateAndNormalize();
        sprite.ValidateAndNormalize();
        title.ValidateAndNormalize();
        scaling.ValidateAndNormalize();
    }

    void ISerializationHooks.AfterDeserialization()
    {
        ValidateAndNormalize();
    }
}
