using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Map;

public sealed class RMCMapSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly INetManager _net = default!;
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
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West
    );

    public override void Initialize()
    {
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<RMCDeleteAnchoredOnInitComponent, MapInitEvent>(OnDestroyAnchoredOnInit);
    }

    private void OnDestroyAnchoredOnInit(Entity<RMCDeleteAnchoredOnInitComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (_net.IsClient)
            return;

        var anchored = GetAnchoredEntitiesEnumerator(ent);
        while (anchored.MoveNext(out var uid))
        {
            if (uid == ent.Owner)
                continue;

            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                continue;

            if (_entityWhitelist.IsWhitelistFailOrNull(ent.Comp.Whitelist, uid))
                continue;

            QueueDel(uid);
        }
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
        if (!_turf.TryGetTileRef(coordinates, out var turf))
            return false;

        return _turf.IsTileBlocked(turf.Value, group);
    }

    public bool IsTileBlocked(MapCoordinates coordinates, CollisionGroup group = CollisionGroup.Impassable)
    {
        return IsTileBlocked(_transform.ToCoordinates(coordinates), group);
    }

    public bool TileHasAnyTag(EntityCoordinates coordinates, params ProtoId<TagPrototype>[] tag)
    {
        var anchored = GetAnchoredEntitiesEnumerator(coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (_tag.HasAnyTag(uid, tag))
                return true;
        }

        return false;
    }

    public bool TileHasStructure(EntityCoordinates coordinates)
    {
        return TileHasAnyTag(coordinates, StructureTag);
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
}
