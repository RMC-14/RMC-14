using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

using SharedMover = Content.Shared.Vehicle.GridVehicleMoverSystem;

namespace Content.Client.Vehicle;

public sealed class GridVehicleMoverOverlay : Overlay
{
    private const float MovementDecisionFadeSeconds = 2.5f;

    private static readonly Color[] EntryColors =
    {
        Color.Red,
        Color.Green,
        Color.Blue,
        Color.Yellow,
        Color.Magenta,
        Color.Cyan,
        Color.Orange,
    };

    private static readonly Color ClearProbeFill = new(0.19f, 0.95f, 0.55f, 0.08f);
    private static readonly Color ClearProbeOutline = new(0.18f, 1f, 0.55f, 0.74f);
    private static readonly Color AppliedProbeFill = new(0.22f, 0.78f, 1f, 0.08f);
    private static readonly Color AppliedProbeOutline = new(0.25f, 0.84f, 1f, 0.72f);
    private static readonly Color BlockedProbeFill = new(1f, 0.16f, 0.22f, 0.18f);
    private static readonly Color BlockedProbeOutline = new(1f, 0.16f, 0.22f, 0.92f);
    private static readonly Color RideSurfaceOutline = Color.Cyan.WithAlpha(0.75f);
    private static readonly Color RideSoftBorderOutline = Color.Yellow.WithAlpha(0.65f);
    private static readonly Color RideClimbOutline = Color.LimeGreen.WithAlpha(0.85f);

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public bool DebugEnabled { get; set; }
    public bool CollisionsEnabled { get; set; } = true;
    public bool MovementEnabled { get; set; } = true;

    private readonly IEntityManager _ents;
    private readonly IGameTiming _timing;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly EntityQuery<MapGridComponent> _gridQuery;
    private readonly EntityQuery<FixturesComponent> _fixturesQuery;
    private readonly EntityQuery<PhysicsComponent> _physicsQuery;

    private readonly Dictionary<EntityUid, Vector2> _lastProbePositions = new();
    private readonly List<FadingMovementDecision> _movementDecisions = new();

    public GridVehicleMoverOverlay(IEntityManager ents)
    {
        _ents = ents;
        _timing = IoCManager.Resolve<IGameTiming>();
        _transform = ents.System<SharedTransformSystem>();
        _lookup = ents.System<EntityLookupSystem>();
        _gridQuery = ents.GetEntityQuery<MapGridComponent>();
        _fixturesQuery = ents.GetEntityQuery<FixturesComponent>();
        _physicsQuery = ents.GetEntityQuery<PhysicsComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        if (DebugEnabled)
            DrawVehicleDebug(handle, args.MapId);

        if (CollisionsEnabled)
            DrawCollisionDebug(handle, args.MapId);

        if (MovementEnabled)
            DrawMovementDecisions(handle, args.MapId);

        ClearFrameDebug();
    }

