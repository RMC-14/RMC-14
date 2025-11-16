using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared._NC14.DayNight;

public sealed class NCDayNightTimeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedGameTicker _ticker = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public bool TryGetTime(MapId mapId, out NCDayTimeInfo info)
    {
        info = default;

        if (!TryGetMap(mapId, out var mapUid))
            return false;

        if (!HasComp<NCDayNightCycleComponent>(mapUid))
            return false;

        if (!TryComp(mapUid, out LightCycleComponent? lightCycle))
            return false;

        var duration = Math.Max(1.0, lightCycle.Duration.TotalSeconds);

        var pausedTime = _metaData.GetPauseTime(mapUid);
        var timeSeconds = (_timing.CurTime + lightCycle.Offset - _ticker.RoundStartTimeSpan - pausedTime).TotalSeconds;
        var cyclePos = Mod(timeSeconds, duration);
        var normalized = cyclePos / duration;

        var daySeconds = normalized * SecondsPerFullDay();
        var hour = (int)(daySeconds / 3600.0) % 24;
        var minute = (int)((daySeconds % 3600.0) / 60.0);

        var totalDays = (long)Math.Floor(timeSeconds / duration);

        info = new NCDayTimeInfo(hour, minute, normalized, GetPhase(hour, minute), totalDays + 1);
        return true;
    }

    private bool TryGetMap(MapId mapId, out EntityUid map)
    {
        map = default;
        if (!_mapSystem.TryGetMap(mapId, out var mapUid) || mapUid == null)
            return false;
        map = mapUid.Value;
        return true;
    }

    private static double Mod(double x, double m)
    {
        var r = x % m;
        return r < 0 ? r + m : r;
    }

    private static NCDayPhase GetPhase(int hour, int minute)
    {
        var totalMinutes = hour * 60 + minute;
        if (totalMinutes < 4 * 60)
            return NCDayPhase.DeepNight;
        if (totalMinutes < 6 * 60)
            return NCDayPhase.Night;
        if (totalMinutes < 7 * 60)
            return NCDayPhase.Dawn;
        if (totalMinutes < 12 * 60)
            return NCDayPhase.Morning;
        if (totalMinutes < 17 * 60)
            return NCDayPhase.Day;
        if (totalMinutes < 19 * 60)
            return NCDayPhase.Afternoon;
        if (totalMinutes < 21 * 60)
            return NCDayPhase.Evening;
        if (totalMinutes < 23 * 60)
            return NCDayPhase.LateEvening;
        return NCDayPhase.DeepNight;
    }

    private static double SecondsPerFullDay() => 86400.0;
}

public enum NCDayPhase
{
    DeepNight,
    Night,
    Dawn,
    Morning,
    Day,
    Afternoon,
    Evening,
    LateEvening,
}

public readonly record struct NCDayTimeInfo(int Hour, int Minute, double Normalized, NCDayPhase Phase, long DayNumber);
