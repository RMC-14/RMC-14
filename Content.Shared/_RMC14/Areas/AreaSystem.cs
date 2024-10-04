using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Coordinates;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AreaComponent> _areaQuery;
    private EntityQuery<AreaGridComponent> _areaGridQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<MinimapColorComponent> _minimapColorQuery;

    private readonly List<EntityUid> _toRender = new();

    public override void Initialize()
    {
        _areaQuery = GetEntityQuery<AreaComponent>();
        _areaGridQuery = GetEntityQuery<AreaGridComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _minimapColorQuery = GetEntityQuery<MinimapColorComponent>();

        SubscribeLocalEvent<AreaGridComponent, MapInitEvent>(OnAreaGridMapInit);
    }

    private void OnAreaGridMapInit(Entity<AreaGridComponent> ent, ref MapInitEvent args)
    {
        _toRender.Add(ent);

        var areas = ent.Comp.Areas.Values.DistinctBy(a => a.Id);
        foreach (var area in areas)
        {
            if (ent.Comp.AreaEntities.ContainsKey(area))
            {
                Log.Warning($"Duplicate area {area} found in entity {ToPrettyString(ent)}");
                continue;
            }

            ent.Comp.AreaEntities[area] = Spawn(area, MapCoordinates.Nullspace);
        }
    }

    public bool TryGetArea(EntityCoordinates coordinates, out Entity<AreaComponent> area)
    {
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

        if (!areaGrid.AreaEntities.TryGetValue(areaProtoId, out var areaEnt) ||
            !_areaQuery.TryComp(areaEnt, out var areaComp))
        {
            return false;
        }

        area = (areaEnt, areaComp);
        return true;
    }

    public bool TryGetArea(MapCoordinates coordinates, out Entity<AreaComponent> area)
    {
        return TryGetArea(_transform.ToCoordinates(coordinates), out area);
    }

    public bool TryGetArea(EntityUid coordinates, out Entity<AreaComponent> area)
    {
        return TryGetArea(coordinates.ToCoordinates(), out area);
    }

    public bool TryGetAllAreas(EntityCoordinates coordinates, [NotNullWhen(true)] out Entity<AreaGridComponent>? areaGrid)
    {
        areaGrid = null;
        if (_transform.GetMap(coordinates) is not { } mapId ||
            !_areaGridQuery.TryComp(mapId, out var areaGridComp))
        {
            return false;
        }

        areaGrid = (mapId, areaGridComp);
        return true;
    }

    public bool BioscanBlocked(EntityUid coordinates, out Entity<AreaComponent> area)
    {
        if (!TryGetArea(coordinates, out area))
            return false;

        return area.Comp.AvoidBioscan;
    }

    public bool CanCAS(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanCAS))
            return false;

        return area.Comp.CAS;
    }

    public bool CanMortarFire(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanMortar))
            return false;

        return area.Comp.MortarFire;
    }

    public bool CanMortarPlacement(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanMortar))
            return false;

        return area.Comp.MortarPlacement;
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

    public override void Update(float frameTime)
    {
        try
        {
            foreach (var ent in _toRender)
            {
                if (!TryComp(ent, out AreaGridComponent? areaGrid) ||
                    !TryComp(ent, out MapGridComponent? mapGrid))
                {
                    continue;
                }

                areaGrid.Colors.Clear();

                var tiles = _map.GetAllTilesEnumerator(ent, mapGrid);
                while (tiles.MoveNext(out var tileRefNullable))
                {
                    var tileRef = tileRefNullable.Value;
                    var pos = tileRef.GridIndices;
                    var anchoredEnumerator = _map.GetAnchoredEntitiesEnumerator(ent, mapGrid, pos);

                    var found = false;
                    while (anchoredEnumerator.MoveNext(out var anchored))
                    {
                        if (_minimapColorQuery.TryComp(anchored, out var minimapColor))
                        {
                            areaGrid.Colors[pos] = minimapColor.Color;
                            found = true;
                        }
                    }

                    if (found)
                        continue;

                    var tile = tileRef.GetContentTileDefinition(_tile);
                    if (tile.MinimapColor != default)
                    {
                        areaGrid.Colors[pos] = tile.MinimapColor;
                        continue;
                    }

                    if (areaGrid.Areas.TryGetValue(pos, out var area) &&
                        area.TryGet(out var areaComp, _prototypes, _compFactory) &&
                        areaComp.MinimapColor != default)
                    {
                        areaGrid.Colors[pos] = areaComp.MinimapColor.WithAlpha(0.5f);
                        continue;
                    }

                    areaGrid.Colors[pos] = Color.FromHex("#6c6767d8");
                }

                Dirty(ent, areaGrid);
            }
        }
        finally
        {
            _toRender.Clear();
        }
    }
}
