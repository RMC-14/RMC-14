using Content.Server._RMC14.Power;
using Content.Shared._RMC14.Gibbing;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Sensor;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Nuke;

public sealed class RMCNukeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly RMCGibSystem _rmcGib = default!;
    [Dependency] private readonly SensorTowerSystem _sensorTower = default!;
    [Dependency] private readonly RMCPowerSystem _power = default!;

    private readonly DamageSpecifier _damage = new() { DamageDict = { ["Blunt"] = 1e5, ["Heat"] = 1e5 } };
    private EntityQuery<RMCRepairableComponent> _repairable;

    public override void Initialize()
    {
        base.Initialize();
        _repairable = GetEntityQuery<RMCRepairableComponent>();
    }

    private void KillEverythingOnMap(MapId mapId)
    {
        var toDamage = new HashSet<EntityUid>();
        var toDelete = new HashSet<EntityUid>();

        var living = EntityQueryEnumerator<DamageableComponent, TransformComponent>();
        var tunnels = EntityQueryEnumerator<XenoTunnelComponent, TransformComponent>();
        var vents = EntityQueryEnumerator<VentCrawlableComponent, TransformComponent>();
        while (living.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            AddNukeTarget(uid, toDamage, toDelete);
        }
        while (tunnels.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            AddNukeTarget(uid, toDamage, toDelete);
        }
        while (vents.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            AddNukeTarget(uid, toDamage, toDelete);
        }

        toDelete.ExceptWith(toDamage);

        // Mobs and repairables go through damage so death/destruction events can run before the map cleanup.
        foreach (var uid in toDamage)
        {
            _damageable.TryChangeDamage(uid, _damage, true);
        }

        foreach (var uid in toDelete)
        {
            _rmcGib.ScatterInventoryItems(uid);
            _entity.TryQueueDeleteEntity(uid);
        }

        var sensors = EntityQueryEnumerator<SensorTowerComponent, TransformComponent>();
        var generators = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (sensors.MoveNext(out var uid, out var sensor, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            _sensorTower.FullyDestroy(new(uid, sensor));
        }
        while (generators.MoveNext(out var uid, out var generator, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            _power.FullyDestroy(new(uid, generator));
        }
    }

    private void AddNukeTarget(EntityUid uid, HashSet<EntityUid> toDamage, HashSet<EntityUid> toDelete)
    {
        if (HasComp<MobStateComponent>(uid) || _repairable.HasComp(uid))
            toDamage.Add(uid);
        else
            toDelete.Add(uid);
    }

    public void NukeMap(MapId mapId)
    {
        for (var i = 0; i < 3; i++)
            KillEverythingOnMap(mapId);
    }
}
