using Content.Shared._Forge.DayNight;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Forge.DayNight;

/// <summary>
/// Server-side updater that writes the current day/time info into the DayNightCycleComponent for consumers.
/// </summary>
public sealed class DayNightTimeServerSystem : EntitySystem
{
    [Dependency] private readonly DayNightTimeSystem _timeSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Throttle updates to once per second
        var curTime = _timing.CurTime;
        if (curTime < _nextUpdate)
            return;

        _nextUpdate = curTime + TimeSpan.FromSeconds(1.0);

        var query = EntityQueryEnumerator<DayNightCycleComponent, MapComponent>();
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
