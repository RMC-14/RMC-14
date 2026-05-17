using System.Numerics;
using Content.Client.Weather;
using Content.Shared._RMC14.Weather;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using RobustVector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client._RMC14.Weather;

public static class RMCWeatherOverlayHelpers
{
    public static bool TryGetCurrentMapOverlay(IEntityManager entity, MapId mapId, out RMCWeatherOverlayContext context)
    {
        context = default;
        var overlay = RMCWeatherScreenOverlay.None;
        var style = RMCWeatherObstructionStyle.Neutral;
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
            style = cycle.CurrentScreenOverlayStyle;
        }

        if (overlay == RMCWeatherScreenOverlay.None)
            return false;

        context = new RMCWeatherOverlayContext(overlay, style);
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

    public static (RobustVector3 Primary, RobustVector3 Secondary, Vector2 Wind, float NoiseScale, float NoiseStrength) GetShaderStyle(
        RMCWeatherObstructionStyle style)
    {
        return style switch
        {
            RMCWeatherObstructionStyle.Rain => (ToVec("#05080A"), ToVec("#151B1F"), new Vector2(0.45f, -1.0f), 36f, 0.30f),
            RMCWeatherObstructionStyle.Dust => (ToVec("#080706"), ToVec("#18140F"), new Vector2(1.0f, -0.18f), 24f, 0.36f),
            RMCWeatherObstructionStyle.Sand => (ToVec("#090806"), ToVec("#1B160E"), new Vector2(1.0f, -0.08f), 30f, 0.40f),
            RMCWeatherObstructionStyle.Snow => (ToVec("#080C0F"), ToVec("#1B252A"), new Vector2(0.3f, -0.65f), 42f, 0.30f),
            _ => (ToVec("#050708"), ToVec("#14191D"), new Vector2(0.4f, -0.4f), 28f, 0.28f),
        };
    }

    private static RobustVector3 ToVec(string hex)
    {
        var color = Color.FromHex(hex);
        return new RobustVector3(color.R, color.G, color.B);
    }
}

public readonly record struct RMCWeatherOverlayContext(
    RMCWeatherScreenOverlay Overlay,
    RMCWeatherObstructionStyle Style);
