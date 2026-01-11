using Robust.Shared.Audio;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared._RMC14.Announce;

[Serializable, NetSerializable]
public enum AnnouncementAnimation : byte
{
    Typewriter,
    Slide,
    Zoom,
    Bounce,
    Fade,
    Pulse,
    Heartbeat,
    Warp,
    Glitch,
    None
}

[Serializable, NetSerializable]
public enum AnnouncementPosition : byte
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    FullScreen
}

[Serializable, NetSerializable]
public enum AnnouncementTarget : byte
{
    All,
    Marines,
    Xenos
}

[Serializable, NetSerializable]
public enum AnnouncementDisplayPreference
{
    Stylized = 0,
    Simplified = 1,
    Disabled = 2,
    Default = 3
}

[Serializable, NetSerializable]
public enum AnnouncementState : byte
{
    Animating,
    Holding,
    FadingOut
}

[Serializable, NetSerializable]
public enum BackgroundType : byte
{
    None,
    Solid,
    Gradient
}

[Serializable, NetSerializable]
public enum GradientDirection : byte
{
    Horizontal,
    Vertical,
    Diagonal,
    Radial
}

[Serializable, NetSerializable]
public enum TypewriterStyle : byte
{
    Linear,
    Burst,
    Random,
    Word,
    Sentence
}

[Serializable, NetSerializable]
public enum AnnouncementSpritePosition : byte
{
    Left,
    Right,
    Center,
    Above,
    Below
}

[Serializable, NetSerializable]
public enum AnnouncementSpeakerNamePosition : byte
{
    Above,
    Below,
    Left,
    Right
}

[Serializable, NetSerializable]
public enum AnnouncementTitlePosition : byte
{
    Above,
    Below
}

[Serializable, NetSerializable]
public enum AnnouncementDecalPlacement : byte
{
    ReplaceSprite,
    BehindSprite,
    Left,
    Right,
    Above,
    Below
}

[Serializable, NetSerializable]
public enum EasingType : byte
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    BounceIn,
    BounceOut,
    Elastic
}

[Serializable, NetSerializable]
public enum SlideDirection : byte
{
    Top,
    Bottom,
    Left,
    Right
}

[Serializable, NetSerializable]
public enum FrameStyle : byte
{
    Solid,
    Raised,
    Inset,
    Glowing,
    CRT
}

[Serializable, NetSerializable]
public enum SpriteDisplayMode : byte
{
    TopHalf,
    FullSprite,
    HeadOnly,
    CustomClip
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

    // Glow Settings
    [DataField]
    public bool ShowGlow { get; set; } = true;

    [DataField]
    public float GlowIntensity { get; set; } = 0.8f;

    [DataField]
    public float GlowSpeed { get; set; } = 2f;

    [DataField]
    public Color GlowColor { get; set; } = Color.FromHex("#00ff41");

    // Curvature Settings
    [DataField]
    public bool ShowCurvature { get; set; } = false;

    [DataField]
    public float CurvatureAmount { get; set; } = 0.2f;

    [DataField]
    public int CurvatureSteps { get; set; } = 20;

    [DataField]
    public float CurvatureWidthMultiplier { get; set; } = 0.1f;

    [DataField]
    public Color CurvatureColor { get; set; } = Color.Black;

    [DataField]
    public float CurvatureAlpha { get; set; } = 0.2f;

    [DataField]
    public float CurvatureAnimationSpeed { get; set; } = 0.5f;

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
    public TypewriterStyle TypewriterMode { get; set; } = TypewriterStyle.Linear;

    [DataField]
    public bool RandomizeSpeed { get; set; }

    [DataField]
    public float SpeedVariation { get; set; } = 0.2f;

    [DataField]
    public bool EnableSlide { get; set; }

    [DataField]
    public float SlideDuration { get; set; } = 1.0f;

    [DataField]
    public SlideDirection SlideFrom { get; set; } = SlideDirection.Top;

    [DataField]
    public bool EnableZoom { get; set; }

    [DataField]
    public float ZoomStartScale { get; set; } = 0.1f;

    [DataField]
    public float ZoomDuration { get; set; } = 1.0f;

    [DataField]
    public bool EnableBounce { get; set; }

    [DataField]
    public int BounceCount { get; set; } = 3;

