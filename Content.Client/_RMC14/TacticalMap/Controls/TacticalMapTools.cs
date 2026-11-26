using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed class TacticalMapCanvasDraft(EntityUid channel)
{
    private const int MaxUndo = 64;

    private readonly List<(List<TacticalMapStroke> Strokes, Dictionary<Vector2i, TacticalMapLabelData> Labels)> _undo = new();

    public EntityUid Channel { get; } = channel;

    // canvas version this draft was copied from
    public int BaseVersion { get; private set; } = -1;

    // sent, waiting for the server to send the new version back
    public bool Submitted { get; private set; }
    public readonly List<TacticalMapStroke> Strokes = new();
    public readonly Dictionary<Vector2i, TacticalMapLabelData> Labels = new();

    private bool _dirty;

    public bool Dirty
    {
        get => _dirty;
        private set
        {
            _dirty = value;
            if (value)
                Submitted = false;
        }
    }

    public int PointCount => Strokes.Sum(s => s.Points.Count);

    // take the servers copy unless there are unsent edits
    public bool ShouldResetTo(TacticalMapCanvasLayer layer)
    {
        return BaseVersion != layer.Version && (!Dirty || Submitted);
    }

    public void ResetFrom(TacticalMapCanvasLayer layer)
    {
        Strokes.Clear();
        Strokes.AddRange(layer.Strokes);
        Labels.Clear();
        foreach (var (tile, label) in layer.Labels)
        {
            Labels[tile] = label;
        }

        BaseVersion = layer.Version;
        Dirty = false;
        Submitted = false;
        _undo.Clear();
    }

    public void MarkSubmitted()
    {
        Submitted = true;
    }

    public void Checkpoint()
    {
        _undo.Add((new List<TacticalMapStroke>(Strokes), new Dictionary<Vector2i, TacticalMapLabelData>(Labels)));
        if (_undo.Count > MaxUndo)
            _undo.RemoveAt(0);
    }

    public void AddStroke(TacticalMapStroke stroke, int pointLimit)
    {
        Checkpoint();
        Strokes.Add(stroke);

        // drop the oldest strokes once over the point limit
        while (pointLimit > 0 && Strokes.Count > 1 && PointCount > pointLimit)
        {
            Strokes.RemoveAt(0);
        }

        Dirty = true;
    }

    // no checkpoint, the eraser takes one per drag instead of per step
    public void SetStrokes(List<TacticalMapStroke> strokes)
    {
        Strokes.Clear();
        Strokes.AddRange(strokes);
        Dirty = true;
    }

    public void SetLabel(Vector2i tile, TacticalMapLabelData? label)
    {
        Checkpoint();
        if (label == null)
            Labels.Remove(tile);
        else
            Labels[tile] = label.Value;

        Dirty = true;
    }

    public void MoveLabel(Vector2i from, Vector2i to)
    {
        if (from == to || !Labels.TryGetValue(from, out var label))
            return;

        Checkpoint();
        Labels.Remove(from);
        Labels[to] = label;
        Dirty = true;
    }

    public void Clear()
    {
        Checkpoint();
        Strokes.Clear();
        Labels.Clear();
        Dirty = true;
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
            return false;

        var (strokes, labels) = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        Strokes.Clear();
        Strokes.AddRange(strokes);
        Labels.Clear();
        foreach (var (tile, label) in labels)
        {
            Labels[tile] = label;
        }

        Dirty = true;
        return true;
    }
}

public abstract class TacticalMapTool
{
    public virtual void Begin(TacticalMapControl map, Vector2 pixel)
    {
    }

    public virtual void Move(TacticalMapControl map, Vector2 pixel)
    {
    }

    public virtual void End(TacticalMapControl map, Vector2 pixel)
    {
    }

    public virtual void Cancel()
    {
    }

    public virtual void DrawPreview(TacticalMapControl map, DrawingHandleScreen handle)
    {
    }
}

public sealed class TacticalMapPenTool : TacticalMapTool
{
    private const float MinSegmentPixels = 6f;

    private List<Vector2>? _points;
    private Vector2 _lastPixel;

    public override void Begin(TacticalMapControl map, Vector2 pixel)
    {
        _points = new List<Vector2> { map.PixelToTile(pixel) };
        _lastPixel = pixel;
    }

