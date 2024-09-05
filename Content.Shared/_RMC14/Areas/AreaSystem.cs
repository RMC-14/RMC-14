using System.Diagnostics.CodeAnalysis;
using Content.Shared.Coordinates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AreaGridComponent> _areaGridQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _areaGridQuery = GetEntityQuery<AreaGridComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
    }

    public bool TryGetArea(
        EntityCoordinates coordinates,
        [NotNullWhen(true)] out EntityPrototype? areaProto,
        [NotNullWhen(true)] out AreaComponent? area)
    {
        areaProto = default;
        area = default;
        if (_transform.GetGrid(coordinates) is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var grid) ||
            !_areaGridQuery.TryComp(gridId, out var areaGrid))
        {
            return false;
        }

        var indices = _map.CoordinatesToTile(gridId, grid, coordinates);
        if (!areaGrid.Areas.TryGetValue(indices, out var areaProtoId))
            return false;

        if (!_prototypes.TryIndex(areaProtoId, out areaProto))
            return false;

        if (!areaProto.TryGetComponent(out area, _compFactory))
            return false;

        return true;
    }

    public bool TryGetArea(
        MapCoordinates coordinates,
        [NotNullWhen(true)] out EntityPrototype? areaProto,
        [NotNullWhen(true)] out AreaComponent? area)
    {
        return TryGetArea(_transform.ToCoordinates(coordinates), out areaProto, out area);
    }

    public bool TryGetArea(
        EntityUid coordinates,
        [NotNullWhen(true)] out EntityPrototype? areaProto,
        [NotNullWhen(true)] out AreaComponent? area)
    {
        return TryGetArea(coordinates.ToCoordinates(), out areaProto, out area);
    }

    public bool BioscanBlocked(EntityUid coordinates, out EntityPrototype? areaProto, out AreaComponent? area)
    {
        if (!TryGetArea(coordinates, out areaProto, out area))
            return false;

        return area.AvoidBioscan;
    }

    public bool CanCAS(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out _, out var area))
            return false;

        return area.CAS;
    }
}
