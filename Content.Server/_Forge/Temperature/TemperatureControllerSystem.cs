using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server._Forge.RoundSeed;
using Content.Shared._Forge.DayNight;
using Content.Shared._Forge.Temperature;
using Content.Shared.Atmos;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Forge.Temperature;

/// <summary>
/// Adjusts map temperature based on day/night cycle, round seed and zone.
/// </summary>
public sealed class TemperatureControllerSystem : EntitySystem
{
    [Dependency] private readonly RoundSeedSystem _roundSeed = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextUpdate;

    // Cache current day's jitter to avoid recreating Random every second
    private long _cachedDay = -1;
    private int _cachedSeed = -1;
    private TemperatureZone _cachedZone;
    private float _cachedJitter;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Throttle updates to once per second
        var curTime = _timing.CurTime;
        if (curTime < _nextUpdate)
            return;

        _nextUpdate = curTime + TimeSpan.FromSeconds(1.0);

        var query = EntityQueryEnumerator<TemperatureControllerComponent, MapComponent, MapAtmosphereComponent, DayNightCycleComponent>();
        while (query.MoveNext(out var uid, out var tempCtrl, out var map, out var atmosphere, out var dayCycle))
        {
            if (atmosphere.Space)
                continue;

            var (baseTemp, amplitude, jitter) = GetZoneSettings(tempCtrl.Zone);

            var (seed, _, _) = _roundSeed.EnsureSeed();

            // Cache current day's jitter to avoid creating Random every second
            // Recalculate only when day, seed, or zone changes
            if (dayCycle.DayNumber != _cachedDay || seed != _cachedSeed || tempCtrl.Zone != _cachedZone)
            {
                var rng = new Random(MakeSeed(seed, tempCtrl.Zone, dayCycle.DayNumber));
                _cachedJitter = (float)((rng.NextDouble() * 2 - 1) * jitter);
                _cachedDay = dayCycle.DayNumber;
                _cachedSeed = seed;
                _cachedZone = tempCtrl.Zone;
            }

            var dailyJitter = _cachedJitter;

            var normalized = dayCycle.NormalizedTime;
            var seasonal = (float)Math.Sin((normalized - 0.25f) * Math.Tau); // peak around midday
            var targetTemp = baseTemp + amplitude * seasonal + dailyJitter;

            if (Math.Abs(atmosphere.Mixture.Temperature - targetTemp) < 0.01f)
                continue;

            // Directly modify immutable mixture temperature without creating a clone
            atmosphere.Mixture.SetMapTemperature(targetTemp);
            // Update overlay and dirty the component (mixture is already immutable, so no clone will be made)
            _atmosphere.SetMapGasMixture(uid, atmosphere.Mixture, atmosphere, updateTiles: false);

            // Push temperature to day/night state for consumers.
            dayCycle.TemperatureKelvin = targetTemp;
            Dirty(uid, dayCycle);
        }
    }

    private static (float Base, float Amplitude, float Jitter) GetZoneSettings(TemperatureZone zone)
    {
        return zone switch
        {
            TemperatureZone.Temperate => (290f, 10f, 2f),
            TemperatureZone.Desert => (300f, 18f, 3f),
            TemperatureZone.Arctic => (270f, 8f, 2f),
            TemperatureZone.Jungle => (303f, 12f, 2.5f),
            _ => (290f, 10f, 2f),
        };
    }

    private static int MakeSeed(int roundSeed, TemperatureZone zone, long dayNumber)
    {
        unchecked
        {
            var s = roundSeed;
            s = (s * 397) ^ (int)zone;
            s = (s * 397) ^ (int)dayNumber;
            return s;
        }
    }
}
