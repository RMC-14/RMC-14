using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Light;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Weather;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weather;

public sealed class RMCWeatherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roof = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly RMCAmbientLightSystem _rmcLight = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<BlockWeatherComponent> _blockQuery;

    public override void Initialize()
    {
        base.Initialize();
        _blockQuery = GetEntityQuery<BlockWeatherComponent>();

        SubscribeLocalEvent<RMCWeatherCycleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCWeatherCycleComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.WeatherEvents.Count <= 0)
            return;

        EnsureComp<RMCAmbientLightComponent>(ent);
        EnsureComp<RMCAmbientLightEffectsComponent>(ent);

        ent.Comp.LastEventCooldown = _random.Next(ent.Comp.MinTimeBetweenEvents);

    }

    public bool CanWeatherAffectArea(EntityUid uid, MapGridComponent grid, TileRef tileRef, RoofComponent? roofComp = null)
    {
        if (Resolve(uid, ref roofComp, false) && _roof.IsRooved((uid, grid, roofComp), tileRef.GridIndices))
            return false;

        if (!_area.IsWeatherEnabled((uid, grid), tileRef.GridIndices))
            return false;

        var anchoredEntities = _mapSystem.GetAnchoredEntitiesEnumerator(uid, grid, tileRef.GridIndices);

        while (anchoredEntities.MoveNext(out var ent))
        {
            if (_blockQuery.HasComponent(ent.Value))
                return false;
        }

        return true;
    }

    public void HandleWeatherEffects(Entity<RMCWeatherCycleComponent> ent)
    {
        if (ent.Comp.CurrentEvent == null)
            return;

        if (ent.Comp.CurrentEvent.LightningChance <= 0 || ent.Comp.CurrentEvent.LightningEffects.Count <= 0)
            return;

        if (!_random.Prob(ent.Comp.CurrentEvent.LightningChance))
            return;

        EnsureComp<RMCAmbientLightComponent>(ent, out var lightComp);
        EnsureComp<MapLightComponent>(ent, out var mapLightComp);

        //Since lightning animation runs from Black to Black, we don't run it if the lighting is different
        if (lightComp.IsAnimating || mapLightComp.AmbientLightColor != Color.Black)
            return;

        var lightningEffect = _rmcLight.ProcessPrototype(_random.Pick(ent.Comp.CurrentEvent.LightningEffects));
        var lightningDuration = ent.Comp.CurrentEvent.LightningDuration;
        _rmcLight.SetColor((ent, lightComp), lightningEffect, lightningDuration);

        var sound = ent.Comp.CurrentEvent.LightningSound;
        _audio.PlayGlobal(_audio.ResolveSound(sound), Filter.BroadcastMap(Transform(ent).MapID), true);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();

        while (weatherQuery.MoveNext(out var uid, out var cycle))
        {
            cycle.LastEventCooldown -= TimeSpan.FromSeconds(frameTime);

            // Process whether to start a weather event
            if(cycle.LastEventCooldown <= TimeSpan.Zero)
            {
                var weatherPick = _random.Pick(cycle.WeatherEvents);
                _proto.TryIndex(weatherPick.WeatherType, out var weatherProto);
                var endTime = _timing.CurTime + weatherPick.Duration;

                cycle.CurrentEvent = weatherPick;
                _weather.SetWeather(Transform(uid).MapID, weatherProto, endTime);

                var minTimeVariance = (-cycle.MinTimeVariance * 0.5) + _random.Next(cycle.MinTimeVariance);
                cycle.LastEventCooldown = weatherPick.Duration + cycle.MinTimeBetweenEvents + minTimeVariance;
            }

            // Process effects for ongoing weather event
            if (cycle.CurrentEvent != null)
            {
                cycle.CurrentEvent.LightningCooldown -= TimeSpan.FromSeconds(frameTime);
                if (cycle.CurrentEvent.LightningCooldown <= TimeSpan.Zero)
                {
                    HandleWeatherEffects((uid, cycle));
                    cycle.CurrentEvent.LightningCooldown = cycle.CurrentEvent.LightningCooldownDuration;
                }
            }
        }
    }
}
