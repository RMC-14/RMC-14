using System.Numerics;
using Content.Shared._RMC14.Announce;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client._RMC14.Announce;

public sealed class CRTOverlay : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "RMCAnnouncementCRT";
    private ShaderInstance? _shader;
    private float _elapsed;
    private float _scanlineOffset;

    public CRTSettings Settings { get; set; } = new();

    public CRTOverlay()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
        CanKeyboardFocus = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!Settings.Enabled || PixelWidth <= 0 || PixelHeight <= 0)
            return;

        var deltaTime = (float) _timing.FrameTime.TotalSeconds;
        _elapsed += deltaTime;
        var spacing = Math.Max(2f, Settings.ScanlineSpacing);
        _scanlineOffset = (_scanlineOffset + Settings.ScanlineSpeed * deltaTime * 60f) % spacing;

        _shader ??= _prototypes.Index(Shader).InstanceUnique();
        _shader.SetParameter("size", (Vector2) PixelSize);
        _shader.SetParameter("elapsed", _elapsed);
        _shader.SetParameter("effects", new Vector4(
            Settings.ShowVignette ? 1 : 0, Settings.ShowNoise ? 1 : 0,
            Settings.ShowScanlines ? 1 : 0, Settings.ShowChromaticAberration ? 1 : 0));
        _shader.SetParameter("scanline", new Vector4(spacing, Math.Max(1f, Settings.ScanlineThickness),
            _scanlineOffset, Math.Clamp(Settings.ScanlineAlpha, 0f, 1f)));
        _shader.SetParameter("scanlineAnimation", new Vector4(Settings.ScanlineWaveFrequency,
            Settings.ScanlineWaveAmplitude, Settings.ScanlineFlickerIntensity, Settings.ScanlineFlickerSpeed));
        _shader.SetParameter("scanlineColor", Settings.ScanlineColor);
        _shader.SetParameter("glitchColor", Settings.ScanlineGlitchColor.WithAlpha(Settings.ScanlineGlitchAlpha));
        _shader.SetParameter("glitchChance", Settings.ScanlineGlitchChance);
        _shader.SetParameter("noise", new Vector4(Math.Max(0f, Settings.NoiseIntensity), Settings.NoiseAlpha,
            Math.Max(0.1f, Settings.NoiseMinSize), Math.Max(0.1f, Math.Max(Settings.NoiseMinSize, Settings.NoiseMaxSize))));
        _shader.SetParameter("noiseSeed", MathF.Floor(_elapsed / Math.Max(0.001f, Settings.NoiseUpdateFrequency)));
        _shader.SetParameter("staticSize", new Vector4(Settings.NoiseStaticMinWidth, Settings.NoiseStaticMaxWidth,
            Settings.NoiseStaticMinHeight, Settings.NoiseStaticMaxHeight));
        _shader.SetParameter("staticEffect", new Vector2(Settings.NoiseStaticChance, Settings.NoiseStaticAlpha));
        _shader.SetParameter("vignette", new Vector4(Settings.VignetteSizeMultiplier,
            Settings.VignetteIntensity * Settings.VignetteAlphaMultiplier,
            Settings.VignettePulseSpeed, Settings.VignettePulseAmplitude));
        _shader.SetParameter("vignetteEdges", new Vector2(Settings.VignetteCornerSize, Settings.VignetteEdgeAlpha));
        _shader.SetParameter("vignetteColor", Settings.VignetteColor);
        _shader.SetParameter("chromatic", new Vector4(Settings.ChromaticAmount, Settings.ChromaticParticleMinSize,
            Settings.ChromaticParticleMaxSize, Settings.ChromaticParticleAlpha));
        _shader.SetParameter("chromaticCount", Math.Max(0, Settings.ChromaticParticleCount));
        _shader.SetParameter("chromaticAnimation", new Vector2(Settings.ChromaticParticleChance, Settings.ChromaticAnimationSpeed));
        _shader.SetParameter("flicker", new Vector4(Settings.FlickerThreshold, Settings.FlickerChance,
            Settings.FlickerAlpha, Settings.FlashChance));
        _shader.SetParameter("flickerColor", Settings.FlickerColor);
        _shader.SetParameter("glowColor", Settings.GlowColor.WithAlpha(Settings.FlashMaxBrightness));

        var previousShader = handle.GetShader();
        try
        {
            handle.UseShader(_shader);
            handle.DrawRect(PixelSizeBox, Color.White);
        }
        finally
        {
            handle.UseShader(previousShader);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shader?.Dispose();
            _shader = null;
        }

        base.Dispose(disposing);
    }
}
