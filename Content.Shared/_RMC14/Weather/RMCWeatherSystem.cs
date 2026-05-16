using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weather;
using Robust.Shared.Audio;
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
    private static readonly TimeSpan WeatherEffectInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan WeatherCleanInterval = TimeSpan.FromSeconds(5);
    private const float CleanDecalChance = 0.05f;

    private static readonly SoundSpecifier MarineWeatherWarningSound =
        new SoundPathSpecifier("/Audio/_RMC14/Effects/radiostatic.ogg", AudioParams.Default.WithVolume(-4));

    private static readonly SoundSpecifier XenoWeatherWarningSound =
        new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_distantroar_3.ogg", AudioParams.Default.WithVolume(-4));

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
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedXenoAcidSystem _acid = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDecalSystem _decals = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;

    private EntityQuery<BlockWeatherComponent> _blockQuery;

    public override void Initialize()
    {
        base.Initialize();
        _blockQuery = GetEntityQuery<BlockWeatherComponent>();

        SubscribeLocalEvent<RMCWeatherCycleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);
    }

    private void OnMapInit(Entity<RMCWeatherCycleComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.WeatherEvents.Count <= 0)
            return;

        EnsureComp<RMCAmbientLightComponent>(ent);
        EnsureComp<RMCAmbientLightEffectsComponent>(ent);

        ent.Comp.State = RMCWeatherCycleState.Idle;
        ent.Comp.CurrentEventIndex = null;
        ent.Comp.CheckCooldown = ent.Comp.MinTimeBetweenEvents > TimeSpan.Zero
            ? _random.Next(ent.Comp.MinTimeBetweenEvents)
            : TimeSpan.Zero;
        ent.Comp.WarningRemaining = TimeSpan.Zero;
        ent.Comp.EventRemaining = TimeSpan.Zero;
        ent.Comp.LightningCooldown = TimeSpan.Zero;
        ent.Comp.EffectCooldown = WeatherEffectInterval;
        ent.Comp.CleanCooldown = WeatherCleanInterval;
        ent.Comp.FirstDropComplete = false;
        Dirty(ent);
    }

    private void OnDropshipLandedOnPlanet(ref DropshipLandedOnPlanetEvent ev)
    {
        if (_net.IsClient)
            return;

        var mapId = Transform(ev.Dropship.Owner).MapID;
        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();
        while (weatherQuery.MoveNext(out var uid, out var cycle))
        {
            if (Transform(uid).MapID != mapId || cycle.FirstDropComplete)
                continue;

            cycle.FirstDropComplete = true;
            Dirty(uid, cycle);
        }
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

    public void HandleWeatherEffects(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        if (weatherEvent.LightningChance <= 0 || weatherEvent.LightningEffects.Count <= 0)
            return;

        if (!_random.Prob(weatherEvent.LightningChance))
            return;

        EnsureComp<RMCAmbientLightComponent>(ent, out var lightComp);
        EnsureComp<MapLightComponent>(ent, out var mapLightComp);

        // The lightning color sequences animate black to black, so skip if something else is already driving light.
        if (lightComp.IsAnimating || mapLightComp.AmbientLightColor != Color.Black)
            return;

        var lightningEffect = _rmcLight.ProcessPrototype(_random.Pick(weatherEvent.LightningEffects));
        _rmcLight.SetColor((ent, lightComp), lightningEffect, weatherEvent.LightningDuration);

        _audio.PlayGlobal(_audio.ResolveSound(weatherEvent.LightningSound), Filter.BroadcastMap(Transform(ent).MapID), true);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var delta = TimeSpan.FromSeconds(frameTime);
        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();

        while (weatherQuery.MoveNext(out var uid, out var cycle))
        {
            if (cycle.WeatherEvents.Count <= 0)
                continue;

            switch (cycle.State)
            {
                case RMCWeatherCycleState.Idle:
                    UpdateIdle((uid, cycle), delta);
                    break;
                case RMCWeatherCycleState.Warning:
                    UpdateWarning((uid, cycle), delta);
                    break;
                case RMCWeatherCycleState.Running:
                    UpdateRunning((uid, cycle), delta);
                    break;
                case RMCWeatherCycleState.Cooldown:
                    UpdateCooldown((uid, cycle), delta);
                    break;
            }
        }
    }

    private void UpdateIdle(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        ent.Comp.CheckCooldown -= delta;
        if (ent.Comp.CheckCooldown > TimeSpan.Zero)
            return;

        if (!_random.Prob(Math.Clamp(ent.Comp.StartChance, 0, 1)))
        {
            ent.Comp.CheckCooldown = GetCheckDelay(ent.Comp);
            Dirty(ent);
            return;
        }

        StartWarning(ent, _random.Next(ent.Comp.WeatherEvents.Count));
    }

    private void UpdateWarning(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        ent.Comp.WarningRemaining -= delta;
        if (ent.Comp.WarningRemaining > TimeSpan.Zero)
            return;

        StartWeather(ent);
    }

    private void UpdateRunning(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        if (!TryGetCurrentEvent(ent.Comp, out var weatherEvent))
        {
            EndWeather(ent);
            return;
        }

        ent.Comp.EventRemaining -= delta;
        if (ent.Comp.EventRemaining <= TimeSpan.Zero)
        {
            EndWeather(ent);
            return;
        }

        ent.Comp.LightningCooldown -= delta;
        if (ent.Comp.LightningCooldown <= TimeSpan.Zero)
        {
            HandleWeatherEffects(ent, weatherEvent);
            ent.Comp.LightningCooldown = weatherEvent.LightningCooldownDuration;
        }

        ent.Comp.EffectCooldown -= delta;
        if (ent.Comp.EffectCooldown <= TimeSpan.Zero)
        {
            ProcessGameplayEffects(ent, weatherEvent);
            ent.Comp.EffectCooldown = WeatherEffectInterval;
        }

        if (!weatherEvent.CleansDecals || !ent.Comp.FirstDropComplete)
            return;

        ent.Comp.CleanCooldown -= delta;
        if (ent.Comp.CleanCooldown > TimeSpan.Zero)
            return;

        ProcessWeatherCleaning(ent, weatherEvent);
        ent.Comp.CleanCooldown = WeatherCleanInterval;
    }

    private void UpdateCooldown(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        ent.Comp.CheckCooldown -= delta;
        if (ent.Comp.CheckCooldown > TimeSpan.Zero)
            return;

        ent.Comp.State = RMCWeatherCycleState.Idle;
        ent.Comp.CheckCooldown = TimeSpan.Zero;
        Dirty(ent);
    }

    private void StartWarning(Entity<RMCWeatherCycleComponent> ent, int eventIndex)
    {
        ent.Comp.CurrentEventIndex = eventIndex;
        ent.Comp.WarningRemaining = ent.Comp.WarnTime;
        ent.Comp.State = RMCWeatherCycleState.Warning;
        SendWeatherWarning(ent, ent.Comp.WeatherEvents[eventIndex]);
        Dirty(ent);

        if (ent.Comp.WarningRemaining <= TimeSpan.Zero)
            StartWeather(ent);
    }

    private void StartWeather(Entity<RMCWeatherCycleComponent> ent)
    {
        if (!TryGetCurrentEvent(ent.Comp, out var weatherEvent))
        {
            ent.Comp.State = RMCWeatherCycleState.Idle;
            ent.Comp.CheckCooldown = GetCheckDelay(ent.Comp);
            Dirty(ent);
            return;
        }

        if (!_proto.TryIndex(weatherEvent.WeatherType, out var weatherProto))
        {
            Log.Error($"Unable to find RMC weather prototype {weatherEvent.WeatherType} for {ToPrettyString(ent)}.");
            ent.Comp.State = RMCWeatherCycleState.Idle;
            ent.Comp.CheckCooldown = GetCheckDelay(ent.Comp);
            Dirty(ent);
            return;
        }

        var endTime = _timing.CurTime + weatherEvent.Duration;
        ent.Comp.State = RMCWeatherCycleState.Running;
        ent.Comp.EventRemaining = weatherEvent.Duration;
        ent.Comp.LightningCooldown = TimeSpan.Zero;
        ent.Comp.EffectCooldown = WeatherEffectInterval;
        ent.Comp.CleanCooldown = WeatherCleanInterval;
        ent.Comp.EventSequence++;
        _weather.SetWeather(Transform(ent).MapID, weatherProto, endTime);
        Dirty(ent);
    }

    private void EndWeather(Entity<RMCWeatherCycleComponent> ent)
    {
        _weather.SetWeather(Transform(ent).MapID, null, null);
        ent.Comp.State = RMCWeatherCycleState.Cooldown;
        ent.Comp.CurrentEventIndex = null;
        ent.Comp.CheckCooldown = ent.Comp.MinTimeBetweenEvents;
        ent.Comp.WarningRemaining = TimeSpan.Zero;
        ent.Comp.EventRemaining = TimeSpan.Zero;
        ent.Comp.LightningCooldown = TimeSpan.Zero;
        ent.Comp.EffectCooldown = WeatherEffectInterval;
        ent.Comp.CleanCooldown = WeatherCleanInterval;
        Dirty(ent);
    }

    private bool TryGetCurrentEvent(RMCWeatherCycleComponent cycle, out RMCWeatherEvent weatherEvent)
    {
        weatherEvent = default!;
        if (cycle.CurrentEventIndex is not { } index ||
            index < 0 ||
            index >= cycle.WeatherEvents.Count)
        {
            return false;
        }

        weatherEvent = cycle.WeatherEvents[index];
        return true;
    }

    private TimeSpan GetCheckDelay(RMCWeatherCycleComponent cycle)
    {
        var delay = cycle.MinTimeBetweenChecks;
        if (cycle.MinCheckVariance > TimeSpan.Zero)
            delay += _random.Next(cycle.MinCheckVariance) - TimeSpan.FromTicks(cycle.MinCheckVariance.Ticks / 2);

        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private void SendWeatherWarning(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        if (weatherEvent.WarningMode == RMCWeatherWarningMode.None)
            return;

        var mapId = Transform(ent).MapID;
        if (weatherEvent.WarningSound is { } warningSound)
            _audio.PlayGlobal(_audio.ResolveSound(warningSound), Filter.BroadcastMap(mapId), true);

        if (weatherEvent.WarningMode == RMCWeatherWarningMode.SirenOnly)
            return;

        var displayName = weatherEvent.DisplayName ?? weatherEvent.Name;
        var marineMessage = $"[bold]Weather Alert:[/bold] Incoming {displayName}. Seek shelter from exterior conditions.";
        var marineFilter = Filter.BroadcastMap(mapId).AddWhereAttachedEntity(e =>
            HasComp<MarineComponent>(e) ||
            HasComp<GhostComponent>(e));

        _marineAnnounce.AnnounceToMarines(marineMessage, MarineWeatherWarningSound, marineFilter);

        var xenoMessage = $"A distant roar echoes through the hive. {displayName} approaches.";
        var xenoFilter = Filter.BroadcastMap(mapId).AddWhereAttachedEntity(HasComp<XenoComponent>);
        _xenoAnnounce.Announce(default,
            xenoFilter,
            xenoMessage,
            _xenoAnnounce.WrapHive(xenoMessage),
            XenoWeatherWarningSound);
    }

    private void ProcessGameplayEffects(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        var mapId = Transform(ent).MapID;

        if (weatherEvent.FireSmotheringStrength > 0)
        {
            ProcessBurningEntities(mapId, weatherEvent.FireSmotheringStrength);
            ProcessTileFires(mapId, weatherEvent.FireSmotheringStrength);
            ProcessAcidDilution(mapId, weatherEvent.FireSmotheringStrength, ent.Comp.EventSequence);
        }

        if (!weatherEvent.ExposureDamage.Empty || !string.IsNullOrWhiteSpace(weatherEvent.EffectMessage))
            ProcessMobEffects(mapId, weatherEvent);
    }

    private void ProcessBurningEntities(MapId mapId, int fireSmotheringStrength)
    {
        var query = EntityQueryEnumerator<FlammableComponent>();
        while (query.MoveNext(out var uid, out var flammable))
        {
            if (!flammable.OnFire || !IsWeatherExposed(uid, mapId))
                continue;

            _flammable.AdjustStacks((uid, flammable), -fireSmotheringStrength);
        }
    }

    private void ProcessTileFires(MapId mapId, int fireSmotheringStrength)
    {
        var reduction = TimeSpan.FromSeconds(fireSmotheringStrength);
        var query = EntityQueryEnumerator<TileFireComponent>();
        while (query.MoveNext(out var uid, out var fire))
        {
            if (!IsWeatherExposed(uid, mapId))
                continue;

            var remaining = fire.SpawnedAt + fire.Duration - _timing.CurTime;
            if (remaining <= reduction)
            {
                QueueDel(uid);
                continue;
            }

            fire.Duration -= reduction;
            Dirty(uid, fire);
        }
    }

    private void ProcessAcidDilution(MapId mapId, int fireSmotheringStrength, int eventSequence)
    {
        var multiplier = Math.Clamp(1 - fireSmotheringStrength * 0.1f, 0, 1);
        if (multiplier >= 1)
            return;

        var timedQuery = EntityQueryEnumerator<TimedCorrodingComponent>();
        while (timedQuery.MoveNext(out var uid, out var timed))
        {
            if (!TryMarkAcidDiluted(uid, mapId, eventSequence))
                continue;

            if (multiplier <= 0)
            {
                _acid.RemoveAcid(uid);
                continue;
            }

            timed.CorrodesAt = _timing.CurTime + (timed.CorrodesAt - _timing.CurTime) * multiplier;
            timed.Dps *= multiplier;
            timed.LightDps *= multiplier;
            Dirty(uid, timed);
        }

        var damageableQuery = EntityQueryEnumerator<DamageableCorrodingComponent>();
        while (damageableQuery.MoveNext(out var uid, out var damageable))
        {
            if (!TryMarkAcidDiluted(uid, mapId, eventSequence))
                continue;

            if (multiplier <= 0)
            {
                _acid.RemoveAcid(uid);
                continue;
            }

            damageable.AcidExpiresAt = _timing.CurTime + (damageable.AcidExpiresAt - _timing.CurTime) * multiplier;
            damageable.Dps *= multiplier;
            damageable.Damage = damageable.Damage * multiplier;
            Dirty(uid, damageable);
        }
    }

    private bool TryMarkAcidDiluted(EntityUid uid, MapId mapId, int eventSequence)
    {
        if (!IsWeatherExposed(uid, mapId))
            return false;

        var weatherAcid = EnsureComp<RMCWeatherDilutedAcidComponent>(uid);
        if (weatherAcid.LastWeatherSequence == eventSequence)
            return false;

        weatherAcid.LastWeatherSequence = eventSequence;
        Dirty(uid, weatherAcid);
        return true;
    }

    private void ProcessMobEffects(MapId mapId, RMCWeatherEvent weatherEvent)
    {
        var query = EntityQueryEnumerator<MobStateComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var mobState, out var damageable))
        {
            if (!_mobState.IsAlive(uid, mobState) || !IsWeatherExposed(uid, mapId))
                continue;

            if (!weatherEvent.ExposureDamage.Empty)
            {
                var damage = HasComp<XenoComponent>(uid)
                    ? weatherEvent.ExposureDamage * 3
                    : weatherEvent.ExposureDamage;

                _damageable.TryChangeDamage(uid, damage, true, false, damageable);
            }

            if (!string.IsNullOrWhiteSpace(weatherEvent.EffectMessage) &&
                _random.Prob(Math.Clamp(weatherEvent.EffectMessageChance, 0, 1)))
            {
                _popup.PopupEntity(weatherEvent.EffectMessage, uid, uid, PopupType.SmallCaution);
            }
        }
    }

    private void ProcessWeatherCleaning(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        var mapId = Transform(ent).MapID;
        var grids = EntityQueryEnumerator<MapGridComponent, DecalGridComponent>();
        while (grids.MoveNext(out var gridUid, out var mapGrid, out _))
        {
            if (Transform(gridUid).MapID != mapId)
                continue;

            var tiles = _mapSystem.GetAllTilesEnumerator(gridUid, mapGrid);
            while (tiles.MoveNext(out var tile))
            {
                if (tile is not { } tileRef ||
                    !_weather.CanWeatherAffect(gridUid, mapGrid, tileRef))
                {
                    continue;
                }

                var coords = _mapSystem.GridTileToLocal(gridUid, mapGrid, tileRef.GridIndices);
                var decals = _decals.GetDecalsInRange(gridUid,
                    coords.Position,
                    0.75f,
                    decal => decal.Cleanable);

                foreach (var (decalId, _) in decals)
                {
                    if (_random.Prob(CleanDecalChance))
                        _decals.RemoveDecal(gridUid, decalId);
                }
            }
        }
    }

    private bool IsWeatherExposed(EntityUid uid, MapId mapId)
    {
        if (!TryGetWeatherTile(uid, mapId, out var grid, out var tile))
            return false;

        return _weather.CanWeatherAffect(grid.Owner, grid.Comp, tile);
    }

    private bool TryGetWeatherTile(EntityUid uid, MapId mapId, out Entity<MapGridComponent> grid, out TileRef tile)
    {
        grid = default;
        tile = default;

        var coords = _transform.GetMoverCoordinates(uid);
        if (_transform.GetGrid(coords) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? gridComp) ||
            Transform(gridUid).MapID != mapId ||
            !_mapSystem.TryGetTileRef(gridUid, gridComp, coords, out tile))
        {
            return false;
        }

        grid = (gridUid, gridComp);
        return true;
    }
}
