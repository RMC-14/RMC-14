using System.Numerics;
using Content.Shared._RMC14.Admin.AdminGhost;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weather;

public readonly record struct RMCWeatherVisionTarget(MapId MapId, EntityUid GridUid, Vector2i Tile);

public sealed class RMCWeatherVisionSystem : EntitySystem
{
    private const float WeatherBlockerLookupRadius = 0.05f;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly HashSet<Entity<RMCBlockWeatherComponent>> _weatherBlockers = new();
    private readonly List<Box2> _weatherBlockerBounds = new();

    public bool HasActiveWeatherVision(EntityUid viewer)
    {
        return TryGetViewerContext(viewer, out _, out _, out _, out _, out _, out _, out _);
    }

    public bool IsWeatherVisionBlocked(EntityUid viewer, EntityUid target)
    {
        return GetWeatherVisionAlpha(viewer, target) >= RMCWeatherSightObstruction.VisionBlockAlpha;
    }

    public bool IsWeatherVisionBlocked(EntityUid viewer, EntityCoordinates target)
    {
        return GetWeatherVisionAlpha(viewer, target) >= RMCWeatherSightObstruction.VisionBlockAlpha;
    }

    public bool IsWeatherVisionBlocked(EntityUid viewer, MapCoordinates target)
    {
        return GetWeatherVisionAlpha(viewer, target) >= RMCWeatherSightObstruction.VisionBlockAlpha;
    }

    public float GetWeatherVisionAlpha(EntityUid viewer, EntityUid target)
    {
        if (!TryComp(target, out TransformComponent? targetXform) ||
            targetXform.GridUid == null ||
            targetXform.MapUid == null)
        {
            return 0;
        }

        return GetWeatherVisionAlpha(viewer, _transform.GetMapCoordinates(target, targetXform));
    }

    public float GetWeatherVisionAlpha(EntityUid viewer, EntityCoordinates target)
    {
        return GetWeatherVisionAlpha(viewer, _transform.ToMapCoordinates(target));
    }

    public float GetWeatherVisionAlpha(EntityUid viewer, MapCoordinates target)
    {
        if (!TryGetVisionDepth(viewer, target, out var overlay, out var depth, out var weatherFade))
            return 0;

        return RMCWeatherSightObstruction.GetAlpha(overlay, depth, weatherFade);
    }

    public bool TryGetEntityTarget(EntityUid target, out RMCWeatherVisionTarget targetTile)
    {
        targetTile = default;
        if (!TryComp(target, out TransformComponent? xform) ||
            xform.GridUid is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid) ||
            !_map.TryGetTileRef(gridUid, grid, xform.Coordinates, out var tile))
        {
            return false;
        }

