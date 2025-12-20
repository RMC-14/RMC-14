using Content.Server._RMC14.Power;
using Content.Shared._RMC14.Gibbing;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Sensor;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared.Damage;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Nuke;

public sealed class RMCNukeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly RMCGibSystem _rmcGib = default!;
    [Dependency] private readonly SensorTowerSystem _sensorTower = default!;
    [Dependency] private readonly RMCPowerSystem _power = default!;

    private readonly DamageSpecifier _damage = new() { DamageDict = { { "Blunt", 1e10 } } };
    private EntityQuery<RMCRepairableComponent> _repairable;

    public override void Initialize()
    {
        base.Initialize();
        _repairable = GetEntityQuery<RMCRepairableComponent>();
    }

    private void KillEverythingOnMap(MapId mapId)
    {
        var toDelete = new HashSet<EntityUid>();

        var living = EntityQueryEnumerator<DamageableComponent, TransformComponent>();
        var tunnels = EntityQueryEnumerator<XenoTunnelComponent, TransformComponent>();
        var vents = EntityQueryEnumerator<VentCrawlableComponent, TransformComponent>();
        while (living.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            toDelete.Add(uid);
        }
        while (tunnels.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            toDelete.Add(uid);
        }
        while (vents.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            toDelete.Add(uid);
        }

        foreach (var uid in toDelete)
        {
            if (_repairable.HasComp(uid))
            {
                _damageable.TryChangeDamage(uid, _damage, true);
            }
            else
            {
                _rmcGib.ScatterInventoryItems(uid);
                _entity.TryQueueDeleteEntity(uid);
            }
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

    public void NukeMap(MapId mapId)
    {
        for (var i = 0; i < 3; i++)
            KillEverythingOnMap(mapId);
    }
}
