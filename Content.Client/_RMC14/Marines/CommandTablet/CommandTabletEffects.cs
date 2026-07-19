using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Marines.CommandTablet;

public sealed class CommandTabletScanlinePanel : PanelContainer
{
    private static readonly Color ScanlineColor = Color.FromHex("#0000002E");

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        DrawScanlines(handle, PixelWidth, PixelHeight, UIScale);
    }

    internal static void DrawScanlines(
        DrawingHandleScreen handle,
        float width,
        float height,
        float uiScale)
    {
        var spacing = Math.Max(2f, 3f * uiScale);
        for (var y = 1f; y < height; y += spacing)
            handle.DrawRect(new UIBox2(0, y, width, Math.Min(y + 1, height)), ScanlineColor);
    }
}

public sealed class CommandTabletWarningPanel : PanelContainer
{
    private static readonly Color StripeColor = Color.FromHex("#5F270A66");

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var stripeWidth = 18f * UIScale;
        var vertices = new Vector2[6];
        for (var x = -(float) PixelHeight; x < PixelWidth; x += stripeWidth * 2)
        {
            var topLeft = new Vector2(x, 0);
            var topRight = new Vector2(x + stripeWidth, 0);
            var bottomRight = new Vector2(x + stripeWidth + PixelHeight, PixelHeight);
            var bottomLeft = new Vector2(x + PixelHeight, PixelHeight);

            vertices[0] = topLeft;
            vertices[1] = topRight;
            vertices[2] = bottomRight;
            vertices[3] = topLeft;
            vertices[4] = bottomRight;
            vertices[5] = bottomLeft;
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, StripeColor);
        }

        CommandTabletScanlinePanel.DrawScanlines(handle, PixelWidth, PixelHeight, UIScale);
    }
}
