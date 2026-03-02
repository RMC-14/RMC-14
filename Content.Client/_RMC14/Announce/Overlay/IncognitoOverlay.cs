using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.Announce;

/// <summary>
/// Simple static/noise overlay used for incognito mode on speaker sprites.
/// </summary>
public sealed class IncognitoOverlay : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private float _noiseTimer;

    public float NoiseIntensity { get; set; } = 6f;
    public float NoiseAlpha { get; set; } = 0.8f;
    public float NoiseUpdateFrequency { get; set; } = 0.05f;
    public float BaseAlpha { get; set; } = 0.65f;
    public float CenterMaskScale { get; set; } = 0.7f;
    public float CenterMaskAlpha { get; set; } = 0.35f;
    public float CenterNoiseMultiplier { get; set; } = 2.5f;
    public float EyeBandHeightFraction { get; set; } = 0.18f;
    public float EyeBandOffsetFraction { get; set; } = -0.05f;
    public float EyeBandAlpha { get; set; } = 0.9f;

    public IncognitoOverlay()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
        CanKeyboardFocus = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var rect = PixelSizeBox;
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var deltaTime = (float) _timing.FrameTime.TotalSeconds;
        _noiseTimer += deltaTime;

        // Base translucent cover.
        handle.DrawRect(rect, Color.Black.WithAlpha(BaseAlpha));
        DrawCenterMask(handle, rect);
        DrawEyeBand(handle, rect);

        if (_noiseTimer >= NoiseUpdateFrequency)
        {
            _noiseTimer = 0f;
        }

        var noiseCount = (int)(rect.Width * rect.Height * NoiseIntensity * 0.0005f);
        for (var i = 0; i < noiseCount; i++)
        {
            var x = rect.Left + _random.NextFloat() * rect.Width;
            var y = rect.Top + _random.NextFloat() * rect.Height;
            var size = 1.5f + _random.NextFloat() * 4.5f;
            var alpha = _random.NextFloat() * NoiseAlpha;
            var noiseRect = new UIBox2(x, y, x + size, y + size);
            var noiseColor = (_random.NextFloat() > 0.5f ? Color.White : Color.Black).WithAlpha(alpha);
            handle.DrawRect(noiseRect, noiseColor);
        }

        // Extra dense noise in the center to obscure faces.
        var centerCount = (int)(noiseCount * CenterNoiseMultiplier);
        var centerRect = GetCenterRect(rect);
        for (var i = 0; i < centerCount; i++)
        {
            var x = centerRect.Left + _random.NextFloat() * centerRect.Width;
            var y = centerRect.Top + _random.NextFloat() * centerRect.Height;
            var size = 2f + _random.NextFloat() * 5f;
            var alpha = 0.5f + _random.NextFloat() * (NoiseAlpha * 0.5f);
            var noiseRect = new UIBox2(x, y, x + size, y + size);
            var noiseColor = (_random.NextFloat() > 0.5f ? Color.White : Color.Black).WithAlpha(alpha);
            handle.DrawRect(noiseRect, noiseColor);
        }
    }

    private void DrawCenterMask(DrawingHandleScreen handle, UIBox2 rect)
    {
        var width = rect.Width * CenterMaskScale;
        var height = rect.Height * CenterMaskScale;
        var left = rect.Left + (rect.Width - width) / 2f;
        var top = rect.Top + (rect.Height - height) / 2f;
        var centerRect = new UIBox2(left, top, left + width, top + height);
        handle.DrawRect(centerRect, Color.Black.WithAlpha(CenterMaskAlpha));
    }

    private UIBox2 GetCenterRect(UIBox2 rect)
    {
        var width = rect.Width * CenterMaskScale;
        var height = rect.Height * CenterMaskScale;
        var left = rect.Left + (rect.Width - width) / 2f;
        var top = rect.Top + (rect.Height - height) / 2f;
        return new UIBox2(left, top, left + width, top + height);
    }

    private void DrawEyeBand(DrawingHandleScreen handle, UIBox2 rect)
    {
        var bandHeight = rect.Height * EyeBandHeightFraction;
        var bandTop = rect.Top + (rect.Height * 0.5f) - bandHeight * 0.5f + rect.Height * EyeBandOffsetFraction;
        var bandRect = new UIBox2(rect.Left, bandTop, rect.Right, bandTop + bandHeight);
        handle.DrawRect(bandRect, Color.Black.WithAlpha(EyeBandAlpha));
    }
}
