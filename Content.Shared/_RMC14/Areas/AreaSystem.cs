using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Map;
using Content.Shared.Coordinates;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private EntityQuery<AreaGridComponent> _areaGridQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _areaGridQuery = GetEntityQuery<AreaGridComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<AreaGridComponent, MapInitEvent>(OnAreaGridMapInit);
    }

    private void OnAreaGridMapInit(Entity<AreaGridComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Colors.Clear();

        var done = new HashSet<Vector2>();
        foreach (var kvp in ent.Comp.Areas)
        {
            var (indices, areaProto) = kvp;
            if (!done.Add(indices))
                continue;

            if (!areaProto.TryGet(out var area, _prototypes, _compFactory) ||
                area.MinimapColor == default)
            {
                continue;
            }

            var xAdjacent = 0;
            var x = indices.X;
            while (ent.Comp.Areas.GetValueOrDefault((++x, indices.Y)) == areaProto)
            {
                xAdjacent++;
                done.Add(new Vector2(x, indices.Y));
            }

            var xAdjacentMinus = 0;
            x = indices.X;
            while (ent.Comp.Areas.GetValueOrDefault((--x, indices.Y)) == areaProto)
            {
                xAdjacentMinus++;
                done.Add(new Vector2(x, indices.Y));
            }

            var bottom = new Vector2(indices.X - xAdjacentMinus, indices.Y);
            var right = new Vector2(indices.X + 1 + xAdjacent, indices.Y);
            var top = new Vector2(indices.X - xAdjacentMinus, indices.Y + 1);
            var left = new Vector2(indices.X + 1 + xAdjacent, indices.Y + 1);
            ent.Comp.Colors.GetOrNew(Color.FromSrgb(area.MinimapColor)).AddRange([bottom, right, top, left]);
        }

        Dirty(ent);
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

        if (IsRoofed(coordinates, r => !r.Comp.CanCAS))
            return false;

        return area.CAS;
    }

    public bool CanMortarFire(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out _, out var area))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanMortar))
            return false;

        return area.MortarFire;
    }

    public bool CanMortarPlacement(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out _, out var area))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanMortar))
            return false;

        return area.MortarPlacement;
    }

    private bool IsRoofed(EntityCoordinates coordinates, Predicate<Entity<RoofingEntityComponent>> predicate)
    {
        var roofs = EntityQueryEnumerator<RoofingEntityComponent>();
        while (roofs.MoveNext(out var uid, out var roof))
        {
            if (!predicate((uid, roof)))
                continue;

            if (coordinates.TryDistance(EntityManager, uid.ToCoordinates(), out var distance) &&
                distance <= roof.Range)
            {
                return true;
            }
        }

        return false;
    }
}
