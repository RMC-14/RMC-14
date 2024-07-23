using System.Collections.Immutable;
using Content.Shared.Coordinates;
using Content.Shared.Directions;
using Robust.Shared.Map.Components;

namespace Content.Shared._RMC14.Map;

public sealed class RMCMapSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MapGridComponent> _mapGridQuery;

    public readonly ImmutableArray<Direction> CardinalDirections = ImmutableArray.Create(
        Direction.South,
        Direction.East,
        Direction.South,
        Direction.West
    );

    public override void Initialize()
    {
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
    }

    public RMCAnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(EntityUid ent, Direction? offset = null, DirectionFlag facing = DirectionFlag.None)
    {
        if (_transform.GetGrid(ent) is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var gridComp))
        {
            return RMCAnchoredEntitiesEnumerator.Empty;
        }

        var coords = ent.ToCoordinates();
        if (offset != null)
            coords = coords.Offset(offset.Value);

        var indices = _map.CoordinatesToTile(gridId, gridComp, coords);
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridId, gridComp, indices);
        return new RMCAnchoredEntitiesEnumerator(_transform, anchored, facing);
    }
}
