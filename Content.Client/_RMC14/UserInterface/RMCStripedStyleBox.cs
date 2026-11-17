using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public sealed class RMCStripedStyleBox : StyleBox
{
    private const int StripeWidth = 14;
    private const int StripePeriod = 28;

    private static readonly Color StripeColor = Color.Black.WithAlpha(0.2f);

    public Color BackgroundColor { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        handle.DrawRect(box, BackgroundColor);
        DrawStripes(handle, box, uiScale);
    }

    public static void DrawStripes(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var left = MathF.Floor(box.Left);
        var top = MathF.Floor(box.Top);
        var right = MathF.Ceiling(box.Right);
        var bottom = MathF.Ceiling(box.Bottom);
        var width = StripeWidth * uiScale;
        var period = StripePeriod * uiScale;

        for (var start = left + top; start < right + bottom; start += period)
        {
            for (var offset = 0; offset < width; offset++)
            {
                var sum = start + offset;
                var x0 = MathF.Max(left, sum - bottom);
                var x1 = MathF.Min(right, sum - top);
                if (x0 >= x1)
                    continue;

                handle.DrawLine(new Vector2(x0, sum - x0), new Vector2(x1, sum - x1), StripeColor);
            }
        }
    }
}
