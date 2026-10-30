using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public sealed class RMCBevelStyleBox : StyleBox
{
    private const float Outline = 1;
    private const float Bevel = 2;

    public Color BackgroundColor { get; set; }
    public Color BevelColor { get; set; } = RMCMachineUiStyle.BevelColor;
    public Color OutlineColor { get; set; } = RMCMachineUiStyle.ButtonTextColor;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var outline = Outline * uiScale;
        var bevel = Bevel * uiScale;

        handle.DrawRect(box, OutlineColor);

        var inner = new UIBox2(box.Left + outline, box.Top + outline, box.Right - outline, box.Bottom - outline);
        handle.DrawRect(inner, Color.InterpolateBetween(BevelColor, Color.Black, 0.4f));
        handle.DrawRect(new UIBox2(inner.Left, inner.Top, inner.Right - bevel, inner.Top + bevel), BevelColor);
        handle.DrawRect(new UIBox2(inner.Left, inner.Top, inner.Left + bevel, inner.Bottom - bevel), BevelColor);

        handle.DrawRect(new UIBox2(inner.Left + bevel, inner.Top + bevel, inner.Right - bevel, inner.Bottom - bevel), BackgroundColor);
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return Outline + Bevel;
    }
}
