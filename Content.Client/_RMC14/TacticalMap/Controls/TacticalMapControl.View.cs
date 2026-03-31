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
    }

    public void ApplyViewSettings()
    {
        if (Texture == null)
            return;

        Vector2 availableSize = new(PixelWidth, PixelHeight);
        if (availableSize.X <= 0 || availableSize.Y <= 0)
            return;

        _zoomFactor = Math.Clamp(_zoomFactor, MinZoom, MaxZoom);
        _panOffset = ClampPanOffset(availableSize, _zoomFactor, _panOffset);
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
        _panOffset = ClampPanOffset(availableSize, _zoomFactor, _panOffset);

        return true;
    }

    public void ResetZoomAndPan()
    {
        _zoomFactor = FitZoom;
        _panOffset = Vector2.Zero;
        ApplyViewSettings();
        NotifyViewChanged();
    }

    public bool IsAtFitZoom(float? zoomFactor = null)
    {
        return (zoomFactor ?? _zoomFactor) <= FitZoom + FitZoomEpsilon;
    }

    public Vector2 IndicesToPosition(Vector2i indices)
    {
        return GetDrawPosition(indices) * MapScale;
    }

    public Vector2i PositionToIndices(Vector2 controlPosition)
    {
        if (Texture == null)
            return Vector2i.Zero;

        if (!TryGetMapPixelPosition(controlPosition, out var pixelPosition))
            return Vector2i.Zero;

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

    private Vector2 ClampPanOffset(Vector2 availableSize, float zoomFactor, Vector2 panOffset)
    {
        if (Texture == null)
            return Vector2.Zero;

        if (IsAtFitZoom(zoomFactor))
            return Vector2.Zero;

        Vector2 textureSize = Texture.Size;
        float baseScaleX = availableSize.X / textureSize.X;
        float baseScaleY = availableSize.Y / textureSize.Y;
        float baseScale = Math.Min(baseScaleX, baseScaleY);
        Vector2 actualSize = textureSize * (baseScale * zoomFactor);

        Vector2 maxPan = new(
            Math.Max(0f, (actualSize.X - availableSize.X) * 0.5f),
            Math.Max(0f, (actualSize.Y - availableSize.Y) * 0.5f));

        return new Vector2(
            Math.Clamp(panOffset.X, -maxPan.X, maxPan.X),
            Math.Clamp(panOffset.Y, -maxPan.Y, maxPan.Y));
    }

    private float GetPanResistance(float zoomFactor)
    {
        if (IsAtFitZoom(zoomFactor))
            return 0f;

        if (zoomFactor >= FitZoom + PanResistanceZoomRange)
            return 1f;

        float normalized = (zoomFactor - FitZoom) / PanResistanceZoomRange;
        return MathF.Pow(Math.Clamp(normalized, 0f, 1f), PanResistanceExponent);
    }

    private bool TryGetMapPixelPosition(Vector2 controlPosition, out Vector2 pixelPosition, bool requireInside = false)
    {
        pixelPosition = Vector2.Zero;

        if (Texture == null)
            return false;

        Vector2 logicalPixelPosition = LogicalToPixel(controlPosition);
        (Vector2 actualSize, Vector2 actualTopLeft, _) = GetDrawParameters();
        UIBox2 mapRect = UIBox2.FromDimensions(actualTopLeft, actualSize);

        if (requireInside && !mapRect.Contains(logicalPixelPosition))
            return false;

        pixelPosition = new Vector2(
            Math.Clamp(logicalPixelPosition.X, mapRect.Left, mapRect.Right),
            Math.Clamp(logicalPixelPosition.Y, mapRect.Top, mapRect.Bottom));
        return true;
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
