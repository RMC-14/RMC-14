using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed partial class TacticalMapControl
{
    public float GetCurrentZoomFactor()
    {
        return _zoomFactor;
    }

    public Vector2 GetCurrentPanOffset()
    {
        return _panOffset;
    }

    public void SetViewUpdateCallback(Action<float, Vector2> callback)
    {
        _viewUpdateCallback = callback;
    }

    private void NotifyViewChanged()
    {
        _viewUpdateCallback?.Invoke(_zoomFactor, _panOffset);
    }

    public void LoadViewSettings(float zoomFactor, Vector2 panOffset, EntityUid? mapEntity)
    {
        _currentMapEntity = mapEntity;
        _zoomFactor = Math.Clamp(zoomFactor, MinZoom, MaxZoom);
        _panOffset = panOffset;

        ApplyViewSettings();
        NotifyViewChanged();
    }

    public void ApplyViewSettings()
    {
        if (Texture == null)
            return;

        Vector2 availableSize = new(PixelWidth, PixelHeight);
        if (availableSize.X <= 0 || availableSize.Y <= 0)
            return;

        _zoomFactor = Math.Clamp(_zoomFactor, MinZoom, MaxZoom);

        float maxPan = Math.Max(availableSize.X, availableSize.Y) * _zoomFactor * 0.5f;
        _panOffset = Vector2.Clamp(_panOffset, new Vector2(-maxPan), new Vector2(maxPan));
    }

    public bool CenterOnPosition(Vector2i indices)
    {
        if (Texture == null)
            return false;

        Vector2 availableSize = new(PixelWidth, PixelHeight);

        if (availableSize.X <= 0 || availableSize.Y <= 0)
            return false;

        Vector2 targetPosition = IndicesToPosition(indices);

        Vector2 textureSize = Texture.Size;
        float baseScaleX = availableSize.X / textureSize.X;
        float baseScaleY = availableSize.Y / textureSize.Y;
        float baseScale = Math.Min(baseScaleX, baseScaleY);
        float actualScale = baseScale * _zoomFactor;
        Vector2 actualSize = textureSize * actualScale;
        float overlayScale = actualScale / MapScale;

        Vector2 screenCenter = availableSize / 2;
        _panOffset = screenCenter - targetPosition * overlayScale - (availableSize - actualSize) / 2;

        float maxPan = Math.Max(availableSize.X, availableSize.Y) * _zoomFactor * 0.5f;
        _panOffset = Vector2.Clamp(_panOffset, new Vector2(-maxPan), new Vector2(maxPan));

        return true;
    }

    public void ResetZoomAndPan()
    {
        _zoomFactor = 1.0f;
        _panOffset = Vector2.Zero;
        NotifyViewChanged();
    }

    public Vector2 IndicesToPosition(Vector2i indices)
    {
        return GetDrawPosition(indices) * MapScale;
    }

    public Vector2i PositionToIndices(Vector2 controlPosition)
    {
        if (Texture == null)
            return Vector2i.Zero;

        Vector2 pixelPosition = LogicalToPixel(controlPosition);
        (Vector2 actualSize, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();

        Vector2 relativeToTexture = (pixelPosition - actualTopLeft) / overlayScale;

        return new Vector2i(
            (int)(relativeToTexture.X / MapScale) + _min.X,
            _delta.Y - (int)(relativeToTexture.Y / MapScale) + _min.Y
        );
    }

    public Vector2 ConvertIndicesToLineCoordinates(Vector2i indices)
    {
        return GetDrawPosition(indices) * MapScale;
    }

    private float GetUIScale()
    {
        return Width > 0 ? PixelWidth / Width : 1.0f;
    }

    private Vector2 LogicalToPixel(Vector2 logicalPosition)
    {
        float uiScale = GetUIScale();
        return logicalPosition * uiScale;
    }

    private Vector2 PixelToLogical(Vector2 pixelPosition)
    {
        float uiScale = GetUIScale();
        return pixelPosition / uiScale;
    }

    private float GetScaledBlipSize(float overlayScale)
    {
        float baseScaledSize = BaseBlipSize * (1.0f + (overlayScale - 1.0f) * 0.6f);
        float zoomReduction = 1.0f / (float)Math.Pow(Math.Max(_zoomFactor, 0.3f), 0.3f);
        return baseScaledSize * zoomReduction * BlipSizeMultiplier;
    }

    private (Vector2 actualSize, Vector2 actualTopLeft, float overlayScale) GetDrawParameters()
    {
        if (Texture == null)
            return (Vector2.Zero, Vector2.Zero, 1.0f);

        Vector2 textureSize = Texture.Size;
        Vector2 availableSize = new(PixelWidth, PixelHeight);

        float baseScaleX = availableSize.X / textureSize.X;
        float baseScaleY = availableSize.Y / textureSize.Y;
        float baseScale = Math.Min(baseScaleX, baseScaleY);
        float actualScale = baseScale * _zoomFactor;

        Vector2 actualSize = textureSize * actualScale;
        Vector2 actualTopLeft = (availableSize - actualSize) / 2 + _panOffset;
        float overlayScale = actualScale / MapScale;

        return (actualSize, actualTopLeft, overlayScale);
    }

    private Vector2i GetDrawPosition(Vector2i pos)
    {
        return new Vector2i(pos.X - _min.X, _delta.Y - (pos.Y - _min.Y));
    }

    private Vector2 PositionToLineCoordinatesFloat(Vector2 controlPosition, Vector2 actualTopLeft, float overlayScale)
    {
        Vector2 pixelPosition = LogicalToPixel(controlPosition);
        return (pixelPosition - actualTopLeft) / overlayScale;
    }
}
