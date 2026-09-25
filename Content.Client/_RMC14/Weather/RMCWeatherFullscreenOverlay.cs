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

namespace Content.Client._RMC14.Weather;

public sealed class RMCWeatherFullscreenOverlay : Overlay
{
    private const float MaxOpacity = 1f;
    private const float FadeInPerSecond = 1.8f;
    private const float FadeOutPerSecond = 2.4f;

    private readonly IEntityManager _entity;
    private readonly IPlayerManager _player;
    private readonly WeatherSystem _weather;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly IGameTiming _timing;
    private readonly ShaderInstance _shader;

    private RMCWeatherScreenOverlay _targetOverlay = RMCWeatherScreenOverlay.None;
    private RMCWeatherScreenOverlay _drawOverlay = RMCWeatherScreenOverlay.None;
    private Vector2 _drawClearHalfSize;
    private Vector2 _drawFullHalfSize;
    private float _drawAlpha;
    private readonly HashSet<Entity<RMCBlockWeatherComponent>> _weatherBlockers = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public RMCWeatherFullscreenOverlay(
        IEntityManager entity,
        IPlayerManager player,
        WeatherSystem weather,
        SharedMapSystem map,
        SharedTransformSystem transform,
        EntityLookupSystem lookup,
        IGameTiming timing,
        IPrototypeManager prototypes)
    {
        _entity = entity;
        _player = player;
        _weather = weather;
        _map = map;
        _transform = transform;
        _lookup = lookup;
        _timing = timing;
        _shader = prototypes.Index<ShaderPrototype>("RMCWeatherPersonalOverlay").InstanceUnique();
        // Draw above night vision and xeno world-info overlays so weather limits their visibility.
        ZIndex = 3;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        // Smooth the local personal overlay in and out as Robust weather alpha changes.
        var targetAlpha = 0f;
        _targetOverlay = RMCWeatherScreenOverlay.None;
        if (TryGetOverlay(args, out var overlay, out var alpha, out var clearHalfSize, out var fullHalfSize))
        {
            _targetOverlay = overlay;
            targetAlpha = alpha;
            _drawOverlay = overlay;
            _drawClearHalfSize = clearHalfSize;
            _drawFullHalfSize = fullHalfSize;
        }

        var frameTime = (float) _timing.FrameTime.TotalSeconds;
        var fadeSpeed = targetAlpha > _drawAlpha ? FadeInPerSecond : FadeOutPerSecond;
        _drawAlpha += Math.Clamp(targetAlpha - _drawAlpha, -fadeSpeed * frameTime, fadeSpeed * frameTime);

        if (_drawAlpha <= 0.005f)
        {
            _drawAlpha = 0f;
            if (_targetOverlay == RMCWeatherScreenOverlay.None)
                _drawOverlay = RMCWeatherScreenOverlay.None;
        }

        return _drawOverlay != RMCWeatherScreenOverlay.None && _drawAlpha > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _shader.SetParameter("clearHalfSize", _drawClearHalfSize);
        _shader.SetParameter("fullHalfSize", _drawFullHalfSize);
        _shader.SetParameter("overlayAlpha", _drawAlpha);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldAABB, Color.White);
        args.WorldHandle.UseShader(null);
    }

    private bool TryGetOverlay(
        OverlayDrawArgs args,
        out RMCWeatherScreenOverlay overlay,
        out float alpha,
        out Vector2 clearHalfSize,
        out Vector2 fullHalfSize)
    {
        // The personal overlay only applies to the local living mob on its own viewport.
        overlay = RMCWeatherScreenOverlay.None;
        alpha = 0f;
        clearHalfSize = default;
        fullHalfSize = default;

        if (_player.LocalEntity is not { } player ||
            _entity.HasComponent<GhostComponent>(player) ||
            _entity.HasComponent<RMCAdminGhostComponent>(player) ||
            !_entity.HasComponent<MobStateComponent>(player) ||
            !_entity.TryGetComponent(player, out TransformComponent? playerXform) ||
            !_entity.TryGetComponent(player, out EyeComponent? eye) ||
            args.Viewport.Eye != eye.Eye ||
            playerXform.MapUid is not { } mapUid ||
            playerXform.GridUid is not { } gridUid ||
            playerXform.MapID != args.MapId)
        {
            return false;
        }

        if (!RMCWeatherOverlayHelpers.TryGetCurrentMapOverlay(_entity, playerXform.MapID, out var overlayContext))
            return false;

        if (!IsExposedToWeather(player, playerXform, gridUid))
            return false;

        overlay = overlayContext.Overlay;
        (clearHalfSize, fullHalfSize) = GetPersonalOverlayRadii(args, overlay);
        alpha = RMCWeatherOverlayHelpers.GetWeatherAlpha(_entity, _weather, mapUid) * MaxOpacity;
        return alpha > 0f &&
            clearHalfSize.X > 0f &&
            clearHalfSize.Y > 0f &&
            fullHalfSize.X > clearHalfSize.X &&
            fullHalfSize.Y > clearHalfSize.Y;
    }

    private bool IsExposedToWeather(EntityUid player, TransformComponent playerXform, EntityUid gridUid)
    {
        // Match server exposure checks: Robust weather tile first, then RMC partial blockers.
        var moverCoords = _transform.GetMoverCoordinates(player, playerXform);

        if (_transform.GetGrid(moverCoords) is not { } moverGridUid ||
            moverGridUid != gridUid ||
            !_entity.TryGetComponent(gridUid, out MapGridComponent? grid) ||
            !_map.TryGetTileRef(gridUid, grid, moverCoords, out var tile))
        {
            return false;
        }

        _entity.TryGetComponent(gridUid, out RoofComponent? roof);
        if (!_weather.CanWeatherAffect(gridUid, grid, tile, roof))
            return false;

        var playerMapPos = _transform.ToMapCoordinates(moverCoords).Position;
        return !IsRMCWeatherBlocked(playerXform.MapID, playerMapPos);
    }

    private bool IsRMCWeatherBlocked(MapId mapId, Vector2 position)
    {
        // Client overlay uses the same sprite-bound blockers as server gameplay effects.
        _weatherBlockers.Clear();
        var radius = RMCWeatherConstants.BlockerLookupRadius;
        var bounds = new Box2(
            position - new Vector2(radius, radius),
            position + new Vector2(radius, radius));
        _lookup.GetEntitiesIntersecting(mapId, bounds, _weatherBlockers, LookupFlags.Uncontained);

        foreach (var blocker in _weatherBlockers)
        {
            var uid = blocker.Owner;
            if (!_entity.TryGetComponent(uid, out TransformComponent? xform))
                continue;

            var blockerBounds = _lookup.GetAABBNoContainer(uid,
                _transform.GetWorldPosition(xform),
                _transform.GetWorldRotation(xform));

            if (blockerBounds.Contains(position))
                return true;
        }

        return false;
    }

    private static (Vector2 ClearHalfSize, Vector2 FullHalfSize) GetPersonalOverlayRadii(
        OverlayDrawArgs args,
        RMCWeatherScreenOverlay overlay)
    {
        // Keep the clean center on the shared 15x15 square reference, while letting the fade cover the real viewport.
        var viewport = args.ViewportBounds;
        return RMCWeatherOverlayHelpers.GetOverlayHalfSizes(overlay, viewport.Width, viewport.Height);
    }
}
