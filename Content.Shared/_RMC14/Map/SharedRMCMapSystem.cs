using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Map;

public abstract class SharedRMCMapSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    private EntityQuery<MapGridComponent> _mapGridQuery;

    public readonly ImmutableArray<AtmosDirection> AtmosCardinalDirections = ImmutableArray.Create(
        AtmosDirection.South,
        AtmosDirection.East,
        AtmosDirection.North,
        AtmosDirection.West
    );

    public readonly ImmutableArray<Direction> CardinalDirections = ImmutableArray.Create(
        Direction.South,
        Direction.East,
        Direction.North,
        Direction.West
    );

    public override void Initialize()
    {
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
    }

    public RMCAnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(EntityUid ent, Direction? offset = null, DirectionFlag facing = DirectionFlag.None)
    {
        return GetAnchoredEntitiesEnumerator(ent.ToCoordinates(), offset, facing);
    }

    public RMCAnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(EntityCoordinates coords, Direction? offset = null, DirectionFlag facing = DirectionFlag.None)
    {
        if (_transform.GetGrid(coords) is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var gridComp))
        {
            return RMCAnchoredEntitiesEnumerator.Empty;
        }

        var indices = _map.CoordinatesToTile(gridId, gridComp, coords);
        return GetAnchoredEntitiesEnumerator((gridId, gridComp), indices, offset, facing);
    }

    public RMCAnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(Entity<MapGridComponent> grid, Vector2i indices, Direction? offset = null, DirectionFlag facing = DirectionFlag.None)
    {
        if (offset != null)
            indices = indices.Offset(offset.Value);

        var anchored = _map.GetAnchoredEntitiesEnumerator(grid, grid, indices);
        return new RMCAnchoredEntitiesEnumerator(_transform, anchored, facing);
    }

    public bool HasAnchoredEntityEnumerator<T>(EntityCoordinates coords, out Entity<T> ent, Direction? offset = null, DirectionFlag facing = DirectionFlag.None) where T : IComponent
    {
        ent = default;
        var anchored = GetAnchoredEntitiesEnumerator(coords, offset, facing);
        while (anchored.MoveNext(out var uid))
        {
            if (!TryComp(uid, out T? comp))
                continue;

            ent = (uid, comp);
            return true;
        }

        return false;
    }

    public bool HasAnchoredEntityEnumerator<T>(EntityCoordinates coords, Direction? offset = null, DirectionFlag facing = DirectionFlag.None) where T : IComponent
    {
        return HasAnchoredEntityEnumerator<T>(coords, out _, offset, facing);
    }

    public bool TryGetTileRefForEnt(EntityCoordinates ent, out Entity<MapGridComponent> grid, out TileRef tile)
    {
        grid = default;
        tile = default;
        if (_transform.GetGrid(ent) is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var gridComp))
        {
            return false;
        }

        var coords = _transform.GetMoverCoordinates(ent);
        grid = (gridId, gridComp);
        if (!_map.TryGetTileRef(gridId, gridComp, coords, out tile))
            return false;

        return true;
    }

    public bool IsTileBlocked(EntityCoordinates coordinates, CollisionGroup group = CollisionGroup.Impassable)
    {
        if (!coordinates.TryGetTileRef(out var turf, EntityManager, _mapManager))
            return false;

        return _turf.IsTileBlocked(turf.Value, group);
    }

    public bool IsTileBlocked(MapCoordinates coordinates, CollisionGroup group = CollisionGroup.Impassable)
    {
        return IsTileBlocked(_transform.ToCoordinates(coordinates), group);
    }

    public bool TileHasStructure(EntityCoordinates coordinates)
    {
        var anchored = GetAnchoredEntitiesEnumerator(coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (_tag.HasTag(uid, StructureTag))
                return true;
        }

        return false;
    }

    public bool TryGetTileDef(EntityCoordinates coordinates, [NotNullWhen(true)] out ContentTileDefinition? def)
    {
        def = default;
        if (_transform.GetGrid(coordinates) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return false;
        }

        var indices = _map.TileIndicesFor(gridId, grid, coordinates);
        if (!_map.TryGetTileDef(grid, indices, out var defUncast))
            return false;

        def = (ContentTileDefinition) defUncast;
        return true;
    }

    public bool TryGetTileDef(MapCoordinates coordinates, [NotNullWhen(true)] out ContentTileDefinition? def)
    {
        return TryGetTileDef(_transform.ToCoordinates(coordinates), out def);
    }

    public bool CanBuildOn(EntityCoordinates coordinates, CollisionGroup group = CollisionGroup.Impassable)
    {
        return !IsTileBlocked(coordinates, group) && !TileHasStructure(coordinates);
    }

    public virtual bool TryLoad(
        MapId mapId,
        string path,
        [NotNullWhen(true)] out IReadOnlyList<EntityUid>? ents,
        Matrix3x2? transform = null)
    {
        ents = null;
        return false;
    }
}
