using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._RMC14.TacticalMap;

public sealed partial class TacticalMapControl
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        if (Texture == null)
            return;

        SpriteSystem system = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>();
        TimeSpan curTime = IoCManager.Resolve<IGameTiming>().CurTime;
        SpriteSpecifier.Rsi backgroundRsi = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "background");
        SpriteSpecifier.Rsi defibbableRsi = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "defibbable");
        SpriteSpecifier.Rsi defibbableRsi2 = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "defibbable2");
        SpriteSpecifier.Rsi defibbableRsi3 = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "defibbable3");
        SpriteSpecifier.Rsi defibbableRsi4 = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "defibbable4");
        SpriteSpecifier.Rsi undefibbableRsi = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "undefibbable");
        SpriteSpecifier.Rsi hiveLeaderRsi = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "xenoleader");
        SpriteSpecifier.Rsi mortarRsi = new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "mortar");
        Texture background = system.Frame0(backgroundRsi);

        (Vector2 actualSize, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();

        UIBox2 textureRect = UIBox2.FromDimensions(actualTopLeft, actualSize);
        if (_backgroundTexture != null)
            handle.DrawTextureRect(_backgroundTexture, textureRect);
        handle.DrawTextureRect(Texture, textureRect);

        if (ShowLinkedLzOverlay && _overlayLinkedLzTexture != null)
            handle.DrawTextureRect(_overlayLinkedLzTexture, textureRect);
        DrawIconOverlay(handle, textureRect, "roof0", ShowRoof0Overlay);
        DrawIconOverlay(handle, textureRect, "roof1", ShowRoof1Overlay);
        DrawIconOverlay(handle, textureRect, "roof2", ShowRoof2Overlay);
        DrawIconOverlay(handle, textureRect, "roof3", ShowRoof3Overlay);
        DrawIconOverlay(handle, textureRect, "roof4", ShowRoof4Overlay);

        DrawMortarOverlay(handle, actualTopLeft, overlayScale);
        DrawTileGrid(handle, actualTopLeft, actualSize);
        DrawModeBorder(handle, actualTopLeft, actualSize, overlayScale);
        DrawLines(handle, overlayScale, actualTopLeft);
        DrawPreviewLine(handle, overlayScale, actualTopLeft);
        DrawPreviewSquare(handle, overlayScale, actualTopLeft);
        DrawRadiusPreview(handle, actualTopLeft, actualSize, overlayScale);
        DrawHiveStructureOverlays(handle, system, background, actualTopLeft, overlayScale, curTime);
        DrawBlips(handle, system, background, defibbableRsi, defibbableRsi2, defibbableRsi3, defibbableRsi4, undefibbableRsi, hiveLeaderRsi, actualTopLeft, overlayScale, curTime);
        DrawMortarMarker(handle, system, mortarRsi, actualTopLeft, overlayScale, curTime);
        DrawEraserPreview(handle);
        DrawLabels(handle, overlayScale, actualTopLeft);
    }

    private const float RadiusPreviewFillAlpha = 0.12f;
    private const float MortarOverlayFillAlpha = 0.4f;
    private const float HiveOverlayFillAlpha = 0.3f;
    private static readonly ResPath MapBlipsRsiPath = new("/Textures/_RMC14/Interface/map_blips.rsi");
    private static readonly Color HiveCoreOverlayColor = Color.FromHex("#9B59B6");
    private static readonly Color HivePylonOverlayColor = Color.FromHex("#4FD2A3");
    private const string HiveCoreBlipState = "core";
    private const string HivePylonBlipState = "pylon";

    private void DrawRadiusPreview(DrawingHandleScreen handle, Vector2 actualTopLeft, Vector2 actualSize, float overlayScale)
    {
        if (!RadiusPreviewEnabled || RadiusPreviewTiles <= 0f || _lastMousePosition == null)
            return;

        if (overlayScale <= 0.0001f)
            return;

        Vector2 pixelPosition = LogicalToPixel(_lastMousePosition.Value);
        UIBox2 mapRect = UIBox2.FromDimensions(actualTopLeft, actualSize);
        if (!mapRect.Contains(pixelPosition))
            return;

        Vector2 relativeToTexture = (pixelPosition - actualTopLeft) / overlayScale;
        Vector2i indices = new(
            (int)(relativeToTexture.X / MapScale) + _min.X,
            _delta.Y - (int)(relativeToTexture.Y / MapScale) + _min.Y);

        if (!IsWithinMap(indices))
            return;

        DrawTileRadiusOverlay(handle, actualTopLeft, overlayScale, indices, RadiusPreviewTiles, RadiusPreviewColor, RadiusPreviewFillAlpha, RadiusPreviewFilter);
    }

    private void DrawMortarOverlay(DrawingHandleScreen handle, Vector2 actualTopLeft, float overlayScale)
    {
        if (_mortarOverlayCenter == null || _mortarOverlayTiles.Count == 0)
            return;

        float tileSize = MapScale * overlayScale;
        if (tileSize <= 0.5f)
            return;

        Color fillColor = _mortarOverlayColor.WithAlpha(MortarOverlayFillAlpha);
        Vector2 tileDimensions = new(tileSize, tileSize);

        foreach (var indices in _mortarOverlayTiles)
        {
            Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
            handle.DrawRect(UIBox2.FromDimensions(position, tileDimensions), fillColor);
        }
    }

    private void DrawMortarMarker(
        DrawingHandleScreen handle,
        SpriteSystem system,
        SpriteSpecifier.Rsi mortarRsi,
        Vector2 actualTopLeft,
        float overlayScale,
        TimeSpan curTime)
    {
        if (_mortarOverlayCenter == null)
            return;

        Vector2 position = IndicesToPosition(_mortarOverlayCenter.Value) * overlayScale + actualTopLeft;
        float tileSize = MapScale * overlayScale;
        float scaledBlipSize = GetScaledBlipSize(overlayScale);
        Vector2 centeredPosition = GetCenteredMarkerPosition(position, tileSize, scaledBlipSize);
        UIBox2 rect = UIBox2.FromDimensions(centeredPosition, new Vector2(scaledBlipSize, scaledBlipSize));
        handle.DrawTextureRect(system.GetFrame(mortarRsi, curTime), rect);
    }

    private void DrawHiveStructureOverlays(
        DrawingHandleScreen handle,
        SpriteSystem system,
        Texture background,
        Vector2 actualTopLeft,
        float overlayScale,
        TimeSpan curTime)
    {
        if (_blips == null)
            return;

        if (overlayScale <= 0.0001f)
            return;

        float tileSize = MapScale * overlayScale;
        if (tileSize <= 0.5f)
            return;

        foreach (var blip in _blips)
        {
            if (!TryGetHiveOverlay(blip, out var radiusTiles, out var overlayColor))
                continue;

            Vector2 position = IndicesToPosition(blip.Indices) * overlayScale + actualTopLeft;

            if (radiusTiles > 0f)
                DrawTileRadiusOverlay(handle, actualTopLeft, overlayScale, blip.Indices, radiusTiles, overlayColor, HiveOverlayFillAlpha, RadiusPreviewFilterMode.None);

            float scaledBlipSize = GetScaledBlipSize(overlayScale);
            Vector2 centeredPosition = GetCenteredMarkerPosition(position, tileSize, scaledBlipSize);
            UIBox2 rect = UIBox2.FromDimensions(centeredPosition, new Vector2(scaledBlipSize, scaledBlipSize));
            var backTexture = blip.Background != null ? system.GetFrame(blip.Background, curTime) : background;
            handle.DrawTextureRect(backTexture, rect, blip.Color);
            handle.DrawTextureRect(system.GetFrame(blip.Image, curTime), rect);
        }
    }

    private bool TryGetHiveOverlay(TacticalMapBlip blip, out float radiusTiles, out Color overlayColor)
    {
        radiusTiles = 0f;
        overlayColor = Color.Transparent;

        if (blip.Image.RsiPath != MapBlipsRsiPath)
            return false;

        switch (blip.Image.RsiState)
        {
            case HiveCoreBlipState:
                radiusTiles = HiveCoreRangeTiles;
                overlayColor = HiveCoreOverlayColor;
                return true;
            case HivePylonBlipState:
                radiusTiles = HivePylonRangeTiles;
                overlayColor = HivePylonOverlayColor;
                return true;
            default:
                return false;
        }
    }

    private bool IsHiveStructureBlip(TacticalMapBlip blip)
    {
        if (blip.Image.RsiPath != MapBlipsRsiPath)
            return false;

        return blip.Image.RsiState == HiveCoreBlipState || blip.Image.RsiState == HivePylonBlipState;
    }

    private static Vector2 GetCenteredMarkerPosition(Vector2 topLeft, float tileSize, float markerSize)
    {
        return topLeft + new Vector2((tileSize - markerSize) * 0.5f, (tileSize - markerSize) * 0.5f);
    }

    private void DrawTileRadiusOverlay(
        DrawingHandleScreen handle,
        Vector2 actualTopLeft,
        float overlayScale,
        Vector2i center,
        float radiusTiles,
        Color color,
        float alpha,
        RadiusPreviewFilterMode filterMode)
    {
        if (radiusTiles <= 0f)
            return;

        float tileSize = MapScale * overlayScale;
        if (tileSize <= 0.5f)
            return;

        int maxRange = (int)MathF.Ceiling(radiusTiles);
        float radiusSquared = radiusTiles * radiusTiles;
        Color fillColor = color.WithAlpha(alpha);
        Vector2 tileDimensions = new(tileSize, tileSize);

        for (int dx = -maxRange; dx <= maxRange; dx++)
        {
            for (int dy = -maxRange; dy <= maxRange; dy++)
            {
                float distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > radiusSquared)
                    continue;

                Vector2i indices = new(center.X + dx, center.Y + dy);
                if (!IsWithinMap(indices))
                    continue;

                if (!IsRadiusPreviewTileAllowed(indices, filterMode))
                    continue;

                Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
                handle.DrawRect(UIBox2.FromDimensions(position, tileDimensions), fillColor);
            }
        }
    }

    private bool IsRadiusPreviewTileAllowed(Vector2i indices, RadiusPreviewFilterMode filterMode)
    {
        return filterMode switch
        {
            RadiusPreviewFilterMode.Mortar => IsMortarFireAllowed(indices),
            RadiusPreviewFilterMode.OrbitalBombardment => IsOrbitalBombardAllowed(indices),
            _ => true
        };
    }

    private void DrawIconOverlay(DrawingHandleScreen handle, UIBox2 textureRect, string key, bool enabled)
    {
        if (!enabled)
            return;

        if (_overlayIconTextures.TryGetValue(key, out var overlay))
            handle.DrawTextureRect(overlay, textureRect);
    }

    private void DrawTileGrid(DrawingHandleScreen handle, Vector2 actualTopLeft, Vector2 actualSize)
    {
        if (Texture == null)
            return;

        Vector2 textureSize = Texture.Size;
        if (textureSize.X <= 0 || textureSize.Y <= 0)
            return;

        if (_tileMaskWidth <= 0 || _tileMaskHeight <= 0)
            return;

        float tileWidth = actualSize.X / _tileMaskWidth;
        float tileHeight = actualSize.Y / _tileMaskHeight;
        float minTile = Math.Min(tileWidth, tileHeight);
        if (minTile < 1.5f)
            return;

        float thickness = Math.Clamp(minTile * 0.12f, 1f, 1.25f);
        if (thickness > minTile)
            thickness = minTile;
        Color gridColor = Color.FromHex("#88C7FA").WithAlpha(0.06f);
        _gridShader.SetParameter("tile_size", new Vector2(tileWidth, tileHeight));
        _gridShader.SetParameter("line_thickness", thickness);
        _gridShader.SetParameter("grid_color", gridColor);

        handle.UseShader(_gridShader);
        handle.DrawTextureRect(Texture, UIBox2.FromDimensions(actualTopLeft, actualSize));
        handle.UseShader(null);
    }

    private void DrawModeBorder(DrawingHandleScreen handle, Vector2 actualTopLeft, Vector2 actualSize, float overlayScale)
    {
        float borderWidth = 3f * overlayScale;
        UIBox2 rect = UIBox2.FromDimensions(actualTopLeft, actualSize);

        if (LabelEditMode)
        {
            Color borderColor = Color.Green.WithAlpha(0.3f);
            DrawBorder(handle, rect, borderColor, borderWidth);
        }

        if (QueenEyeMode)
        {
            Color borderColor = Color.Purple.WithAlpha(0.3f);
            DrawBorder(handle, rect, borderColor, borderWidth);
        }
    }

    private void DrawBorder(DrawingHandleScreen handle, UIBox2 rect, Color color, float width)
    {
        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Right, rect.Top + width), color);
        handle.DrawRect(new UIBox2(rect.Left, rect.Bottom - width, rect.Right, rect.Bottom), color);
        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Left + width, rect.Bottom), color);
        handle.DrawRect(new UIBox2(rect.Right - width, rect.Top, rect.Right, rect.Bottom), color);
    }

    private void DrawBlips(DrawingHandleScreen handle, SpriteSystem system, Texture background,
        SpriteSpecifier.Rsi defibbableRsi, SpriteSpecifier.Rsi defibbableRsi2, SpriteSpecifier.Rsi defibbableRsi3, SpriteSpecifier.Rsi defibbableRsi4,
        SpriteSpecifier.Rsi undefibbableRsi, SpriteSpecifier.Rsi hiveLeaderRsi, Vector2 actualTopLeft, float overlayScale, TimeSpan curTime)
    {
        if (_blips == null)
            return;

        for (int i = 0; i < _blips.Length; i++)
        {
            TacticalMapBlip blip = _blips[i];
            if (IsHiveStructureBlip(blip))
                continue;
            Vector2 position = IndicesToPosition(blip.Indices) * overlayScale + actualTopLeft;
            float scaledBlipSize = GetScaledBlipSize(overlayScale);
            UIBox2 rect = UIBox2.FromDimensions(position, new Vector2(scaledBlipSize, scaledBlipSize));

            handle.DrawTextureRect(blip.Background != null ? system.GetFrame(blip.Background, curTime) : background, rect, blip.Color);
            handle.DrawTextureRect(system.GetFrame(blip.Image, curTime), rect);

            if (_localPlayerEntityId.HasValue && _blipEntityIds != null && i < _blipEntityIds.Length)
            {
                if (_blipEntityIds[i] == _localPlayerEntityId.Value)
                {
                    DrawPingEffect(handle, position, scaledBlipSize, overlayScale, curTime, blip.Color);
                }
            }

            if (blip.HiveLeader)
                handle.DrawTextureRect(system.GetFrame(hiveLeaderRsi, curTime), rect);

            var defibTexture = blip.Status switch
            {
                TacticalMapBlipStatus.Defibabble => defibbableRsi,
                TacticalMapBlipStatus.Defibabble2 => defibbableRsi2,
                TacticalMapBlipStatus.Defibabble3 => defibbableRsi3,
                TacticalMapBlipStatus.Defibabble4 => defibbableRsi4,
                TacticalMapBlipStatus.Undefibabble => undefibbableRsi,
                _ => null,
            };
            if (defibTexture != null)
                handle.DrawTextureRect(system.GetFrame(defibTexture, curTime), rect);
        }
    }

    private void DrawPingEffect(DrawingHandleScreen handle, Vector2 center, float blipSize, float overlayScale, TimeSpan curTime, Color blipColor)
    {
        float totalSeconds = (float)curTime.TotalSeconds;
        float cycleProgress = (totalSeconds % PingDuration) / PingDuration;

        float scale = PingMinScale + (PingMaxScale - PingMinScale) * cycleProgress;
        float alpha = PingMaxAlpha - (PingMaxAlpha - PingMinAlpha) * cycleProgress;

        float ringSize = blipSize * scale;
        Vector2 ringCenter = center + new Vector2(blipSize / 2, blipSize / 2);

        Color pingColor = Color.FromHex("#00FFFF").WithAlpha(alpha);

        float thickness = PingRingThickness * overlayScale;
        int segments = 32;
        float angleStep = MathF.PI * 2 / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            Vector2 outerP1 = ringCenter + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * (ringSize / 2);
            Vector2 outerP2 = ringCenter + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * (ringSize / 2);

            Vector2 innerP1 = ringCenter + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * (ringSize / 2 - thickness);
            Vector2 innerP2 = ringCenter + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * (ringSize / 2 - thickness);

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,
                new[] { outerP1, outerP2, innerP1, innerP1, outerP2, innerP2 },
                pingColor);
        }
    }

    private void DrawLines(DrawingHandleScreen handle, float overlayScale, Vector2 actualTopLeft)
    {
        int i = 0;
        float joinEpsilonSquared = LineJoinEpsilon * LineJoinEpsilon;

        while (i < Lines.Count)
        {
            TacticalMapLine line = Lines[i];
            float thickness = GetLineThickness(i, line);
            Color color = line.Color;

            if (!line.Smooth)
            {
                DrawLineSegment(handle, line.Start, line.End, color, thickness, overlayScale, actualTopLeft, true);
                i++;
                continue;
            }

            var points = new List<Vector2> { line.Start, line.End };
            int j = i + 1;
            for (; j < Lines.Count; j++)
            {
                TacticalMapLine next = Lines[j];
                float nextThickness = GetLineThickness(j, next);

                if (next.Color != color || MathF.Abs(nextThickness - thickness) > 0.001f)
                    break;

                if ((points[^1] - next.Start).LengthSquared() > joinEpsilonSquared)
                    break;

                if (!next.Smooth)
                    break;

                Vector2 prevDir = points[^1] - points[^2];
                Vector2 nextDir = next.End - next.Start;
                if (prevDir.LengthSquared() > 0.0001f && nextDir.LengthSquared() > 0.0001f)
                {
                    float dot = Vector2.Dot(Vector2.Normalize(prevDir), Vector2.Normalize(nextDir));
                    if (dot < LineSmoothMinDot)
                        break;
                }

                points.Add(next.End);
            }

            if (points.Count >= 3)
            {
                DrawSmoothPolyline(handle, points, color, thickness, overlayScale, actualTopLeft);
            }
            else
            {
                for (int p = 0; p < points.Count - 1; p++)
                {
                    DrawLineSegment(handle, points[p], points[p + 1], color, thickness, overlayScale, actualTopLeft, true);
                }
            }

            i = j;
        }
    }

    private void DrawPreviewLine(DrawingHandleScreen handle, float overlayScale, Vector2 actualTopLeft)
    {
        if (!_dragging || !Drawing || !StraightLineMode || _dragStart == null || _previewEnd == null || Texture == null)
            return;

        Vector2i diff = _previewEnd.Value - _dragStart.Value;
        if (diff.Length < MinDragDistance)
            return;

        Vector2i startIndices = PositionToIndices(PixelToLogical(new Vector2(_dragStart.Value.X, _dragStart.Value.Y)));
        Vector2i endIndices = PositionToIndices(PixelToLogical(new Vector2(_previewEnd.Value.X, _previewEnd.Value.Y)));

        endIndices = SnapToStraightLine(startIndices, endIndices);

        Vector2 lineStart = ConvertIndicesToLineCoordinates(startIndices);
        Vector2 lineEnd = ConvertIndicesToLineCoordinates(endIndices);
        TacticalMapLine previewLine = new(lineStart, lineEnd, Color.WithAlpha(0.5f), LineThickness);

        DrawLineWithThickness(handle, previewLine, overlayScale, actualTopLeft, LineThickness);
    }

    private void DrawEraserPreview(DrawingHandleScreen handle)
    {
        if (!Drawing || !EraserMode || _lastMousePosition == null)
            return;

        float radius = EraserRadiusPixels + LineThickness * 1.5f;
        Vector2 center = LogicalToPixel(_lastMousePosition.Value);
        handle.DrawCircle(center, radius, EraserPreviewColor);
    }

    private void DrawPreviewSquare(DrawingHandleScreen handle, float overlayScale, Vector2 actualTopLeft)
    {
        if (!_dragging || !Drawing || !SquareMode || _dragStart == null || _previewEnd == null || Texture == null)
            return;

        Vector2i diff = _previewEnd.Value - _dragStart.Value;
        if (diff.Length < MinDragDistance)
            return;

        Vector2i startIndices = PositionToIndices(PixelToLogical(new Vector2(_dragStart.Value.X, _dragStart.Value.Y)));
        Vector2i endIndices = PositionToIndices(PixelToLogical(new Vector2(_previewEnd.Value.X, _previewEnd.Value.Y)));

        if (startIndices == endIndices)
            return;

        int minX = Math.Min(startIndices.X, endIndices.X);
        int maxX = Math.Max(startIndices.X, endIndices.X);
        int minY = Math.Min(startIndices.Y, endIndices.Y);
        int maxY = Math.Max(startIndices.Y, endIndices.Y);

        Vector2 topLeft = ConvertIndicesToLineCoordinates(new Vector2i(minX, maxY));
        Vector2 topRight = ConvertIndicesToLineCoordinates(new Vector2i(maxX, maxY));
        Vector2 bottomRight = ConvertIndicesToLineCoordinates(new Vector2i(maxX, minY));
        Vector2 bottomLeft = ConvertIndicesToLineCoordinates(new Vector2i(minX, minY));

        Color previewColor = Color.WithAlpha(0.5f);
        DrawLineWithThickness(handle, new TacticalMapLine(topLeft, topRight, previewColor, LineThickness), overlayScale, actualTopLeft, LineThickness);
        DrawLineWithThickness(handle, new TacticalMapLine(topRight, bottomRight, previewColor, LineThickness), overlayScale, actualTopLeft, LineThickness);
        DrawLineWithThickness(handle, new TacticalMapLine(bottomRight, bottomLeft, previewColor, LineThickness), overlayScale, actualTopLeft, LineThickness);
        DrawLineWithThickness(handle, new TacticalMapLine(bottomLeft, topLeft, previewColor, LineThickness), overlayScale, actualTopLeft, LineThickness);
    }

    private void DrawLabels(DrawingHandleScreen handle, float overlayScale, Vector2 actualTopLeft)
    {
        if (CurrentLabelMode == LabelMode.None)
            return;

        if (CurrentLabelMode == LabelMode.All)
        {
            foreach ((Vector2i indices, string label) in _areaLabels)
            {
                Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
                position = position with { Y = position.Y - LabelYOffset * overlayScale };
                DrawLabelAtPosition(handle, label, position, overlayScale, AreaLabelColor, AreaLabelBackground, false);

                if (TacticalLabels.TryGetValue(indices, out var tacticalData) &&
                    !string.IsNullOrWhiteSpace(tacticalData.Text))
                {
                    Vector2 tacticalPosition = position + new Vector2(0f, LabelStackOffset * overlayScale);
                    var tacticalColor = GetTacticalLabelColor(tacticalData);
                    if (_draggingLabel == indices && _currentDragPosition != null)
                    {
                        DrawLabelAtPosition(handle, tacticalData.Text, _currentDragPosition.Value, overlayScale, tacticalColor, TacticalLabelBackground, true);
                    }
                    else
                    {
                        DrawLabelAtPosition(handle, tacticalData.Text, tacticalPosition, overlayScale, tacticalColor, TacticalLabelBackground, false);
                    }
                }
            }

            foreach ((Vector2i indices, TacticalMapLabelData data) in TacticalLabels)
            {
                if (_areaLabels.ContainsKey(indices))
                    continue;

                if (string.IsNullOrWhiteSpace(data.Text))
                    continue;

                var tacticalColor = GetTacticalLabelColor(data);
                if (_draggingLabel == indices && _currentDragPosition != null)
                {
                    DrawLabelAtPosition(handle, data.Text, _currentDragPosition.Value, overlayScale, tacticalColor, TacticalLabelBackground, true);
                }
                else
                {
                    Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
                    position = position with { Y = position.Y - LabelYOffset * overlayScale };
                    DrawLabelAtPosition(handle, data.Text, position, overlayScale, tacticalColor, TacticalLabelBackground, false);
                }
            }

            return;
        }

        if (CurrentLabelMode == LabelMode.Tactical)
        {
            foreach ((Vector2i indices, TacticalMapLabelData data) in TacticalLabels)
            {
                if (string.IsNullOrWhiteSpace(data.Text))
                    continue;

                var tacticalColor = GetTacticalLabelColor(data);
                if (_draggingLabel == indices && _currentDragPosition != null)
                {
                    DrawLabelAtPosition(handle, data.Text, _currentDragPosition.Value, overlayScale, tacticalColor, TacticalLabelBackground, true);
                }
                else
                {
                    Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
                    position = position with { Y = position.Y - LabelYOffset * overlayScale };
                    DrawLabelAtPosition(handle, data.Text, position, overlayScale, tacticalColor, TacticalLabelBackground, false);
                }
            }

            return;
        }

        foreach ((Vector2i indices, string label) in _areaLabels)
        {
            Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
            position = position with { Y = position.Y - LabelYOffset * overlayScale };
            DrawLabelAtPosition(handle, label, position, overlayScale, AreaLabelColor, AreaLabelBackground, false);
        }
    }

    private void DrawLabelAtPosition(
        DrawingHandleScreen handle,
        string label,
        Vector2 position,
        float overlayScale,
        Color textColor,
        Color backgroundColor,
        bool isDragging)
    {
        VectorFont labelFont = GetLabelFont(overlayScale);

        Vector2 textSize = handle.GetDimensions(labelFont, label, 1f);
        position -= textSize / 2;

        float padding = LabelPadding * overlayScale;
        Vector2 boxSize = textSize + new Vector2(padding * 2f, padding * 2f);

        Color bgColor = isDragging ? LabelDragBackground : backgroundColor;
        if (bgColor.A > 0)
            handle.DrawRect(UIBox2.FromDimensions(position - new Vector2(padding, padding), boxSize), bgColor);

        handle.DrawString(labelFont, position, label, textColor);
    }

    private VectorFont GetLabelFont(float overlayScale)
    {
        float fontSize = Math.Max(LabelMinFontSize, overlayScale * LabelFontScale);
        int size = (int)MathF.Round(fontSize);
        if (size < 1)
            size = 1;

        if (_labelFontCache.TryGetValue(size, out var font))
            return font;

        font = new VectorFont(_resourceCache.GetResource<FontResource>(LabelFontPath), size);
        _labelFontCache[size] = font;
        return font;
    }

    private void DrawLineWithThickness(DrawingHandleScreen handle, TacticalMapLine line, float overlayScale, Vector2 actualTopLeft, float thickness)
    {
        DrawLineSegment(handle, line.Start, line.End, line.Color, thickness, overlayScale, actualTopLeft, true);
    }

    private void DrawLineSegment(
        DrawingHandleScreen handle,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness,
        float overlayScale,
        Vector2 actualTopLeft,
        bool drawCaps)
    {
        Vector2 startScreen = start * overlayScale + actualTopLeft;
        Vector2 endScreen = end * overlayScale + actualTopLeft;
        Vector2 diff = endScreen - startScreen;

        float actualThickness = thickness * overlayScale;
        float softThickness = actualThickness + LineSoftEdgePixels;
        Color softColor = color.WithAlpha(color.A * LineSoftEdgeAlpha);

        if (diff.Length() < 1.0f)
        {
            if (!drawCaps)
                return;

            DrawLineCap(handle, startScreen, softThickness, softColor);
            DrawLineCap(handle, startScreen, actualThickness, color);
            return;
        }

        DrawLineBox(handle, startScreen, diff, softThickness, softColor);
        DrawLineBox(handle, startScreen, diff, actualThickness, color);

        if (drawCaps)
        {
            DrawLineCap(handle, startScreen, softThickness, softColor);
            DrawLineCap(handle, endScreen, softThickness, softColor);
            DrawLineCap(handle, startScreen, actualThickness, color);
            DrawLineCap(handle, endScreen, actualThickness, color);
        }
    }

    private void DrawLineCaps(
        DrawingHandleScreen handle,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness,
        float overlayScale,
        Vector2 actualTopLeft)
    {
        Vector2 startScreen = start * overlayScale + actualTopLeft;
        Vector2 endScreen = end * overlayScale + actualTopLeft;

        float actualThickness = thickness * overlayScale;
        float softThickness = actualThickness + LineSoftEdgePixels;
        Color softColor = color.WithAlpha(color.A * LineSoftEdgeAlpha);

        DrawLineCap(handle, startScreen, softThickness, softColor);
        DrawLineCap(handle, endScreen, softThickness, softColor);
        DrawLineCap(handle, startScreen, actualThickness, color);
        DrawLineCap(handle, endScreen, actualThickness, color);
    }

    private void DrawSmoothPolyline(
        DrawingHandleScreen handle,
        List<Vector2> points,
        Color color,
        float thickness,
        float overlayScale,
        Vector2 actualTopLeft)
    {
        if (points.Count < 2)
            return;

        float step = SmoothSamplePixels / Math.Max(overlayScale, 0.001f);
        Vector2? previous = null;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p0 = i > 0 ? points[i - 1] : points[i];
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 p3 = i + 2 < points.Count ? points[i + 2] : points[i + 1];

            float segmentLength = (p2 - p1).Length();
            int steps = Math.Max(1, (int)MathF.Ceiling(segmentLength / step));

            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector2 position = CatmullRom(p0, p1, p2, p3, t);

                if (previous != null)
                    DrawLineSegment(handle, previous.Value, position, color, thickness, overlayScale, actualTopLeft, false);

                previous = position;
            }
        }

        DrawLineCaps(handle, points[0], points[^1], color, thickness, overlayScale, actualTopLeft);
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private void DrawLineBox(DrawingHandleScreen handle, Vector2 start, Vector2 diff, float thickness, Color color)
    {
        if (diff.Length() < 0.001f)
            return;

        Vector2 center = start + diff / 2;
        Box2 box = Box2.CenteredAround(center, new Vector2(thickness, diff.Length()));
        Box2Rotated boxRotated = new(box, diff.ToWorldAngle(), center);

        _reusableLineVectors[0] = boxRotated.BottomLeft;
        _reusableLineVectors[1] = boxRotated.BottomRight;
        _reusableLineVectors[2] = boxRotated.TopRight;
        _reusableLineVectors[3] = boxRotated.BottomLeft;
        _reusableLineVectors[4] = boxRotated.TopLeft;
        _reusableLineVectors[5] = boxRotated.TopRight;

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _reusableLineVectors, color);
    }

    private void DrawLineCap(DrawingHandleScreen handle, Vector2 position, float thickness, Color color)
    {
        float radius = thickness / 2f;
        if (radius <= 0.01f)
            return;

        handle.DrawCircle(position, radius, color);
    }
}
