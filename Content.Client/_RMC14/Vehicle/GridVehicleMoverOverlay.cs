using System;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Client.Vehicle;

public sealed class GridVehicleMoverOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public bool DebugEnabled { get; set; }
    public bool CollisionsEnabled { get; set; } = true;

    private readonly IEntityManager _ents;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly EntityQuery<MapGridComponent> _gridQ;
    private readonly EntityQuery<FixturesComponent> _fixturesQ;
    private readonly EntityQuery<PhysicsComponent> _physicsQ;
    private readonly EntityQuery<VehicleEnterComponent> _enterQ;
    private readonly EntityQuery<VehicleExitComponent> _exitQ;

    private readonly Color[] _colors =
    {
        Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Magenta, Color.Cyan, Color.Orange
    };

    public GridVehicleMoverOverlay(IEntityManager ents)
    {
        _ents = ents;
        _transform = ents.System<SharedTransformSystem>();
        _lookup = ents.System<EntityLookupSystem>();
        _gridQ = ents.GetEntityQuery<MapGridComponent>();
        _fixturesQ = ents.GetEntityQuery<FixturesComponent>();
        _physicsQ = ents.GetEntityQuery<PhysicsComponent>();
        _enterQ = ents.GetEntityQuery<VehicleEnterComponent>();
        _exitQ = ents.GetEntityQuery<VehicleExitComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (DebugEnabled)
        {
            var handle = args.WorldHandle;
            var q = _ents.EntityQueryEnumerator<GridVehicleMoverComponent, TransformComponent>();

            while (q.MoveNext(out var uid, out var mover, out var xform))
            {
                if (xform.GridUid is not { } grid || !_gridQ.TryComp(grid, out _))
                    continue;

                DrawMovement(handle, grid, mover);
                DrawFacing(handle, uid, mover);
                DrawFixtures(handle, uid);
                DrawPhysics(handle, uid);
            }

            DrawEntryAndExitPoints(handle, args.MapId);

            foreach (var entry in Content.Shared.Vehicle.GridVehicleMoverSystem.DebugTestedTiles)
            {
                var grid = entry.grid;
                var tile = entry.tile;

                if (!_gridQ.TryComp(grid, out _))
                    continue;

                var center = _transform.ToMapCoordinates(new EntityCoordinates(grid, new Vector2(tile.X + 0.5f, tile.Y + 0.5f))).Position;
                var min = center - new Vector2(0.5f, 0.5f);
                var max = center + new Vector2(0.5f, 0.5f);
                var box = new Box2(min, max);

                handle.DrawRect(box, Color.Black.WithAlpha(0.4f), false);
            }

            if (CollisionsEnabled)
                DrawCollisions(handle, args.MapId);
        }
        else if (CollisionsEnabled)
        {
            DrawCollisions(args.WorldHandle, args.MapId);
        }

        Content.Shared.Vehicle.GridVehicleMoverSystem.DebugTestedTiles.Clear();
        GridVehicleMoverSystem.DebugCollisionPositions.Clear();
    }

    private void DrawMovement(DrawingHandleWorld h, EntityUid grid, GridVehicleMoverComponent mover)
    {
        var start = _transform.ToMapCoordinates(new EntityCoordinates(grid, mover.Position)).Position;
        var target = _transform.ToMapCoordinates(new EntityCoordinates(grid, new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f))).Position;

        h.DrawLine(start, target, Color.Lime);

        var color = mover.IsCommittedToMove ? Color.White : Color.Red;

        h.DrawCircle(start, 0.15f, color);
        h.DrawCircle(target, 0.15f, Color.Yellow);
    }

    private void DrawFacing(DrawingHandleWorld h, EntityUid uid, GridVehicleMoverComponent mover)
    {
        if (mover.CurrentDirection == Vector2i.Zero)
            return;

        var xform = _ents.GetComponent<TransformComponent>(uid);
        if (xform.GridUid is not { } grid)
            return;

        var pos = _transform.ToMapCoordinates(new EntityCoordinates(grid, mover.Position)).Position;
        var dir = Vector2.Normalize(new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y));

        h.DrawLine(pos, pos + dir * 0.7f, Color.Orange);
    }

    private void DrawFixtures(DrawingHandleWorld h, EntityUid uid)
    {
        if (!_physicsQ.TryComp(uid, out var body))
            return;
        if (!_fixturesQ.TryComp(uid, out var fixtures))
            return;

        var index = 0;

        foreach (var f in fixtures.Fixtures.Values)
        {
            for (var i = 0; i < f.ProxyCount; i++)
            {
                var proxy = f.Proxies[i];
                var box = proxy.AABB;
                var col = _colors[index % _colors.Length];
                h.DrawRect(box, col, false);
            }
            index++;
        }
    }

    private void DrawPhysics(DrawingHandleWorld h, EntityUid uid)
    {
        var aabb = _lookup.GetWorldAABB(uid);
        h.DrawRect(aabb, Color.Magenta, false);

        var pos = _transform.GetWorldPosition(uid);
        h.DrawCircle(pos, 0.12f, Color.Magenta);
    }

    private void DrawEntryAndExitPoints(DrawingHandleWorld h, MapId mapId)
    {
        EntityUid? interiorGrid = null;
        var gridEnum = _ents.EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        
        while (gridEnum.MoveNext(out var gridUid, out _, out var gridXform))
        {
            if (gridXform.MapID != mapId)
                continue;
            interiorGrid = gridUid;
            break;
        }

        var enterEnum = _ents.EntityQueryEnumerator<VehicleEnterComponent, TransformComponent>();
        while (enterEnum.MoveNext(out var uid, out var enter, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var basePos = _transform.GetWorldPosition(xform);
            var rot = xform.LocalRotation;

            for (var i = 0; i < enter.EntryPoints.Count; i++)
            {
                var point = enter.EntryPoints[i];
                var color = _colors[i % _colors.Length];
                var world = basePos + rot.RotateVec(point.Offset);
                var radius = Math.Max(0.05f, point.Radius);

                h.DrawCircle(world, radius, color.WithAlpha(0.22f), true);
                h.DrawCircle(world, radius, color.WithAlpha(0.85f), false);

                if (interiorGrid is { } grid && point.InteriorCoords is { } interior)
                {
                    var target = _transform.ToMapCoordinates(new EntityCoordinates(grid, interior)).Position;
                    h.DrawCircle(target, 0.15f, color.WithAlpha(0.22f), true);
                    h.DrawCircle(target, 0.15f, color.WithAlpha(0.85f), false);
                }

            }
        }

        var exitEnum = _ents.EntityQueryEnumerator<VehicleExitComponent, TransformComponent>();
        while (exitEnum.MoveNext(out _, out var exit, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var pos = _transform.GetWorldPosition(xform);
            var color = _colors[Math.Abs(exit.EntryIndex) % _colors.Length];

            h.DrawCircle(pos, 0.12f, color.WithAlpha(0.2f), true);
            h.DrawCircle(pos, 0.12f, color.WithAlpha(0.9f), false);
        }
    }

    private void DrawCollisions(DrawingHandleWorld h, MapId mapId)
    {
        foreach (var hit in Content.Shared.Vehicle.GridVehicleMoverSystem.DebugCollisions)
        {
            if (hit.Map != mapId)
                continue;

            var testedFill = new Color(0.92f, 0.27f, 0.36f, 0.16f);
            var testedOutline = new Color(0.97f, 0.38f, 0.46f, 0.85f);

            h.DrawRect(hit.TestedAabb, testedFill, true);
            h.DrawRect(hit.TestedAabb, testedOutline, false);

            var blockerFill = new Color(1f, 0.8f, 0.32f, 0.14f);
            var blockerOutline = new Color(1f, 0.82f, 0.22f, 0.9f);

            h.DrawRect(hit.BlockerAabb, blockerFill, true);
            h.DrawRect(hit.BlockerAabb, blockerOutline, false);

            var testedCenter = hit.TestedAabb.Center;
            var blockerCenter = hit.BlockerAabb.Center;

            h.DrawCircle(testedCenter, 0.09f, testedOutline);
            h.DrawCircle(blockerCenter, 0.09f, blockerOutline);

            var overlap = hit.TestedAabb.Intersect(hit.BlockerAabb);
            if (overlap.Width > 0f && overlap.Height > 0f)
            {
                var overlapFill = new Color(0.83f, 0.23f, 1f, 0.25f);
                var overlapOutline = new Color(0.74f, 0.16f, 0.95f, 0.9f);
                h.DrawRect(overlap, overlapFill, true);
                h.DrawRect(overlap, overlapOutline, false);
            }

            var toBlocker = blockerCenter - testedCenter;
            var dist = toBlocker.Length();

            if (dist > 0.01f)
            {
                var dir = toBlocker / dist;
                var maxArrow = MathF.Min(dist, 0.9f);
                var arrowEnd = testedCenter + dir * maxArrow;
                var arrowColor = new Color(0.7f, 0.35f, 1f, 0.85f);

                h.DrawLine(testedCenter, arrowEnd, arrowColor);

                var headLength = 0.12f;
                var headWidth = 0.06f;
                var basePoint = arrowEnd - dir * headLength;
                var perp = new Vector2(-dir.Y, dir.X) * headWidth;

                h.DrawLine(arrowEnd, basePoint + perp, arrowColor);
                h.DrawLine(arrowEnd, basePoint - perp, arrowColor);
            }

            var gap = hit.Distance - hit.Skin + hit.Clearance;
            var mid = (testedCenter + blockerCenter) * 0.5f;
            var gapColor = gap <= 0 ? new Color(1f, 0.32f, 0.32f, 0.92f) : new Color(0.45f, 1f, 0.58f, 0.95f);
            var gapRadius = 0.07f + MathF.Min(MathF.Abs(gap) * 0.03f, 0.09f);

            h.DrawCircle(mid, gapRadius, gapColor);
            h.DrawCircle(mid, gapRadius * 0.55f, gapColor.WithAlpha(0.55f));
        }
    }
}
