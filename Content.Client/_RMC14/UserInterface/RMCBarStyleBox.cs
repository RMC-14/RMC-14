using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public sealed class RMCBarStyleBox : StyleBox
{
    private const float Outline = 1;
    private const float Inset = 1;

    private static readonly Color ShadowColor = Color.Black.WithAlpha(0.6f);
    private static readonly Color LightColor = Color.White.WithAlpha(0.1f);
    private static readonly Color HighlightColor = Color.White.WithAlpha(0.25f);
    private static readonly Color EdgeColor = Color.Black.WithAlpha(0.45f);

    public Color FillColor { get; set; }
    public bool Track { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var outline = Outline * uiScale;
        var inset = Inset * uiScale;
        var inner = new UIBox2(box.Left + outline + inset, box.Top + outline + inset, box.Right - outline - inset, box.Bottom - outline - inset);

        if (Track)
        {
            handle.DrawRect(box, RMCMachineUiStyle.ButtonTextColor);

            var well = new UIBox2(box.Left + outline, box.Top + outline, box.Right - outline, box.Bottom - outline);
            handle.DrawRect(well, LightColor);
            handle.DrawRect(new UIBox2(well.Left, well.Top, well.Right - inset, well.Bottom - inset), ShadowColor);
            handle.DrawRect(inner, RMCMachineUiStyle.FieldColor);
            return;
        }

        if (inner.Width <= 0 || inner.Height <= 0)
            return;

        handle.DrawRect(inner, Color.InterpolateBetween(FillColor, Color.Black, 0.25f));
        RMCStripedStyleBox.DrawStripes(handle, inner, uiScale);
        handle.DrawRect(new UIBox2(inner.Left, inner.Top, inner.Right, inner.Top + uiScale), HighlightColor);
        handle.DrawRect(new UIBox2(inner.Right - uiScale, inner.Top, inner.Right, inner.Bottom), EdgeColor);
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return Outline + Inset;
    }
}
