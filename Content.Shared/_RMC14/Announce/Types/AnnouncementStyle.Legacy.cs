using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared._RMC14.Announce;

// Compatibility shim for legacy flat preset fields. New code should use grouped configs.
public sealed partial class AnnouncementStyle
{
    [DataField]
    public AnnouncementAnimation Animation
    {
        get => _animation.Animation;
        set => _animation.Animation = value;
    }

    [DataField]
    public AnnouncementPosition Position
    {
        get => _layout.Position;
        set => _layout.Position = value;
    }

    [DataField]
    public bool ShowBackground
    {
        get => _background.ShowBackground;
        set => _background.ShowBackground = value;
    }

    [DataField]
    public float BackgroundAlpha
    {
        get => _background.BackgroundAlpha;
        set => _background.BackgroundAlpha = value;
    }

    [DataField]
    public Color BackgroundColor
    {
        get => _background.BackgroundColor;
        set => _background.BackgroundColor = value;
    }

    [DataField]
    public Color PrimaryColor
    {
        get => _text.PrimaryColor;
        set => _text.PrimaryColor = value;
    }

    [DataField]
    public string Font
    {
        get => _text.Font;
        set => _text.Font = value;
    }

    [DataField]
    public float FontSize
    {
        get => _text.FontSize;
        set => _text.FontSize = value;
    }

    [DataField]
    public float LineHeight
    {
        get => _text.LineHeight;
        set => _text.LineHeight = value;
    }

    [DataField]
    public float PrintSpeed
    {
        get => _animation.PrintSpeed;
        set => _animation.PrintSpeed = value;
    }

    [DataField]
    public float HoldDuration
    {
        get => _animation.HoldDuration;
        set => _animation.HoldDuration = value;
    }

    [DataField]
    public float ShakeIntensity
    {
        get => _animation.ShakeIntensity;
        set => _animation.ShakeIntensity = value;
    }

    [DataField]
    public float FlickerChance
    {
        get => _animation.FlickerChance;
        set => _animation.FlickerChance = value;
    }

    [DataField]
    public float GlitchChance
    {
        get => _animation.GlitchChance;
        set => _animation.GlitchChance = value;
    }

    [DataField]
    public bool ShowSpriteBox
    {
        get => _sprite.ShowSpriteBox;
        set => _sprite.ShowSpriteBox = value;
    }

    [DataField]
    public Color SpriteBoxColor
    {
        get => _sprite.SpriteBoxColor;
        set => _sprite.SpriteBoxColor = value;
    }

    [DataField]
    public Color SpriteBoxBorderColor
    {
        get => _sprite.SpriteBoxBorderColor;
        set => _sprite.SpriteBoxBorderColor = value;
    }

    [DataField]
    public float SpriteBoxBorderThickness
    {
        get => _sprite.SpriteBoxBorderThickness;
        set => _sprite.SpriteBoxBorderThickness = value;
    }

    [DataField]
    public float SpriteBoxPadding
    {
        get => _sprite.SpriteBoxPadding;
        set => _sprite.SpriteBoxPadding = value;
    }

    [DataField]
    public string? SpriteBoxShader
    {
        get => _sprite.SpriteBoxShader;
        set => _sprite.SpriteBoxShader = value;
    }

    [DataField]
    public bool SpriteGlow
    {
        get => _sprite.SpriteGlow;
        set => _sprite.SpriteGlow = value;
    }

    [DataField]
    public Color SpriteGlowColor
    {
        get => _sprite.SpriteGlowColor;
        set => _sprite.SpriteGlowColor = value;
    }

    [DataField]
    public float SpriteGlowIntensity
    {
        get => _sprite.SpriteGlowIntensity;
        set => _sprite.SpriteGlowIntensity = value;
    }

    [DataField]
    public bool ShowSpeakerName
    {
        get => _text.ShowSpeakerName;
        set => _text.ShowSpeakerName = value;
    }

