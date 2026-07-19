using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Marines.GroundsideOperations;

/// <summary>
/// Draws the phosphor scanlines used by CM-SS13's crtblue theme.
/// </summary>
public sealed class GroundsideOperationsCrtPanel : PanelContainer
{
    private static readonly Color ScanlineColor = Color.FromHex("#00000040");

    private float _cachedHeight = -1;
    private float _cachedScale = -1;
    private float _cachedWidth = -1;
    private Vector2[] _scanlineVertices = [];

    public bool Scanlines { get; set; }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        EnsureGeometry();

        if (Scanlines && _scanlineVertices.Length > 0)
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _scanlineVertices, ScanlineColor);
    }

    private void EnsureGeometry()
    {
        if (MathHelper.CloseTo(_cachedWidth, PixelWidth) &&
            MathHelper.CloseTo(_cachedHeight, PixelHeight) &&
            MathHelper.CloseTo(_cachedScale, UIScale))
        {
            return;
        }

        _cachedWidth = PixelWidth;
        _cachedHeight = PixelHeight;
        _cachedScale = UIScale;

        var scanlineSpacing = Math.Max(2f, 2f * UIScale);
        _scanlineVertices = CreateHorizontalLines(PixelWidth, PixelHeight, scanlineSpacing);
    }

    private static Vector2[] CreateHorizontalLines(float width, float height, float spacing)
    {
        var vertices = new List<Vector2>();
        for (var y = 1f; y < height; y += spacing)
            AddRectangle(vertices, 0, y, width, Math.Min(y + 1, height));

        return vertices.ToArray();
    }

    private static void AddRectangle(List<Vector2> vertices, float left, float top, float right, float bottom)
    {
        if (left >= right || top >= bottom)
            return;

        var topLeft = new Vector2(left, top);
        var topRight = new Vector2(right, top);
        var bottomRight = new Vector2(right, bottom);
        var bottomLeft = new Vector2(left, bottom);

        vertices.Add(topLeft);
        vertices.Add(topRight);
        vertices.Add(bottomRight);
        vertices.Add(topLeft);
        vertices.Add(bottomRight);
        vertices.Add(bottomLeft);
    }
}
