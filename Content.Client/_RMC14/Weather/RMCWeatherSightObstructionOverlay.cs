using System.Numerics;
using Content.Client.Weather;
using Content.Shared._RMC14.Admin.AdminGhost;
using Content.Shared._RMC14.Weather;
using Content.Shared.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Weather;

public sealed class RMCWeatherSightObstructionOverlay : Overlay
{
    private static readonly TimeSpan CacheInterval = TimeSpan.FromMilliseconds(125);
    private const float WorldBoundsPadding = 1f;
    private const float MinimumDrawAlpha = 0.01f;
    private const int MaxCachedTiles = 32768;

    private readonly IEntityManager _entity;
    private readonly IPlayerManager _player;
    private readonly WeatherSystem _weather;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly IGameTiming _timing;
    private readonly IClyde _clyde;
    private readonly ShaderInstance _shader;

    private readonly HashSet<Entity<RMCBlockWeatherComponent>> _weatherBlockers = new();
    private readonly List<Box2> _weatherBlockerBounds = new();
    private readonly List<ObstructionTile> _obstructedTiles = new();
    private bool[] _validTiles = Array.Empty<bool>();
    private bool[] _exposedTiles = Array.Empty<bool>();

    private EntityUid? _cachedGrid;
    private CacheKey _cacheKey;
    private TimeSpan _nextCacheRebuild;
    private bool _cacheValid;
    private int _minX;
    private int _minY;
    private int _width;
    private int _height;
    private IRenderTexture? _mask;
    private RMCWeatherOverlayContext _drawContext;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public RMCWeatherSightObstructionOverlay(
        IEntityManager entity,
        IPlayerManager player,
        WeatherSystem weather,
        SharedMapSystem map,
        SharedTransformSystem transform,
        EntityLookupSystem lookup,
        IGameTiming timing,
        IClyde clyde,
        IPrototypeManager prototypes)
    {
        _entity = entity;
        _player = player;
        _weather = weather;
        _map = map;
        _transform = transform;
        _lookup = lookup;
        _timing = timing;
        _clyde = clyde;
        _shader = prototypes.Index<ShaderPrototype>("RMCWeatherObstruction").InstanceUnique();
        ZIndex = 100;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!TryGetContext(args, out var context))
        {
            ClearCache();
            return false;
        }

        if (_cacheValid &&
            _timing.CurTime < _nextCacheRebuild &&
            _cacheKey.Equals(context.Key))
        {
            return _obstructedTiles.Count > 0;
        }