    [DataField]
    public Color SpeakerNameColor
    {
        get => _text.SpeakerNameColor;
        set => _text.SpeakerNameColor = value;
    }

    [DataField]
    public float SpeakerNameFontSize
    {
        get => _text.SpeakerNameFontSize;
        set => _text.SpeakerNameFontSize = value;
    }

    [DataField]
    public AnnouncementSpeakerNamePosition SpeakerNamePosition
    {
        get => _layout.SpeakerNamePosition;
        set => _layout.SpeakerNamePosition = value;
    }

    [DataField]
    public AnnouncementSpritePosition SpritePosition
    {
        get => _layout.SpritePosition;
        set => _layout.SpritePosition = value;
    }

    [DataField]
    public float SpriteSpacing
    {
        get => _layout.SpriteSpacing;
        set => _layout.SpriteSpacing = value;
    }

    [DataField]
    public RealisticAnimations AnimationEnhancements
    {
        get => _animation.AnimationEnhancements;
        set => _animation.AnimationEnhancements = value;
    }

    [DataField]
    public SpriteDisplayMode SpriteDisplayMode
    {
        get => _layout.SpriteDisplayMode;
        set => _layout.SpriteDisplayMode = value;
    }

    [DataField]
    public float SpriteScale
    {
        get => _sprite.SpriteScale;
        set => _sprite.SpriteScale = value;
    }

    [DataField]
    public float UIScale
    {
        get => _layout.UIScale;
        set => _layout.UIScale = value;
    }

    [DataField]
    public bool ShowTitle
    {
        get => _title.ShowTitle;
        set => _title.ShowTitle = value;
    }

    [DataField]
    public string Title
    {
        get => _title.Title;
        set => _title.Title = value;
    }

    [DataField]
    public string TitleFont
    {
        get => _title.TitleFont;
        set => _title.TitleFont = value;
    }

    [DataField]
    public Color TitleColor
    {
        get => _title.TitleColor;
        set => _title.TitleColor = value;
    }

    [DataField]
    public float TitleFontSize
    {
        get => _title.TitleFontSize;
        set => _title.TitleFontSize = value;
    }

    [DataField]
    public bool TitleUnderline
    {
        get => _title.TitleUnderline;
        set => _title.TitleUnderline = value;
    }

    [DataField]
    public float TitleUnderlineThickness
    {
        get => _title.TitleUnderlineThickness;
        set => _title.TitleUnderlineThickness = value;
    }

    [DataField]
    public AnnouncementTitlePosition TitlePosition
    {
        get => _layout.TitlePosition;
        set => _layout.TitlePosition = value;
    }

    [DataField]
    public bool EnableResponsiveScaling
    {
        get => _scaling.EnableResponsiveScaling;
        set => _scaling.EnableResponsiveScaling = value;
    }

    [DataField]
    public float ResponsiveScaleFactor
    {
        get => _scaling.ResponsiveScaleFactor;
        set => _scaling.ResponsiveScaleFactor = value;
    }

    [DataField]
    public float MinScale
    {
        get => _scaling.MinScale;
        set => _scaling.MinScale = value;
    }

    [DataField]
    public float MaxScale
    {
        get => _scaling.MaxScale;
        set => _scaling.MaxScale = value;
    }

    [DataField]
    public Vector2 SpriteClipOffset
    {
        get => _layout.SpriteClipOffset;
        set => _layout.SpriteClipOffset = value;
    }

    [DataField]
    public Vector2 SpriteClipSize
    {
        get => _layout.SpriteClipSize;
        set => _layout.SpriteClipSize = value;
    }

    public float PrintSecondsPerChar
    {
        get => _animation.PrintSpeed;
        set => _animation.PrintSpeed = value;
    }

    public float HoldSeconds
    {
        get => _animation.HoldDuration;
        set => _animation.HoldDuration = value;
    }

    public float LineHeightPx
    {
        get => _text.LineHeight;
        set => _text.LineHeight = value;
    }
}