    [DataField]
    public float BounceHeight { get; set; } = 15f;

    [DataField]
    public EasingType EasingIn { get; set; } = EasingType.EaseInOut;

    [DataField]
    public EasingType EasingOut { get; set; } = EasingType.EaseInOut;

    [DataField]
    public bool EnableCRT { get; set; } = false;

    [DataField]
    public CRTSettings? CRTSettings { get; set; }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class EnhancedTextStyle
{
    [DataField]
    public bool ShowOutline { get; set; } = true;

    [DataField]
    public Color OutlineColor { get; set; } = Color.Black;

    [DataField]
    public float OutlineThickness { get; set; } = 1f;

    [DataField]
    public bool ShowShadow { get; set; } = false;

    [DataField]
    public Color ShadowColor { get; set; } = Color.Black;

    [DataField]
    public Vector2 ShadowOffset { get; set; } = new Vector2(2f, 2f);

    [DataField]
    public float ShadowAlpha { get; set; } = 0.8f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BackgroundEnhancements
{
    [DataField]
    public BackgroundType Type { get; set; } = BackgroundType.Solid;

    [DataField]
    public bool ShowGradient { get; set; }

    [DataField]
    public Color GradientStart { get; set; } = Color.Black;

    [DataField]
    public Color GradientEnd { get; set; } = Color.Gray;

    [DataField]
    public GradientDirection Direction { get; set; } = GradientDirection.Vertical;

    [DataField]
    public bool ShowPattern { get; set; } = false;

    [DataField]
    public string PatternTexture { get; set; } = string.Empty;

    [DataField]
    public float PatternScale { get; set; } = 1.0f;

    [DataField]
    public float PatternAlpha { get; set; } = 0.5f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class EnhancedSpriteBox
{
    [DataField]
    public bool ShowFrame { get; set; } = true;

    [DataField]
    public FrameStyle FrameType { get; set; } = FrameStyle.Solid;

    [DataField]
    public float FramePadding { get; set; } = 2f;

    [DataField]
    public float FrameRadius { get; set; } = 0f;

    [DataField]
    public bool ShowInnerShadow { get; set; }

    [DataField]
    public bool ShowBevel { get; set; }

    [DataField]
    public Color BevelHighlight { get; set; } = Color.White;

    [DataField]
    public Color BevelShadow { get; set; } = Color.Gray;

    [DataField]
    public float BevelSize { get; set; } = 1f;

    [DataField]
    public bool ShowBorder { get; set; } = true;

    [DataField]
    public float BorderRadius { get; set; } = 0f;

    [DataField]
    public bool EnableGlow { get; set; }

    [DataField]
    public float GlowRadius { get; set; } = 5f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SpriteEnhancements
{
    [DataField]
    public bool EnableFloat { get; set; }

    [DataField]
    public float FloatAmplitude { get; set; } = 5f;

    [DataField]
    public float FloatSpeed { get; set; } = 2f;

    [DataField]
    public bool EnableBreathe { get; set; }

    [DataField]
    public float BreatheAmplitude { get; set; } = 0.1f;

    [DataField]
    public float BreatheSpeed { get; set; } = 1f;

    [DataField]
    public bool AnimateScale { get; set; }

    [DataField]
    public float ScalePulseSpeed { get; set; } = 1f;

    [DataField]
    public float ScaleAmplitude { get; set; } = 0.2f;

    [DataField]
    public bool RotateSprite { get; set; }

    [DataField]
    public float RotationSpeed { get; set; } = 45f;

    [DataField]
    public float SpriteRotation { get; set; } = 0f;

    [DataField]
    public bool EnableTint { get; set; }

    [DataField]
    public Color TintColor { get; set; } = Color.White;

    [DataField]
    public bool AnimateTint { get; set; }

    [DataField]
    public float TintSpeed { get; set; } = 1f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SpriteEffects
{
    [DataField]
    public bool EnableShake { get; set; }

    [DataField]
    public float ShakeIntensity { get; set; } = 2f;

    [DataField]
    public bool EnableFlash { get; set; }

    [DataField]
    public Color FlashColor { get; set; } = Color.White;

    [DataField]
    public float FlashDuration { get; set; } = 0.2f;

    [DataField]
    public int FlashCount { get; set; } = 1;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ScreenEffects
{
    [DataField]
    public bool EnableScreenShake { get; set; }

    [DataField]
    public float ScreenShakeIntensity { get; set; } = 5f;

    [DataField]
    public float ScreenShakeDuration { get; set; } = 1f;

    [DataField]
    public bool EnableScreenFlash { get; set; }

    [DataField]
    public Color ScreenFlashColor { get; set; } = Color.White;

    [DataField]
    public float ScreenFlashDuration { get; set; } = 0.5f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record AnnouncementStyle
{
    [DataField]
    public AnnouncementAnimation Animation { get; set; } = AnnouncementAnimation.Typewriter;

    [DataField]
    public AnnouncementPosition Position { get; set; } = AnnouncementPosition.MiddleCenter;

    [DataField]
    public bool ShowBackground { get; set; } = true;

    [DataField]
    public float BackgroundAlpha { get; set; } = 0.8f;

    [DataField]
    public Color BackgroundColor { get; set; } = Color.Black;

    [DataField]
    public Color PrimaryColor { get; set; } = Color.White;

    [DataField]
    public Color? SecondaryColor { get; set; }

    [DataField]
    public Color? AccentColor { get; set; }

    [DataField]
    public string Font { get; set; } = "Default";

    [DataField]
    public float FontSize { get; set; } = 16f;

    [DataField]
    public float LineHeight { get; set; } = 40f;

    [DataField]
    public float PrintSpeed { get; set; } = 0.03f;

    [DataField]
    public float HoldDuration { get; set; } = 3f;

    [DataField]
    public float ShakeIntensity { get; set; } = 0.5f;

    [DataField]
    public float FlickerChance { get; set; } = 0.01f;

    [DataField]
    public float GlitchChance { get; set; } = 0.005f;

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
    public float SpriteBoxCornerRadius { get; set; } = 0f;

    [DataField]
    public string? SpriteBoxShader { get; set; }

    [DataField]
    public bool SpriteGlow { get; set; }

    [DataField]
    public Color SpriteGlowColor { get; set; } = Color.White;

    [DataField]
    public float SpriteGlowIntensity { get; set; } = 0.5f;

    [DataField]
    public bool SpritePulse { get; set; }

    [DataField]
    public float SpritePulseSpeed { get; set; } = 1f;

    [DataField]
    public bool ShowSpeakerName { get; set; } = true;

    [DataField]
    public Color SpeakerNameColor { get; set; } = Color.White;

    [DataField]
    public float SpeakerNameFontSize { get; set; } = 12f;

    [DataField]
    public AnnouncementSpeakerNamePosition SpeakerNamePosition { get; set; } = AnnouncementSpeakerNamePosition.Below;

    [DataField]
    public AnnouncementSpritePosition SpritePosition { get; set; } = AnnouncementSpritePosition.Left;

    [DataField]
    public float SpriteSpacing { get; set; } = 20f;

    [DataField]
    public bool ShowOutline { get; set; } = false;

    [DataField]
    public Color OutlineColor { get; set; } = Color.Black;

    [DataField]
    public float OutlineThickness { get; set; } = 1f;

    [DataField]
    public RealisticAnimations AnimationEnhancements { get; set; } = new();

    [DataField]
    public EnhancedTextStyle TextEnhancements { get; set; } = new();

    [DataField]
    public BackgroundEnhancements BackgroundStyle { get; set; } = new();

    [DataField]
    public EnhancedSpriteBox SpriteBoxEnhancements { get; set; } = new();

    [DataField]
    public SpriteEnhancements SpriteEnhancements { get; set; } = new();

    [DataField]
    public SpriteEffects SpriteEffects { get; set; } = new();

    [DataField]
    public ScreenEffects ScreenEffects { get; set; } = new();

    [DataField]
    public bool EnableScreenShake { get; set; }

    [DataField]
    public float ShakeDuration { get; set; } = 1f;

    [DataField]
    public bool EnableFlash { get; set; }

    [DataField]
    public Color FlashColor { get; set; } = Color.White;

    [DataField]
    public float FlashDuration { get; set; } = 0.5f;

    [DataField]
    public int FlashCount { get; set; } = 1;

    [DataField]
    public SpriteDisplayMode SpriteDisplayMode { get; set; } = SpriteDisplayMode.TopHalf;

    [DataField]
    public float SpriteScale { get; set; } = 1.0f;

    [DataField]
    public float UIScale { get; set; } = 1.0f;

    [DataField]
    public bool ShowTitle { get; set; } = false;

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
    public AnnouncementTitlePosition TitlePosition { get; set; } = AnnouncementTitlePosition.Above;

    [DataField]
    public int MaxLineLength { get; set; } = 60;

    [DataField]
    public int MaxLines { get; set; } = 8;

    [DataField]
    public bool EnableTextWrapping { get; set; } = true;

    [DataField]
    public bool EnableResponsiveScaling { get; set; } = true;

    [DataField]
    public float ResponsiveScaleFactor { get; set; } = 1.0f;

    [DataField]
    public bool EnableAutoUIScale { get; set; } = true;

    [DataField]
    public float MinScale { get; set; } = 0.5f;

    [DataField]
    public float MaxScale { get; set; } = 2.0f;

    [DataField]
    public Vector2 BaseResolution { get; set; } = new Vector2(1920f, 1080f);

    [DataField]
    public Vector2 SpriteClipOffset { get; set; } = Vector2.Zero;

    [DataField]
    public Vector2 SpriteClipSize { get; set; } = new Vector2(64f, 64f);
}

[Serializable, NetSerializable]
public sealed class AnnouncementNetData
{
    public string[] Text { get; set; } = Array.Empty<string>();
    public string ConfigId { get; set; } = string.Empty;
    public float Priority { get; set; }
    public bool CanInterrupt { get; set; }
    public bool CanBeInterrupted { get; set; }
    public AnnouncementStyle Style { get; set; } = new();
    public AnnouncementStyleOverride? StyleOverride { get; set; }
    public NetEntity? SpeakerEntity { get; set; }
    public string? SpeakerName { get; set; }
    public bool ShowSprite { get; set; } = true;
    public float SpriteScale { get; set; } = 1.0f;
    public Vector2 SpriteOffset { get; set; }
    public Vector2 TextOffset { get; set; }
    public string? Title { get; set; }
    public string? DecalRsi { get; set; }
    public string? DecalState { get; set; }
    public AnnouncementDecalPlacement? DecalPlacement { get; set; }
    public float DecalScale { get; set; } = 4f;
    public float DecalAlpha { get; set; } = 1f;
    public Vector2 DecalOffset { get; set; } = Vector2.Zero;
    public bool IncognitoMask { get; set; }
}

[Serializable, NetSerializable]
public sealed class AnnouncementNetMessage : EntityEventArgs
{
    public AnnouncementNetData Data { get; }

    public AnnouncementNetMessage(AnnouncementNetData data)
    {
        Data = data;
    }
}

[Serializable, NetSerializable]
public sealed class AnnouncementPreferenceNetMessage : EntityEventArgs
{
    public AnnouncementDisplayPreference Preference { get; }

    public AnnouncementPreferenceNetMessage(AnnouncementDisplayPreference preference)
    {
        Preference = preference;
    }
}

public sealed class AnnouncementRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Preset { get; set; }
    public AnnouncementTarget Target { get; set; } = AnnouncementTarget.All;
    public EntityUid? Speaker { get; set; }
    public EntityUid? Source { get; set; }
    public EntityUid? TargetEntity { get; set; }
    public AnnouncementStyleOverride? StyleOverride { get; set; }
    public SoundSpecifier? SoundOverride { get; set; }
    public float? VolumeOverride { get; set; }
    public float? PriorityOverride { get; set; }
    public bool? CanInterrupt { get; set; }
    public bool? CanBeInterrupted { get; set; }
    public bool ShowSprite { get; set; } = true;
    public float SpriteScale { get; set; } = 1.0f;
    public Vector2? SpriteOffset { get; set; }
    public string? SpeakerNameOverride { get; set; }
    public string? Title { get; set; }
    public string? DecalRsi { get; set; }
    public string? DecalState { get; set; }
    public AnnouncementDecalPlacement? DecalPlacement { get; set; }
    public float? DecalScale { get; set; }
    public float? DecalAlpha { get; set; }
    public Vector2? DecalOffset { get; set; }
    public Vector2? TextOffset { get; set; }
    public bool IncognitoMask { get; set; }
}
