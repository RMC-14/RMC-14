using System.Numerics;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed partial class TacticalMapControl
{
    private const float MinZoom = 0.3f;
    private const float MaxZoom = 5f;
    private const float FitZoom = 1f;
    private const float FitZoomEpsilon = 0.001f;
    private const float PanResistanceZoomRange = 0.35f;
    private const float PanResistanceExponent = 1.5f;
    private const float ZoomSpeed = 1.2f;

    private float _zoom = FitZoom;
    private Vector2 _pan;
    private (float Zoom, Vector2 Pan)? _pendingView;

    public float Zoom => _zoom;
    public Vector2 Pan => _pan;

    public void SetView(float zoom, Vector2 pan)
    {
        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        _pan = pan;
        if (PixelWidth <= 0 || PixelHeight <= 0 || !_hasBase)
        {
            _pendingView = (_zoom, pan);
            return;
        }

        _pendingView = null;
        ClampView();
    }

    public void ResetView()
    {
        _zoom = FitZoom;
        _pan = Vector2.Zero;
    }

    public float PixelsPerTile
    {
        get
        {
            if (!_hasBase || PixelWidth <= 0 || PixelHeight <= 0)
                return 1f;

            var baseScale = MathF.Min((float) PixelWidth / _width, (float) PixelHeight / _height);
            return baseScale * _zoom;
        }
    }

    private float GetOverlayScale()
    {
        return PixelsPerTile / OverlayUnitsPerTile;
    }

    private Vector2 GetTopLeft()
    {
        var size = new Vector2(_width, _height) * PixelsPerTile;
        return (new Vector2(PixelWidth, PixelHeight) - size) / 2f + _pan;
    }

    public Vector2 TileToPixel(Vector2 tile)
    {
        var scale = PixelsPerTile;
        var topLeft = GetTopLeft();
        return new Vector2(
            topLeft.X + (tile.X - _min.X) * scale,
            topLeft.Y + (_min.Y + _height - tile.Y) * scale);
    }

    public Vector2 PixelToTile(Vector2 pixel)
    {
        var scale = PixelsPerTile;
        var topLeft = GetTopLeft();
        return new Vector2(
            _min.X + (pixel.X - topLeft.X) / scale,
            _min.Y + _height - (pixel.Y - topLeft.Y) / scale);
    }

    public Vector2i PixelToIndices(Vector2 pixel)
    {
        var tile = PixelToTile(pixel);
        return new Vector2i((int) MathF.Floor(tile.X), (int) MathF.Floor(tile.Y));
    }

    public Vector2 IndicesToPixelCenter(Vector2i indices)
    {
        return TileToPixel(new Vector2(indices.X + 0.5f, indices.Y + 0.5f));
    }

    public bool CenterOn(Vector2i indices)
    {
        if (!_hasBase)
            return false;

        var scale = PixelsPerTile;
        var available = new Vector2(PixelWidth, PixelHeight);
        var centered = (available - new Vector2(_width, _height) * scale) / 2f;
        var offset = new Vector2(indices.X + 0.5f - _min.X, _min.Y + _height - (indices.Y + 0.5f)) * scale;
        _pan = available / 2f - centered - offset;
        ClampView();
        return true;
    }

    private void ClampView()
    {
        _zoom = Math.Clamp(_zoom, MinZoom, MaxZoom);
        if (!_hasBase || _zoom <= FitZoom + FitZoomEpsilon)
        {
            _pan = Vector2.Zero;
            return;
        }

        var size = new Vector2(_width, _height) * PixelsPerTile;
        var max = new Vector2(
            MathF.Max(0f, (size.X - PixelWidth) / 2f),
            MathF.Max(0f, (size.Y - PixelHeight) / 2f));

        _pan = new Vector2(Math.Clamp(_pan.X, -max.X, max.X), Math.Clamp(_pan.Y, -max.Y, max.Y));
    }

    private float GetPanResistance()
    {
        if (_zoom <= FitZoom + FitZoomEpsilon)
            return 0f;

        if (_zoom >= FitZoom + PanResistanceZoomRange)
            return 1f;

        var normalized = (_zoom - FitZoom) / PanResistanceZoomRange;
        return MathF.Pow(Math.Clamp(normalized, 0f, 1f), PanResistanceExponent);
    }

    private void ZoomAt(Vector2 pixel, bool zoomIn)
    {
        if (!_hasBase)
            return;

        var anchor = PixelToTile(pixel);
        var target = Math.Clamp(zoomIn ? _zoom * ZoomSpeed : _zoom / ZoomSpeed, MinZoom, MaxZoom);
        if (MathF.Abs(target - _zoom) < 0.001f)
            return;

        _zoom = target;
        if (_zoom <= FitZoom + FitZoomEpsilon)
        {
            _pan = Vector2.Zero;
        }
        else
        {
            _pan += pixel - TileToPixel(anchor);
            ClampView();
        }

    }

    private float GetBlipSize()
    {
        var overlay = GetOverlayScale();
        var baseSize = BaseBlipSize * (1f + (overlay - 1f) * 0.6f);
        var zoomReduction = 1f / MathF.Pow(MathF.Max(_zoom, 0.3f), 0.3f);
        return baseSize * zoomReduction * BlipSizeMultiplier;
    }

    protected override void Resized()
    {
        base.Resized();
        ApplyPendingView();
        ClampView();
    }

    private void ApplyPendingView()
    {
        if (_pendingView is not { } pending || PixelWidth <= 0 || PixelHeight <= 0 || !_hasBase)
            return;

        _pendingView = null;
        _zoom = pending.Zoom;
        _pan = pending.Pan;
    }
}