        targetTile = new RMCWeatherVisionTarget(xform.MapID, gridUid, tile.GridIndices);
        return true;
    }

    public bool TryGetVisionDepth(
        EntityUid viewer,
        MapCoordinates target,
        out RMCWeatherScreenOverlay overlay,
        out int depth,
        out float weatherFade)
    {
        overlay = RMCWeatherScreenOverlay.None;
        depth = 0;
        weatherFade = 0;

        if (!TryGetViewerContext(viewer,
                out var viewerXform,
                out var gridUid,
                out var grid,
                out var origin,
                out overlay,
                out weatherFade,
                out var mapUid))
        {
            return false;
        }

        if (target.MapId != viewerXform.MapID ||
            !_map.TryGetTileRef(gridUid, grid, target.Position, out var targetTile) ||
            targetTile.Tile.IsEmpty)
        {
            return false;
        }

        var mapId = viewerXform.MapID;
        TryComp(gridUid, out RoofComponent? roof);
        var blockerBounds = GetLineMapBounds(gridUid, grid, origin, targetTile.GridIndices);
        CacheWeatherBlockers(mapId, blockerBounds);

        bool IsExposed(Vector2i tile)
        {
            if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) ||
                tileRef.Tile.IsEmpty)
            {
                return false;
            }

            if (!_weather.CanWeatherAffect(gridUid, grid, tileRef, roof))
                return false;

            var localCenter = tile + grid.TileSizeHalfVector;
            var mapPosition = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, localCenter)).Position;
            return !IsRMCWeatherBlocked(mapPosition);
        }

        depth = RMCWeatherSightObstruction.CalculateWeatherDepth(origin, targetTile.GridIndices, IsExposed);
        return depth > 0;
    }

    public void PopupBlocked(EntityUid viewer)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _popup.PopupClient(Loc.GetString("rmc-weather-vision-blocked"), viewer, viewer, PopupType.SmallCaution);
    }

    private bool TryGetViewerContext(
        EntityUid viewer,
        out TransformComponent viewerXform,
        out EntityUid gridUid,
        out MapGridComponent grid,
        out Vector2i origin,
        out RMCWeatherScreenOverlay overlay,
        out float weatherFade,
        out EntityUid mapUid)
    {
        viewerXform = default!;
        gridUid = default;
        grid = default!;
        origin = default;
        overlay = RMCWeatherScreenOverlay.None;
        weatherFade = 0;
        mapUid = default;

        if (HasComp<GhostComponent>(viewer) ||
            HasComp<RMCAdminGhostComponent>(viewer) ||
            !HasComp<MobStateComponent>(viewer) ||
            !TryComp(viewer, out TransformComponent? xform) ||
            xform.GridUid is not { } viewerGrid ||
            xform.MapUid is not { } viewerMap ||
            !TryComp(viewerGrid, out MapGridComponent? gridComp) ||
            !_map.TryGetTileRef(viewerGrid, gridComp, xform.Coordinates, out var originTile))
        {
            return false;
        }

        viewerXform = xform;
        grid = gridComp;
        if (!TryGetActiveMapWeather(viewerXform.MapID, out overlay))
            return false;

        weatherFade = GetWeatherFade(viewerMap);
        if (weatherFade <= 0)
            return false;

        gridUid = viewerGrid;
        mapUid = viewerMap;
        origin = originTile.GridIndices;
        return true;
    }

    private bool TryGetActiveMapWeather(MapId mapId, out RMCWeatherScreenOverlay overlay)
    {
        overlay = RMCWeatherScreenOverlay.None;
        var query = EntityQueryEnumerator<RMCWeatherCycleComponent, TransformComponent>();
        while (query.MoveNext(out _, out var cycle, out var xform))
        {
            if (xform.MapID != mapId ||
                cycle.State != RMCWeatherCycleState.Running ||
                cycle.CurrentScreenOverlay == RMCWeatherScreenOverlay.None)
            {
                continue;
            }

            if ((byte) cycle.CurrentScreenOverlay > (byte) overlay)
                overlay = cycle.CurrentScreenOverlay;
        }

        return overlay != RMCWeatherScreenOverlay.None;
    }

    private float GetWeatherFade(EntityUid mapUid)
    {
        if (!TryComp(mapUid, out WeatherComponent? weather))
            return 0;

        var alpha = 0f;
        foreach (var data in weather.Weather.Values)
        {
            alpha = MathF.Max(alpha, _weather.GetPercent(data, mapUid));
        }

        return Math.Clamp(alpha, 0f, 1f);
    }

    private Box2 GetLineMapBounds(EntityUid gridUid, MapGridComponent grid, Vector2i origin, Vector2i target)
    {
        var originCenter = origin + grid.TileSizeHalfVector;
        var targetCenter = target + grid.TileSizeHalfVector;
        var originMap = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, originCenter)).Position;
        var targetMap = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, targetCenter)).Position;
        var padding = new Vector2(WeatherBlockerLookupRadius, WeatherBlockerLookupRadius);

        return new Box2(Vector2.Min(originMap, targetMap) - padding, Vector2.Max(originMap, targetMap) + padding);
    }

    private void CacheWeatherBlockers(MapId mapId, Box2 bounds)
    {
        _weatherBlockers.Clear();
        _weatherBlockerBounds.Clear();
        _lookup.GetEntitiesIntersecting(mapId, bounds, _weatherBlockers, LookupFlags.Uncontained);

        foreach (var blocker in _weatherBlockers)
        {
            var uid = blocker.Owner;
            if (!TryComp(uid, out TransformComponent? xform))
                continue;

            _weatherBlockerBounds.Add(_lookup.GetAABBNoContainer(uid,
                _transform.GetWorldPosition(xform),
                _transform.GetWorldRotation(xform)));
        }
    }

    private bool IsRMCWeatherBlocked(Vector2 position)
    {
        foreach (var blockerBounds in _weatherBlockerBounds)
        {
            if (blockerBounds.Contains(position))
                return true;
        }

        return false;
    }
}
