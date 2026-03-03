using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using Content.Shared._RMC14.Announce;

namespace Content.Client._RMC14.Announce;

public sealed class CRTOverlay : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public CRTSettings Settings { get; set; } = new();

    private float _scanlineOffset;
    private float _noiseTimer;
    private float _flickerTimer;
    private float _chromaticTimer;
    private readonly Random _random = new();

    public CRTOverlay()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
        CanKeyboardFocus = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!Settings.Enabled)
            return;

        var rect = PixelSizeBox;
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var deltaTime = (float)_timing.FrameTime.TotalSeconds;
        UpdateAnimations(deltaTime);

        if (Settings.ShowVignette)
            DrawVignette(handle, rect);

        if (Settings.ShowNoise)
            DrawNoise(handle, rect);

        if (Settings.ShowScanlines)
            DrawScanlines(handle, rect);

        if (Settings.ShowChromaticAberration)
            DrawChromaticAberration(handle, rect);

        DrawFlicker(handle, rect);
    }

    private void UpdateAnimations(float deltaTime)
    {
        _scanlineOffset += Settings.ScanlineSpeed * deltaTime * 60f;
        if (_scanlineOffset > Settings.ScanlineSpacing)
            _scanlineOffset -= Settings.ScanlineSpacing;

        _noiseTimer += deltaTime;
        _flickerTimer += deltaTime;
        _chromaticTimer += deltaTime * Settings.ChromaticAnimationSpeed;
    }

    private void DrawScanlines(DrawingHandleScreen handle, UIBox2 rect)
    {
        var spacing = Math.Max(2f, Settings.ScanlineSpacing);
        var thickness = Math.Max(1f, Settings.ScanlineThickness);
        var alpha = Math.Clamp(Settings.ScanlineAlpha, 0f, 1f);

        var scanlineColor = Settings.ScanlineColor.WithAlpha(alpha);

        for (float y = rect.Top - _scanlineOffset; y < rect.Bottom + spacing; y += spacing)
        {
            if (y >= rect.Top && y <= rect.Bottom)
            {
                var wavePhase = _flickerTimer * 0.5f;
                var wave = MathF.Sin((y / rect.Height) * MathF.PI * Settings.ScanlineWaveFrequency + wavePhase) * Settings.ScanlineWaveAmplitude;
                var scanlineRect = new UIBox2(rect.Left + wave, y, rect.Right + wave, y + thickness);

                var clampedRect = new UIBox2(
                    Math.Max(scanlineRect.Left, rect.Left),
                    Math.Max(scanlineRect.Top, rect.Top),
                    Math.Min(scanlineRect.Right, rect.Right),
                    Math.Min(scanlineRect.Bottom, rect.Bottom)
                );

                if (clampedRect.Width > 0 && clampedRect.Height > 0)
                {
                    var baseIntensity = 1f - Settings.ScanlineFlickerIntensity;
                    var intensity = baseIntensity + MathF.Sin(y * 0.05f + _flickerTimer * Settings.ScanlineFlickerSpeed) * Settings.ScanlineFlickerIntensity;
                    var dynamicColor = scanlineColor.WithAlpha(alpha * intensity);
                    handle.DrawRect(clampedRect, dynamicColor);
                }
            }
        }

        if (_random.NextSingle() < Settings.ScanlineGlitchChance)
        {
            var randomY = rect.Top + _random.NextSingle() * rect.Height;
            var glitchRect = new UIBox2(rect.Left, randomY, rect.Right, randomY + thickness);
            var glitchColor = Settings.ScanlineGlitchColor.WithAlpha(Settings.ScanlineGlitchAlpha);
            handle.DrawRect(glitchRect, glitchColor);
        }
    }

    private void DrawNoise(DrawingHandleScreen handle, UIBox2 rect)
    {
        if (_noiseTimer < Settings.NoiseUpdateFrequency)
            return;

        _noiseTimer = 0f;
        var noiseCount = (int)(rect.Width * rect.Height * Settings.NoiseIntensity * 0.001f);

        for (int i = 0; i < noiseCount; i++)
        {
            var x = rect.Left + _random.NextSingle() * rect.Width;
            var y = rect.Top + _random.NextSingle() * rect.Height;
            var size = Settings.NoiseMinSize + _random.NextSingle() * (Settings.NoiseMaxSize - Settings.NoiseMinSize);
            var alpha = _random.NextSingle() * Settings.NoiseAlpha;

            var noiseRect = new UIBox2(x, y, x + size, y + size);
            var noiseColor = _random.NextSingle() > 0.7f ? Color.White : Color.Gray;
            handle.DrawRect(noiseRect, noiseColor.WithAlpha(alpha));
        }

        if (_random.NextSingle() < Settings.NoiseStaticChance)
        {
            var x = rect.Left + _random.NextSingle() * rect.Width;
            var y = rect.Top + _random.NextSingle() * rect.Height;
            var width = Settings.NoiseStaticMinWidth + _random.NextSingle() * (Settings.NoiseStaticMaxWidth - Settings.NoiseStaticMinWidth);
            var height = Settings.NoiseStaticMinHeight + _random.NextSingle() * (Settings.NoiseStaticMaxHeight - Settings.NoiseStaticMinHeight);

            var staticRect = new UIBox2(x, y, x + width, y + height);
            var staticColor = Color.White.WithAlpha(Settings.NoiseStaticAlpha);
            handle.DrawRect(staticRect, staticColor);
        }
    }

    private void DrawVignette(DrawingHandleScreen handle, UIBox2 rect)
    {
        var vignetteSize = Math.Min(rect.Width, rect.Height) * Settings.VignetteSizeMultiplier;
        var baseAlpha = Settings.VignetteIntensity * Settings.VignetteAlphaMultiplier;
        var pulse = MathF.Sin(_flickerTimer * Settings.VignettePulseSpeed) * Settings.VignettePulseAmplitude;
        var alpha = baseAlpha + pulse;

        var vignetteColor = Settings.VignetteColor.WithAlpha(alpha);

        var cornerSize = vignetteSize * Settings.VignetteCornerSize;

        var topLeft = new UIBox2(rect.Left, rect.Top, rect.Left + cornerSize, rect.Top + cornerSize);
        handle.DrawRect(topLeft, vignetteColor);

        var topRight = new UIBox2(rect.Right - cornerSize, rect.Top, rect.Right, rect.Top + cornerSize);
        handle.DrawRect(topRight, vignetteColor);

        var bottomLeft = new UIBox2(rect.Left, rect.Bottom - cornerSize, rect.Left + cornerSize, rect.Bottom);
        handle.DrawRect(bottomLeft, vignetteColor);

        var bottomRight = new UIBox2(rect.Right - cornerSize, rect.Bottom - cornerSize, rect.Right, rect.Bottom);
        handle.DrawRect(bottomRight, vignetteColor);

        var edgeAlpha = alpha * Settings.VignetteEdgeAlpha;
        var edgeColor = vignetteColor.WithAlpha(edgeAlpha);

        var edgeThickness = vignetteSize * 0.3f;
        var topEdge = new UIBox2(rect.Left + cornerSize, rect.Top, rect.Right - cornerSize, rect.Top + edgeThickness);
        handle.DrawRect(topEdge, edgeColor);

        var bottomEdge = new UIBox2(rect.Left + cornerSize, rect.Bottom - edgeThickness, rect.Right - cornerSize, rect.Bottom);
        handle.DrawRect(bottomEdge, edgeColor);

        var leftEdge = new UIBox2(rect.Left, rect.Top + cornerSize, rect.Left + edgeThickness, rect.Bottom - cornerSize);
        handle.DrawRect(leftEdge, edgeColor);

        var rightEdge = new UIBox2(rect.Right - edgeThickness, rect.Top + cornerSize, rect.Right, rect.Bottom - cornerSize);
        handle.DrawRect(rightEdge, edgeColor);
    }

    private void DrawChromaticAberration(DrawingHandleScreen handle, UIBox2 rect)
    {
        if (!Settings.ShowChromaticAberration)
            return;

        var aberrationAmount = Settings.ChromaticAmount;
        var intensity = MathF.Sin(_chromaticTimer) * 0.5f + 0.5f;

        for (int i = 0; i < Settings.ChromaticParticleCount; i++)
        {
            if (_random.NextSingle() < Settings.ChromaticParticleChance)
            {
                var x = rect.Left + _random.NextSingle() * rect.Width;
                var y = rect.Top + _random.NextSingle() * rect.Height;
                var size = Settings.ChromaticParticleMinSize + _random.NextSingle() * (Settings.ChromaticParticleMaxSize - Settings.ChromaticParticleMinSize);

                var redOffset = aberrationAmount * intensity;
                var blueOffset = -aberrationAmount * intensity;

                var redRect = new UIBox2(x + redOffset, y, x + redOffset + size, y + size);
                var blueRect = new UIBox2(x + blueOffset, y, x + blueOffset + size, y + size);

                handle.DrawRect(redRect, Color.Red.WithAlpha(Settings.ChromaticParticleAlpha));
                handle.DrawRect(blueRect, Color.Blue.WithAlpha(Settings.ChromaticParticleAlpha));
            }
        }
    }

    private void DrawFlicker(DrawingHandleScreen handle, UIBox2 rect)
    {
        var flickerIntensity = MathF.Sin(_flickerTimer * 5f) * 0.5f + 0.5f;
        if (flickerIntensity > Settings.FlickerThreshold && _random.NextSingle() < Settings.FlickerChance)
        {
            var flickerColor = Settings.FlickerColor.WithAlpha(Settings.FlickerAlpha);
            handle.DrawRect(rect, flickerColor);
        }

        if (_random.NextSingle() < Settings.FlashChance)
        {
            var brightness = _random.NextSingle() * Settings.FlashMaxBrightness;
            var flashColor = Settings.GlowColor.WithAlpha(brightness);
            handle.DrawRect(rect, flashColor);
        }
    }
}
