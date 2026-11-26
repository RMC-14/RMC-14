using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed partial class TacticalMapControl
{
    private const float SmoothSamplePixels = 3f;
    private const float MinPathSpacing = 0.75f;
    private const float MiterLimit = 1.5f;
    private const float LineSoftEdgePixels = 1.5f;
    private const float LineSoftEdgeAlpha = 0.4f;
    private const float LabelYOffset = 6f;
    private const float LabelStackOffset = 9f;
    private const float LabelMinFontSize = 6f;
    private const float LabelFontScale = 6.5f;
    private const float LabelPadding = 2f;
    private const float LabelClickPadding = 4f;
    private const float PingDuration = 2f;
    private const float PingMinScale = 1f;
    private const float PingMaxScale = 2.5f;
    private const float PingMaxAlpha = 0.65f;
    private const float PingRingThickness = 1.5f;
    private const int PingSegments = 32;
    private const float MortarOverlayFillAlpha = 0.4f;
    private const float StructureOverlayFillAlpha = 0.3f;
    private const float GlowAlpha = 1f;
    private const float CrtTexelsPerTile = 2f;
    private const float CrtStrength = 1f;
    private const float TooltipOffset = 14f;
    private const float RouteThickness = 2.5f;
    private const float RouteGlowScale = 3.5f;
    private const float RouteGlowAlpha = 0.18f;
    private const float RouteChevronSpacing = 18f;
    private const float RouteChevronSpeed = 24f;
    private const float RouteChevronSize = 4f;
    private const float RouteArrowLength = 10f;
    private const float RouteArrowWidth = 6f;
    private const float TooltipPadding = 5f;
    private const float TooltipLineSpacing = 2f;

    private static readonly Color AreaLabelColor = Color.White;
    private static readonly Color AreaLabelBackground = Color.Black.WithAlpha(0.5f);
    private static readonly Color TacticalLabelBackground = Color.FromHex("#0D1A2F").WithAlpha(0.5f);
    private static readonly Color LabelDragBackground = Color.Black.WithAlpha(0.7f);
    private static readonly Color MortarRangeColor = Color.FromHex("#43B581");
    private static readonly Color GridColor = Color.FromHex("#88C7FA").WithAlpha(0.06f);
    private static readonly Color PingColor = Color.FromHex("#00FFFF");
    private static readonly Color WatchedPingColor = Color.Orange;
    private static readonly Color LabelToolBorderColor = Color.Green.WithAlpha(0.3f);
    private static readonly Color PointModeBorderColor = Color.Purple.WithAlpha(0.3f);
    private static readonly ResPath LabelFontPath = new("/Fonts/NotoSans/NotoSans-Bold.ttf");

    private readonly Dictionary<int, VectorFont> _labelFonts = new();
    private readonly Dictionary<Vector2i, TacticalMapLabelData> _labelBuffer = new();
    private readonly List<Vector2> _pixelBuffer = new();
    private readonly Vector2[] _quad = new Vector2[6];
    private readonly List<Vector2> _pathBuffer = new();
    private readonly List<Vector2> _stripBuffer = new();
    private readonly List<Vector2> _joinBuffer = new();
    private readonly List<(string Text, Font Font, Color Color)> _tooltipLines = new();

    public string? InfoText { get; set; }
    public Vector2i? InfoIndices { get; set; }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (!_hasBase || _base == null)
            return;

        var time = _timing.CurTime;
        var scale = PixelsPerTile;
        var topLeft = GetTopLeft();
        var rect = UIBox2.FromDimensions(topLeft, new Vector2(_width, _height) * scale);

        // back to front, the order matters
        DrawBase(handle, topLeft, rect, scale);
        DrawOverlays(handle, rect);
        DrawTiles(handle, _mortarTiles, MortarRangeColor.WithAlpha(MortarOverlayFillAlpha));

        if (!CrtOverlay)
            DrawGrid(handle, rect, scale);

        DrawModeBorder(handle, rect);
        DrawStrokes(handle);
        _tool?.DrawPreview(this, handle);
        DrawLabels(handle);
        DrawRoute(handle, time);
        DrawStructures(handle, time);
        DrawBlips(handle, time);
        DrawRouteTarget(handle, time);
        DrawSelf(handle, time);
        DrawWatched(handle, time);
        DrawMortarMarker(handle, time);
        DrawInfoText(handle);
        DrawBlipTooltip(handle, time);
    }

    // glow, then the black outline, then the map on top
    private void DrawBase(DrawingHandleScreen handle, Vector2 topLeft, UIBox2 rect, float scale)
    {
        var glowPadding = new Vector2(GlowPadding, GlowPadding) * scale;
        handle.DrawTextureRect(_base!.Glow,
            new UIBox2(topLeft - glowPadding, rect.BottomRight + glowPadding),
            Color.White.WithAlpha(GlowAlpha));

        var outlinePadding = new Vector2(OutlineTiles, OutlineTiles) * scale;
        handle.DrawTextureRect(_base.Backdrop, new UIBox2(topLeft - outlinePadding, rect.BottomRight + outlinePadding));

        if (!CrtOverlay || _crtOverlayTexture == null)
        {
            handle.DrawTextureRect(_base.Map, rect);
            return;
        }

        _crtShader.SetParameter("overlay_texture", _crtOverlayTexture);
        _crtShader.SetParameter("overlay_scale", new Vector2(CrtTexelsPerTile / _crtOverlayTexture.Width, CrtTexelsPerTile / _crtOverlayTexture.Height));
        _crtShader.SetParameter("overlay_strength", CrtStrength);
        handle.UseShader(_crtShader);
        handle.DrawTextureRect(_base.Map, rect);
        handle.UseShader(null);
    }

    private void DrawOverlays(DrawingHandleScreen handle, UIBox2 rect)
    {
        for (var level = 0; level < TacticalMapRoofLevels.Count; level++)
        {
            var flag = (TacticalMapOverlays) (1 << level);
            if ((Overlays & flag) != 0 && _base!.Roofs[level] is { } roof)
                handle.DrawTextureRect(roof, rect);
        }

        if ((Overlays & TacticalMapOverlays.LinkedLz) != 0 && _base!.LinkedLz is { } linkedLz)
            handle.DrawTextureRect(linkedLz, rect);
    }

    private void DrawTile(DrawingHandleScreen handle, Vector2i tile, Color color, float scale)
    {
        // y + 1 is the tiles top edge, which is the top left once flipped to pixels
        var topLeft = TileToPixel(new Vector2(tile.X, tile.Y + 1));
        handle.DrawRect(UIBox2.FromDimensions(topLeft, new Vector2(scale, scale)), color);
    }

    private void DrawTiles(DrawingHandleScreen handle, List<Vector2i> tiles, Color color)
    {
        if (tiles.Count == 0)
            return;

        var scale = PixelsPerTile;
        foreach (var tile in tiles)
        {
            DrawTile(handle, tile, color, scale);
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, UIBox2 rect, float scale)
    {
        // lines would just be noise this zoomed out
        if (scale < 1.5f)
            return;

        var thickness = Math.Clamp(scale * 0.12f, 1f, 1.25f);
        _gridShader.SetParameter("tile_size", new Vector2(scale, scale));
        _gridShader.SetParameter("line_thickness", thickness);
        _gridShader.SetParameter("grid_color", GridColor);
        handle.UseShader(_gridShader);
        handle.DrawTextureRect(_base!.Map, rect);
        handle.UseShader(null);
    }

    private void DrawModeBorder(DrawingHandleScreen handle, UIBox2 rect)
    {
        Color color;
        if (_tool is TacticalMapLabelTool)
            color = LabelToolBorderColor;
        else if (_tool == null && PointMode)
            color = PointModeBorderColor;
        else
            return;

        var width = 3f * GetOverlayScale();
        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Right, rect.Top + width), color);
        handle.DrawRect(new UIBox2(rect.Left, rect.Bottom - width, rect.Right, rect.Bottom), color);
        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Left + width, rect.Bottom), color);
        handle.DrawRect(new UIBox2(rect.Right - width, rect.Top, rect.Right, rect.Bottom), color);
    }

    private void DrawStrokes(DrawingHandleScreen handle)
    {
        var draftVisible = IsDraftVisible;
        foreach (var layer in _layers)
        {
            if (draftVisible && layer.Channel == Draft!.Channel)
                continue;

            foreach (var stroke in layer.Strokes)
            {
                DrawStroke(handle, stroke);
            }
        }

        if (draftVisible)
        {
            foreach (var stroke in Draft!.Strokes)
            {
                DrawStroke(handle, stroke);
            }
        }

        foreach (var stroke in OverlayStrokes)
        {
            DrawStroke(handle, stroke);
        }
    }

    public void DrawStroke(DrawingHandleScreen handle, TacticalMapStroke stroke)
    {
        var points = stroke.Points;
        if (points == null || points.Count == 0)
            return;

        var thickness = stroke.Thickness * GetOverlayScale();
        var softColor = stroke.Color.WithAlpha(stroke.Color.A * LineSoftEdgeAlpha);

        _pixelBuffer.Clear();
        foreach (var point in points)
        {
            _pixelBuffer.Add(TileToPixel(point));
        }

        // a slightly wider faint pass underneath softens the edges
        BuildPath(_pixelBuffer, stroke.Smooth, _pathBuffer);
        if (_pathBuffer.Count == 1)
        {
            DrawCap(handle, _pathBuffer[0], thickness + LineSoftEdgePixels, softColor);
            DrawCap(handle, _pathBuffer[0], thickness, stroke.Color);
            return;
        }

        DrawPath(handle, _pathBuffer, thickness + LineSoftEdgePixels, softColor);
        DrawPath(handle, _pathBuffer, thickness, stroke.Color);
    }

    private static void BuildPath(List<Vector2> points, bool smooth, List<Vector2> path)
    {
        path.Clear();

        // catmull rom through the points, sampled every few pixels
        if (smooth && points.Count >= 3)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                var p0 = i > 0 ? points[i - 1] : points[i];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = i + 2 < points.Count ? points[i + 2] : points[i + 1];
                var steps = Math.Max(1, (int) MathF.Ceiling((p2 - p1).Length() / SmoothSamplePixels));
                for (var s = i == 0 ? 0 : 1; s <= steps; s++)
                {
                    AddPathPoint(path, CatmullRom(p0, p1, p2, p3, s / (float) steps));
                }
            }
        }
        else
        {
            foreach (var point in points)
            {
                AddPathPoint(path, point);
            }
        }

        // the spacing check can drop the real end point, put it back
        var last = points[^1];
        if (path.Count > 1 && path[^1] != last)
            path[^1] = last;
    }

    private static void AddPathPoint(List<Vector2> path, Vector2 point)
    {
        if (path.Count > 0 && (path[^1] - point).LengthSquared() < MinPathSpacing * MinPathSpacing)
            return;

        path.Add(point);
    }

    // one triangle strip with mitered joins. corners too sharp to miter get capped & covered with a circle
    private void DrawPath(DrawingHandleScreen handle, List<Vector2> path, float thickness, Color color)
    {
        var half = thickness / 2f;
        var maxMiter = half * MiterLimit;
        _stripBuffer.Clear();
        _joinBuffer.Clear();
        for (var i = 0; i < path.Count; i++)
        {
            var dirIn = i > 0 ? Direction(path[i - 1], path[i]) : Direction(path[0], path[1]);
            var dirOut = i < path.Count - 1 ? Direction(path[i], path[i + 1]) : dirIn;
            var tangent = dirIn + dirOut;
            tangent = tangent.LengthSquared() < 0.000001f ? dirOut : Vector2.Normalize(tangent);

            var normal = new Vector2(-tangent.Y, tangent.X);
            var segmentNormal = new Vector2(-dirOut.Y, dirOut.X);
            var dot = Vector2.Dot(normal, segmentNormal);
            var miter = dot > 0.0001f ? half / dot : maxMiter;
            if (miter > maxMiter)
            {
                miter = maxMiter;
                _joinBuffer.Add(path[i]);
            }

            _stripBuffer.Add(path[i] + normal * miter);
            _stripBuffer.Add(path[i] - normal * miter);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, CollectionsMarshal.AsSpan(_stripBuffer), color);

        foreach (var join in _joinBuffer)
        {
            DrawCap(handle, join, thickness, color);
        }

        DrawCap(handle, path[0], thickness, color);
        DrawCap(handle, path[^1], thickness, color);
    }

    private static Vector2 Direction(Vector2 from, Vector2 to)
    {
        var diff = to - from;
        var length = diff.Length();
        return length < 0.0001f ? new Vector2(1, 0) : diff / length;
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * (2f * p1 +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private static void DrawCap(DrawingHandleScreen handle, Vector2 position, float thickness, Color color)
    {
        var radius = thickness / 2f;
        if (radius > 0.01f)
            handle.DrawCircle(position, radius, color);
    }

    private void DrawLabels(DrawingHandleScreen handle)
    {
        if (!LabelsEnabled)
            return;

        var showArea = LabelMode is TacticalMapLabelMode.All or TacticalMapLabelMode.Area;
        var showTactical = LabelMode is TacticalMapLabelMode.All or TacticalMapLabelMode.Tactical;
        var overlay = GetOverlayScale();
        var font = GetLabelFont(overlay);

        if (showArea)
        {
            foreach (var (tile, text) in _areaLabels)
            {
                DrawLabel(handle, font, text, GetLabelAnchor(tile, false), AreaLabelColor, AreaLabelBackground);
            }
        }

        if (!showTactical)
            return;

        CollectTacticalLabels();
        foreach (var (tile, label) in _labelBuffer)
        {
            if (string.IsNullOrWhiteSpace(label.Text) || _tool is TacticalMapLabelTool { DraggingLabel: var dragging } && dragging == tile)
                continue;

            var stacked = showArea && _areaLabels.ContainsKey(tile);
            DrawLabel(handle, font, label.Text, GetLabelAnchor(tile, stacked), GetLabelColor(label), TacticalLabelBackground);
        }
    }

    private void CollectTacticalLabels()
    {
        _labelBuffer.Clear();
        var draftVisible = IsDraftVisible;
        foreach (var layer in _layers)
        {
            if (draftVisible && layer.Channel == Draft!.Channel)
                continue;

            foreach (var (tile, label) in layer.Labels)
            {
                _labelBuffer[tile] = label;
            }
        }

        if (!draftVisible)
            return;

        foreach (var (tile, label) in Draft!.Labels)
        {
            _labelBuffer[tile] = label;
        }
    }

    private Vector2 GetLabelAnchor(Vector2i tile, bool stacked)
    {
        var overlay = GetOverlayScale();
        var anchor = IndicesToPixelCenter(tile);
        anchor.Y -= LabelYOffset * overlay;
        if (stacked)
            anchor.Y += LabelStackOffset * overlay;

        return anchor;
    }

    private static Color GetLabelColor(TacticalMapLabelData label)
    {
        return label.Color.A <= 0f ? Color.White : label.Color;
    }

    public void DrawDraggedLabel(DrawingHandleScreen handle, string text, Vector2 pixel, Color color)
    {
        DrawLabel(handle, GetLabelFont(GetOverlayScale()), text, pixel, color, LabelDragBackground);
    }

    private void DrawLabel(DrawingHandleScreen handle, Font font, string text, Vector2 center, Color color, Color background)
    {
        var size = handle.GetDimensions(font, text, 1f);
        var position = center - size / 2f;
        var padding = LabelPadding * GetOverlayScale();
        if (background.A > 0f)
        {
            handle.DrawRect(UIBox2.FromDimensions(position - new Vector2(padding, padding), size + new Vector2(padding * 2f, padding * 2f)),
                background);
        }

        handle.DrawString(font, position, text, color);
    }

    private Font GetLabelFont(float overlay)
    {
        var size = Math.Max(1, (int) MathF.Round(GetLabelFontSize(overlay)));
        if (_labelFonts.TryGetValue(size, out var font))
            return font;

        font = new VectorFont(_resourceCache.GetResource<FontResource>(LabelFontPath), size);
        _labelFonts[size] = font;
        return font;
    }

    private static float GetLabelFontSize(float overlay)
    {
        return MathF.Max(LabelMinFontSize, overlay * LabelFontScale);
    }

    public bool TryGetDraftLabelAt(Vector2 pixel, out Vector2i tile)
    {
        tile = default;
        if (Draft == null || !LabelsEnabled || LabelMode == TacticalMapLabelMode.Area)
            return false;

        var overlay = GetOverlayScale();
        var fontSize = GetLabelFontSize(overlay);
        var showArea = LabelMode == TacticalMapLabelMode.All;
        var padding = LabelClickPadding * overlay;
        foreach (var (position, label) in Draft.Labels)
        {
            if (string.IsNullOrWhiteSpace(label.Text))
                continue;

            var anchor = GetLabelAnchor(position, showArea && _areaLabels.ContainsKey(position));

            // rough size from the character count, no font measuring needed
            var halfWidth = label.Text.Length * fontSize * 0.3f + padding;
            var halfHeight = fontSize * 0.5f + padding;
            if (MathF.Abs(pixel.X - anchor.X) <= halfWidth && MathF.Abs(pixel.Y - anchor.Y) <= halfHeight)
            {
                tile = position;
                return true;
            }
        }

        return false;
    }

    // drawn before the other blips so they sit underneath
    private void DrawStructures(DrawingHandleScreen handle, TimeSpan time)
    {
        if (_structureBlips.Count == 0)
            return;

        var scale = PixelsPerTile;
        foreach (var (tile, color) in _structureTiles)
        {
            DrawTile(handle, tile, color.WithAlpha(StructureOverlayFillAlpha), scale);
        }

        foreach (var index in _structureBlips)
        {
            if (index >= _blips.Count)
                continue;

            var blip = _blips[index].Blip;
            DrawBlip(handle, blip, IndicesToPixelCenter(blip.Indices), BlipAlpha, time);
        }
    }

    private void DrawBlips(DrawingHandleScreen handle, TimeSpan time)
    {
        for (var i = 0; i < _blips.Count; i++)
        {
            var view = _blips[i];
            if (IsStructureBlip(view.Blip, out _))
                continue;

            // DrawSelf handles it when we have our own position, otherwise ping the feed copy
            if (_selfIndices != null && view.Entity == _selfEntity)
                continue;

            var alpha = view.Entity == _selfEntity ? 1f : BlipAlpha;
            var center = IndicesToPixelCenter(view.Blip.Indices);
            DrawBlip(handle, view.Blip, center, alpha, time);
            if (view.Entity == _selfEntity)
                DrawPing(handle, center, GetBlipSize(), time, PingColor);
        }
    }

    private void DrawSelf(DrawingHandleScreen handle, TimeSpan time)
    {
        if (_selfIndices is not { } indices || !IsWithinMap(indices))
            return;

        var center = IndicesToPixelCenter(indices);
        if (_selfBlip is { } blip)
            DrawBlip(handle, blip with { Indices = indices }, center, 1f, time);

        DrawPing(handle, center, GetBlipSize(), time, PingColor);
    }

    private void DrawWatched(DrawingHandleScreen handle, TimeSpan time)
    {
        if (_watchedIndices is not { } indices || !IsWithinMap(indices))
            return;

        DrawPing(handle, IndicesToPixelCenter(indices), GetBlipSize(), time, WatchedPingColor);
    }

    private void DrawBlip(DrawingHandleScreen handle, TacticalMapBlip blip, Vector2 center, float alpha, TimeSpan time)
    {
        var size = GetBlipSize();
        var rect = UIBox2.FromDimensions(center - new Vector2(size / 2f, size / 2f), new Vector2(size, size));
        alpha *= blip.Opacity;
        var color = blip.Color.WithAlpha(blip.Color.A * alpha);
        var overlay = Color.White.WithAlpha(alpha);

        // only the background takes the squad tint, icon & status stay white
        handle.DrawTextureRect(GetFrame(blip.Background ?? BackgroundRsi, time), rect, color);
        handle.DrawTextureRect(GetFrame(blip.Icon, time), rect, overlay);

        if ((blip.Flags & TacticalMapBlipFlags.HiveLeader) != 0)
            handle.DrawTextureRect(GetFrame(HiveLeaderRsi, time), rect, overlay);

        SpriteSpecifier.Rsi? status = null;
        if ((blip.Flags & TacticalMapBlipFlags.Undefibbable) != 0)
        {
            status = UndefibbableRsi;
        }
        else if ((blip.Flags & TacticalMapBlipFlags.Dead) != 0)
        {
            status = blip.GetDefibStage(time) switch
            {
                <= 1 => DefibbableRsi,
                2 => Defibbable2Rsi,
                3 => Defibbable3Rsi,
                _ => Defibbable4Rsi,
            };
        }

        if (status != null)
            handle.DrawTextureRect(GetFrame(status, time), rect, overlay);
    }

    // expanding, fading ring. built from quads since DrawCircle cant do hollow rings with a thickness
    private void DrawPing(DrawingHandleScreen handle, Vector2 center, float blipSize, TimeSpan time, Color pingColor)
    {
        var progress = (float) (time.TotalSeconds % PingDuration) / PingDuration;
        var radius = blipSize * (PingMinScale + (PingMaxScale - PingMinScale) * progress) / 2f;
        var color = pingColor.WithAlpha(PingMaxAlpha * (1f - progress));
        var thickness = PingRingThickness * GetOverlayScale();
        var step = MathF.PI * 2f / PingSegments;
        for (var i = 0; i < PingSegments; i++)
        {
            var a1 = i * step;
            var a2 = (i + 1) * step;
            var d1 = new Vector2(MathF.Cos(a1), MathF.Sin(a1));
            var d2 = new Vector2(MathF.Cos(a2), MathF.Sin(a2));
            _quad[0] = center + d1 * radius;
            _quad[1] = center + d2 * radius;
            _quad[2] = center + d1 * (radius - thickness);
            _quad[3] = _quad[2];
            _quad[4] = _quad[1];
            _quad[5] = center + d2 * (radius - thickness);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _quad, color);
        }
    }

    private void DrawMortarMarker(DrawingHandleScreen handle, TimeSpan time)
    {
        if (_mortarCenter is not { } center)
            return;

        var size = GetBlipSize();
        var pixel = IndicesToPixelCenter(center);
        var rect = UIBox2.FromDimensions(pixel - new Vector2(size / 2f, size / 2f), new Vector2(size, size));
        handle.DrawTextureRect(GetFrame(MortarRsi, time), rect);
    }

    private void DrawInfoText(DrawingHandleScreen handle)
    {
        if (InfoText is not { } text || InfoIndices is not { } indices)
            return;

        var anchor = IndicesToPixelCenter(indices) - new Vector2(0f, GetBlipSize() * 1.5f);
        DrawLabel(handle, GetLabelFont(GetOverlayScale()), text, anchor, Color.White, LabelDragBackground);
    }

    private void DrawBlipTooltip(DrawingHandleScreen handle, TimeSpan time)
    {
        if (_mouse is not { } mouse || _panning && _panMoved || _toolActive || !TryFindHoveredBlip(mouse, out var blip))
            return;

        _tooltipLines.Clear();
        if (!string.IsNullOrWhiteSpace(blip.Name))
            _tooltipLines.Add((blip.Name, TacticalMapStyle.Font(MapTheme, TacticalMapTextStyle.Title), MapTheme.Text));

        if (!string.IsNullOrWhiteSpace(blip.Role))
            _tooltipLines.Add((blip.Role, TacticalMapStyle.Font(MapTheme, TacticalMapTextStyle.Small), MapTheme.TextMuted));

        if ((blip.Flags & TacticalMapBlipFlags.Dead) != 0)
        {
            var small = TacticalMapStyle.Font(MapTheme, TacticalMapTextStyle.Small);
            if ((blip.Flags & TacticalMapBlipFlags.Undefibbable) != 0)
            {
                _tooltipLines.Add((Loc.GetString("ui-tactical-map-tooltip-unrevivable"), small, MapTheme.Danger));
            }
            else if (blip.UnrevivableEnd > time)
            {
                var left = blip.UnrevivableEnd - time;
                var text = Loc.GetString("ui-tactical-map-tooltip-revivable",
                    ("time", $"{(int) left.TotalMinutes}:{left.Seconds:D2}"));
                _tooltipLines.Add((text, small, MapTheme.Warning));
            }
            else
            {
                _tooltipLines.Add((Loc.GetString("ui-tactical-map-tooltip-dead"), small, MapTheme.Danger));
            }
        }

        if (_tooltipLines.Count == 0)
            return;

        var scale = UIScale;
        var padding = TooltipPadding * scale;
        var spacing = TooltipLineSpacing * scale;
        var width = 0f;
        var height = 0f;
        foreach (var (text, font, _) in _tooltipLines)
        {
            var size = handle.GetDimensions(font, text, scale);
            width = MathF.Max(width, size.X);
            height += font.GetLineHeight(scale) + spacing;
        }

        height -= spacing;
        var box = new Vector2(width + padding * 2f, height + padding * 2f);
        // flip to the other side of the cursor near the edges
        var position = mouse + new Vector2(TooltipOffset, TooltipOffset) * scale;
        if (position.X + box.X > PixelSize.X)
            position.X = mouse.X - TooltipOffset * scale - box.X;

        if (position.Y + box.Y > PixelSize.Y)
            position.Y = mouse.Y - TooltipOffset * scale - box.Y;

        position = Vector2.Max(position, Vector2.Zero);
        var rect = UIBox2.FromDimensions(position, box);
        handle.DrawRect(rect, MapTheme.Background.WithAlpha(0.92f));
        handle.DrawRect(rect, MapTheme.Border, false);

        var cursor = position + new Vector2(padding, padding);
        foreach (var (text, font, color) in _tooltipLines)
        {
            handle.DrawString(font, cursor, text, scale, color);
            cursor.Y += font.GetLineHeight(scale) + spacing;
        }
    }

    private void DrawRoute(DrawingHandleScreen handle, TimeSpan time)
    {
        if (_routeFrom is not { } from || _routeTo is not { } to || from == to ||
            !IsWithinMap(from) || !IsWithinMap(to))
        {
            return;
        }

        var scale = UIScale;
        var blip = GetBlipSize();
        var start = IndicesToPixelCenter(from);
        var end = IndicesToPixelCenter(to);
        var delta = end - start;
        var length = delta.Length();

        // trim both ends so the line doesn't cover the blips
        var startTrim = blip * 0.7f;
        var endTrim = blip * 0.8f;
        if (length <= startTrim + endTrim)
            return;

        var direction = delta / length;
        var normal = new Vector2(-direction.Y, direction.X);
        var lineStart = start + direction * startTrim;
        var tip = end - direction * endTrim;
        var arrowLength = RouteArrowLength * scale;
        var arrowWidth = RouteArrowWidth * scale;
        var lineEnd = tip - direction * arrowLength * 0.6f;
        var thickness = RouteThickness * scale;

        // DrawPrimitives treats colours as linear
        var color = Color.ToSrgb(_routeColor);

        var glow = normal * thickness * RouteGlowScale / 2f;
        _quad[0] = lineStart + glow;
        _quad[1] = lineEnd + glow;
        _quad[2] = lineStart - glow;
        _quad[3] = _quad[2];
        _quad[4] = _quad[1];
        _quad[5] = lineEnd - glow;
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _quad, color.WithAlpha(RouteGlowAlpha));

        _pathBuffer.Clear();
        _pathBuffer.Add(lineStart);
        _pathBuffer.Add(lineEnd);
        DrawPath(handle, _pathBuffer, thickness, color);

        var spacing = RouteChevronSpacing * scale;
        var chevron = RouteChevronSize * scale;
        var travel = (lineEnd - lineStart).Length() - chevron;

        // chevrons scroll toward the target
        var offset = (float) (time.TotalSeconds * RouteChevronSpeed * scale % spacing);
        var chevronColor = Color.ToSrgb(Color.InterpolateBetween(_routeColor, Color.White, 0.6f));
        for (var distance = offset + chevron; distance < travel; distance += spacing)
        {
            var center = lineStart + direction * distance;
            _pathBuffer.Clear();
            _pathBuffer.Add(center - direction * chevron + normal * chevron);
            _pathBuffer.Add(center);
            _pathBuffer.Add(center - direction * chevron - normal * chevron);
            DrawPath(handle, _pathBuffer, thickness * 0.6f, chevronColor);
        }

        var back = tip - direction * arrowLength;
        _quad[0] = tip;
        _quad[1] = back + normal * arrowWidth;
        _quad[2] = back - normal * arrowWidth;
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _quad.AsSpan(0, 3), color);
    }

    private void DrawRouteTarget(DrawingHandleScreen handle, TimeSpan time)
    {
        if (_routeTo is not { } to || !IsWithinMap(to))
            return;

        DrawPing(handle, IndicesToPixelCenter(to), GetBlipSize(), time, _routeColor);
    }
}
