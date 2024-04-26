using System.Linq;
using System.Runtime.InteropServices;
using Content.Server._CM14.Marines;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles.Jobs;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Marines.HyperSleep;
using Content.Shared._CM14.Marines.Squads;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._CM14.Rules;

public sealed class CMRuleSystem : GameRuleSystem<CMRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly XenoHiveSystem _hive = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MarineSystem _marines = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnRulePlayerSpawning);
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning,
            before: [typeof(ArrivalsSystem), typeof(SpawnPointSystem)]);

        SubscribeLocalEvent<MarineComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MarineComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<XenoComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<XenoComponent, ComponentRemove>(OnCompRemove);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, "Distress Signal");
    }

    private void OnRulePlayerSpawning(RulePlayerSpawningEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            comp.Hive = Spawn(comp.HiveId);
            if (!SpawnXenoMap((uid, comp)))
            {
                Log.Error("Failed to load xeno map");
                continue;
            }

            var totalXenos = ev.PlayerPool.Count / comp.PlayersPerXeno;
            var profiles = ev.Profiles.ToDictionary();

            // TODO CM14 preferences
            // Xenos
            for (var i = 0; i < totalXenos; i++)
            {
                var player = _random.PickAndTake(ev.PlayerPool);
                profiles.Remove(player.UserId);
                GameTicker.PlayerJoinGame(player);

                // TODO CM14 xeno spawn points
                var xenoEnt = Spawn("CMXenoDrone", comp.XenoMap.ToCoordinates());

                _xeno.MakeXeno(xenoEnt);
                _xeno.SetHive(xenoEnt, comp.Hive);

                // TODO CM14 mind name
                var mind = _mind.GetOrCreateMind(player.UserId);
                _mind.TransferTo(mind, xenoEnt);
            }

            // Marines
            // var marineSpawners = GetSpawners();
            // var lastSquadSpawners = new Dictionary<EntProtoId, EntityUid>();
            // var lastJobSpawners = new Dictionary<EntProtoId, EntityUid>();
            // var spawnableStations = GetSpawnableStations();
            // var assignedJobs = _stationJobs.AssignJobs(profiles, spawnableStations);
            // var playerIds = ev.PlayerPool.Select(p => p.UserId).ToList();
            // _stationJobs.AssignOverflowJobs(ref assignedJobs, playerIds, profiles, spawnableStations);
            //
            // if (comp.SquadIds.Count > 0)
            // {
            //     // TODO CM14 make this support multi-station
            //     foreach (var (playerId, (assignedJobId, _)) in assignedJobs)
            //     {
            //         if (assignedJobId == null)
            //             continue;
            //
            //         if (!_prototypes.TryIndex(assignedJobId, out JobPrototype? job))
            //             continue;
            //
            //         EntityUid spawner;
            //         if (job.HasSquad)
            //         {
            //             if (comp.NextSquad >= comp.SquadIds.Count)
            //                 comp.NextSquad = 0;
            //
            //             var nextSquad = comp.SquadIds[comp.NextSquad++];
            //             ref var squad = ref CollectionsMarshal.GetValueRefOrAddDefault(comp.Squads, nextSquad, out var exists);
            //             if (!exists)
            //                 squad = Spawn(nextSquad);
            //
            //             if (marineSpawners.Squad.TryGetValue(nextSquad, out var squadSpawners) &&
            //                 squadSpawners.TryGetValue(job.ID, out var spawners) &&
            //                 spawners.Count > 0)
            //             {
            //                 spawner = _random.PickAndTake(spawners);
            //                 lastSquadSpawners[nextSquad] = spawner;
            //             }
            //             else if (!lastSquadSpawners.TryGetValue(nextSquad, out spawner))
            //             {
            //                 Log.Error($"No valid spawn found for player {playerId}. Squad: {nextSquad}, job: {assignedJobId}");
            //
            //                 if (lastSquadSpawners.Count > 0)
            //                     spawner = lastSquadSpawners.First().Value;
            //                 else
            //                     continue;
            //             }
            //         }
            //         else
            //         {
            //             if (marineSpawners.NonSquad.TryGetValue(job.ID, out var spawners))
            //             {
            //                 spawner = _random.PickAndTake(spawners);
            //                 lastJobSpawners[job.ID] = spawner;
            //             }
            //             else
            //             {
            //                 Log.Error($"No valid spawn found for player {playerId}. Job: {assignedJobId}");
            //
            //                 if (lastJobSpawners.Count > 0)
            //                     spawner = lastJobSpawners.First().Value;
            //                 else
            //                     continue;
            //             }
            //         }
            //
            //         EntityUid playerEnt;
            //         if (TryComp(spawner, out HyperSleepChamberComponent? hyperSleep))
            //         {
            //             playerEnt = SpawnInContainerOrDrop();
            //         }
            //
            //         SpriteSpecifier? icon = null;
            //         var mindId = _mind.GetMind(playerEnt);
            //         if (_prototypes.TryIndex(job.Icon, out StatusIconPrototype? jobIcon))
            //             icon = jobIcon.Icon;
            //
            //         _marines.MakeMarine(playerEnt, icon);
            //
            //         if (_player.TryGetSessionById(playerId, out var session))
            //             _playTime.PlayerRolesChanged(session);
            //
            //         if (job.HasSquad)
            //             _squad.SetSquad(playerEnt, squad);
            //     }
            // }

            // TODO CM14 other roles
        }
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.Job?.Prototype is not { } jobId ||
            !_prototypes.TryIndex(jobId, out var job) ||
            !job.IsCM)
        {
            return;
        }

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            if (GetSpawner(comp, job) is not { } spawnerInfo)
                return;

            var (spawner, squad) = spawnerInfo;
            if (TryComp(spawner, out HyperSleepChamberComponent? hyperSleep) &&
                _containers.TryGetContainer(spawner, hyperSleep.ContainerId, out var container))
            {
                ev.SpawnResult = _stationSpawning.SpawnPlayerMob(spawner.ToCoordinates(), ev.Job, ev.HumanoidCharacterProfile, ev.Station);
                _containers.Insert(ev.SpawnResult.Value, container);
            }
            else
            {
                var coordinates = _transform.GetMoverCoordinates(spawner);
                ev.SpawnResult = _stationSpawning.SpawnPlayerMob(coordinates, ev.Job, ev.HumanoidCharacterProfile, ev.Station);
            }

            SpriteSpecifier? icon = null;
            if (job.HasIcon && _prototypes.TryIndex(job.Icon, out StatusIconPrototype? jobIcon))
                icon = jobIcon.Icon;

            _marines.MakeMarine(ev.SpawnResult.Value, icon);

            if (squad != null)
            {
                _squad.SetSquad(ev.SpawnResult.Value, squad.Value, ev.Job);

                // TODO CM14 add this to the map file
                if (TryComp(spawner, out TransformComponent? xform) &&
                    xform.GridUid != null)
                {
                    EnsureComp<AlmayerComponent>(xform.GridUid.Value);
                }
            }

            return;
        }
    }

    private void OnMobStateChanged<T>(Entity<T> ent, ref MobStateChangedEvent args) where T : IComponent?
    {
        if (args.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    private void OnCompRemove<T>(Entity<T> ent, ref ComponentRemove args) where T : IComponent?
    {
        CheckRoundShouldEnd();
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var cmRule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var xenos = EntityQueryEnumerator<XenoComponent, MobStateComponent, TransformComponent>();
            var xenosAlive = false;
            var xenosOnShip = false;
            while (xenos.MoveNext(out var xenoId, out var xeno, out var mobState, out var xform))
            {
                if (_mobState.IsAlive(xenoId, mobState))
                    xenosAlive = true;

                if (HasComp<AlmayerComponent>(xform.GridUid))
                    xenosOnShip = true;

                if (xenosAlive && xenosOnShip)
                    break;
            }

            var marines = EntityQueryEnumerator<MarineComponent, MobStateComponent, TransformComponent>();
            var marinesAlive = false;
            var marinesOnShip = false;
            while (marines.MoveNext(out var marineId, out var marine, out var mobState, out var xform))
            {
                if (_mobState.IsAlive(marineId, mobState))
                    marinesAlive = true;

                if (HasComp<AlmayerComponent>(xform.GridUid))
                    marinesOnShip = true;

                if (marinesAlive && marinesOnShip)
                    break;
            }

            if (!xenosAlive && !marinesAlive)
            {
                _roundEnd.EndRound();
                return;
            }

            if (cmRule.XenosEverOnShip)
            {
                if (xenosAlive && !marinesOnShip)
                {
                    // TODO CM14 major xeno victory
                    _roundEnd.EndRound();
                    return;
                }

                if (!xenosAlive || !xenosOnShip)
                {
                    // TODO CM14 minor xeno victory
                    _roundEnd.EndRound();
                    return;
                }
            }
            else
            {
                if (xenosAlive && !marinesAlive)
                {
                    // TODO CM14 major xeno victory
                    _roundEnd.EndRound();
                    return;
                }

                if (!xenosAlive && marinesAlive)
                {
                    // TODO CM14 major marine victory
                    _roundEnd.EndRound();
                    return;
                }
            }

            // TODO CM14 no queen minor marine victory
        }
    }

    private bool SpawnXenoMap(Entity<CMRuleComponent> rule)
    {
        var mapId = _mapManager.CreateMap();
        _console.ExecuteCommand($"planet {mapId} Grasslands");
        rule.Comp.XenoMap = _mapManager.GetMapEntityIdOrThrow(mapId);
        return true;
    }

    private Spawners GetSpawners()
    {
        var spawners = new Spawners();
        var squadQuery = EntityQueryEnumerator<SquadSpawnerComponent>();
        while (squadQuery.MoveNext(out var uid, out var spawner))
        {
            if (TryComp(uid, out HyperSleepChamberComponent? hyperSleep) &&
                _containers.TryGetContainer(uid, hyperSleep.ContainerId, out var container) &&
                container.Count > 0)
            {
                if (spawner.Role == null)
                    spawners.SquadAnyFull.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.SquadFull.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
            }
            else
            {
                if (spawner.Role == null)
                    spawners.SquadAny.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.Squad.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
            }
        }

        var nonSquadQuery = EntityQueryEnumerator<SpawnPointComponent>();
        while (nonSquadQuery.MoveNext(out var uid, out var spawner))
        {
            if (spawner.Job == null)
                continue;

            if (TryComp(uid, out HyperSleepChamberComponent? hyperSleep) &&
                _containers.TryGetContainer(uid, hyperSleep.ContainerId, out var container) &&
                container.Count > 0)
            {
                spawners.NonSquadFull.GetOrNew(spawner.Job.ID).Add(uid);
            }
            else
            {
                spawners.NonSquad.GetOrNew(spawner.Job.ID).Add(uid);
            }
        }

        return spawners;
    }

    private (EntProtoId Id, EntityUid Ent) NextSquad(CMRuleComponent rule)
    {
        if (rule.NextSquad >= rule.SquadIds.Count)
            rule.NextSquad = 0;

        var id = rule.SquadIds[rule.NextSquad++];
        ref var squad = ref CollectionsMarshal.GetValueRefOrAddDefault(rule.Squads, id, out var exists);
        if (!exists)
            squad = Spawn(id);

        return (id, squad);
    }

    private (EntityUid Spawner, EntityUid? Squad)? GetSpawner(CMRuleComponent rule, JobPrototype job)
    {
        var allSpawners = GetSpawners();
        EntityUid? squad = null;

        if (job.HasSquad)
        {
            var (squadId, squadEnt) = NextSquad(rule);
            squad = squadEnt;

            if (allSpawners.Squad.TryGetValue(squadId, out var jobSpawners) &&
                jobSpawners.TryGetValue(job.ID, out var spawners))
            {
                return (_random.Pick(spawners), squadEnt);
            }

            if (allSpawners.SquadAny.TryGetValue(squadId, out var anySpawners))
                return (_random.Pick(anySpawners), squadEnt);

            if (allSpawners.SquadFull.TryGetValue(squadId, out jobSpawners) &&
                jobSpawners.TryGetValue(job.ID, out spawners))
            {
                return (_random.Pick(spawners), squadEnt);
            }

            if (allSpawners.SquadAnyFull.TryGetValue(squadId, out anySpawners))
                return (_random.Pick(anySpawners), squadEnt);

            Log.Error($"No valid spawn found for player. Squad: {squadId}, job: {job.ID}");

            if (allSpawners.NonSquad.TryGetValue(job.ID, out spawners))
                return (_random.Pick(spawners), null);

            if (allSpawners.NonSquadFull.TryGetValue(job.ID, out spawners))
                return (_random.Pick(spawners), null);

            Log.Error($"No valid spawn found for player. Job: {job.ID}");
        }
        else
        {
            if (allSpawners.NonSquad.TryGetValue(job.ID, out var spawners))
                return (_random.Pick(spawners), null);

            if (allSpawners.NonSquadFull.TryGetValue(job.ID, out spawners))
                return (_random.Pick(spawners), null);

            Log.Error($"No valid spawn found for player. Job: {job.ID}");
        }

        var pointsQuery = EntityQueryEnumerator<SpawnPointComponent>();
        var jobPoints = new List<EntityUid>();
        var anyJobPoints = new List<EntityUid>();
        var latePoints = new List<EntityUid>();

        while (pointsQuery.MoveNext(out var uid, out var point))
        {
            if (point.SpawnType == SpawnPointType.Job)
            {
                if (point.Job?.ID == job.ID)
                    jobPoints.Add(uid);
                else
                    anyJobPoints.Add(uid);
            }

            if (point.SpawnType == SpawnPointType.LateJoin)
                latePoints.Add(uid);
        }

        if (jobPoints.Count > 0)
            return (_random.Pick(jobPoints), squad);

        if (anyJobPoints.Count > 0)
            return (_random.Pick(anyJobPoints), squad);

        if (latePoints.Count > 0)
            return (_random.Pick(latePoints), squad);

        return null;
    }
}

public sealed class Spawners
{
    public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> Squad = new();
    public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAny = new();
    public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> SquadFull = new();
    public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAnyFull = new();
    public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquad = new();
    public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquadFull = new();
}
