using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Campfire;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Weather;

public sealed partial class RMCWeatherSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDecalSystem _decals = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private const int GameplayBatchSize = 32;
    private const int DecalBatchSize = 64;
    private const float CleanDecalChance = 0.05f;

    private readonly Dictionary<EntityUid, WeatherWork> _weatherWork = new();

    private void OnCycleShutdown(Entity<RMCWeatherCycleComponent> ent, ref ComponentShutdown args)
    {
        _weatherWork.Remove(ent);
    }

    private void UpdateWeatherWork(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weather, TimeSpan delta)
    {
        if (!_weatherWork.TryGetValue(ent, out var work))
        {
            work = new WeatherWork();
            _weatherWork.Add(ent, work);
        }

        var mapId = Transform(ent).MapID;
        ent.Comp.EffectCooldown -= delta;
        if (ent.Comp.EffectCooldown <= TimeSpan.Zero && work.Gameplay.Count == 0)
        {
            QueueGameplayEffects(work, weather);
            ent.Comp.EffectCooldown = WeatherEffectInterval;
        }

        for (var count = 0; count < GameplayBatchSize && work.Gameplay.TryDequeue(out var job); count++)
        {
            var (uid, effect) = job;
            if (TerminatingOrDeleted(uid) ||
                Paused(uid) ||
                Transform(uid).MapID != mapId ||
                !IsWeatherExposed(uid, mapId))
            {
                continue;
            }

            switch (effect)
            {
                case WeatherEffect.SmotherMob:
                    if (TryComp(uid, out FlammableComponent? flammable) && flammable.OnFire)
                        _flammable.AdjustStacks((uid, flammable), -weather.FireSmotheringStrength);
                    break;
                case WeatherEffect.SmotherTile:
                    if (TryComp(uid, out TileFireComponent? fire))
                        SmotherTileFire(uid, fire, weather.FireSmotheringStrength);
                    break;
                case WeatherEffect.SmotherCampfire:
                    if (TryComp(uid, out CampfireComponent? campfire) && campfire.Lit)
                    {
                        var ev = new CampfireWeatherSmotherEvent(TimeSpan.FromSeconds(weather.FireSmotheringStrength));
                        RaiseLocalEvent(uid, ref ev);
                    }
                    break;
                case WeatherEffect.DiluteTimedAcid:
                    if (TryComp(uid, out TimedCorrodingComponent? timed) &&
                        TryMarkAcidDiluted(uid, ent.Comp.EventSequence))
                    {
                        DiluteTimedAcid(uid, timed, work.AcidMultiplier);
                    }
                    break;
                case WeatherEffect.DiluteDamageableAcid:
                    if (TryComp(uid, out DamageableCorrodingComponent? acid) &&
                        TryMarkAcidDiluted(uid, ent.Comp.EventSequence))
                    {
                        DiluteDamageableAcid(uid, acid, work.AcidMultiplier);
                    }
                    break;
                case WeatherEffect.ExposeMob:
                    ProcessMobEffect(uid, weather);
                    break;
            }
        }

        if (!weather.CleansDecals || !ent.Comp.FirstDropComplete)
            return;

        ent.Comp.CleanCooldown -= delta;
        if (ent.Comp.CleanCooldown <= TimeSpan.Zero &&
            work.Chunks.Count == 0 &&
            work.Decals.Count == 0)
        {
            var grids = EntityQueryEnumerator<MapGridComponent, DecalGridComponent>();
            while (grids.MoveNext(out var grid, out _, out var decals))
            {
                if (Transform(grid).MapID != mapId)
                    continue;

                foreach (var indices in decals.ChunkCollection.ChunkCollection.Keys)
                {
                    work.Chunks.Enqueue((grid, indices));
                }
            }

            ent.Comp.CleanCooldown = WeatherCleanInterval;
        }

        CleanDecalBatch(work, mapId);
    }

    private void QueueGameplayEffects(WeatherWork work, RMCWeatherEvent weather)
    {
        if (weather.FireSmotheringStrength > 0)
        {
            QueueGameplayEffects<OnFireComponent>(work.Gameplay, WeatherEffect.SmotherMob);
            QueueGameplayEffects<TileFireComponent>(work.Gameplay, WeatherEffect.SmotherTile);
            QueueGameplayEffects<CampfireComponent>(work.Gameplay, WeatherEffect.SmotherCampfire);
            work.AcidMultiplier = Math.Clamp(1 - weather.FireSmotheringStrength * weather.AcidDilutionPerFireSmotheringStrength, 0, 1);
            if (work.AcidMultiplier < 1)
            {
                QueueGameplayEffects<TimedCorrodingComponent>(work.Gameplay, WeatherEffect.DiluteTimedAcid);
                QueueGameplayEffects<DamageableCorrodingComponent>(work.Gameplay, WeatherEffect.DiluteDamageableAcid);
            }
        }

        if (!weather.ExposureDamage.Empty || weather.EffectMessage != null)
            QueueGameplayEffects<MobStateComponent>(work.Gameplay, WeatherEffect.ExposeMob);
    }

    private void QueueGameplayEffects<T>(Queue<(EntityUid, WeatherEffect)> jobs, WeatherEffect effect)
        where T : IComponent
    {
        var entities = EntityQueryEnumerator<T>();
        while (entities.MoveNext(out var uid, out _))
        {
            jobs.Enqueue((uid, effect));
        }
    }

    private void SmotherTileFire(EntityUid uid, TileFireComponent fire, int strength)
    {
        var reduction = TimeSpan.FromSeconds(strength);
        var remaining = fire.SpawnedAt + fire.Duration - _timing.CurTime;
        if (remaining <= reduction)
        {
            QueueDel(uid);
            return;
        }

        fire.Duration -= reduction;
        Dirty(uid, fire);
    }

    private void DiluteTimedAcid(EntityUid uid, TimedCorrodingComponent timed, float multiplier)
    {
        if (multiplier <= 0)
        {
            _acid.RemoveAcid(uid);
            return;
        }

        timed.CorrodesAt = _timing.CurTime + (timed.CorrodesAt - _timing.CurTime) * multiplier;
        timed.Dps *= multiplier;
        timed.LightDps *= multiplier;
        Dirty(uid, timed);
    }

    private void DiluteDamageableAcid(EntityUid uid, DamageableCorrodingComponent damageable, float multiplier)
    {
        if (multiplier <= 0)
        {
            _acid.RemoveAcid(uid);
            return;
        }

        damageable.AcidExpiresAt = _timing.CurTime + (damageable.AcidExpiresAt - _timing.CurTime) * multiplier;
        damageable.Dps *= multiplier;
        damageable.Damage *= multiplier;
        Dirty(uid, damageable);
    }

    private void ProcessMobEffect(EntityUid uid, RMCWeatherEvent weatherEvent)
    {
        if (!TryComp(uid, out MobStateComponent? mobState) ||
            !TryComp(uid, out DamageableComponent? damageable) ||
            !_mobState.IsAlive(uid, mobState) ||
            HasComp<GhostComponent>(uid))
        {
            return;
        }

        if (!weatherEvent.ExposureDamage.Empty)
        {
            var xenoMultiplier = Math.Max(weatherEvent.XenoExposureDamageMultiplier, 0f);
            var damage = HasComp<XenoComponent>(uid)
                ? weatherEvent.ExposureDamage * xenoMultiplier
                : weatherEvent.ExposureDamage;

            _damageable.TryChangeDamage(uid, damage, true, false, damageable);
        }

        if (weatherEvent.EffectMessage is { } effectMessage &&
            CanShowWeatherEffectMessage(uid, weatherEvent))
        {
            _popup.PopupEntity(Loc.GetString(effectMessage), uid, uid, PopupType.Small);
        }
    }

    private void CleanDecalBatch(WeatherWork work, MapId mapId)
    {
        if (work.Decals.Count == 0 && work.Chunks.TryDequeue(out var next))
        {
            work.CurrentChunk = next;
            if (TryComp(next.Grid, out DecalGridComponent? decals) &&
                decals.ChunkCollection.ChunkCollection.TryGetValue(next.Indices, out var nextChunk))
            {
                foreach (var id in nextChunk.Decals.Keys)
                {
                    work.Decals.Enqueue(id);
                }
            }
        }

        var (grid, indices) = work.CurrentChunk;
        if (work.Decals.Count == 0)
            return;

        if (!TryComp(grid, out MapGridComponent? mapGrid) ||
            !TryComp(grid, out DecalGridComponent? decalGrid) ||
            !decalGrid.ChunkCollection.ChunkCollection.TryGetValue(indices, out var chunk) ||
            Transform(grid).MapID != mapId)
        {
            work.Decals.Clear();
            return;
        }

        for (var count = 0; count < DecalBatchSize && work.Decals.TryDequeue(out var decalId); count++)
        {
            if (!chunk.Decals.TryGetValue(decalId, out var decal) || !decal.Cleanable)
                continue;

            var coords = new EntityCoordinates(grid, decal.Coordinates);
            if (_mapSystem.TryGetTileRef(grid, mapGrid, coords, out var tile) &&
                _weather.CanWeatherAffect(grid, mapGrid, tile) &&
                !IsRMCWeatherBlocked(mapId, _transform.ToMapCoordinates(coords).Position) &&
                _random.Prob(CleanDecalChance))
            {
                _decals.RemoveDecal(grid, decalId);
            }
        }
    }

    private sealed class WeatherWork
    {
        public readonly Queue<(EntityUid, WeatherEffect)> Gameplay = new();
        public readonly Queue<(EntityUid Grid, Vector2i Indices)> Chunks = new();
        public readonly Queue<uint> Decals = new();
        public (EntityUid Grid, Vector2i Indices) CurrentChunk;
        public float AcidMultiplier;
    }

    private enum WeatherEffect
    {
        SmotherMob,
        SmotherTile,
        SmotherCampfire,
        DiluteTimedAcid,
        DiluteDamageableAcid,
        ExposeMob,
    }
}
