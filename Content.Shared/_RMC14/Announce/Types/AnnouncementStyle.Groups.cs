using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Numerics;

namespace Content.Shared._RMC14.Announce;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementAnimationConfig
{
    [DataField]
    public AnnouncementAnimation Animation { get; set; } = AnnouncementAnimation.Typewriter;

    [DataField]
    public float PrintSpeed { get; set; } = 0.03f;

    [DataField]
    public float HoldDuration { get; set; } = 3f;

    [DataField]
    public float FlickerChance { get; set; } = 0.01f;

    [DataField]
    public float GlitchChance { get; set; } = 0.005f;

    [DataField]
    public RealisticAnimations AnimationEnhancements { get; set; } = new();

    public AnnouncementAnimationConfig Clone()
    {
        return new AnnouncementAnimationConfig
        {
            Animation = Animation,
            PrintSpeed = PrintSpeed,
            HoldDuration = HoldDuration,
            FlickerChance = FlickerChance,
            GlitchChance = GlitchChance,
            AnimationEnhancements = AnimationEnhancements?.Clone() ?? new RealisticAnimations(),
        };
    }

    public void ValidateAndNormalize()
    {
        PrintSpeed = MathF.Max(0.001f, PrintSpeed);
        HoldDuration = MathF.Max(0f, HoldDuration);
        FlickerChance = MathHelper.Clamp(FlickerChance, 0f, 1f);
        GlitchChance = MathHelper.Clamp(GlitchChance, 0f, 1f);

        AnimationEnhancements ??= new RealisticAnimations();
        AnimationEnhancements.ValidateAndNormalize();
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementLayoutConfig
{
    [DataField]
    public AnnouncementPosition Position { get; set; } = AnnouncementPosition.MiddleCenter;

    [DataField]
    public AnnouncementSpeakerNamePosition SpeakerNamePosition { get; set; } = AnnouncementSpeakerNamePosition.Below;

    [DataField]
    public AnnouncementSpritePosition SpritePosition { get; set; } = AnnouncementSpritePosition.Left;

    [DataField]
    public float SpriteSpacing { get; set; } = 20f;

    [DataField]
    public SpriteDisplayMode SpriteDisplayMode { get; set; } = SpriteDisplayMode.TopHalf;

    [DataField]
    public float UIScale { get; set; } = 1f;

    [DataField]
    public AnnouncementTitlePosition TitlePosition { get; set; } = AnnouncementTitlePosition.Above;

    [DataField]
    public Vector2 SpriteClipOffset { get; set; } = Vector2.Zero;

    [DataField]
    public Vector2 SpriteClipSize { get; set; } = new(64f, 64f);

    public AnnouncementLayoutConfig Clone()
    {
        return (AnnouncementLayoutConfig) MemberwiseClone();
    }

    public void ValidateAndNormalize()
    {
        SpriteSpacing = MathF.Max(0f, SpriteSpacing);
        UIScale = MathF.Max(0.1f, UIScale);
        SpriteClipSize = new Vector2(
            MathF.Max(1f, SpriteClipSize.X),
            MathF.Max(1f, SpriteClipSize.Y));
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementBackgroundConfig
{
    [DataField]
    public bool ShowBackground { get; set; } = true;

    [DataField]
    public float BackgroundAlpha { get; set; } = 0.8f;

    [DataField]
    public Color BackgroundColor { get; set; } = Color.Black;

    public AnnouncementBackgroundConfig Clone()
    {
        return (AnnouncementBackgroundConfig) MemberwiseClone();
    }

    public void ValidateAndNormalize()
    {
        BackgroundAlpha = MathHelper.Clamp(BackgroundAlpha, 0f, 1f);
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementTextConfig
{
    [DataField]
    public Color PrimaryColor { get; set; } = Color.White;

    [DataField]
    public string Font { get; set; } = "Default";

    [DataField]
    public float FontSize { get; set; } = 16f;

    [DataField]
    public float LineHeight { get; set; } = 40f;

    [DataField]
    public bool ShowSpeakerName { get; set; } = true;

    [DataField]
    public Color SpeakerNameColor { get; set; } = Color.White;

    [DataField]
    public float SpeakerNameFontSize { get; set; } = 12f;

    public AnnouncementTextConfig Clone()
    {
        return (AnnouncementTextConfig) MemberwiseClone();
    }

    public void ValidateAndNormalize()
    {
        if (string.IsNullOrWhiteSpace(Font))
            Font = "Default";

        FontSize = MathF.Max(1f, FontSize);
        LineHeight = MathF.Max(1f, LineHeight);
        SpeakerNameFontSize = MathF.Max(1f, SpeakerNameFontSize);
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementSpriteConfig
{
    [DataField]
    public bool ShowSpriteBox { get; set; } = true;

    [DataField]
    public Color SpriteBoxColor { get; set; } = Color.Black;

    [DataField]
    public Color SpriteBoxBorderColor { get; set; } = Color.White;

    [DataField]
    public float SpriteBoxBorderThickness { get; set; } = 2f;

    [DataField]
    public float SpriteBoxPadding { get; set; } = 10f;

    [DataField]
    public string? SpriteBoxShader { get; set; }

    [DataField]
    public bool SpriteGlow { get; set; }

    [DataField]
    public Color SpriteGlowColor { get; set; } = Color.White;

    [DataField]
    public float SpriteGlowIntensity { get; set; } = 0.5f;

    [DataField]
    public float SpriteScale { get; set; } = 1f;

    public AnnouncementSpriteConfig Clone()
    {
        return (AnnouncementSpriteConfig) MemberwiseClone();
    }

    public void ValidateAndNormalize()
    {
        SpriteBoxBorderThickness = MathF.Max(0f, SpriteBoxBorderThickness);
        SpriteBoxPadding = MathF.Max(0f, SpriteBoxPadding);
        SpriteGlowIntensity = MathF.Max(0f, SpriteGlowIntensity);
        SpriteScale = MathF.Max(0.1f, SpriteScale);
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementTitleConfig
{
    [DataField]
    public bool ShowTitle { get; set; }

    [DataField]
    public string Title { get; set; } = string.Empty;

    [DataField]
    public string TitleFont { get; set; } = "DefaultBold";

    [DataField]
    public Color TitleColor { get; set; } = Color.White;

    [DataField]
    public float TitleFontSize { get; set; } = 20f;

    [DataField]
    public bool TitleUnderline { get; set; }

    [DataField]
    public float TitleUnderlineThickness { get; set; } = 2f;

    public AnnouncementTitleConfig Clone()
    {
        return (AnnouncementTitleConfig) MemberwiseClone();
    }

    public void ValidateAndNormalize()
    {
        Title ??= string.Empty;

        if (string.IsNullOrWhiteSpace(TitleFont))
            TitleFont = "DefaultBold";

        TitleFontSize = MathF.Max(1f, TitleFontSize);
        TitleUnderlineThickness = MathF.Max(0f, TitleUnderlineThickness);
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementScalingConfig
{
    [DataField]
    public bool EnableResponsiveScaling { get; set; } = true;

    [DataField]
    public float ResponsiveScaleFactor { get; set; } = 1f;

    [DataField]
    public float MinScale { get; set; } = 0.5f;

    [DataField]
    public float MaxScale { get; set; } = 2f;

    public AnnouncementScalingConfig Clone()
    {
        return (AnnouncementScalingConfig) MemberwiseClone();
    }

    public void ValidateAndNormalize()
    {
        ResponsiveScaleFactor = MathF.Max(0.01f, ResponsiveScaleFactor);
        MinScale = MathF.Max(0.1f, MinScale);
        MaxScale = MathF.Max(0.1f, MaxScale);

        if (MinScale > MaxScale)
            (MinScale, MaxScale) = (MaxScale, MinScale);
    }
}
