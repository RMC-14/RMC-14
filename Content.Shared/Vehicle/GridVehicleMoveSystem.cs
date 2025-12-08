using System;
using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Vehicle;

public sealed class GridVehicleMoverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming timing = default!;
    [Dependency] private readonly SharedTransformSystem transform = default!;
    [Dependency] private readonly SharedMapSystem map = default!;
    [Dependency] private readonly SharedPhysicsSystem physics = default!;

    private EntityQuery<MapGridComponent> gridQ;
    private EntityQuery<PhysicsComponent> physicsQ;
    private EntityQuery<FixturesComponent> fixtureQ;

    public override void Initialize()
    {
        base.Initialize();
        gridQ = GetEntityQuery<MapGridComponent>();
        physicsQ = GetEntityQuery<PhysicsComponent>();
        fixtureQ = GetEntityQuery<FixturesComponent>();
        SubscribeLocalEvent<GridVehicleMoverComponent, ComponentStartup>(OnMoverStartup);
    }

    private void OnMoverStartup(Entity<GridVehicleMoverComponent> ent, ref ComponentStartup args)
    {
        var uid = ent.Owner;
        var xform = Transform(uid);
        if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out var gridComp))
            return;

        var coords = xform.Coordinates.WithEntityId(grid, transform, EntityManager);
        var indices = map.TileIndicesFor(grid, gridComp, coords);
        ent.Comp.Tile = indices;
        ent.Comp.Origin = new EntityCoordinates(grid, new Vector2(indices.X + 0.5f, indices.Y + 0.5f));
        ent.Comp.Destination = ent.Comp.Origin.Position;
        Dirty(uid, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GridVehicleMoverComponent, VehicleComponent>();
        while (query.MoveNext(out var uid, out var mover, out var vehicle))
        {
            if (vehicle.MovementKind != VehicleMovementKind.Grid)
                continue;

            if (mover.IsSliding)
            {
                UpdateSlide(uid, ref mover);
                continue;
            }

            if (vehicle.Operator is not { } op)
                continue;

            if (!TryComp<InputMoverComponent>(op, out var input))
                continue;

            var dir = GetInputDirection(input);
            if (dir == Vector2i.Zero)
                continue;

            StartSlide(uid, ref mover, dir);
        }
    }

    private Vector2i GetInputDirection(InputMoverComponent input)
    {
        var buttons = input.HeldMoveButtons;
        var dir = Vector2i.Zero;

        if ((buttons & MoveButtons.Up) != 0)
            dir += new Vector2i(0, 1);
        if ((buttons & MoveButtons.Down) != 0)
            dir += new Vector2i(0, -1);
        if ((buttons & MoveButtons.Right) != 0)
            dir += new Vector2i(1, 0);
        if ((buttons & MoveButtons.Left) != 0)
            dir += new Vector2i(-1, 0);

        if (dir == Vector2i.Zero)
            return dir;

        if (dir.X != 0 && dir.Y != 0)
        {
            if (Math.Abs(dir.X) >= Math.Abs(dir.Y))
                dir = new Vector2i(Math.Sign(dir.X), 0);
            else
                dir = new Vector2i(0, Math.Sign(dir.Y));
        }
        else
        {
            dir = new Vector2i(Math.Sign(dir.X), Math.Sign(dir.Y));
        }

        return dir;
    }

    private void StartSlide(EntityUid uid, ref GridVehicleMoverComponent mover, Vector2i dir)
    {
        var now = timing.CurTime;
        if (mover.LastSlideEnd is { } end && now - end < mover.SlideDelay)
            return;

        var xform = Transform(uid);
        if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out var gridComp))
            return;

        var current = SnapToTile(uid, ref mover, grid, gridComp);

        var newTile = mover.Tile + dir;
        if (!CanBeOnTile(uid, grid, gridComp, newTile))
            return;

        mover.Tile = newTile;
        mover.Origin = current;
        mover.Destination = new Vector2(newTile.X + 0.5f, newTile.Y + 0.5f);

        var distance = (mover.Destination - mover.Origin.Position).Length();
        var speed = MathF.Max(0.1f, mover.SlideSpeed);
        var time = distance / speed;

        mover.SlideDuration = TimeSpan.FromSeconds(time);
        mover.SlideStart = now;
        mover.LastSlideEnd = null;

        var angle = new Vector2(dir.X, dir.Y).ToWorldAngle();
        transform.SetLocalRotation(uid, angle, xform);

        Dirty(uid, mover);
    }

    private void UpdateSlide(EntityUid uid, ref GridVehicleMoverComponent mover)
    {
        var start = mover.SlideStart ?? timing.CurTime;
        var duration = mover.SlideDuration.TotalSeconds;
        if (duration <= 0)
        {
            EndSlide(uid, ref mover);
            return;
        }

        var progress = (float)((timing.CurTime - start).TotalSeconds / duration);
        progress = Math.Clamp(progress, 0f, 1f);

        var pos = LerpSlide(mover.Origin, mover.Destination, progress, mover.LinearInterp);
        SetCoordinates(uid, pos);

        physics.WakeBody(uid);

        if (progress >= 0.99f)
            EndSlide(uid, ref mover);
    }

    private void EndSlide(EntityUid uid, ref GridVehicleMoverComponent mover)
    {
        mover.LastSlideEnd = timing.CurTime;
        mover.SlideStart = null;
        mover.Origin = new EntityCoordinates(mover.Origin.EntityId, mover.Destination);
        SetCoordinates(uid, mover.Origin);
        Dirty(uid, mover);
    }

    private EntityCoordinates SnapToTile(EntityUid uid, ref GridVehicleMoverComponent mover, EntityUid grid, MapGridComponent gridComp)
    {
        var xform = Transform(uid);
        var local = xform.Coordinates.WithEntityId(grid, transform, EntityManager);
        var pos = local.Position;
        var x = (int)Math.Floor(pos.X);
        var y = (int)Math.Floor(pos.Y);
        mover.Tile = new Vector2i(x, y);
        var snapped = new EntityCoordinates(grid, new Vector2(x + 0.5f, y + 0.5f));
        SetCoordinates(uid, snapped);
        return snapped;
    }

    private void SetCoordinates(EntityUid uid, EntityCoordinates newPos)
    {
        var xform = Transform(uid);
        if (!xform.ParentUid.IsValid())
            return;

        var local = newPos.WithEntityId(xform.ParentUid, transform, EntityManager).Position;
        transform.SetLocalPosition(uid, local, xform);
    }

    private bool CanBeOnTile(EntityUid uid, EntityUid grid, MapGridComponent gridComp, Vector2i tile)
    {
        if (!physicsQ.TryComp(uid, out var phys) || !phys.CanCollide)
            return true;

        var coords = new EntityCoordinates(grid, new Vector2(tile.X + 0.5f, tile.Y + 0.5f));
        DebugTools.Assert(grid == coords.EntityId);

        var indices = map.TileIndicesFor(grid, gridComp, coords);
        var enumerator = map.GetAnchoredEntitiesEnumerator(grid, gridComp, indices);

        var (moverLayer, moverMask) = physics.GetHardCollision(uid);

        while (enumerator.MoveNext(out var anchored))
        {
            if (!physicsQ.TryComp(anchored, out var anchoredPhys) || !anchoredPhys.CanCollide)
                continue;

            if (!fixtureQ.TryComp(anchored, out var fixture))
                continue;

            var (anchoredLayer, anchoredMask) = physics.GetHardCollision(anchored.Value, fixture);

            if ((anchoredLayer & moverMask) != 0)
                return false;

            if ((anchoredMask & moverLayer) != 0)
                return false;
        }

        return true;
    }

    private EntityCoordinates LerpSlide(EntityCoordinates a, Vector2 b, float progress, bool linear)
    {
        if (!linear)
            progress = progress - MathF.Sin(MathF.Tau * progress) / MathF.Tau;

        var c = (b - a.Position) * progress + a.Position;
        return new EntityCoordinates(a.EntityId, c);
    }
}
