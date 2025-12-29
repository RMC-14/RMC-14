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
        Texture background = system.Frame0(backgroundRsi);

        (Vector2 actualSize, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();

        UIBox2 textureRect = UIBox2.FromDimensions(actualTopLeft, actualSize);
        if (_backgroundTexture != null)
            handle.DrawTextureRect(_backgroundTexture, textureRect);
        handle.DrawTextureRect(Texture, textureRect);

        DrawTileGrid(handle, actualTopLeft, actualSize);
        DrawModeBorder(handle, actualTopLeft, actualSize, overlayScale);
        DrawLines(handle, overlayScale, actualTopLeft);
        DrawPreviewLine(handle, overlayScale, actualTopLeft);
        DrawBlips(handle, system, background, defibbableRsi, defibbableRsi2, defibbableRsi3, defibbableRsi4, undefibbableRsi, hiveLeaderRsi, actualTopLeft, overlayScale, curTime);
        DrawEraserPreview(handle);
        DrawLabels(handle, overlayScale, actualTopLeft);
    }

    private void DrawTileGrid(DrawingHandleScreen handle, Vector2 actualTopLeft, Vector2 actualSize)
    {
        if (Texture == null || _tileMask == null)
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

        int columns = _tileMaskWidth;
        int rows = _tileMaskHeight;

        for (int y = 0; y < rows; y++)
        {
            float yTop = actualTopLeft.Y + y * tileHeight;
            float yBottom = yTop + tileHeight;
            float yTopEdge = Math.Min(yTop + thickness, yBottom);
            float yBottomEdge = Math.Max(yBottom - thickness, yTop);

            for (int x = 0; x < columns; x++)
            {
                if (!IsTilePresent(x, y))
                    continue;

                float xLeft = actualTopLeft.X + x * tileWidth;
                float xRight = xLeft + tileWidth;
                float xLeftEdge = Math.Min(xLeft + thickness, xRight);
                float xRightEdge = Math.Max(xRight - thickness, xLeft);

                handle.DrawRect(new UIBox2(xLeft, yTop, xRight, yTopEdge), gridColor);
                handle.DrawRect(new UIBox2(xLeft, yTop, xLeftEdge, yBottom), gridColor);

                if (!IsTilePresent(x + 1, y))
                    handle.DrawRect(new UIBox2(xRightEdge, yTop, xRight, yBottom), gridColor);

                if (!IsTilePresent(x, y + 1))
                    handle.DrawRect(new UIBox2(xLeft, yBottomEdge, xRight, yBottom), gridColor);
            }
        }
    }

    private bool IsTilePresent(int x, int y)
    {
        if (_tileMask == null)
            return false;

        if ((uint)x >= (uint)_tileMaskWidth || (uint)y >= (uint)_tileMaskHeight)
            return false;

        return _tileMask[y * _tileMaskWidth + x];
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
        float fontSize = Math.Max(LabelMinFontSize, overlayScale * LabelFontScale);
        VectorFont labelFont = new(_resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), (int)fontSize);

        Vector2 textSize = handle.GetDimensions(labelFont, label, 1f);
        position -= textSize / 2;

        float padding = LabelPadding * overlayScale;
        Vector2 boxSize = textSize + new Vector2(padding * 2f, padding * 2f);

        Color bgColor = isDragging ? LabelDragBackground : backgroundColor;
        if (bgColor.A > 0)
            handle.DrawRect(UIBox2.FromDimensions(position - new Vector2(padding, padding), boxSize), bgColor);

        handle.DrawString(labelFont, position, label, textColor);
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
