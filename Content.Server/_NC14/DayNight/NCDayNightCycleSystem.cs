using System;
using Content.Server._NC14.RoundSeed;
using Content.Shared._NC14.DayNight;
using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._NC14.DayNight;

/// <summary>
/// Drives the map light cycle for maps that opt-in via <see cref="NCDayNightCycleComponent"/>,
/// using the global round seed for deterministic timing.
/// </summary>
public sealed class NCDayNightCycleSystem : EntitySystem
{
    [Dependency] private readonly NCRoundSeedSystem _roundSeed = default!;
    [Dependency] private readonly SharedLightCycleSystem _lightCycle = default!;

    private double? _durationSample;
    private double? _offsetSample;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NCDayNightCycleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NCDayNightCycleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => ResetSamples());
    }

    private void OnMapInit(Entity<NCDayNightCycleComponent> ent, ref MapInitEvent args)
    {
        ApplyCycle(ent);
    }

    private void OnStartup(Entity<NCDayNightCycleComponent> ent, ref ComponentStartup args)
    {
        ApplyCycle(ent);
    }

    private void ResetSamples()
    {
        _durationSample = null;
        _offsetSample = null;
    }

    private void ApplyCycle(Entity<NCDayNightCycleComponent> ent)
    {
        if (!ent.Comp.Enabled)
            return;

        var target = GetTargetMap(ent.Owner);
        if (target is not EntityUid mapUid)
            return;

        var lightCycle = EnsureComp<LightCycleComponent>(mapUid);
        lightCycle.Enabled = true;
        lightCycle.InitialOffset = false;

        var mapLight = EnsureComp<MapLightComponent>(mapUid);

        // If there's no meaningful ambient color yet, default to white so cycling is visible.
        if (mapLight.AmbientLightColor == default || mapLight.AmbientLightColor == MapLightComponent.DefaultColor)
            mapLight.AmbientLightColor = Color.FromSrgb(Color.White);

        // Always track the current ambient as the baseline for cycling.
        lightCycle.OriginalColor = mapLight.AmbientLightColor;

        var (seed, _, _) = _roundSeed.EnsureSeed();
        var rng = new Random(seed);

        _durationSample ??= (rng.NextDouble() * 2) - 1; // [-1, 1] seeded
        _offsetSample ??= rng.NextDouble(); // [0, 1) seeded

        var baseSeconds = ent.Comp.BaseDuration.TotalSeconds;
        var jitterMax = ent.Comp.DurationJitter.TotalSeconds;
        var jitterSeconds = _durationSample.Value * jitterMax;
        var durationSeconds = Math.Max(1, baseSeconds + jitterSeconds);
        lightCycle.Duration = TimeSpan.FromSeconds(durationSeconds);

        var offsetSeconds = _offsetSample.Value * lightCycle.Duration.TotalSeconds;
        _lightCycle.SetOffset((mapUid, lightCycle), TimeSpan.FromSeconds(offsetSeconds));

        Dirty(mapUid, lightCycle);
        Dirty(mapUid, mapLight);
    }

    private EntityUid? GetTargetMap(EntityUid owner)
    {
        if (HasComp<MapComponent>(owner))
            return owner;

        if (!TryComp(owner, out TransformComponent? xform) || xform.MapUid is not { } mapUid)
            return null;

        return mapUid;
    }
}
