using System.Numerics;
using Content.Shared._RMC14.Weather;
using Content.Shared.Light.Components;
using Content.Shared.Weather;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Client.Overlays;

public sealed partial class StencilOverlay
{
    private List<Entity<MapGridComponent>> _grids = new();
    private readonly HashSet<Entity<RMCBlockWeatherComponent>> _rmcWeatherBlockers = new();

    private void DrawWeather(in OverlayDrawArgs args, WeatherPrototype weatherProto, float alpha, Matrix3x2 invMatrix)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        worldHandle.RenderInRenderTarget(_blep!, () =>
        {
            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
            _grids.Clear();

            // idk if this is safe to cache in a field and clear sloth help
            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref _grids);

            foreach (var grid in _grids)
            {
                var matrix = _transform.GetWorldMatrix(grid, xformQuery);
                var matty =  Matrix3x2.Multiply(matrix, invMatrix);
                worldHandle.SetTransform(matty);
                _entManager.TryGetComponent(grid.Owner, out RoofComponent? roofComp);

                foreach (var tile in _map.GetTilesIntersecting(grid.Owner, grid, worldAABB))
                {
                    // Ignored tiles for stencil
                    if (_weather.CanWeatherAffect(grid.Owner, grid, tile, roofComp))
                    {
                        continue;
                    }

                    var gridTile = new Box2(tile.GridIndices * grid.Comp.TileSize,
                        (tile.GridIndices + Vector2i.One) * grid.Comp.TileSize);

                    worldHandle.DrawRect(gridTile, Color.White);
                }
            }

            // RMC14 start - weather partial blockers.
            if (_playerManager.LocalEntity is { } player &&
                _entManager.TryGetComponent(player, out TransformComponent? playerXform) &&
                playerXform.MapID == mapId)
            {
                var playerPos = _transform.GetMapCoordinates(player, playerXform).Position;

                _rmcWeatherBlockers.Clear();
                _entLookup.GetEntitiesIntersecting(mapId, worldAABB, _rmcWeatherBlockers, LookupFlags.Uncontained);
                worldHandle.SetTransform(invMatrix);

                foreach (var blocker in _rmcWeatherBlockers)
                {
                    var uid = blocker.Owner;
                    if (!_entManager.TryGetComponent(uid, out TransformComponent? xform) ||
                        xform.MapID != mapId)
                    {
                        continue;
                    }

                    var roofBounds = _entLookup.GetAABBNoContainer(uid,
                        _transform.GetWorldPosition(xform),
                        _transform.GetWorldRotation(xform));

                    if (roofBounds.Contains(playerPos))
                        worldHandle.DrawRect(roofBounds, Color.White);
                }
            }
            // RMC14 end

        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
        worldHandle.DrawTextureRect(_blep!.Texture, worldBounds);
        var curTime = _timing.RealTime;
        var sprite = _sprite.GetFrame(weatherProto.Sprite, curTime);

        // Draw the rain
        worldHandle.UseShader(_protoManager.Index(StencilDraw).Instance());
        _parallax.DrawParallax(worldHandle, worldAABB, sprite, curTime, position, Vector2.Zero, modulate: (weatherProto.Color ?? Color.White).WithAlpha(alpha));

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }
}
