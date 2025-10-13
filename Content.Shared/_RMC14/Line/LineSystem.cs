using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared.Beam.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
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
    [Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly float MaxBeamDistance = 500;

    private EntityQuery<BarricadeComponent> _barricadeQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
    }

    public List<LineTile> DrawLine(EntityCoordinates start, EntityCoordinates end, TimeSpan delayPer, float? range, out EntityUid? blocker, bool hitBlocker = false)
    {
        blocker = null;
        start = _mapSystem.AlignToGrid(_transform.GetMoverCoordinates(start));
        end = _mapSystem.AlignToGrid(_transform.GetMoverCoordinates(end));
        var tiles = new List<LineTile>();
        if (!start.TryDistance(EntityManager, _transform, end, out var distance))
            return tiles;

        if (range != null)
            distance = Math.Min(range.Value, distance);

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

            var direction = (entityCoords.Position - lastCoords.Position).ToWorldAngle();
            var blocked = IsTileBlocked(grid, entityCoords, direction, out blocker);
            if (blocked && !hitBlocker)
                break;

            lastCoords = entityCoords;
            var mapCoords = _transform.ToMapCoordinates(entityCoords);
            tiles.Add(new LineTile(mapCoords, time + delayPer * delay));
            delay++;

            if (blocked)
                break;
        }

        return tiles;
    }

    private bool IsTileBlocked(Entity<MapGridComponent>? grid, EntityCoordinates coords, Angle angle, [NotNullWhen(true)] out EntityUid? blocker)
    {
        blocker = default;
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
                var direction = angle.GetDir();
                if (barricadeDir == direction || barricadeDir == direction.GetOpposite())
                {
                    blocker = uid.Value;
                    return true;
                }

                if (!direction.IsCardinal())
                {
                    var blocked = direction switch
                    {
                        Direction.SouthEast => barricadeDir is Direction.North or Direction.West,
                        Direction.NorthEast => barricadeDir is Direction.South or Direction.West,
                        Direction.NorthWest => barricadeDir is Direction.South or Direction.East,
                        Direction.SouthWest => barricadeDir is Direction.North or Direction.East,
                        _ => false,
                    };

                    if (blocked)
                    {
                        blocker = uid.Value;
                        return true;
                    }
                }
            }
            else if (_doorQuery.TryComp(uid, out var door))
            {
                if (door.State != DoorState.Closed)
                    continue;

                blocker = uid.Value;
                return true;
            }
            else if (_tag.HasAnyTag(uid.Value, StructureTag, WallTag))
            {
                blocker = uid.Value;
                return true;
            }
        }

        return false;
    }

    //RMC Beam code
    //Purely visual, to have a line update on move etc
    //Delete the previous line this returned and make a new one

    public bool TryCreateLine(EntityUid source, EntityUid target, string proto, out List<EntityUid> lines)
    {
        lines = new List<EntityUid>();

        //Can't run on client
        if (_net.IsClient)
            return false;

        if (Deleted(source) || Deleted(target))
            return false;

        if (_transform.GetMapId(source) != _transform.GetMapId(target))
            return false;

        var sourceMapPos = _transform.GetMapCoordinates(source);
        var targetMapPos = _transform.GetMapCoordinates(target);

        var calculatedDistance = targetMapPos.Position - sourceMapPos.Position;
        var sourceAngle = calculatedDistance.ToWorldAngle();

        if (sourceMapPos.MapId != targetMapPos.MapId)
            return false;

        var beamStartPos = sourceMapPos.Offset(calculatedDistance.Normalized());

        //Don't divide by zero or make a mega beam
        if (calculatedDistance.Length() == 0 || calculatedDistance.Length() > MaxBeamDistance)
            return false;

        var distanceCorrection = calculatedDistance - calculatedDistance.Normalized();

        var beam = Spawn(proto, beamStartPos);
        lines.Add(beam);

        var distanceLength = distanceCorrection.Length();

        var beamVisualizerEvent = new BeamVisualizerEvent(GetNetEntity(beam), distanceLength, sourceAngle, shader: "shaded");
        RaiseNetworkEvent(beamVisualizerEvent);

        for (var i = 0; i < distanceLength - 1; i++)
        {
            beamStartPos = beamStartPos.Offset(calculatedDistance.Normalized());
            var newEnt = Spawn(proto, beamStartPos);
            lines.Add(newEnt);

            var ev = new BeamVisualizerEvent(GetNetEntity(newEnt), distanceLength, sourceAngle, shader: "shaded");
            RaiseNetworkEvent(ev);
        }

        return true;
    }


    public void DeleteBeam(List<EntityUid> beam)
    {
        if (_net.IsClient)
            return;

        foreach (var ent in beam)
        {
            QueueDel(ent);
        }

        beam.Clear();
    }
}