    public override void Move(TacticalMapControl map, Vector2 pixel)
    {
        if (_points == null || (pixel - _lastPixel).Length() < MinSegmentPixels)
            return;

        _points.Add(map.PixelToTile(pixel));
        _lastPixel = pixel;
    }

    public override void End(TacticalMapControl map, Vector2 pixel)
    {
        if (_points is { Count: >= 2 } && map.Draft != null)
        {
            map.Draft.AddStroke(new TacticalMapStroke(map.DrawColor, map.DrawThickness, true, _points), map.LineLimit);
            map.NotifyDraftChanged();
        }

        _points = null;
    }

    public override void Cancel()
    {
        _points = null;
    }

    public override void DrawPreview(TacticalMapControl map, DrawingHandleScreen handle)
    {
        if (_points is { Count: >= 2 })
            map.DrawStroke(handle, new TacticalMapStroke(map.DrawColor, map.DrawThickness, true, _points));
    }
}

public abstract class TacticalMapShapeTool : TacticalMapTool
{
    private Vector2i? _start;
    private Vector2i _end;
    private Vector2 _startPixel;

    public override void Begin(TacticalMapControl map, Vector2 pixel)
    {
        _start = map.PixelToIndices(pixel);
        _end = _start.Value;
        _startPixel = pixel;
    }

    public override void Move(TacticalMapControl map, Vector2 pixel)
    {
        if (_start is { } start)
            _end = Snap(start, map.PixelToIndices(pixel));
    }

    public override void End(TacticalMapControl map, Vector2 pixel)
    {
        if (_start is { } start &&
            map.Draft != null &&
            _end != start &&
            (pixel - _startPixel).Length() >= TacticalMapControl.MinDragDistance)
        {
            map.Draft.AddStroke(new TacticalMapStroke(map.DrawColor, map.DrawThickness, false, BuildPoints(start, _end)), map.LineLimit);
            map.NotifyDraftChanged();
        }

        _start = null;
    }

    public override void Cancel()
    {
        _start = null;
    }

    public override void DrawPreview(TacticalMapControl map, DrawingHandleScreen handle)
    {
        if (_start is not { } start || start == _end)
            return;

        var color = map.DrawColor.WithAlpha(0.5f);
        map.DrawStroke(handle, new TacticalMapStroke(color, map.DrawThickness, false, BuildPoints(start, _end)));
    }

    protected static Vector2 Center(Vector2i tile)
    {
        return new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
    }

    protected virtual Vector2i Snap(Vector2i start, Vector2i end)
    {
        return end;
    }

    protected abstract List<Vector2> BuildPoints(Vector2i start, Vector2i end);
}

public sealed class TacticalMapLineTool : TacticalMapShapeTool
{
    // snaps to horizontal, vertical or 45 degrees
    protected override Vector2i Snap(Vector2i start, Vector2i end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var absX = Math.Abs(dx);
        var absY = Math.Abs(dy);
        if (absX > absY * 2)
            return new Vector2i(end.X, start.Y);

        if (absY > absX * 2)
            return new Vector2i(start.X, end.Y);

        var diagonal = Math.Min(absX, absY);
        return new Vector2i(start.X + (dx >= 0 ? diagonal : -diagonal), start.Y + (dy >= 0 ? diagonal : -diagonal));
    }

    protected override List<Vector2> BuildPoints(Vector2i start, Vector2i end)
    {
        return new List<Vector2> { Center(start), Center(end) };
    }
}

public sealed class TacticalMapRectangleTool : TacticalMapShapeTool
{
    protected override List<Vector2> BuildPoints(Vector2i start, Vector2i end)
    {
        var min = Center(Vector2i.ComponentMin(start, end));
        var max = Center(Vector2i.ComponentMax(start, end));
        return new List<Vector2>
        {
            new(min.X, max.Y),
            new(max.X, max.Y),
            new(max.X, min.Y),
            new(min.X, min.Y),
            new(min.X, max.Y),
        };
    }
}

public sealed class TacticalMapEraserTool : TacticalMapTool
{
    private const float RadiusPixels = 6f;
    private const float MinStepPixels = 3f;

    private bool _active;
    private Vector2 _lastPixel;

    public override void Begin(TacticalMapControl map, Vector2 pixel)
    {
        if (map.Draft == null)
            return;

        _active = true;
        map.Draft.Checkpoint();
        EraseAt(map, pixel);
        _lastPixel = pixel;
    }

