using Robust.Shared.Map;

namespace Content.Shared._RMC14.Weather;

public readonly record struct RMCWeatherSightObstructionProfile(float ClearDepth, float FullDepth);

public static class RMCWeatherSightObstruction
{
    public const float VisionBlockAlpha = 0.85f;

    public static RMCWeatherSightObstructionProfile GetProfile(RMCWeatherScreenOverlay overlay)
    {
        return overlay switch
        {
            // Based on CMSS13 impairedoverlay1/2/3 on the default 15x15 view.
            RMCWeatherScreenOverlay.Low => new RMCWeatherSightObstructionProfile(6.5f, 9),
            RMCWeatherScreenOverlay.Medium => new RMCWeatherSightObstructionProfile(5.5f, 8),
            RMCWeatherScreenOverlay.High => new RMCWeatherSightObstructionProfile(4.5f, 7),
            _ => new RMCWeatherSightObstructionProfile(float.PositiveInfinity, float.PositiveInfinity),
        };
    }

    public static float GetAlpha(RMCWeatherScreenOverlay overlay, int weatherDepth, float weatherFade)
    {
        if (overlay == RMCWeatherScreenOverlay.None || weatherDepth <= 0 || weatherFade <= 0)
            return 0;

        var profile = GetProfile(overlay);
        return SmoothStepAlpha(weatherDepth, profile) * Math.Clamp(weatherFade, 0, 1);
    }

    public static bool IsBlocked(RMCWeatherScreenOverlay overlay, int weatherDepth, float weatherFade)
    {
        return GetAlpha(overlay, weatherDepth, weatherFade) >= VisionBlockAlpha;
    }

    public static float SmoothStepAlpha(float weatherDepth, RMCWeatherSightObstructionProfile profile)
    {
        if (weatherDepth <= profile.ClearDepth)
            return 0;

        if (weatherDepth >= profile.FullDepth)
            return 1;

        var t = (weatherDepth - profile.ClearDepth) / (profile.FullDepth - profile.ClearDepth);
        return t * t * (3 - 2 * t);
    }

    public static int CalculateWeatherDepth(Vector2i origin, Vector2i target, Func<Vector2i, bool> isExposed)
    {
        if (!isExposed(target))
            return 0;

        var x0 = origin.X;
        var y0 = origin.Y;
        var x1 = target.X;
        var y1 = target.Y;
        var dx = Math.Abs(x1 - x0);
        var dy = -Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;
        var depth = 0;

        while (true)
        {
            var tile = new Vector2i(x0, y0);
            depth = isExposed(tile) ? depth + 1 : 0;

            if (x0 == x1 && y0 == y1)
                return depth;

            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
