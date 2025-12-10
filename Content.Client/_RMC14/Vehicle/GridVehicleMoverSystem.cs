using System.Collections.Generic;
using System.Numerics;
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

public sealed class GridVehicleMoverSystem : EntitySystem
{
    private GridVehicleMoverOverlay? overlay;

    public override void Initialize()
    {
        overlay = new GridVehicleMoverOverlay(EntityManager);
        IoCManager.Resolve<IOverlayManager>().AddOverlay(overlay);
    }

    public override void Shutdown()
    {
        if (overlay != null)
            IoCManager.Resolve<IOverlayManager>().RemoveOverlay(overlay);
    }
}

public sealed class GridVehicleMoverOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly IEntityManager ents;
    private readonly SharedTransformSystem transform;
    private readonly EntityLookupSystem lookup;
    private readonly EntityQuery<MapGridComponent> gridQ;
    private readonly EntityQuery<FixturesComponent> fixturesQ;
    private readonly EntityQuery<PhysicsComponent> physicsQ;

    private readonly Color[] colors =
    {
        Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Magenta, Color.Cyan, Color.Orange
    };

    public GridVehicleMoverOverlay(IEntityManager ents)
    {
        this.ents = ents;
        transform = ents.System<SharedTransformSystem>();
        lookup = ents.System<EntityLookupSystem>();
        gridQ = ents.GetEntityQuery<MapGridComponent>();
        fixturesQ = ents.GetEntityQuery<FixturesComponent>();
        physicsQ = ents.GetEntityQuery<PhysicsComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var q = ents.EntityQueryEnumerator<GridVehicleMoverComponent, TransformComponent>();
        while (q.MoveNext(out var uid, out var mover, out var xform))
        {
            if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out _))
                continue;
            DrawMovement(handle, grid, mover);
            DrawFacing(handle, uid, mover);
            DrawFixtures(handle, uid);
            DrawPhysics(handle, uid);
        }
        DrawCollisions(handle);
        foreach (var entry in Content.Shared.Vehicle.GridVehicleMoverSystem.DebugTestedTiles)
        {
            var grid = entry.grid;
            var tile = entry.tile;
            if (!gridQ.TryComp(grid, out _))
                continue;
            var center = transform.ToMapCoordinates(new EntityCoordinates(grid, new Vector2(tile.X + 0.5f, tile.Y + 0.5f))).Position;
            var min = center - new Vector2(0.5f, 0.5f);
            var max = center + new Vector2(0.5f, 0.5f);
            var box = new Box2(min, max);
            handle.DrawRect(box, Color.Black.WithAlpha(0.4f), false);
        }
        Content.Shared.Vehicle.GridVehicleMoverSystem.DebugTestedTiles.Clear();
    }

    private void DrawMovement(DrawingHandleWorld h, EntityUid grid, GridVehicleMoverComponent mover)
    {
        var start = transform.ToMapCoordinates(new EntityCoordinates(grid, mover.Position)).Position;
        var target = transform.ToMapCoordinates(new EntityCoordinates(grid, new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f))).Position;
        h.DrawLine(start, target, Color.Lime);
        var color = mover.IsCommittedToMove ? Color.White : Color.Red;
        h.DrawCircle(start, 0.15f, color);
        h.DrawCircle(target, 0.15f, Color.Yellow);
    }

    private void DrawFacing(DrawingHandleWorld h, EntityUid uid, GridVehicleMoverComponent mover)
    {
        if (mover.CurrentDirection == Vector2i.Zero)
            return;
        var xform = ents.GetComponent<TransformComponent>(uid);
        if (xform.GridUid is not { } grid)
            return;
        var pos = transform.ToMapCoordinates(new EntityCoordinates(grid, mover.Position)).Position;
        var dir = Vector2.Normalize(new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y));
        h.DrawLine(pos, pos + dir * 0.7f, Color.Orange);
    }

    private void DrawFixtures(DrawingHandleWorld h, EntityUid uid)
    {
        if (!physicsQ.TryComp(uid, out var body))
            return;
        if (!fixturesQ.TryComp(uid, out var fixtures))
            return;
        var index = 0;
        foreach (var f in fixtures.Fixtures.Values)
        {
            for (var i = 0; i < f.ProxyCount; i++)
            {
                var proxy = f.Proxies[i];
                var box = proxy.AABB;
                var col = colors[index % colors.Length];
                h.DrawRect(box, col, false);
            }
            index++;
        }
    }

    private void DrawPhysics(DrawingHandleWorld h, EntityUid uid)
    {
        var aabb = lookup.GetWorldAABB(uid);
        h.DrawRect(aabb, Color.Magenta, false);
        var pos = transform.GetWorldPosition(uid);
        h.DrawCircle(pos, 0.12f, Color.Magenta);
    }

    private void DrawCollisions(DrawingHandleWorld h)
    {
        foreach (var hit in Content.Shared.Vehicle.GridVehicleMoverSystem.DebugCollisions)
        {
            h.DrawRect(hit.TestedAabb, Color.Red.WithAlpha(0.6f), false);
            h.DrawRect(hit.BlockerAabb, Color.Yellow.WithAlpha(0.6f), false);

            var testedCenter = hit.TestedAabb.Center;
            var blockerCenter = hit.BlockerAabb.Center;
            h.DrawCircle(testedCenter, 0.08f, Color.Red);
            h.DrawCircle(blockerCenter, 0.08f, Color.Yellow);
            h.DrawLine(testedCenter, blockerCenter, Color.Magenta);

            var gap = hit.Distance - hit.Skin + hit.Clearance;
            var mid = (testedCenter + blockerCenter) * 0.5f;
            var gapColor = gap <= 0 ? Color.Red : Color.Lime;
            h.DrawCircle(mid, 0.06f, gapColor.WithAlpha(0.8f));
        }
    }
}
