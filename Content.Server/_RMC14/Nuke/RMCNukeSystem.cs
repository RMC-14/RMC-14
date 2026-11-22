using Content.Server._RMC14.Power;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Nuke;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Sensor;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Nuke;

public sealed class RMCNukeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SensorTowerSystem _sensorTower = default!;
    [Dependency] private readonly RMCPowerSystem _power = default!;

    private readonly DamageSpecifier _damage = new() { DamageDict = { ["Blunt"] = 1e5, ["Heat"] = 1e5 } };
    private EntityQuery<RMCApcComponent> _apc;
    private EntityQuery<RMCRepairableComponent> _repairable;
    private EntityQuery<RMCNukeShelterComponent> _shelter;

    private int _chunkSize;
    private readonly List<(MapId, bool)> _currentlyNuking = [];
    private readonly List<EntityUid> _toDamage = [];
    private readonly List<EntityUid> _toDelete = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        Subs.CVar(_config, RMCCVars.RMCNukeChunkSize, v => _chunkSize = v, true);

        _apc = GetEntityQuery<RMCApcComponent>();
        _repairable = GetEntityQuery<RMCRepairableComponent>();
        _shelter = GetEntityQuery<RMCNukeShelterComponent>();
    }

    public void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _currentlyNuking.Clear();
        _toDamage.Clear();
        _toDelete.Clear();
    }

    private void MarkForDestruction(MapId mapId, bool spareShelters)
    {
        var living = EntityQueryEnumerator<DamageableComponent, TransformComponent>();
        var tunnels = EntityQueryEnumerator<XenoTunnelComponent, TransformComponent>();
        var vents = EntityQueryEnumerator<VentCrawlableComponent, TransformComponent>();
        while (living.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            if (spareShelters && IsSheltered(uid, transform))
                continue;

            AddNukeTarget(uid);
        }
        while (tunnels.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            AddNukeTarget(uid);
        }
        while (vents.MoveNext(out var uid, out var _, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            AddNukeTarget(uid);
        }
    }

    private void ProcessChunk()
    {
        var counter = 0;

        // Mobs and repairables go through damage so death/destruction events can run before the map cleanup.
        while (counter < _chunkSize && _toDamage.Count > 0)
        {
            var uid = _toDamage.Pop();
            _damageable.TryChangeDamage(uid, _damage, true);
            counter++;
        }

        while (counter < _chunkSize && _toDelete.Count > 0)
        {
            var uid = _toDelete.Pop();
            _entity.TryQueueDeleteEntity(uid);
            counter++;
        }

        // Register the second loop.
        if (_toDamage.Count == 0 && _toDelete.Count == 0 && _currentlyNuking.Count > 0)
        {
            var (mapId, spareShelters) = _currentlyNuking.Pop();
            MarkForDestruction(mapId, spareShelters);
        }
    }

    private void DestroySpecialBuildings(MapId mapId)
    {
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

    private bool IsSheltered(EntityUid uid, TransformComponent transform)
    {
        return _shelter.HasComp(uid) || _shelter.HasComp(transform.ParentUid);
    }

    private void AddNukeTarget(EntityUid uid)
    {
        if (_mobState.IsDead(uid))
            return;

        if (HasComp<MobStateComponent>(uid) || _repairable.HasComp(uid) || _apc.HasComp(uid))
            _toDamage.Add(uid);
        else
            _toDelete.Add(uid);
    }

    public void NukeMap(MapId mapId, bool spareShelters)
    {
        _currentlyNuking.Add((mapId, spareShelters));
        DestroySpecialBuildings(mapId);
        MarkForDestruction(mapId, spareShelters);
    }

    public override void Update(float frameTime)
    {
        ProcessChunk();
    }
}
