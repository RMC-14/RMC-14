using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.Maths;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._RMC14.TacticalMap;

public sealed partial class TacticalMapControl
{
    private void AddLineSegment(Vector2 start, Vector2 end)
    {
        if ((end - start).LengthSquared() < 0.01f)
            return;

        AddLineToCanvas(start, end);
    }

    private void TryEraseAt(Vector2 controlPosition)
    {
        if (Texture == null || Lines.Count == 0)
            return;

        (_, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();
        if (overlayScale <= 0f)
            return;

        Vector2 linePosition = PositionToLineCoordinatesFloat(controlPosition, actualTopLeft, overlayScale);
        float eraserRadius = (EraserRadiusPixels + LineThickness * 1.5f) / Math.Max(overlayScale, 0.001f);

        EraseLinesNear(linePosition, eraserRadius);
    }

    private void EraseLinesNear(Vector2 linePosition, float radius)
    {
        float radiusSquared = radius * radius;

        for (int i = Lines.Count - 1; i >= 0; i--)
        {
            TacticalMapLine line = Lines[i];
            float thickness = GetLineThickness(i, line);
            float effectiveRadius = radius + thickness * 0.6f;
            float effectiveRadiusSquared = effectiveRadius * effectiveRadius;

            if (DistanceSquaredPointToSegment(linePosition, line.Start, line.End) <= effectiveRadiusSquared)
            {
                Lines.RemoveAt(i);
                if (i < LineThicknesses.Count)
                    LineThicknesses.RemoveAt(i);
            }
        }
    }

    private static float DistanceSquaredPointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLengthSquared = ab.LengthSquared();
        if (abLengthSquared <= 0.0001f)
            return Vector2.DistanceSquared(point, a);

        float t = Vector2.Dot(point - a, ab) / abLengthSquared;
        t = Math.Clamp(t, 0f, 1f);
        Vector2 closest = a + ab * t;
        return Vector2.DistanceSquared(point, closest);
    }

    private Vector2i SnapToStraightLine(Vector2i start, Vector2i end)
    {
        int deltaX = end.X - start.X;
        int deltaY = end.Y - start.Y;
        int absDeltaX = Math.Abs(deltaX);
        int absDeltaY = Math.Abs(deltaY);

        if (absDeltaX > absDeltaY * 2)
        {
            return new Vector2i(end.X, start.Y);
        }
        else if (absDeltaY > absDeltaX * 2)
        {
            return new Vector2i(start.X, end.Y);
        }
        else
        {
            int diagDist = Math.Min(absDeltaX, absDeltaY);
            return new Vector2i(
                start.X + (deltaX >= 0 ? diagDist : -diagDist),
                start.Y + (deltaY >= 0 ? diagDist : -diagDist)
            );
        }
    }

    private void AddLineToCanvas(Vector2 start, Vector2 end)
    {
        Lines.Add(new TacticalMapLine(start, end, Color, LineThickness));
        LineThicknesses.Add(LineThickness);

        while (LineLimit >= 0 && Lines.Count > LineLimit)
        {
            Lines.RemoveAt(0);
            if (LineThicknesses.Count > 0)
                LineThicknesses.RemoveAt(0);
        }
    }

    public void RemoveLinesByColor(Color color)
    {
        for (int i = Lines.Count - 1; i >= 0; i--)
        {
            if (Lines[i].Color == color)
            {
                Lines.RemoveAt(i);
                if (i < LineThicknesses.Count)
                    LineThicknesses.RemoveAt(i);
            }
        }
    }

    public void AddDashedTunnelPath(List<Vector2i> waypoints, Color pathColor, bool removeExisting = true)
    {
        if (removeExisting)
            RemoveLinesByColor(pathColor);

        if (waypoints.Count < 2)
            return;

        if (waypoints.Count == 2)
        {
            Vector2 startPos = ConvertIndicesToLineCoordinates(waypoints[0]);
            Vector2 endPos = ConvertIndicesToLineCoordinates(waypoints[1]);
            float distance = Vector2.Distance(startPos, endPos);
            float scaledBlipSize = BaseBlipSize;

            if (distance < scaledBlipSize * 1.2f)
                return;
        }

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector2i start = waypoints[i];
            Vector2i end = waypoints[i + 1];

            Vector2 fromPoint = GetEdgePoint(start, end, true);
            Vector2 toPoint = GetEdgePoint(start, end, false);

            if (i == waypoints.Count - 2)
            {
                Vector2 direction = toPoint - fromPoint;
                float lineLength = direction.Length();

                if (lineLength > 0f)
                {
                    float scaledBlipSize = BaseBlipSize;
                    float arrowStartOffset = scaledBlipSize * 0.45f + ArrowLength;

                    if (lineLength > arrowStartOffset)
                    {
                        toPoint -= direction / lineLength * arrowStartOffset;
                    }
                }
            }

            AddDashedSegment(fromPoint, toPoint, pathColor);
        }

