using Robust.Shared.Maths;
using Robust.Shared.Serialization;
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

    [DataField]
    public AnnouncementTitleEffectConfig Effect { get; set; } = new();
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
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class CRTSettings
{
    [DataField]
    public bool Enabled { get; set; } = true;

    // Scanline Settings
    [DataField]
    public bool ShowScanlines { get; set; } = true;

    [DataField]
    public float ScanlineSpacing { get; set; } = 4f;

    [DataField]
    public float ScanlineThickness { get; set; } = 1.3f;

    [DataField]
    public float ScanlineAlpha { get; set; } = 0.3f;

    [DataField]
    public float ScanlineSpeed { get; set; } = 60f;

    [DataField]
    public Color ScanlineColor { get; set; } = Color.Black;

    [DataField]
    public float ScanlineWaveFrequency { get; set; } = 3f;

    [DataField]
    public float ScanlineWaveAmplitude { get; set; } = 1.5f;

    [DataField]
    public float ScanlineFlickerIntensity { get; set; } = 0.5f;

    [DataField]
    public float ScanlineFlickerSpeed { get; set; } = 2f;

    [DataField]
    public float ScanlineGlitchChance { get; set; } = 0.02f;

    [DataField]
    public Color ScanlineGlitchColor { get; set; } = Color.FromHex("#00ff00");

    [DataField]
    public float ScanlineGlitchAlpha { get; set; } = 0.15f;

    // Noise Settings
    [DataField]
    public bool ShowNoise { get; set; } = true;

    [DataField]
    public float NoiseIntensity { get; set; } = 0.5f;

    [DataField]
    public float NoiseAlpha { get; set; } = 0.4f;

    [DataField]
    public float NoiseUpdateFrequency { get; set; } = 0.08f;

    [DataField]
    public float NoiseMinSize { get; set; } = 0.5f;

    [DataField]
    public float NoiseMaxSize { get; set; } = 2f;

    [DataField]
    public float NoiseStaticChance { get; set; } = 0.09f;

    [DataField]
    public float NoiseStaticMinWidth { get; set; } = 1f;

    [DataField]
    public float NoiseStaticMaxWidth { get; set; } = 3f;

    [DataField]
    public float NoiseStaticMinHeight { get; set; } = 3f;

    [DataField]
    public float NoiseStaticMaxHeight { get; set; } = 11f;

    [DataField]
    public float NoiseStaticAlpha { get; set; } = 0.3f;

    // Vignette Settings
    [DataField]
    public bool ShowVignette { get; set; } = true;

    [DataField]
    public float VignetteIntensity { get; set; } = 0.6f;

    [DataField]
    public Color VignetteColor { get; set; } = Color.Black;

    [DataField]
    public float VignetteSizeMultiplier { get; set; } = 0.15f;

    [DataField]
    public float VignetteAlphaMultiplier { get; set; } = 0.4f;

    [DataField]
    public float VignettePulseSpeed { get; set; } = 1.5f;

    [DataField]
    public float VignettePulseAmplitude { get; set; } = 0.1f;

    [DataField]
    public float VignetteCornerSize { get; set; } = 0.7f;

    [DataField]
    public float VignetteEdgeAlpha { get; set; } = 0.6f;

    // Flash Tint Settings
    [DataField]
    public Color GlowColor { get; set; } = Color.FromHex("#00ff41");

    // Chromatic Aberration Settings
    [DataField]
    public bool ShowChromaticAberration { get; set; } = false;

    [DataField]
    public float ChromaticAmount { get; set; } = 2f;

    [DataField]
    public int ChromaticParticleCount { get; set; } = 5;

    [DataField]
    public float ChromaticParticleChance { get; set; } = 0.3f;

    [DataField]
    public float ChromaticParticleMinSize { get; set; } = 2f;

    [DataField]
    public float ChromaticParticleMaxSize { get; set; } = 6f;

    [DataField]
    public float ChromaticParticleAlpha { get; set; } = 0.3f;

    [DataField]
    public float ChromaticAnimationSpeed { get; set; } = 2f;

    // Flicker/Flash Settings
    [DataField]
    public float FlickerThreshold { get; set; } = 0.9f;

    [DataField]
    public float FlickerChance { get; set; } = 0.05f;

    [DataField]
    public float FlickerAlpha { get; set; } = 0.02f;

    [DataField]
    public Color FlickerColor { get; set; } = Color.White;

    [DataField]
    public float FlashChance { get; set; } = 0.01f;

    [DataField]
    public float FlashMaxBrightness { get; set; } = 0.05f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RealisticAnimations
{
    [DataField]
    public float SlideDuration { get; set; } = 1.0f;

    [DataField]
    public SlideDirection SlideFrom { get; set; } = SlideDirection.Top;

    [DataField]
    public float ZoomStartScale { get; set; } = 0.1f;

    [DataField]
    public float ZoomDuration { get; set; } = 1.0f;

    [DataField]
    public int BounceCount { get; set; } = 3;

    [DataField]
    public float BounceHeight { get; set; } = 15f;

    [DataField]
    public bool EnableCRT { get; set; } = false;

    [DataField]
    public CRTSettings? CRTSettings { get; set; }
}