        RebuildCache(args, context);
        _cacheKey = context.Key;
        _drawContext = new RMCWeatherOverlayContext(context.Overlay, context.Style);
        _nextCacheRebuild = _timing.CurTime + CacheInterval;
        _cacheValid = true;
        return _obstructedTiles.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_cachedGrid is not { } gridUid ||
            !_entity.TryGetComponent(gridUid, out TransformComponent? gridXform))
        {
            return;
        }

        var (_, _, worldMatrix, _) = _transform.GetWorldPositionRotationMatrixWithInv(gridXform);
        var handle = args.WorldHandle;
        EnsureMask(args.Viewport.Size);

        handle.RenderInRenderTarget(_mask!, () =>
        {
            handle.SetTransform(worldMatrix);

            foreach (var tile in _obstructedTiles)
            {
                handle.DrawRect(tile.LocalBounds, Color.White.WithAlpha(tile.Alpha));
            }

            handle.SetTransform(Matrix3x2.Identity);
        }, Color.Transparent);

        var style = RMCWeatherOverlayHelpers.GetShaderStyle(_drawContext.Style);
        _shader.SetParameter("primaryColor", style.Primary);
        _shader.SetParameter("secondaryColor", style.Secondary);
        _shader.SetParameter("windDirection", Vector2.Normalize(style.Wind));
        _shader.SetParameter("noiseScale", style.NoiseScale);
        _shader.SetParameter("noiseStrength", style.NoiseStrength);
        _shader.SetParameter("weatherAlpha", 1.0f);

        handle.UseShader(_shader);
        handle.DrawTextureRect(_mask!.Texture, args.WorldBounds);
        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private bool TryGetContext(in OverlayDrawArgs args, out ObstructionContext context)
    {
        context = default;

        if (_player.LocalEntity is not { } player ||
            _entity.HasComponent<GhostComponent>(player) ||
            _entity.HasComponent<RMCAdminGhostComponent>(player) ||
            !_entity.HasComponent<MobStateComponent>(player) ||
            !_entity.TryGetComponent(player, out TransformComponent? playerXform) ||
            !_entity.TryGetComponent(player, out EyeComponent? eye) ||
            args.Viewport.Eye != eye.Eye ||
            playerXform.MapUid is not { } mapUid ||
            playerXform.GridUid is not { } gridUid ||
            playerXform.MapID != args.MapId ||
            !_entity.TryGetComponent(gridUid, out MapGridComponent? grid) ||
            !_map.TryGetTileRef(gridUid, grid, playerXform.Coordinates, out var originTile))
        {
            return false;
        }

        if (!RMCWeatherOverlayHelpers.TryGetCurrentMapOverlay(_entity, playerXform.MapID, out var overlayContext))
            return false;

        var weatherAlpha = RMCWeatherOverlayHelpers.GetWeatherAlpha(_entity, _weather, mapUid);
        if (weatherAlpha <= 0)
            return false;

        var viewportBounds = GetTileBounds(args.WorldAABB);
        var key = new CacheKey(
            playerXform.MapID,
            gridUid,
            originTile.GridIndices,
            overlayContext.Overlay,
            overlayContext.Style,
            args.Viewport.Size,
            args.Viewport.Eye?.Zoom ?? Vector2.One,
            args.Viewport.Eye?.Rotation ?? Angle.Zero,
            viewportBounds);

        context = new ObstructionContext(playerXform.MapID,
            gridUid,
            grid,
            originTile.GridIndices,
            overlayContext.Overlay,
            overlayContext.Style,
            weatherAlpha,
            key);
        return true;
    }

    private void RebuildCache(in OverlayDrawArgs args, ObstructionContext context)
    {
        _obstructedTiles.Clear();
        _cachedGrid = context.GridUid;

        if (!_entity.TryGetComponent(context.GridUid, out TransformComponent? gridXform))
            return;

        var (_, _, worldMatrix, invMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(gridXform);
        var localAabb = invMatrix.TransformBox(args.WorldAABB.Enlarged(WorldBoundsPadding));

        _minX = Math.Min((int) MathF.Floor(localAabb.Left), context.Origin.X) - 1;
        _minY = Math.Min((int) MathF.Floor(localAabb.Bottom), context.Origin.Y) - 1;
        var maxX = Math.Max((int) MathF.Ceiling(localAabb.Right), context.Origin.X) + 1;
        var maxY = Math.Max((int) MathF.Ceiling(localAabb.Top), context.Origin.Y) + 1;
        _width = Math.Max(0, maxX - _minX + 1);
        _height = Math.Max(0, maxY - _minY + 1);

        if (_width <= 0 || _height <= 0 || _width * _height > MaxCachedTiles)
            return;

        EnsureTileArrays(_width * _height);
        Array.Clear(_validTiles, 0, _width * _height);
        Array.Clear(_exposedTiles, 0, _width * _height);

        CacheWeatherBlockers(context.MapId, args.WorldAABB.Enlarged(WorldBoundsPadding));
        _entity.TryGetComponent(context.GridUid, out RoofComponent? roof);

        for (var x = _minX; x <= maxX; x++)
        {
            for (var y = _minY; y <= maxY; y++)
            {
                var indices = new Vector2i(x, y);
                var index = GetIndex(indices);
                if (!_map.TryGetTileRef(context.GridUid, context.Grid, indices, out var tile) ||
                    tile.Tile.IsEmpty)
                {
                    continue;
                }

                _validTiles[index] = true;
                var localCenter = indices + context.Grid.TileSizeHalfVector;
                var mapPosition = Vector2.Transform(localCenter, worldMatrix);
                _exposedTiles[index] = _weather.CanWeatherAffect(context.GridUid, context.Grid, tile, roof) &&
                                       !IsRMCWeatherBlocked(mapPosition);
            }
        }

        var tileSize = context.Grid.TileSize;
        for (var x = _minX; x <= maxX; x++)
        {
            for (var y = _minY; y <= maxY; y++)
            {
                var indices = new Vector2i(x, y);
                if (!IsExposed(indices))
                    continue;

                var depth = RMCWeatherSightObstruction.CalculateWeatherDepth(context.Origin, indices, IsExposed);
                var alpha = RMCWeatherSightObstruction.GetAlpha(context.Overlay, depth, context.WeatherAlpha);
                if (alpha <= MinimumDrawAlpha)
                    continue;

                var localBounds = new Box2(indices * tileSize, (indices + Vector2i.One) * tileSize);
                _obstructedTiles.Add(new ObstructionTile(localBounds, alpha));
            }
        }
    }

    private void CacheWeatherBlockers(MapId mapId, Box2 bounds)
    {
        _weatherBlockers.Clear();
        _weatherBlockerBounds.Clear();
        _lookup.GetEntitiesIntersecting(mapId, bounds, _weatherBlockers, LookupFlags.Uncontained);

        foreach (var blocker in _weatherBlockers)
        {
            var uid = blocker.Owner;
            if (!_entity.TryGetComponent(uid, out TransformComponent? xform))
                continue;

            _weatherBlockerBounds.Add(_lookup.GetAABBNoContainer(uid,
                _transform.GetWorldPosition(xform),
                _transform.GetWorldRotation(xform)));
        }
    }

    private bool IsRMCWeatherBlocked(Vector2 position)
    {
        foreach (var blocker in _weatherBlockerBounds)
        {
            if (blocker.Contains(position))
                return true;
        }

        return false;
    }

    private bool IsExposed(Vector2i indices)
    {
        if (indices.X < _minX ||
            indices.Y < _minY ||
            indices.X >= _minX + _width ||
            indices.Y >= _minY + _height)
        {
            return false;
        }

        var index = GetIndex(indices);
        return _validTiles[index] && _exposedTiles[index];
    }

    private int GetIndex(Vector2i indices)
    {
        return indices.X - _minX + (indices.Y - _minY) * _width;
    }

    private void EnsureTileArrays(int size)
    {
        if (_validTiles.Length < size)
        {
            _validTiles = new bool[size];
            _exposedTiles = new bool[size];
        }
    }

    private void EnsureMask(Vector2i viewportSize)
    {
        if (_mask?.Texture.Size == viewportSize)
            return;

        _mask?.Dispose();
        _mask = _clyde.CreateRenderTarget(viewportSize,
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
            name: "rmc-weather-obstruction-mask");
    }

    private void ClearCache()
    {
        _obstructedTiles.Clear();
        _cachedGrid = null;
        _nextCacheRebuild = TimeSpan.Zero;
        _cacheValid = false;
    }

    private static Box2i GetTileBounds(Box2 worldAabb)
    {
        return new Box2i(
            (int) MathF.Floor(worldAabb.Left),
            (int) MathF.Floor(worldAabb.Bottom),
            (int) MathF.Ceiling(worldAabb.Right),
            (int) MathF.Ceiling(worldAabb.Top));
    }

    private readonly record struct ObstructionTile(Box2 LocalBounds, float Alpha);

    private readonly record struct ObstructionContext(
        MapId MapId,
        EntityUid GridUid,
        MapGridComponent Grid,
        Vector2i Origin,
        RMCWeatherScreenOverlay Overlay,
        RMCWeatherObstructionStyle Style,
        float WeatherAlpha,
        CacheKey Key);

    private readonly record struct CacheKey(
        MapId MapId,
        EntityUid GridUid,
        Vector2i Origin,
        RMCWeatherScreenOverlay Overlay,
        RMCWeatherObstructionStyle Style,
        Vector2i ViewportSize,
        Vector2 Zoom,
        Angle Rotation,
        Box2i WorldTileBounds);
}
