using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;

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

    public CRTSettings Clone()
    {
        return (CRTSettings) MemberwiseClone();
    }

    public void ValidateAndNormalize()
    {
        // Keep these aligned with CRTOverlay draw-time minimums so authoring values stay predictable.
        ScanlineSpacing = MathF.Max(2f, ScanlineSpacing);
        ScanlineThickness = MathF.Max(1f, ScanlineThickness);
        ScanlineAlpha = MathHelper.Clamp(ScanlineAlpha, 0f, 1f);
        ScanlineSpeed = MathF.Max(0f, ScanlineSpeed);
        ScanlineWaveFrequency = MathF.Max(0f, ScanlineWaveFrequency);
        ScanlineWaveAmplitude = MathF.Max(0f, ScanlineWaveAmplitude);
        ScanlineFlickerIntensity = MathF.Max(0f, ScanlineFlickerIntensity);
        ScanlineFlickerSpeed = MathF.Max(0f, ScanlineFlickerSpeed);
        ScanlineGlitchChance = MathHelper.Clamp(ScanlineGlitchChance, 0f, 1f);
        ScanlineGlitchAlpha = MathHelper.Clamp(ScanlineGlitchAlpha, 0f, 1f);

        NoiseIntensity = MathF.Max(0f, NoiseIntensity);
        NoiseAlpha = MathHelper.Clamp(NoiseAlpha, 0f, 1f);
        NoiseUpdateFrequency = MathF.Max(0.001f, NoiseUpdateFrequency);
        NoiseMinSize = MathF.Max(0.1f, NoiseMinSize);
        NoiseMaxSize = MathF.Max(NoiseMinSize, NoiseMaxSize);
        NoiseStaticChance = MathHelper.Clamp(NoiseStaticChance, 0f, 1f);
        NoiseStaticMinWidth = MathF.Max(0.1f, NoiseStaticMinWidth);
        NoiseStaticMaxWidth = MathF.Max(NoiseStaticMinWidth, NoiseStaticMaxWidth);
        NoiseStaticMinHeight = MathF.Max(0.1f, NoiseStaticMinHeight);
        NoiseStaticMaxHeight = MathF.Max(NoiseStaticMinHeight, NoiseStaticMaxHeight);
        NoiseStaticAlpha = MathHelper.Clamp(NoiseStaticAlpha, 0f, 1f);

        VignetteIntensity = MathF.Max(0f, VignetteIntensity);
        VignetteSizeMultiplier = MathF.Max(0f, VignetteSizeMultiplier);
        VignetteAlphaMultiplier = MathF.Max(0f, VignetteAlphaMultiplier);
        VignettePulseSpeed = MathF.Max(0f, VignettePulseSpeed);
        VignettePulseAmplitude = MathF.Max(0f, VignettePulseAmplitude);
        VignetteCornerSize = MathF.Max(0f, VignetteCornerSize);
        VignetteEdgeAlpha = MathHelper.Clamp(VignetteEdgeAlpha, 0f, 1f);

        ChromaticAmount = MathF.Max(0f, ChromaticAmount);
        ChromaticParticleCount = Math.Max(0, ChromaticParticleCount);
        ChromaticParticleChance = MathHelper.Clamp(ChromaticParticleChance, 0f, 1f);
        ChromaticParticleMinSize = MathF.Max(0.1f, ChromaticParticleMinSize);
        ChromaticParticleMaxSize = MathF.Max(ChromaticParticleMinSize, ChromaticParticleMaxSize);
        ChromaticParticleAlpha = MathHelper.Clamp(ChromaticParticleAlpha, 0f, 1f);
        ChromaticAnimationSpeed = MathF.Max(0f, ChromaticAnimationSpeed);

        FlickerThreshold = MathHelper.Clamp(FlickerThreshold, 0f, 1f);
        FlickerChance = MathHelper.Clamp(FlickerChance, 0f, 1f);
        FlickerAlpha = MathHelper.Clamp(FlickerAlpha, 0f, 1f);
        FlashChance = MathHelper.Clamp(FlashChance, 0f, 1f);
        FlashMaxBrightness = MathHelper.Clamp(FlashMaxBrightness, 0f, 1f);
    }
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

    public RealisticAnimations Clone()
    {
        return new RealisticAnimations
        {
            SlideDuration = SlideDuration,
            SlideFrom = SlideFrom,
            ZoomStartScale = ZoomStartScale,
            ZoomDuration = ZoomDuration,
            BounceCount = BounceCount,
            BounceHeight = BounceHeight,
            EnableCRT = EnableCRT,
            CRTSettings = CRTSettings?.Clone(),
        };
    }

    public void ValidateAndNormalize()
    {
        SlideDuration = MathF.Max(0.01f, SlideDuration);
        ZoomStartScale = MathF.Max(0.01f, ZoomStartScale);
        ZoomDuration = MathF.Max(0.01f, ZoomDuration);
        BounceCount = Math.Max(0, BounceCount);
        BounceHeight = MathF.Max(0f, BounceHeight);

        if (EnableCRT)
        {
            CRTSettings ??= new CRTSettings();
            CRTSettings.ValidateAndNormalize();
        }
    }
}
