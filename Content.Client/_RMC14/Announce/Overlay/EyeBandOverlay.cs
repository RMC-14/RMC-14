using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce;

/// <summary>
/// Simple overlay that draws a black bar across the eye region of a sprite.
/// </summary>
public sealed class EyeBandOverlay : Control
{
    public float BandHeightFraction { get; set; } = 0.18f;
    public float BandOffsetFraction { get; set; } = 0.02f;
    public float BandAlpha { get; set; } = 0.9f;
    public Color BandColor { get; set; } = Color.Black;

    public EyeBandOverlay()
    {
        MouseFilter = MouseFilterMode.Ignore;
        CanKeyboardFocus = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var rect = PixelSizeBox;
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var bandHeight = rect.Height * BandHeightFraction;
        var bandTop = rect.Top + (rect.Height * 0.5f) - bandHeight * 0.5f + rect.Height * BandOffsetFraction;
        var bandRect = new UIBox2(rect.Left, bandTop, rect.Right, bandTop + bandHeight);
        handle.DrawRect(bandRect, BandColor.WithAlpha(BandAlpha));
    }
}
