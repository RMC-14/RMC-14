using System.Numerics;
using Content.Shared._RMC14.Entrenching;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Line;

public sealed class LineSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    private EntityQuery<BarricadeComponent> _barricadeQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
    }

    public List<LineTile> DrawLine(EntityCoordinates start, EntityCoordinates end, TimeSpan delayPer)
    {
        start = _mapSystem.AlignToGrid(_transform.GetMoverCoordinates(start));
        end = _mapSystem.AlignToGrid(_transform.GetMoverCoordinates(end));
        var tiles = new List<LineTile>();
        if (!start.TryDistance(EntityManager, _transform, end, out var distance))
            return tiles;

        var distanceX = end.X - start.X;
        var distanceY = end.Y - start.Y;
        var x = start.X;
        var y = start.Y;
        var xOffset = distanceX / distance;
        var yOffset = distanceY / distance;
        var time = _timing.CurTime;
        var gridId = _transform.GetGrid(start.EntityId);
        var gridComp = gridId == null ? null : _mapGridQuery.CompOrNull(gridId.Value);
        Entity<MapGridComponent>? grid = gridComp == null ? null : new Entity<MapGridComponent>(gridId!.Value, gridComp);
        var lastCoords = start;
        var delay = 0;

        for (var i = 0; i < distance; i++)
        {
            x += xOffset;
            y += yOffset;

            var entityCoords = new EntityCoordinates(start.EntityId, x, y).SnapToGrid(EntityManager, _mapManager);
            if (entityCoords == lastCoords)
                continue;

            var direction = (entityCoords.Position - lastCoords.Position).ToWorldAngle().GetCardinalDir();
            var blocked = IsTileBlocked(grid, entityCoords, direction);
            if (blocked)
                break;

            lastCoords = entityCoords;
            var mapCoords = _transform.ToMapCoordinates(entityCoords);
            tiles.Add(new LineTile(mapCoords, time + delayPer * delay, direction));
            delay++;
        }

        return tiles;
    }

    private bool IsTileBlocked(Entity<MapGridComponent>? grid, EntityCoordinates coords, Direction direction)
    {
        if (grid == null)
            return false;

        var indices = _mapSystem.TileIndicesFor(grid.Value, grid, coords);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid.Value, grid, indices);
        while (anchored.MoveNext(out var uid))
        {
            if (_barricadeQuery.HasComp(uid))
            {
                if (_doorQuery.TryComp(uid, out var door) && door.State != DoorState.Closed)
                    continue;

                var barricadeDir = _transform.GetWorldRotation(uid.Value).GetCardinalDir();
                if (barricadeDir == direction || barricadeDir == direction.GetOpposite())
                    return true;
            }
            else if (_tag.HasTag(uid.Value, StructureTag))
            {
                return true;
            }
        }

        return false;
    }
}
