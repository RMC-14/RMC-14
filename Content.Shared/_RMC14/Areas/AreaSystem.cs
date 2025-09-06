using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.GameStates;
using Content.Shared._RMC14.Warps;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedRMCPvsSystem _rmcPvs = default!;
    [Dependency] private readonly SharedRMCWarpSystem _rmcWarp = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private EntityQuery<AreaGridComponent> _areaGridQuery;
    private EntityQuery<AreaLabelComponent> _areaLabelQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<MinimapColorComponent> _minimapColorQuery;

    private readonly List<EntityUid> _toRender = new();

    private TimeSpan _earlySpreadHiveTime;

    public override void Initialize()
    {
        _areaGridQuery = GetEntityQuery<AreaGridComponent>();
        _areaLabelQuery = GetEntityQuery<AreaLabelComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _minimapColorQuery = GetEntityQuery<MinimapColorComponent>();

        SubscribeLocalEvent<AreaGridComponent, MapInitEvent>(OnAreaGridMapInit);

        Subs.CVar(_config, RMCCVars.RMCHiveSpreadEarlyMinutes, v => _earlySpreadHiveTime = TimeSpan.FromMinutes(v), true);
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

            var areaEnt = Spawn(area, MapCoordinates.Nullspace);
            ent.Comp.AreaEntities[area] = areaEnt;
            _rmcPvs.AddGlobalOverride(areaEnt);
        }
    }

    public void ReplaceArea(AreaGridComponent areaGrid, Vector2i position, EntProtoId<AreaComponent> area)
    {
        areaGrid.Areas[position] = area;
    }

    public bool TryGetArea(
        Entity<MapGridComponent, AreaGridComponent?> grid,
        Vector2i indices,
        [NotNullWhen(true)] out Entity<AreaComponent>? area,
        [NotNullWhen(true)] out EntityPrototype? areaPrototype)
    {
        area = default;
        areaPrototype = default;
        if (!Resolve(grid, ref grid.Comp2, false))
            return false;

        if (!grid.Comp2.Areas.TryGetValue(indices, out var areaProtoId))
            return false;

        if (!_prototypes.TryIndex(areaProtoId, out areaPrototype))
            return false;

        if (!grid.Comp2.AreaEntities.TryGetValue(areaProtoId, out var areaEnt) ||
            !TryComp(areaEnt, out AreaComponent? areaComp))
        {
            return false;
        }

        area = (areaEnt, areaComp);
        return true;
    }

    public bool TryGetArea(
        EntityCoordinates coordinates,
        [NotNullWhen(true)] out Entity<AreaComponent>? area,
        [NotNullWhen(true)] out EntityPrototype? areaPrototype)
    {
        area = default;
        areaPrototype = default;
        if (_transform.GetGrid(coordinates) is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var grid) ||
            !_areaGridQuery.TryComp(gridId, out var areaGrid))
        {
            return false;
        }

        var indices = _map.CoordinatesToTile(gridId, grid, coordinates);
        return TryGetArea((gridId, grid, areaGrid), indices, out area, out areaPrototype);
    }

    public bool TryGetArea(
        MapCoordinates coordinates,
        [NotNullWhen(true)] out Entity<AreaComponent>? area,
        [NotNullWhen(true)] out EntityPrototype? areaPrototype)
    {
        return TryGetArea(_transform.ToCoordinates(coordinates), out area, out areaPrototype);
    }

    public bool TryGetArea(
        EntityUid coordinates,
        [NotNullWhen(true)] out Entity<AreaComponent>? area,
        [NotNullWhen(true)] out EntityPrototype? areaPrototype)
    {
        return TryGetArea(coordinates.ToCoordinates(), out area, out areaPrototype);
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

    public bool BioscanBlocked(EntityUid coordinates, out string? name)
    {
        name = default;
        if (!TryGetArea(coordinates, out var area, out var areaProto))
            return false;

        name = areaProto.Name;
        return area.Value.Comp.AvoidBioscan;
    }

    public bool IsWeatherEnabled(Entity<MapGridComponent> grid, Vector2i indices)
    {
        if (!TryGetArea(grid, indices, out var area, out _))
            return false;

        if (IsRoofed(new EntityCoordinates(grid.Owner, indices), r => !r.Comp.CanMortarPlace))
            return false;

        return area.Value.Comp.WeatherEnabled;
    }

    public bool IsLightBlocked(Entity<MapGridComponent> grid, Vector2i indices)
    {
        if (!TryGetArea(grid, indices, out var area, out _))
            return false;

        if (IsRoofed(new EntityCoordinates(grid.Owner, indices), r => !r.Comp.CanMortarPlace))
            return true;

        return !area.Value.Comp.WeatherEnabled;
    }

    public bool CanCAS(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanCAS))
            return false;

        return area.Value.Comp.CAS;
    }

    public bool CanMortarFire(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanMortarFire))
            return false;

        return area.Value.Comp.MortarFire;
    }

    public bool CanMortarPlacement(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanMortarPlace))
            return false;

        return area.Value.Comp.MortarPlacement;
    }

    public bool CanOrbitalBombard(EntityCoordinates coordinates, out bool roofed)
    {
        roofed = false;
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanOrbitalBombard))
        {
            roofed = true;
            return false;
        }

        return area.Value.Comp.OB;
    }

    public bool CanFulton(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanFulton))
            return false;

        return area.Value.Comp.Fulton;
    }

    public bool CanLase(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanLase))
            return false;

        return area.Value.Comp.Lasing;
    }

    public bool CanMedevac(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanMedevac))
            return false;

        return area.Value.Comp.Medevac;
    }

    public bool CanParadrop(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area, out _))
            return false;

        if (IsRoofed(coordinates, r => !r.Comp.CanParadrop))
            return false;

        return area.Value.Comp.Paradropping;
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

    private bool IsRoofed(MapCoordinates mapCoordinates, Predicate<Entity<RoofingEntityComponent>> predicate)
    {
        var roofs = EntityQueryEnumerator<RoofingEntityComponent>();
        while (roofs.MoveNext(out var uid, out var roof))
        {
            if (!predicate((uid, roof)))
                continue;

            var distance = (mapCoordinates.Position - _transform.ToMapCoordinates(uid.ToCoordinates()).Position).Length();

            if (distance <= roof.Range)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanResinPopup(Entity<MapGridComponent, AreaGridComponent?> grid, Vector2i indices, EntityUid? user)
    {
        if (!TryGetArea(grid, indices, out var area, out _))
            return true;

        if (area.Value.Comp.WeedKilling)
        {
            if (user != null)
                _popup.PopupClient("This area is unsuited to host the hive!", user.Value, user.Value, PopupType.MediumCaution);

            return false;
        }

        if (area.Value.Comp.ResinAllowed)
            return true;

        var roundDuration = _gameTicker.RoundDuration();
        if (roundDuration > _earlySpreadHiveTime)
            return true;

        if (user != null)
            _popup.PopupClient("It's too early to spread the hive this far.", user.Value, user.Value, PopupType.MediumCaution);

        return false;
    }

    public bool CanSupplyDrop(MapCoordinates mapCoordinates)
    {
        if (!TryGetArea(mapCoordinates, out var area, out _))
            return false;

        if (IsRoofed(mapCoordinates, r => !r.Comp.CanSupplyDrop))
            return false;

        return area.Value.Comp.SupplyDrop;
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

                        if (_areaLabelQuery.HasComp(anchored))
                            areaGrid.Labels[pos] = _rmcWarp.GetName(anchored.Value) ?? Name(anchored.Value);
                    }

                    if (found)
                        continue;

                    var tile = _turf.GetContentTileDefinition(tileRef);
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
