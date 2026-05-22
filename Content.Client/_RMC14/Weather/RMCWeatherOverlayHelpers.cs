using System.Numerics;
using Content.Client.Weather;
using Content.Shared._RMC14.Weather;
using Content.Shared.Weather;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Weather;

public static class RMCWeatherOverlayHelpers
{
    public static (Vector2 ClearHalfSize, Vector2 FullHalfSize) GetOverlayHalfSizes(
        RMCWeatherScreenOverlay overlay,
        float width,
        float height)
    {
        var referenceHalf = MathF.Min(width, height) / 2f;
        var radii = RMCWeatherScreenOverlayData.GetRadii(overlay);
        var fullscreenTiles = RMCWeatherScreenOverlayData.FullscreenTiles;

        return (new Vector2(
                referenceHalf * radii.ClearX / fullscreenTiles,
                referenceHalf * radii.ClearY / fullscreenTiles),
            new Vector2(
                referenceHalf * radii.FullX / fullscreenTiles,
                referenceHalf * radii.FullY / fullscreenTiles));
    }

    public static bool TryGetCurrentMapOverlay(IEntityManager entity, MapId mapId, out RMCWeatherOverlayContext context)
    {
        context = default;
        var overlay = RMCWeatherScreenOverlay.None;
        // Use the strongest active cycle so drawing, examine range, and click checks agree.
        var query = entity.EntityQueryEnumerator<RMCWeatherCycleComponent, TransformComponent>();
        while (query.MoveNext(out _, out var cycle, out var xform))
        {
            if (xform.MapID != mapId ||
                cycle.State != RMCWeatherCycleState.Running ||
                cycle.CurrentScreenOverlay == RMCWeatherScreenOverlay.None)
            {
                continue;
            }

            if ((byte) cycle.CurrentScreenOverlay <= (byte) overlay)
                continue;

            overlay = cycle.CurrentScreenOverlay;
        }

        if (overlay == RMCWeatherScreenOverlay.None)
            return false;

        context = new RMCWeatherOverlayContext(overlay);
        return true;
    }

    public static RMCWeatherScreenOverlay GetCurrentMapOverlay(IEntityManager entity, MapId mapId)
    {
        return TryGetCurrentMapOverlay(entity, mapId, out var context)
            ? context.Overlay
            : RMCWeatherScreenOverlay.None;
    }

    public static float GetWeatherAlpha(IEntityManager entity, WeatherSystem weatherSystem, EntityUid mapUid)
    {
        if (!entity.TryGetComponent(mapUid, out WeatherComponent? weather))
            return 0f;

        var alpha = 0f;
        foreach (var data in weather.Weather.Values)
        {
            alpha = MathF.Max(alpha, weatherSystem.GetPercent(data, mapUid));
        }

        return Math.Clamp(alpha, 0f, 1f);
    }
}

public readonly record struct RMCWeatherOverlayContext(RMCWeatherScreenOverlay Overlay);