        if (waypoints.Count >= 2)
        {
            AddSimpleArrowHead(
                ConvertIndicesToLineCoordinates(waypoints[^2]),
                ConvertIndicesToLineCoordinates(waypoints[^1]),
                pathColor
            );
        }
    }

    public void AddDashedTunnelPathClosest(List<Vector2i> waypoints, Color pathColor, bool removeExisting = true)
    {
        if (removeExisting)
            RemoveLinesByColor(pathColor);

        if (waypoints.Count < 2)
            return;

        if (waypoints.Count == 2)
        {
            Vector2 startPos = ConvertIndicesToLineCoordinates(waypoints[0]);
            Vector2 endPos = ConvertIndicesToLineCoordinates(waypoints[1]);
            float distance = Vector2.Distance(startPos, endPos);
            float scaledBlipSize = BaseBlipSize;

            if (distance < scaledBlipSize * 1.2f)
                return;
        }

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector2 fromPoint = GetClosestEdgePoint(waypoints[i], waypoints[i + 1], true);
            Vector2 toPoint = GetClosestEdgePoint(waypoints[i], waypoints[i + 1], false);

            if (i == waypoints.Count - 2)
            {
                Vector2 direction = toPoint - fromPoint;
                float lineLength = direction.Length();

                if (lineLength > 0f)
                {
                    float scaledBlipSize = BaseBlipSize;
                    float arrowStartOffset = scaledBlipSize * 0.45f + ArrowLength;

                    if (lineLength > arrowStartOffset)
                    {
                        toPoint -= direction / lineLength * arrowStartOffset;
                    }
                }
            }

            AddDashedSegment(fromPoint, toPoint, pathColor);
        }

        if (waypoints.Count >= 2)
        {
            AddSimpleArrowHead(
                ConvertIndicesToLineCoordinates(waypoints[^2]),
                ConvertIndicesToLineCoordinates(waypoints[^1]),
                pathColor
            );
        }
    }

    private Vector2 GetClosestEdgePoint(Vector2i fromIndices, Vector2i toIndices, bool getFromPoint)
    {
        Vector2 fromUI = ConvertIndicesToLineCoordinates(fromIndices);
        Vector2 toUI = ConvertIndicesToLineCoordinates(toIndices);
        Vector2 sourcePoint = getFromPoint ? fromUI : toUI;
        Vector2 targetPoint = getFromPoint ? toUI : fromUI;
        Vector2 direction = targetPoint - sourcePoint;
        float distance = direction.Length();

        if (distance == 0f)
            return sourcePoint;

        float edgeOffset = BaseBlipSize * BlipEdgeRatio;
        if (distance < edgeOffset * CloseBlipThreshold)
            edgeOffset = edgeOffset * (distance / (edgeOffset * CloseBlipThreshold)) * CloseBlipSafety;

        return sourcePoint + direction / distance * edgeOffset;
    }

    private Vector2 GetEdgePoint(Vector2i fromIndices, Vector2i toIndices, bool getFromPoint)
    {
        Vector2 fromUI = ConvertIndicesToLineCoordinates(fromIndices);
        Vector2 toUI = ConvertIndicesToLineCoordinates(toIndices);
        Vector2 sourcePoint = getFromPoint ? fromUI : toUI;
        Vector2 targetPoint = getFromPoint ? toUI : fromUI;
        Vector2 direction = targetPoint - sourcePoint;
        float distance = direction.Length();

        if (distance == 0f)
            return sourcePoint;

        float scaledBlipSize = BaseBlipSize;
        float edgeOffset = scaledBlipSize * 0.4f;
        float minOffset = scaledBlipSize * 0.2f;

        if (distance < scaledBlipSize * 1.5f)
        {
            edgeOffset = Math.Max(minOffset, distance * 0.3f);
        }

        return sourcePoint + direction / distance * edgeOffset;
    }

    private void AddDashedSegment(Vector2 fromUI, Vector2 toUI, Color color)
    {
        Vector2 direction = toUI - fromUI;
        float totalLength = direction.Length();

        if (totalLength < 1f)
            return;

        for (float distance = 0; distance < totalLength; distance += DashLength + GapLength)
        {
            Lines.Add(new TacticalMapLine(
                fromUI + direction / totalLength * distance,
                fromUI + direction / totalLength * Math.Min(distance + DashLength, totalLength),
                color,
                LineThickness
            ));
            LineThicknesses.Add(LineThickness);
        }
    }

    private void AddSimpleArrowHead(Vector2 start, Vector2 end, Color color)
    {
        Vector2 direction = end - start;
        float length = direction.Length();

        if (length < 10f)
            return;

        float scaledBlipSize = BaseBlipSize;
        float blipEdgeOffset = scaledBlipSize * 0.45f;

        Vector2 arrowTip = end - direction / length * blipEdgeOffset;

        Vector2 arrowBase = arrowTip - direction / length * ArrowLength;

        Vector2 perp = new(-direction.Y / length, direction.X / length);

        Vector2 leftWing = arrowBase + perp * ArrowWidth;

        Vector2 rightWing = arrowBase - perp * ArrowWidth;

        Lines.Add(new TacticalMapLine(arrowTip, leftWing, color, LineThickness));
        LineThicknesses.Add(LineThickness);
        Lines.Add(new TacticalMapLine(arrowTip, rightWing, color, LineThickness));
        LineThicknesses.Add(LineThickness);
        Lines.Add(new TacticalMapLine(leftWing, rightWing, color, LineThickness));
        LineThicknesses.Add(LineThickness);
    }

    private float GetLineThickness(int index, TacticalMapLine line)
    {
        return line.Thickness > 0
            ? line.Thickness
            : (index < LineThicknesses.Count ? LineThicknesses[index] : 2.0f);
    }
}
