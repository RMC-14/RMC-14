using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface.Crt;

internal sealed class RMCCrtEffectRenderer
{
    private float _cachedHeight = -1;
    private float _cachedRgbWidth = -1;
    private float _cachedScale = -1;
    private float _cachedScanlineSpacing = -1;
    private float _cachedScanlineThickness = -1;
    private float _cachedStripeWidth = -1;
    private float _cachedWidth = -1;
    private RMCCrtEffects _cachedEffects;
    private Vector2[] _blueSubpixels = [];
    private Vector2[] _diagonalStripes = [];
    private Vector2[] _greenSubpixels = [];
    private Vector2[] _horizontalScanlines = [];
    private Vector2[] _redSubpixels = [];

    internal int GeometryGeneration { get; private set; }
    internal int HorizontalVertexCount => _horizontalScanlines.Length;
    internal int RgbVertexCount => _redSubpixels.Length + _greenSubpixels.Length + _blueSubpixels.Length;
    internal int StripeVertexCount => _diagonalStripes.Length;

    public void Draw(
        DrawingHandleScreen handle,
        float width,
        float height,
        float uiScale,
        RMCCrtEffects effects,
        float scanlineSpacing,
        float scanlineThickness,
        float rgbWidth,
        float stripeWidth,
        float scanlineOpacity,
        float rgbOpacity,
        Color stripeColor)
    {
        UpdateGeometry(
            width,
            height,
            uiScale,
            effects,
            scanlineSpacing,
            scanlineThickness,
            rgbWidth,
            stripeWidth);

        if (_horizontalScanlines.Length > 0)
            handle.DrawPrimitives(
                DrawPrimitiveTopology.TriangleList,
                _horizontalScanlines,
                Color.Black.WithAlpha(Math.Clamp(scanlineOpacity, 0, 1)));

        if (_redSubpixels.Length > 0)
            handle.DrawPrimitives(
                DrawPrimitiveTopology.TriangleList,
                _redSubpixels,
                Color.Red.WithAlpha(Math.Clamp(rgbOpacity, 0, 1)));
        if (_greenSubpixels.Length > 0)
            handle.DrawPrimitives(
                DrawPrimitiveTopology.TriangleList,
                _greenSubpixels,
                Color.Lime.WithAlpha(Math.Clamp(rgbOpacity * 0.35f, 0, 1)));
        if (_blueSubpixels.Length > 0)
            handle.DrawPrimitives(
                DrawPrimitiveTopology.TriangleList,
                _blueSubpixels,
                Color.Blue.WithAlpha(Math.Clamp(rgbOpacity, 0, 1)));

        if (_diagonalStripes.Length > 0)
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _diagonalStripes, stripeColor);
    }

    internal void UpdateGeometry(
        float width,
        float height,
        float uiScale,
        RMCCrtEffects effects,
        float scanlineSpacing,
        float scanlineThickness,
        float rgbWidth,
        float stripeWidth)
    {
        if (MathHelper.CloseTo(_cachedWidth, width) &&
            MathHelper.CloseTo(_cachedHeight, height) &&
            MathHelper.CloseTo(_cachedScale, uiScale) &&
            MathHelper.CloseTo(_cachedScanlineSpacing, scanlineSpacing) &&
            MathHelper.CloseTo(_cachedScanlineThickness, scanlineThickness) &&
            MathHelper.CloseTo(_cachedRgbWidth, rgbWidth) &&
            MathHelper.CloseTo(_cachedStripeWidth, stripeWidth) &&
            _cachedEffects == effects)
        {
            return;
        }

        _cachedWidth = width;
        _cachedHeight = height;
        _cachedScale = uiScale;
        _cachedEffects = effects;
        _cachedScanlineSpacing = scanlineSpacing;
        _cachedScanlineThickness = scanlineThickness;
        _cachedRgbWidth = rgbWidth;
        _cachedStripeWidth = stripeWidth;
        GeometryGeneration++;

        _horizontalScanlines = effects.HasFlag(RMCCrtEffects.HorizontalScanlines)
            ? CreateHorizontalLines(width, height, uiScale, scanlineSpacing, scanlineThickness)
            : [];

        if (effects.HasFlag(RMCCrtEffects.RgbSubpixels))
            CreateRgbSubpixels(width, height, uiScale, rgbWidth);
        else
            (_redSubpixels, _greenSubpixels, _blueSubpixels) = ([], [], []);

        _diagonalStripes = effects.HasFlag(RMCCrtEffects.DiagonalStripes)
            ? CreateDiagonalStripes(width, height, uiScale, stripeWidth)
            : [];
    }

    private static Vector2[] CreateHorizontalLines(
        float width,
        float height,
        float uiScale,
        float spacing,
        float thickness)
    {
        var scaledSpacing = Math.Max(2f, spacing * uiScale);
        var scaledThickness = Math.Max(1f, thickness * uiScale);
        var vertices = new List<Vector2>();
        for (var y = 1f; y < height; y += scaledSpacing)
            AddRectangle(vertices, 0, y, width, Math.Min(y + scaledThickness, height));

        return vertices.ToArray();
    }

    private void CreateRgbSubpixels(float width, float height, float uiScale, float subpixelWidth)
    {
        var red = new List<Vector2>();
        var green = new List<Vector2>();
        var blue = new List<Vector2>();
        var scaledWidth = Math.Max(1f, subpixelWidth * uiScale);
        var groupWidth = scaledWidth * 3;
        for (var x = 0f; x < width; x += groupWidth)
        {
            AddRectangle(red, x, 0, Math.Min(x + scaledWidth, width), height);
            AddRectangle(green, x + scaledWidth, 0, Math.Min(x + scaledWidth * 2, width), height);
            AddRectangle(blue, x + scaledWidth * 2, 0, Math.Min(x + groupWidth, width), height);
        }

        _redSubpixels = red.ToArray();
        _greenSubpixels = green.ToArray();
        _blueSubpixels = blue.ToArray();
    }

    private static Vector2[] CreateDiagonalStripes(float width, float height, float uiScale, float stripeWidth)
    {
        var scaledWidth = Math.Max(2f, stripeWidth * uiScale);
        var vertices = new List<Vector2>();
        for (var x = -height; x < width; x += scaledWidth * 2)
        {
            var topLeft = new Vector2(x, 0);
            var topRight = new Vector2(x + scaledWidth, 0);
            var bottomRight = new Vector2(x + scaledWidth + height, height);
            var bottomLeft = new Vector2(x + height, height);

            vertices.Add(topLeft);
            vertices.Add(topRight);
            vertices.Add(bottomRight);
            vertices.Add(topLeft);
            vertices.Add(bottomRight);
            vertices.Add(bottomLeft);
        }

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
