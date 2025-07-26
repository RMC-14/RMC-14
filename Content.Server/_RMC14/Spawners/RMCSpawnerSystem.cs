using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Humanoid.Systems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Intel;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Random.Helpers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Spawners;

public sealed class RMCSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;

    private readonly Dictionary<EntProtoId, List<Entity<ProportionalSpawnerComponent>>> _spawners = new();
    private readonly Dictionary<EntProtoId, List<Entity<ItemPoolSpawnerComponent>>> _itemPools = new();
    private readonly List<Entity<CorpseSpawnerComponent>> _corpseSpawners = new();
    private readonly List<Entity<AegisSpawnerComponent>> _aegisSpawners = new();

    private int _maxCorpses;
    private int _corpsesSpawned;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipLaunchedFromWarshipEvent>(OnDropshipLaunchedFromWarship);
        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<GunSpawnerComponent, MapInitEvent>(OnGunSpawnMapInit);
        SubscribeLocalEvent<RandomTimedDespawnComponent, MapInitEvent>(OnTimedDespawnMapInit);

        Subs.CVar(_config, RMCCVars.RMCSpawnerMaxCorpses, v => _maxCorpses = v, true);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _corpsesSpawned = 0;
    }

    private void OnDropshipLaunchedFromWarship(ref DropshipLaunchedFromWarshipEvent ev)
    {
        var deleteQuery = EntityQueryEnumerator<DeleteOnDropshipLaunchFromWarshipComponent>();
        while (deleteQuery.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }
    }

    private void OnDropshipLandedOnPlanet(ref DropshipLandedOnPlanetEvent ev)
    {
        var timedQuery = EntityQueryEnumerator<TimedDespawnOnLandingComponent>();
        while (timedQuery.MoveNext(out var uid, out var comp))
        {
            StartDespawnOnLanding((uid, comp));
        }

        var deleteQuery = EntityQueryEnumerator<DeleteOnLandingComponent>();
        while (deleteQuery.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }
    }

    private void OnGunSpawnMapInit(Entity<GunSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Prototypes.Count <= 0)
            return;

        var (gunID, ammoID) = _random.Pick(ent.Comp.Prototypes);

        var entitesToSpawn = new Dictionary<EntProtoId, int>()
        {
            [gunID] = 1,
            [ammoID] = _random.Next(ent.Comp.MinMagazines, ent.Comp.MaxMagazines)
        };

        if (_random.Prob(ent.Comp.ChanceToSpawn))
        {
            foreach ((var protoID, var amount) in entitesToSpawn)
            {
                for (var i = 0; i < amount; i++) // spawn in the amount of entities
                {
                    var offset = ent.Comp.Offset;
                    var xOffset = _random.NextFloat(-offset, offset);
                    var yOffset = _random.NextFloat(-offset, offset); // Offset it randomly
                    var coordinates = _transform.ToMapCoordinates(ent.Owner.ToCoordinates()).Offset(new Vector2(xOffset, yOffset));
                    Spawn(protoID, coordinates);
                }
            }
        }

        if (ent.Comp.DeleteAfterSpawn)
            QueueDel(ent.Owner);
    }

    private void OnTimedDespawnMapInit(Entity<RandomTimedDespawnComponent> ent, ref MapInitEvent args)
    {
        var time = ent.Comp.Min;
        if (ent.Comp.Max > TimeSpan.Zero)
            time = _random.Next(ent.Comp.Min, ent.Comp.Max + TimeSpan.FromSeconds(1));

        EnsureComp<TimedDespawnComponent>(ent).Lifetime = (float)time.TotalSeconds;
    }

    public void StartDespawnOnLanding(Entity<TimedDespawnOnLandingComponent> landing)
    {
        EnsureComp<TimedDespawnComponent>(landing).Lifetime = landing.Comp.Lifetime;
        RemCompDeferred<TimedDespawnOnLandingComponent>(landing);
    }

    public override void Update(float frameTime)
    {
        _spawners.Clear();
        _itemPools.Clear();
        _corpseSpawners.Clear();
        _aegisSpawners.Clear();

        var roundDuration = _gameTicker.RoundDuration();

        var timedQuery = EntityQueryEnumerator<TimedDespawnOnLandingComponent>();
        while (timedQuery.MoveNext(out var uid, out var comp))
        {
            if (roundDuration >= comp.StartDespawnAt)
                StartDespawnOnLanding((uid, comp));
        }

        var corpseSpawnersQuery = EntityQueryEnumerator<CorpseSpawnerComponent>();
        while (corpseSpawnersQuery.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                continue;

            QueueDel(uid);
            _corpseSpawners.Add((uid, comp));
        }

        foreach (var spawner in _corpseSpawners)
        {
            if (_corpsesSpawned >= _maxCorpses)
                continue;

            _corpsesSpawned++;
            var corpse = _randomHumanoid.SpawnRandomHumanoid(spawner.Comp.Spawn, _transform.GetMoverCoordinates(spawner), MetaData(spawner).EntityName);
            EnsureComp<IntelRecoverCorpseObjectiveComponent>(corpse);
        }

        var proportional = EntityQueryEnumerator<ProportionalSpawnerComponent>();
        while (proportional.MoveNext(out var uid, out var comp))
        {
            _spawners.GetOrNew(comp.Id).Add((uid, comp));
        }

        if (_spawners.Count > 0)
        {
            var players = _gameTicker.PlayersJoinedRoundNormally;
            foreach (var spawners in _spawners.Values)
            {
                _random.Shuffle(spawners);

                var spawned = 0;
                foreach (var spawner in spawners)
                {
                    var coordinates = _transform.ToMapCoordinates(spawner.Owner.ToCoordinates());
                    QueueDel(spawner);

                    var max = Math.Max(1, players / spawner.Comp.Ratio);
                    if (max <= spawned)
                        continue;

                    foreach (var spawn in spawner.Comp.Prototypes)
                    {
                        Spawn(spawn, coordinates);
                    }

                    spawned++;
                }
            }
        }

        var itemPools = EntityQueryEnumerator<ItemPoolSpawnerComponent>();
        while (itemPools.MoveNext(out var uid, out var comp))
        {
            _itemPools.GetOrNew(comp.Id).Add((uid, comp));
        }

        if (_itemPools.Count > 0)
        {
            foreach (var items in _itemPools.Values)
            {
                _random.Shuffle(items);

                var spawned = 0;
                foreach (var item in items)
                {
                    var coordinates = _transform.ToMapCoordinates(item.Owner.ToCoordinates());
                    QueueDel(item);

                    if (item.Comp.Quota <= spawned)
                        continue;

                    foreach (var spawn in item.Comp.Prototypes)
                    {
                        Spawn(spawn, coordinates);
                    }

                    spawned++;
                }
            }
        }
    }
}
