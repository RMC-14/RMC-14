using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce;

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
