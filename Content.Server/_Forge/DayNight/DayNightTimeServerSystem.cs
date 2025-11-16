using Content.Shared._NC14.DayNight;
using Robust.Shared.Map.Components;

namespace Content.Server._NC14.DayNight;

/// <summary>
/// Server-side updater that writes the current day/time info into the NCDayNightCycleComponent for consumers.
/// </summary>
public sealed class NCDayNightTimeServerSystem : EntitySystem
{
    [Dependency] private readonly NCDayNightTimeSystem _timeSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NCDayNightCycleComponent, MapComponent>();
        while (query.MoveNext(out var uid, out var cycle, out var map))
        {
            if (!_timeSystem.TryGetTime(map.MapId, out var info))
                continue;

            var normalized = (float) info.Normalized;
            if (cycle.Hour == info.Hour &&
                cycle.Minute == info.Minute &&
                cycle.DayNumber == info.DayNumber &&
                cycle.Phase == info.Phase &&
                Math.Abs(cycle.NormalizedTime - normalized) < 0.0001f)
            {
                continue;
            }

            cycle.Hour = info.Hour;
            cycle.Minute = info.Minute;
            cycle.DayNumber = info.DayNumber;
            cycle.Phase = info.Phase;
            cycle.NormalizedTime = normalized;
            Dirty(uid, cycle);
        }
    }
}