    private void DrawVehicleDebug(DrawingHandleWorld handle, MapId mapId)
    {
        var query = _ents.EntityQueryEnumerator<GridVehicleMoverComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mover, out var xform))
        {
            if (xform.GridUid is not { } grid || !_gridQuery.HasComp(grid))
                continue;

            DrawMovementTarget(handle, grid, mover);
            DrawFacing(handle, uid, mover);
            DrawFixtures(handle, uid);
            DrawPhysics(handle, uid);
        }

        DrawRideSurfaces(handle, mapId);
        DrawEntryAndExitPoints(handle, mapId);
        DrawTestedTiles(handle);
    }

    private void DrawMovementTarget(DrawingHandleWorld handle, EntityUid grid, GridVehicleMoverComponent mover)
    {
        var start = ToMapPosition(grid, mover.Position);
        var target = ToMapPosition(grid, new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f));
        var startColor = mover.IsCommittedToMove ? Color.White : Color.Red;

        handle.DrawLine(start, target, Color.Lime);
        handle.DrawCircle(start, 0.15f, startColor);
        handle.DrawCircle(target, 0.15f, Color.Yellow);
    }

    private void DrawFacing(DrawingHandleWorld handle, EntityUid uid, GridVehicleMoverComponent mover)
    {
        if (mover.CurrentDirection == Vector2i.Zero)
            return;

        var xform = _ents.GetComponent<TransformComponent>(uid);
        if (xform.GridUid is not { } grid)
            return;

        var position = ToMapPosition(grid, mover.Position);
        var direction = Vector2.Normalize(new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y));
        handle.DrawLine(position, position + direction * 0.7f, Color.Orange);
    }

    private void DrawFixtures(DrawingHandleWorld handle, EntityUid uid)
    {
        if (!_physicsQuery.HasComp(uid) || !_fixturesQuery.TryComp(uid, out var fixtures))
            return;

        var index = 0;
        foreach (var fixture in fixtures.Fixtures.Values)
        {
            var color = EntryColors[index % EntryColors.Length];
            for (var i = 0; i < fixture.ProxyCount; i++)
                handle.DrawRect(fixture.Proxies[i].AABB, color, false);

            index++;
        }
    }

    private void DrawPhysics(DrawingHandleWorld handle, EntityUid uid)
    {
        var aabb = _lookup.GetWorldAABB(uid);
        var position = _transform.GetWorldPosition(uid);

        handle.DrawRect(aabb, Color.Magenta, false);
        handle.DrawCircle(position, 0.12f, Color.Magenta);
    }

    private void DrawEntryAndExitPoints(DrawingHandleWorld handle, MapId mapId)
    {
        var interiorGrid = FindFirstGrid(mapId);
        DrawEntryPoints(handle, mapId, interiorGrid);
        DrawExitPoints(handle, mapId);
    }

    private EntityUid? FindFirstGrid(MapId mapId)
    {
        var query = _ents.EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == mapId)
                return uid;
        }

        return null;
    }

    private void DrawEntryPoints(DrawingHandleWorld handle, MapId mapId, EntityUid? interiorGrid)
    {
        var query = _ents.EntityQueryEnumerator<VehicleEnterComponent, TransformComponent>();
        while (query.MoveNext(out _, out var enter, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var basePosition = _transform.GetWorldPosition(xform);
            var rotation = xform.LocalRotation;

            for (var i = 0; i < enter.EntryPoints.Count; i++)
            {
                var point = enter.EntryPoints[i];
                var color = GetEntryColor(i);
                var world = basePosition + rotation.RotateVec(point.Offset);
                DrawPoint(handle, world, Math.Max(0.05f, point.Radius), color);

                if (interiorGrid is { } grid && point.InteriorCoords is { } interior)
                    DrawPoint(handle, ToMapPosition(grid, interior), 0.15f, color);
            }
        }
    }

    private void DrawExitPoints(DrawingHandleWorld handle, MapId mapId)
    {
        var query = _ents.EntityQueryEnumerator<VehicleExitComponent, TransformComponent>();
        while (query.MoveNext(out _, out var exit, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            DrawPoint(handle, _transform.GetWorldPosition(xform), 0.12f, GetEntryColor(exit.EntryIndex));
        }
    }

    private static void DrawPoint(DrawingHandleWorld handle, Vector2 position, float radius, Color color)
    {
        handle.DrawCircle(position, radius, color.WithAlpha(0.22f), true);
        handle.DrawCircle(position, radius, color.WithAlpha(0.85f), false);
    }

    private void DrawRideSurfaces(DrawingHandleWorld handle, MapId mapId)
    {
        var query = _ents.EntityQueryEnumerator<VehicleRideSurfaceComponent, TransformComponent>();
        while (query.MoveNext(out _, out var surface, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            DrawRideSurfaceBoxes(handle, surface.Bounds, position, rotation, RideSoftBorderOutline, surface.SoftBorderPadding);
            DrawRideSurfaceBoxes(handle, surface.Bounds, position, rotation, RideSurfaceOutline);
            DrawRideSurfaceBoxes(handle, surface.ClimbBounds, position, rotation, RideClimbOutline);
        }
    }

    private static void DrawRideSurfaceBoxes(
        DrawingHandleWorld handle,
        List<Box2> boxes,
        Vector2 position,
        Angle rotation,
        Color color,
        float padding = 0f)
    {
        foreach (var box in boxes)
        {
            if (!box.IsValid())
                continue;

            var padded = box.Enlarged(MathF.Max(0f, padding));
            var bottomLeft = ToWorld(padded.BottomLeft, position, rotation);
            var bottomRight = ToWorld(padded.BottomRight, position, rotation);
            var topRight = ToWorld(padded.TopRight, position, rotation);
            var topLeft = ToWorld(padded.TopLeft, position, rotation);

            handle.DrawLine(bottomLeft, bottomRight, color);
            handle.DrawLine(bottomRight, topRight, color);
            handle.DrawLine(topRight, topLeft, color);
            handle.DrawLine(topLeft, bottomLeft, color);
        }
    }

    private void DrawTestedTiles(DrawingHandleWorld handle)
    {
        foreach (var (grid, tile) in SharedMover.DebugTestedTiles)
        {
            if (!_gridQuery.HasComp(grid))
                continue;

            var center = ToMapPosition(grid, new Vector2(tile.X + 0.5f, tile.Y + 0.5f));
            handle.DrawRect(Box2.UnitCentered.Translated(center), Color.Black.WithAlpha(0.4f), false);
        }
    }

    private void DrawCollisionDebug(DrawingHandleWorld handle, MapId mapId)
    {
        DrawCollisionProbes(handle, mapId);
        DrawBlockedCollisions(handle, mapId);
    }

    private void DrawCollisionProbes(DrawingHandleWorld handle, MapId mapId)
    {
        _lastProbePositions.Clear();

        foreach (var probe in SharedMover.DebugCollisionProbes)
        {
            if (probe.Map != mapId)
                continue;

            DrawProbePath(handle, probe);
            DrawProbeBounds(handle, probe);
            DrawProbeFacing(handle, probe);
        }
    }

    private void DrawProbePath(DrawingHandleWorld handle, SharedMover.DebugCollisionProbe probe)
    {
        if (_lastProbePositions.TryGetValue(probe.Tested, out var last))
            handle.DrawLine(last, probe.Position, new Color(0.55f, 0.9f, 1f, 0.38f));

        _lastProbePositions[probe.Tested] = probe.Position;
    }

    private static void DrawProbeBounds(DrawingHandleWorld handle, SharedMover.DebugCollisionProbe probe)
    {
        var fill = probe.Blocked
            ? BlockedProbeFill
            : probe.ApplyEffects ? AppliedProbeFill : ClearProbeFill;

        var outline = probe.Blocked
            ? BlockedProbeOutline
            : probe.ApplyEffects ? AppliedProbeOutline : ClearProbeOutline;

        // movement aabb is the actual blocker test; rotated bounds show sampled vehicle pose.
        handle.DrawRect(probe.MovementAabb, fill, true);
        handle.DrawRect(probe.MovementAabb, outline, false);
        handle.DrawRect(probe.FixtureBounds, new Color(0.45f, 0.92f, 1f, 0.72f), false);
        handle.DrawRect(probe.MovementBounds, new Color(0.95f, 0.95f, 1f, 0.45f), false);
        handle.DrawRect(probe.TestedAabb, new Color(0.45f, 0.92f, 1f, 0.18f), false);
    }

    private static void DrawProbeFacing(DrawingHandleWorld handle, SharedMover.DebugCollisionProbe probe)
    {
        var color = probe.Blocked
            ? new Color(1f, 0.24f, 0.26f, 0.95f)
            : new Color(0.22f, 1f, 0.58f, 0.95f);

        var facing = probe.Rotation.RotateVec(Vector2.UnitX);
        handle.DrawCircle(probe.Position, probe.Blocked ? 0.11f : 0.07f, color);
        DrawArrow(handle, probe.Position, probe.Position + facing * 0.85f, color);
    }

    private void DrawMovementDecisions(DrawingHandleWorld handle, MapId mapId)
    {
        CaptureMovementDecisions();

        var now = _timing.CurTime;
        for (var i = _movementDecisions.Count - 1; i >= 0; i--)
        {
            var decision = _movementDecisions[i];
            var age = (float) (now - decision.Created).TotalSeconds;
            if (age >= MovementDecisionFadeSeconds)
            {
                _movementDecisions.RemoveAt(i);
                continue;
            }

            if (decision.Map != mapId)
                continue;

            DrawMovementDecision(handle, decision, 1f - age / MovementDecisionFadeSeconds);
        }
    }

    private void CaptureMovementDecisions()
    {
        var now = _timing.CurTime;
        foreach (var decision in SharedMover.DebugMovementDecisions)
        {
            if (!_gridQuery.HasComp(decision.Grid))
                continue;

            var start = _transform.ToMapCoordinates(new EntityCoordinates(decision.Grid, decision.Start));
            var end = _transform.ToMapCoordinates(new EntityCoordinates(decision.Grid, decision.End));
            if (start.MapId != end.MapId)
                continue;

            _movementDecisions.Add(new FadingMovementDecision(
                start.Position,
                end.Position,
                decision.Kind,
                decision.Success,
                start.MapId,
                now));
        }

        SharedMover.DebugMovementDecisions.Clear();
    }

    private static void DrawMovementDecision(DrawingHandleWorld handle, FadingMovementDecision decision, float alpha)
    {
        var color = GetMovementDecisionColor(decision.Kind, decision.Success).WithAlpha(0.85f * alpha);
        var fill = color.WithAlpha(0.16f * alpha);
        var delta = decision.End - decision.Start;
        var radius = decision.Success ? 0.08f : 0.12f;

        if (delta.LengthSquared() > 0.0001f)
            DrawArrow(handle, decision.Start, decision.End, color);
        else
            handle.DrawCircle(decision.Start, 0.1f, color);

        handle.DrawCircle(decision.Start, radius * 0.75f, fill, true);
        handle.DrawCircle(decision.Start, radius * 0.75f, color, false);
        handle.DrawCircle(decision.End, radius, fill, true);
        handle.DrawCircle(decision.End, radius, color, false);

        if (IsBlockedDecision(decision.Kind))
            DrawBlockedDecisionMarker(handle, decision.End, delta, color.WithAlpha(0.95f * alpha));
    }

    private static bool IsBlockedDecision(SharedMover.DebugMovementDecisionKind kind)
    {
        return kind is
            SharedMover.DebugMovementDecisionKind.DirectBlocked or
            SharedMover.DebugMovementDecisionKind.ForwardBlocked;
    }

    private static void DrawBlockedDecisionMarker(DrawingHandleWorld handle, Vector2 end, Vector2 delta, Color color)
    {
        var side = delta.LengthSquared() > 0.0001f
            ? Vector2.Normalize(new Vector2(-delta.Y, delta.X)) * 0.14f
            : new Vector2(0.14f, 0f);

        handle.DrawLine(end - side, end + side, color);
        handle.DrawLine(end - new Vector2(side.X, -side.Y), end + new Vector2(side.X, -side.Y), color);
    }

    private static Color GetMovementDecisionColor(SharedMover.DebugMovementDecisionKind kind, bool success)
    {
        return kind switch
        {
            SharedMover.DebugMovementDecisionKind.DirectClear => new Color(0.2f, 1f, 0.45f),
            SharedMover.DebugMovementDecisionKind.DirectBlocked => new Color(1f, 0.2f, 0.24f),
            SharedMover.DebugMovementDecisionKind.LaneCorrection => new Color(0.3f, 0.72f, 1f),
            SharedMover.DebugMovementDecisionKind.LaneCorrectionFailed => new Color(1f, 0.62f, 0.18f),
            SharedMover.DebugMovementDecisionKind.ForwardAfterCorrection => new Color(0.78f, 0.42f, 1f),
            SharedMover.DebugMovementDecisionKind.ForwardBlocked => new Color(1f, 0.1f, 0.12f),
            _ => success ? Color.Lime : Color.Red,
        };
    }

    private static void DrawBlockedCollisions(DrawingHandleWorld handle, MapId mapId)
    {
        foreach (var hit in SharedMover.DebugCollisions)
        {
            if (hit.Map != mapId)
                continue;

            DrawCollisionAabbs(handle, hit);
            DrawCollisionDirection(handle, hit);
            DrawCollisionGap(handle, hit);
        }
    }

    private static void DrawCollisionAabbs(DrawingHandleWorld handle, SharedMover.DebugCollision hit)
    {
        handle.DrawRect(hit.TestedAabb, new Color(0.92f, 0.27f, 0.36f, 0.16f), true);
        handle.DrawRect(hit.TestedAabb, new Color(0.97f, 0.38f, 0.46f, 0.85f), false);
        handle.DrawRect(hit.BlockerAabb, new Color(1f, 0.8f, 0.32f, 0.14f), true);
        handle.DrawRect(hit.BlockerAabb, new Color(1f, 0.82f, 0.22f, 0.9f), false);

        var overlap = hit.TestedAabb.Intersect(hit.BlockerAabb);
        if (overlap.Width <= 0f || overlap.Height <= 0f)
            return;

        handle.DrawRect(overlap, new Color(0.83f, 0.23f, 1f, 0.25f), true);
        handle.DrawRect(overlap, new Color(0.74f, 0.16f, 0.95f, 0.9f), false);
    }

    private static void DrawCollisionDirection(DrawingHandleWorld handle, SharedMover.DebugCollision hit)
    {
        var testedCenter = hit.TestedAabb.Center;
        var blockerCenter = hit.BlockerAabb.Center;
        var toBlocker = blockerCenter - testedCenter;
        var distance = toBlocker.Length();

        handle.DrawCircle(testedCenter, 0.09f, new Color(0.97f, 0.38f, 0.46f, 0.85f));
        handle.DrawCircle(blockerCenter, 0.09f, new Color(1f, 0.82f, 0.22f, 0.9f));

        if (distance <= 0.01f)
            return;

        var end = testedCenter + toBlocker / distance * MathF.Min(distance, 0.9f);
        DrawArrow(handle, testedCenter, end, new Color(0.7f, 0.35f, 1f, 0.85f));
    }

    private static void DrawCollisionGap(DrawingHandleWorld handle, SharedMover.DebugCollision hit)
    {
        var gap = hit.Distance - hit.Skin + hit.Clearance;
        var center = (hit.TestedAabb.Center + hit.BlockerAabb.Center) * 0.5f;
        var color = gap <= 0f
            ? new Color(1f, 0.32f, 0.32f, 0.92f)
            : new Color(0.45f, 1f, 0.58f, 0.95f);
        var radius = 0.07f + MathF.Min(MathF.Abs(gap) * 0.03f, 0.09f);

        handle.DrawCircle(center, radius, color);
        handle.DrawCircle(center, radius * 0.55f, color.WithAlpha(0.55f));
    }

    private Vector2 ToMapPosition(EntityUid grid, Vector2 localPosition)
    {
        return _transform.ToMapCoordinates(new EntityCoordinates(grid, localPosition)).Position;
    }

    private static Vector2 ToWorld(Vector2 local, Vector2 position, Angle rotation)
    {
        return position + rotation.RotateVec(local);
    }

    private static Color GetEntryColor(int entryIndex)
    {
        var index = entryIndex < 0 ? Math.Abs(entryIndex) : entryIndex;
        return EntryColors[index % EntryColors.Length];
    }

    private static void DrawArrow(DrawingHandleWorld handle, Vector2 start, Vector2 end, Color color)
    {
        handle.DrawLine(start, end, color);

        var delta = end - start;
        var distance = delta.Length();
        if (distance <= 0.01f)
            return;

        var direction = delta / distance;
        var basePoint = end - direction * 0.12f;
        var perpendicular = new Vector2(-direction.Y, direction.X) * 0.06f;

        handle.DrawLine(end, basePoint + perpendicular, color);
        handle.DrawLine(end, basePoint - perpendicular, color);
    }

    private static void ClearFrameDebug()
    {
        SharedMover.DebugTestedTiles.Clear();
        SharedMover.DebugMovementDecisions.Clear();
        GridVehicleMoverSystem.DebugCollisionPositions.Clear();
    }

    private sealed record FadingMovementDecision(
        Vector2 Start,
        Vector2 End,
        SharedMover.DebugMovementDecisionKind Kind,
        bool Success,
        MapId Map,
        TimeSpan Created);
}
