using System.Numerics;
using Content.Client.Weather;
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
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Weather;

public sealed class RMCWeatherFullscreenOverlay : Overlay
{
    private const float MaxOpacity = 1f;
    private const float FadeInPerSecond = 1.8f;
    private const float FadeOutPerSecond = 2.4f;
    private const float FeatherScale = 0.08f;
    private const int FeatherSteps = 6;

    private readonly IEntityManager _entity;
    private readonly IPlayerManager _player;
    private readonly WeatherSystem _weather;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly IGameTiming _timing;

    private RMCWeatherScreenOverlay _targetOverlay = RMCWeatherScreenOverlay.None;
    private RMCWeatherScreenOverlay _drawOverlay = RMCWeatherScreenOverlay.None;
    private float _drawAlpha;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public RMCWeatherFullscreenOverlay(
        IEntityManager entity,
        IPlayerManager player,
        WeatherSystem weather,
        SharedMapSystem map,
        SharedTransformSystem transform,
        EntityLookupSystem lookup,
        IGameTiming timing)
    {
        _entity = entity;
        _player = player;
        _weather = weather;
        _map = map;
        _transform = transform;
        _lookup = lookup;
        _timing = timing;
        ZIndex = -1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var targetAlpha = 0f;
        _targetOverlay = RMCWeatherScreenOverlay.None;
        if (TryGetOverlay(args, out var overlay, out var alpha))
        {
            _targetOverlay = overlay;
            targetAlpha = alpha;
            _drawOverlay = overlay;
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
        var clearScale = GetClearScale(_drawOverlay);
        var outerScale = Math.Min(1f, clearScale + FeatherScale);
        var clear = ScaleBox(bounds, clearScale);
        var featherOuter = ScaleBox(bounds, outerScale);

        DrawRing(args.ScreenHandle, bounds, featherOuter, Color.Black.WithAlpha(_drawAlpha));

        for (var i = FeatherSteps; i > 0; i--)
        {
            var bandOuterScale = clearScale + FeatherScale * i / FeatherSteps;
            var bandInnerScale = clearScale + FeatherScale * (i - 1) / FeatherSteps;
            bandOuterScale = Math.Min(1f, bandOuterScale);
            bandInnerScale = Math.Min(bandOuterScale, bandInnerScale);

            var bandOuter = ScaleBox(bounds, bandOuterScale);
            var bandInner = i == 1 ? clear : ScaleBox(bounds, bandInnerScale);
            var alpha = _drawAlpha * MathF.Pow(i / (float) FeatherSteps, 1.5f);
            DrawRing(args.ScreenHandle, bandOuter, bandInner, Color.Black.WithAlpha(alpha));
        }
    }

    private bool TryGetOverlay(
        OverlayDrawArgs args,
        out RMCWeatherScreenOverlay overlay,
        out float alpha)
    {
        overlay = RMCWeatherScreenOverlay.None;
        alpha = 0f;

        if (_player.LocalEntity is not { } player ||
            _entity.HasComponent<GhostComponent>(player) ||
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

        overlay = GetCurrentMapOverlay(playerXform.MapID);
        if (overlay == RMCWeatherScreenOverlay.None)
            return false;

        if (!IsExposedToWeather(player, playerXform, gridUid))
            return false;

        alpha = GetWeatherAlpha(mapUid) * MaxOpacity;
        return alpha > 0f;
    }

    private RMCWeatherScreenOverlay GetCurrentMapOverlay(MapId mapId)
    {
        var overlay = RMCWeatherScreenOverlay.None;
        var query = _entity.EntityQueryEnumerator<RMCWeatherCycleComponent, TransformComponent>();
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

        return overlay;
    }

    private float GetWeatherAlpha(EntityUid mapUid)
    {
        if (!_entity.TryGetComponent(mapUid, out WeatherComponent? weather))
            return 0f;

        var alpha = 0f;
        foreach (var data in weather.Weather.Values)
        {
            alpha = MathF.Max(alpha, _weather.GetPercent(data, mapUid));
        }

        return Math.Clamp(alpha, 0f, 1f);
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
        var query = _entity.EntityQueryEnumerator<RMCBlockWeatherComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var bounds = _lookup.GetAABBNoContainer(uid,
                _transform.GetWorldPosition(uid),
                _transform.GetWorldRotation(uid));

            if (bounds.Contains(position))
                return true;
        }

        return false;
    }

    private static float GetClearScale(RMCWeatherScreenOverlay overlay)
    {
        return overlay switch
        {
            RMCWeatherScreenOverlay.Low => 0.8f,
            RMCWeatherScreenOverlay.Medium => 0.62f,
            RMCWeatherScreenOverlay.High => 0.45f,
            _ => 1f,
        };
    }

    private static UIBox2 ScaleBox(UIBox2 bounds, float scale)
    {
        return bounds.Scale(scale);
    }

    private static void DrawRing(DrawingHandleScreen handle, UIBox2 outer, UIBox2 inner, Color color)
    {
        if (inner.Top > outer.Top)
            handle.DrawRect(new UIBox2(outer.Left, outer.Top, outer.Right, inner.Top), color);

        if (outer.Bottom > inner.Bottom)
            handle.DrawRect(new UIBox2(outer.Left, inner.Bottom, outer.Right, outer.Bottom), color);

        if (inner.Left > outer.Left && inner.Bottom > inner.Top)
            handle.DrawRect(new UIBox2(outer.Left, inner.Top, inner.Left, inner.Bottom), color);

        if (outer.Right > inner.Right && inner.Bottom > inner.Top)
            handle.DrawRect(new UIBox2(inner.Right, inner.Top, outer.Right, inner.Bottom), color);
    }
}
