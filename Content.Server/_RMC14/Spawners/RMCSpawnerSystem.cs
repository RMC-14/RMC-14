using Content.Server.GameTicking;
using Content.Server.Humanoid.Systems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Intel;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
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
    private readonly List<Entity<CorpseSpawnerComponent>> _corpseSpawners = new();

    private int _maxCorpses;
    private int _corpsesSpawned;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipLaunchedFromWarshipEvent>(OnDropshipLaunchedFromWarship);
        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

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

    private void OnTimedDespawnMapInit(Entity<RandomTimedDespawnComponent> ent, ref MapInitEvent args)
    {
        var time = ent.Comp.Min;
        if (ent.Comp.Max > TimeSpan.Zero)
            time = _random.Next(ent.Comp.Min, ent.Comp.Max + TimeSpan.FromSeconds(1));

        EnsureComp<TimedDespawnComponent>(ent).Lifetime = (float) time.TotalSeconds;
    }

    public void StartDespawnOnLanding(Entity<TimedDespawnOnLandingComponent> landing)
    {
        EnsureComp<TimedDespawnComponent>(landing).Lifetime = landing.Comp.Lifetime;
        RemCompDeferred<TimedDespawnOnLandingComponent>(landing);
    }

    public override void Update(float frameTime)
    {
        _spawners.Clear();
        _corpseSpawners.Clear();

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

        if (_spawners.Count == 0)
            return;

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
}