    public override void Move(TacticalMapControl map, Vector2 pixel)
    {
        if (!_active || (pixel - _lastPixel).Length() < MinStepPixels)
            return;

        EraseAt(map, pixel);
        _lastPixel = pixel;
    }

    public override void End(TacticalMapControl map, Vector2 pixel)
    {
        _active = false;
    }

    public override void Cancel()
    {
        _active = false;
    }

    public override void DrawPreview(TacticalMapControl map, DrawingHandleScreen handle)
    {
        if (map.MousePixel is { } mouse)
            handle.DrawCircle(mouse, RadiusPixels + map.DrawThickness * 1.5f, Color.White.WithAlpha(0.45f));
    }

    // splits strokes where the eraser touches them instead of deleting the whole stroke
    private static void EraseAt(TacticalMapControl map, Vector2 pixel)
    {
        if (map.Draft is not { } draft || draft.Strokes.Count == 0)
            return;

        var center = map.PixelToTile(pixel);
        var scale = MathF.Max(map.PixelsPerTile, 0.001f);
        var radius = (RadiusPixels + map.DrawThickness * 1.5f) / scale;
        var changed = false;
        var result = new List<TacticalMapStroke>(draft.Strokes.Count);
        foreach (var stroke in draft.Strokes)
        {
            // thick strokes are easier to hit
            var effective = radius + stroke.Thickness * 0.6f / TacticalMapControl.OverlayUnitsPerTile;
            var radiusSquared = effective * effective;
            var points = stroke.Points;
            if (points.Count == 1)
            {
                if (Vector2.DistanceSquared(points[0], center) <= radiusSquared)
                    changed = true;
                else
                    result.Add(stroke);

                continue;
            }

            var cut = false;
            var current = new List<Vector2> { points[0] };
            for (var i = 1; i < points.Count; i++)
            {
                if (DistanceSquaredToSegment(center, points[i - 1], points[i]) <= radiusSquared)
                {
                    cut = true;
                    if (current.Count >= 2)
                        result.Add(stroke with { Points = current });

                    current = new List<Vector2> { points[i] };
                    continue;
                }

                current.Add(points[i]);
            }

            if (!cut)
            {
                result.Add(stroke);
                continue;
            }

            changed = true;
            if (current.Count >= 2)
                result.Add(stroke with { Points = current });
        }

        if (!changed)
            return;

        draft.SetStrokes(result);
        map.NotifyDraftChanged();
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        if (lengthSquared <= 0.0001f)
            return Vector2.DistanceSquared(point, a);

        var t = Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.DistanceSquared(point, a + ab * t);
    }
}

public sealed class TacticalMapLabelTool : TacticalMapTool
{
    private Vector2 _startPixel;
    private Vector2 _pixel;

    public Vector2i? DraggingLabel { get; private set; }

    // pressing on a label starts a drag, pressing on an empty tile opens the dialog
    public override void Begin(TacticalMapControl map, Vector2 pixel)
    {
        if (map.TryGetDraftLabelAt(pixel, out var tile))
        {
            DraggingLabel = tile;
            _startPixel = pixel;
            _pixel = pixel;
            return;
        }

        map.OpenLabelDialog(map.PixelToIndices(pixel));
    }

    public override void Move(TacticalMapControl map, Vector2 pixel)
    {
        if (DraggingLabel != null)
            _pixel = pixel;
    }

    public override void End(TacticalMapControl map, Vector2 pixel)
    {
        if (DraggingLabel is not { } tile)
            return;

        DraggingLabel = null;
        if ((pixel - _startPixel).Length() > TacticalMapControl.MinDragDistance)
        {
            map.Draft?.MoveLabel(tile, map.PixelToIndices(pixel));
            map.NotifyDraftChanged();
            return;
        }

        // barely moved, treat it as a click & edit the label
        map.OpenLabelDialog(tile);
    }

    public override void Cancel()
    {
        DraggingLabel = null;
    }

    public override void DrawPreview(TacticalMapControl map, DrawingHandleScreen handle)
    {
        if (DraggingLabel is { } tile && map.Draft != null && map.Draft.Labels.TryGetValue(tile, out var label))
            map.DrawDraggedLabel(handle, label.Text, _pixel, label.Color.A <= 0f ? Color.White : label.Color);
    }
}
