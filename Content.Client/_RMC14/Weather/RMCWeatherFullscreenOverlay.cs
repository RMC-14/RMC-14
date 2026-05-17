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
    private const float WeatherBlockerLookupRadius = 0.05f;

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
    private float _drawClearRadius;
    private float _drawFullRadius;
    private float _drawAlpha;
    private RMCWeatherObstructionStyle _drawStyle = RMCWeatherObstructionStyle.Neutral;
    private readonly HashSet<Entity<RMCBlockWeatherComponent>> _weatherBlockers = new();

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

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
        ZIndex = -1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var targetAlpha = 0f;
        _targetOverlay = RMCWeatherScreenOverlay.None;
        if (TryGetOverlay(args, out var overlay, out var style, out var alpha, out var clearRadius, out var fullRadius))
        {
            _targetOverlay = overlay;
            targetAlpha = alpha;
            _drawOverlay = overlay;
            _drawStyle = style;
            _drawClearRadius = clearRadius;
            _drawFullRadius = fullRadius;
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
        var viewport = args.ViewportBounds;
        var bounds = new UIBox2(viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
        var style = RMCWeatherOverlayHelpers.GetShaderStyle(_drawStyle);
        _shader.SetParameter("primaryColor", style.Primary);
        _shader.SetParameter("secondaryColor", style.Secondary);
        _shader.SetParameter("windDirection", Vector2.Normalize(style.Wind));
        _shader.SetParameter("noiseScale", style.NoiseScale);
        _shader.SetParameter("noiseStrength", style.NoiseStrength);
        _shader.SetParameter("clearRadius", _drawClearRadius);
        _shader.SetParameter("fullRadius", _drawFullRadius);
        _shader.SetParameter("overlayAlpha", _drawAlpha);

        args.ScreenHandle.UseShader(_shader);
        args.ScreenHandle.DrawRect(bounds, Color.White);
        args.ScreenHandle.UseShader(null);
    }

    private bool TryGetOverlay(
        OverlayDrawArgs args,
        out RMCWeatherScreenOverlay overlay,
        out RMCWeatherObstructionStyle style,
        out float alpha,
        out float clearRadius,
        out float fullRadius)
    {
        overlay = RMCWeatherScreenOverlay.None;
        style = RMCWeatherObstructionStyle.Neutral;
        alpha = 0f;
        clearRadius = 0f;
        fullRadius = 0f;

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
        style = overlayContext.Style;
        var profile = RMCWeatherSightObstruction.GetProfile(overlay);
        clearRadius = TilesToPixels(args, profile.ClearDepth);
        fullRadius = TilesToPixels(args, profile.FullDepth);
        alpha = RMCWeatherOverlayHelpers.GetWeatherAlpha(_entity, _weather, mapUid) * MaxOpacity;
        return alpha > 0f && clearRadius > 0f && fullRadius > clearRadius;
    }

    private bool IsExposedToWeather(EntityUid player, TransformComponent playerXform, EntityUid gridUid)
    {
        if (!_entity.TryGetComponent(gridUid, out MapGridComponent? grid) ||
            !_map.TryGetTileRef(gridUid, grid, playerXform.Coordinates, out var tile))
        {
            return false;
        }

        _entity.TryGetComponent(gridUid, out RoofComponent? roof);
        if (!_weather.CanWeatherAffect(gridUid, grid, tile, roof))
            return false;

        var playerMapPos = _transform.GetMapCoordinates(player, playerXform).Position;
        return !IsRMCWeatherBlocked(playerXform.MapID, playerMapPos);
    }

    private bool IsRMCWeatherBlocked(MapId mapId, Vector2 position)
    {
        _weatherBlockers.Clear();
        var bounds = new Box2(
            position - new Vector2(WeatherBlockerLookupRadius, WeatherBlockerLookupRadius),
            position + new Vector2(WeatherBlockerLookupRadius, WeatherBlockerLookupRadius));
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

    private static float TilesToPixels(OverlayDrawArgs args, float tiles)
    {
        var zoom = Math.Max(args.Viewport.Eye?.Zoom.X ?? 1f, 0.01f);
        return tiles * args.Viewport.RenderScale.X / zoom * EyeManager.PixelsPerMeter;
    }

}
