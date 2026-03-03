using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementStyle : ISerializationHooks, IRobustCloneable<AnnouncementStyle>
{
    private AnnouncementAnimationConfig _animation = new();
    private AnnouncementLayoutConfig _layout = new();
    private AnnouncementBackgroundConfig _background = new();
    private AnnouncementTextConfig _text = new();
    private AnnouncementSpriteConfig _sprite = new();
    private AnnouncementTitleConfig _title = new();
    private AnnouncementScalingConfig _scaling = new();

    public AnnouncementAnimationConfig AnimationConfig => _animation;
    public AnnouncementLayoutConfig LayoutConfig => _layout;
    public AnnouncementBackgroundConfig BackgroundConfig => _background;
    public AnnouncementTextConfig TextConfig => _text;
    public AnnouncementSpriteConfig SpriteConfig => _sprite;
    public AnnouncementTitleConfig TitleConfig => _title;
    public AnnouncementScalingConfig ScalingConfig => _scaling;

    public AnnouncementStyle Clone()
    {
        return new AnnouncementStyle
        {
            _animation = _animation.Clone(),
            _layout = _layout.Clone(),
            _background = _background.Clone(),
            _text = _text.Clone(),
            _sprite = _sprite.Clone(),
            _title = _title.Clone(),
            _scaling = _scaling.Clone(),
        };
    }

    public void ValidateAndNormalize()
    {
        _animation ??= new AnnouncementAnimationConfig();
        _layout ??= new AnnouncementLayoutConfig();
        _background ??= new AnnouncementBackgroundConfig();
        _text ??= new AnnouncementTextConfig();
        _sprite ??= new AnnouncementSpriteConfig();
        _title ??= new AnnouncementTitleConfig();
        _scaling ??= new AnnouncementScalingConfig();

        _animation.ValidateAndNormalize();
        _layout.ValidateAndNormalize();
        _background.ValidateAndNormalize();
        _text.ValidateAndNormalize();
        _sprite.ValidateAndNormalize();
        _title.ValidateAndNormalize();
        _scaling.ValidateAndNormalize();
    }

    void ISerializationHooks.AfterDeserialization()
    {
        ValidateAndNormalize();
    }
}
