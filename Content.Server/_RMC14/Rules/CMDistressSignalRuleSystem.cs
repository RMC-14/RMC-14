using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.MapInsert;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Power;
using Content.Server._RMC14.Stations;
using Content.Server._RMC14.Xenonids.Hive;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Fax;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Stunnable;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.Armor.Ghillie;
using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RMC14.Bioscan;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.HyperSleep;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Scaling;
using Content.Shared._RMC14.Spawners;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Actions;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StatusEffect;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Rules;

public sealed class CMDistressSignalRuleSystem : GameRuleSystem<CMDistressSignalRuleComponent>
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly ARESSystem _ares = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly DropshipSystem _dropship = default!;
    [Dependency] private readonly FaxSystem _fax = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly XenoHiveSystem _hive = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ItemCamouflageSystem _camo = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly RMCStationJobsSystem _rmcStationJobs = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ScalingSystem _scaling = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    [Dependency] private readonly XenoTunnelSystem _xenoTunnel = default!;
    [Dependency] private readonly StunSystem _stuns = default!;
    [Dependency] private readonly RMCCameraShakeSystem _rmcCameraShake = default!;
    [Dependency] private readonly ThermalCloakSystem _thermalCloak = default!;
    [Dependency] private readonly SharedGhillieSuitSystem _ghillieSuit = default!;
    [Dependency] private readonly MapInsertSystem _mapInsert = default!;
    [Dependency] private readonly SharedDestructibleSystem _destruction = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;
    [Dependency] private readonly RMCAmbientLightSystem _rmcAmbientLight = default!;
    [Dependency] private readonly RMCPowerSystem _rmcPower = default!;
    [Dependency] private readonly RMCGameRuleExtrasSystem _gameRulesExtras = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private readonly HashSet<string> _operationNames = new();
    private readonly HashSet<string> _operationPrefixes = new();
    private readonly HashSet<string> _operationSuffixes = new();

    private float _marinesPerXeno;
    private bool _autoBalance;
    private float _autoBalanceStep;
    private float _autoBalanceMin;
    private float _autoBalanceMax;
    private float _marinesPerSurvivor;
    private float _maximumSurvivors;
    private float _minimumSurvivors;
    private string _adminFaxAreaMap = string.Empty;
    private int _mapVoteExcludeLast;
    private bool _useCarryoverVoting;
    private readonly TimeSpan _hijackStunTime = TimeSpan.FromSeconds(5);
    private bool _landingZoneMiasmaEnabled;
    private TimeSpan _sunsetDuration;
    private TimeSpan _sunriseDuration;
    private TimeSpan _forceEndHijackTime;
    private float _hijackShipWeight;
    private int _hijackMinBurrowed;
    private int _xenosMinimum;
    private bool _usingCustomOperationName;
    private bool _queenBuildingBoostEnabled;
    private TimeSpan _queenBoostDuration;
    private float _queenBoostSpeedMultiplier;
    private float _queenBoostRemoteRange;


    private readonly List<MapId> _almayerMaps = [];
    private readonly List<EntityUid> _marineList = [];

    private EntityQuery<HyperSleepChamberComponent> _hyperSleepChamberQuery;
    private EntityQuery<XenoNestedComponent> _xenoNestedQuery;

    private readonly Queue<EntProtoId<RMCPlanetMapPrototypeComponent>> _lastPlanetMaps = new();

    [ViewVariables]
    private RMCPlanet? SelectedPlanetMap { get; set; }

    [ViewVariables]
    public string? SelectedPlanetMapName => SelectedPlanetMap?.Proto.Name;

    [ViewVariables]
    public string? OperationName { get; private set; }

    public string? ActiveNightmareScenario { get; set; }

    private readonly Dictionary<EntProtoId<RMCPlanetMapPrototypeComponent>, int> _carryoverVotes = new();

    private IVoteHandle? _currentVote;

    public override void Initialize()
    {
        base.Initialize();

        _hyperSleepChamberQuery = GetEntityQuery<HyperSleepChamberComponent>();
        _xenoNestedQuery = GetEntityQuery<XenoNestedComponent>();

        SubscribeLocalEvent<LoadingMapsEvent>(OnMapLoading);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnRulePlayerSpawning);
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning,
            before: [typeof(ArrivalsSystem), typeof(SpawnPointSystem)]);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEndMessage);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<DropshipHijackStartEvent>(OnDropshipHijackStart);
        SubscribeLocalEvent<DropshipHijackLandedEvent>(OnDropshipHijackLanded);

        SubscribeLocalEvent<MarineComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MarineComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<XenoComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<XenoComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<XenoEvolutionGranterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenoComponent, ComponentInit>(OnXenoComponentInit);
        SubscribeLocalEvent<HiveMemberComponent, HiveChangedEvent>(OnHiveChanged);

        Subs.CVar(_config, RMCCVars.CMMarinesPerXeno, v => _marinesPerXeno = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalance, v => _autoBalance = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalanceStep, v => _autoBalanceStep = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalanceMax, v => _autoBalanceMax = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalanceMin, v => _autoBalanceMin = v, true);
        Subs.CVar(_config, RMCCVars.RMCMarinesPerSurvivor, v => _marinesPerSurvivor = v, true);
        Subs.CVar(_config, RMCCVars.RMCSurvivorsMaximum, v => _maximumSurvivors = v, true);
        Subs.CVar(_config, RMCCVars.RMCSurvivorsMinimum, v => _minimumSurvivors = v, true);
        Subs.CVar(_config, RMCCVars.RMCAdminFaxAreaMap, v => _adminFaxAreaMap = v, true);
        Subs.CVar(_config, RMCCVars.RMCPlanetMapVoteExcludeLast, v => _mapVoteExcludeLast = v, true);
        Subs.CVar(_config, RMCCVars.RMCUseCarryoverVoting, v => _useCarryoverVoting = v, true);
        Subs.CVar(_config, RMCCVars.RMCLandingZoneMiasmaEnabled, v => _landingZoneMiasmaEnabled = v, true);
        Subs.CVar(_config, RMCCVars.RMCSunsetDuration, v => _sunsetDuration = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCSunriseDuration, v => _sunriseDuration = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCForceEndHijackTimeMinutes, v => _forceEndHijackTime = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCHijackShipWeight, v => _hijackShipWeight = v, true);
        Subs.CVar(_config, RMCCVars.RMCMinimumHijackBurrowed, v => _hijackMinBurrowed = v, true);
        Subs.CVar(_config, RMCCVars.RMCDistressXenosMinimum, v => _xenosMinimum = v, true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoost, v => _queenBuildingBoostEnabled = v, true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoostDurationMinutes, v => _queenBoostDuration = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoostSpeedMultiplier, v => _queenBoostSpeedMultiplier = v, true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoostRemoteRange, v => _queenBoostRemoteRange = v, true);

        ReloadPrototypes();
    }
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadPrototypes();
    }

    private void OnMapLoading(LoadingMapsEvent ev)
    {
        SelectRandomPlanet();
        //Just in case the info text is not updated previousely
        GameTicker.UpdateInfoText();
    }

    private void OnRulePlayerSpawning(RulePlayerSpawningEvent ev)
    {
        var spawnedDropships = false;
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            OperationName = GetRandomOperationName();

            // TODO: come up with random name like operation name, in a function that can be reused for hive v hive
            comp.Hive = _hive.CreateHive("xenonid hive", comp.HiveId);
            if (comp.SpawnPlanet && !SpawnXenoMap((uid, comp)))
            {
                Log.Error("Failed to load xeno map");
                // TODO: how should the gamemode handle failure? restart immediately or create an alert for admins
                continue;
            }

            _intel.RunSpawners();

            SetFriendlyHives(comp.Hive);

            if (comp.XenoMap != null)
                UnpowerFaxes(_transform.GetMapId(comp.XenoMap.Value));

            SetCamoType();

            SpawnSquads((uid, comp));
            SpawnAdminAreas(comp);

            if (comp.Bioscan)
            {
                var bioscan = Spawn(null, MapCoordinates.Nullspace);
                EnsureComp<BioscanComponent>(bioscan);
            }

            var xenoSpawnPoints = new List<EntityUid>();
            var spawnQuery = AllEntityQuery<XenoSpawnPointComponent>();
            while (spawnQuery.MoveNext(out var spawnUid, out _))
            {
                xenoSpawnPoints.Add(spawnUid);
            }

            var xenoLeaderSpawnPoints = new List<EntityUid>();
            var leaderSpawnQuery = AllEntityQuery<XenoLeaderSpawnPointComponent>();
            while (leaderSpawnQuery.MoveNext(out var spawnUid, out _))
            {
                xenoLeaderSpawnPoints.Add(spawnUid);
            }

            bool IsAllowed(NetUserId id, ProtoId<JobPrototype> role)
            {
                if (!_player.TryGetSessionById(id, out var player))
                    return false;

                var jobBans = _bans.GetJobBans(player.UserId);
                if (jobBans == null || jobBans.Contains(role))
                    return false;

                if (!_playTime.IsAllowed(player, role))
                    return false;

                return true;
            }

            NetUserId? SpawnXeno(List<NetUserId> list, EntProtoId ent, bool doBurst = false)
            {
                var playerId = _random.PickAndTake(list);
                if (!_player.TryGetSessionById(playerId, out var player))
                {
                    Log.Error($"Failed to find player with id {playerId} during xeno selection.");
                    return null;
                }

                ev.PlayerPool.Remove(player);
                GameTicker.PlayerJoinGame(player);
                var xenoEnt = SpawnXenoEnt(ent, player, doBurst);

                if (!_mind.TryGetMind(playerId, out var mind))
                    mind = _mind.CreateMind(playerId);

                _mind.TransferTo(mind.Value, xenoEnt);
                return playerId;
            }

            var survivorSpawners = new Dictionary<ProtoId<JobPrototype>, List<EntityUid>>();
            var spawnerQuery = EntityQueryEnumerator<SpawnPointComponent>();
            while (spawnerQuery.MoveNext(out var spawnId, out var spawnComp))
            {
                if (spawnComp.Job is not { } job)
                    continue;

                if (comp.SurvivorJobs.Any(t => t.Job == spawnComp.Job))
                    survivorSpawners.GetOrNew(job).Add(spawnId);
            }

            // TODO RMC14 remove defaulting to civ survivor spawners
            foreach (var (job, spawners) in survivorSpawners)
            {
                if (job == comp.CivilianSurvivorJob)
                    continue;

                if (survivorSpawners.TryGetValue(comp.CivilianSurvivorJob, out var civSpawners))
                {
                    spawners.AddRange(civSpawners);
                }
            }

            if (SelectedPlanetMap != null)
            {
                if (SelectedPlanetMap.Value.Comp.SurvivorJobs != null)
                    comp.SurvivorJobs = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobs, notNullableOverride: true);

                comp.SurvivorJobInserts = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobInserts);
                comp.SurvivorJobOverrides = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobOverrides);
            }

            var survivorSpawnersLeft = new Dictionary<ProtoId<JobPrototype>, List<EntityUid>>();
            foreach (var (job, jobSpawners) in survivorSpawners)
            {
                survivorSpawnersLeft[job] = jobSpawners;
            }

            NetUserId? SpawnSurvivor(ProtoId<JobPrototype> job, List<NetUserId> list, out bool stop)
            {
                stop = false;
                if (!survivorSpawnersLeft.TryGetValue(job, out var jobSpawnersLeft) &&
                    !survivorSpawnersLeft.TryGetValue(comp.CivilianSurvivorJob, out jobSpawnersLeft))
                {
                    stop = true;
                    return null;
                }

                if (jobSpawnersLeft.Count == 0)
                {
                    if (survivorSpawners.TryGetValue(job, out var jobSpawners))
                        jobSpawnersLeft.AddRange(jobSpawners);

                    if (jobSpawnersLeft.Count == 0)
                    {
                        stop = true;
                        return null;
                    }
                }

                var spawnAsJob = job;
                var selectRandomInsert = SelectedPlanetMap?.Comp.SelectRandomSurvivorInsert ?? false;

                var playerId = _random.Pick(list);
                if (!_player.TryGetSessionById(playerId, out var player))
                {
                    Log.Error($"Failed to find player with id {playerId} during survivor selection.");
                    return null;
                }

                // select an insert in order, reducing the slot of that insert
                if (comp.SurvivorJobInserts != null && comp.SurvivorJobInserts.TryGetValue(job, out var insert) && !selectRandomInsert)
                {
                    var insertSuccess = false;

                    for (var i = 0; i < insert.Count; i++)
                    {
                        var (insertJob, amount) = insert[i];

                        if (!IsAllowed(playerId, insertJob))
                            continue; // check insert playtime requirements

                        if (amount == -1)
                        {
                            spawnAsJob = insertJob;
                            insertSuccess = true;
                            break;
                        }

                        if (amount <= 0)
                            continue;

                        insert[i] = (insertJob, amount - 1);
                        spawnAsJob = insertJob; // Override the original job with the insert
                        insertSuccess = true;
                        break;
                    }

                    if (!insertSuccess)
                    {
                        stop = true;
                        return null; // All insert slots are filled, do not allow job
                    }
                }

                for (var i = 0; i < comp.SurvivorJobs.Count; i++)
                {
                    var (survJob, amount) = comp.SurvivorJobs[i];
                    if (survJob != job)
                        continue;

                    if (selectRandomInsert) // select a random insert if there are any and if this map supports random inserts
                    {
                        if (comp.SurvivorJobInserts != null && comp.SurvivorJobInserts.TryGetValue(job, out var randomInsertList))
                            spawnAsJob = _random.Pick(randomInsertList).Insert;
                    }

                    if (amount == -1)
                        break;

                    if (amount <= 0)
                    {
                        stop = true;
                        return null;
                    }

                    comp.SurvivorJobs[i] = (survJob, amount - 1);
                }

                list.Remove(playerId); // remove the player from the pool because they passed the checks

                var spawner = _random.PickAndTake(jobSpawnersLeft);
                ev.PlayerPool.Remove(player);
                GameTicker.PlayerJoinGame(player);

                var profile = GameTicker.GetPlayerProfile(player);
                var coordinates = _transform.GetMoverCoordinates(spawner);
                var survivorMob = _stationSpawning.SpawnPlayerMob(coordinates, spawnAsJob, profile, null);

                if (!_mind.TryGetMind(playerId, out var mind))
                    mind = _mind.CreateMind(playerId);

                RemCompDeferred<TacticalMapUserComponent>(survivorMob);
                _mind.TransferTo(mind.Value, survivorMob);

                _roles.MindAddJobRole(mind.Value, jobPrototype: spawnAsJob);

                _playTime.PlayerRolesChanged(player);

                var spawnEv = new PlayerSpawnCompleteEvent(
                    survivorMob,
                    player,
                    spawnAsJob,
                    false,
                    true,
                    0,
                    default,
                    profile
                );
                RaiseLocalEvent(survivorMob, spawnEv, true);
                return playerId;
            }

            EntityUid SpawnXenoEnt(EntProtoId ent, ICommonSession player, bool doBurst = false)
            {
                var leader = _prototypes.TryIndex(ent, out var proto) &&
                             proto.TryGetComponent(out XenoComponent? xeno, _compFactory) &&
                             xeno.SpawnAtLeaderPoint;

                var point = _random.Pick(leader ? xenoLeaderSpawnPoints : xenoSpawnPoints);

                if (doBurst)
                {
                    var profile = GameTicker.GetPlayerProfile(player);
                    var coordinates = _transform.GetMoverCoordinates(point);
                    var corpseMob = _stationSpawning.SpawnPlayerMob(coordinates, comp.XenoSurvivorCorpseJob, profile, null);

                    var spawnEv = new PlayerSpawnCompleteEvent( // for ranks and such
                        corpseMob,
                        player,
                        comp.XenoSurvivorCorpseJob,
                        false,
                        true,
                        0,
                        default,
                        profile
                    );

                    RaiseLocalEvent(corpseMob, spawnEv, true);

                    var victimInfected = EnsureComp<VictimInfectedComponent>(corpseMob);
                    _parasite.SetBurstSpawn((corpseMob, victimInfected), ent);
                    _parasite.SetHive((corpseMob, victimInfected), comp.Hive);
                    _parasite.SpawnLarva((corpseMob, victimInfected), out var newXeno);
                    _parasite.SetBurstDelay((corpseMob, victimInfected), comp.XenoSurvivorCorpseBurstDelay);

                    _xeno.MakeXeno(newXeno);

                    _adminLog.Add(LogType.RMCXenoSpawn, $"Player {player} with mob {ToPrettyString(newXeno):xeno} spawned as a xeno from their corpse {ToPrettyString(corpseMob):corpse}");
                    return newXeno;
                }

                var xenoEnt = SpawnAtPosition(ent, point.ToCoordinates());

                _xeno.MakeXeno(xenoEnt);
                _hive.SetHive(xenoEnt, comp.Hive);
                return xenoEnt;
            }

            var totalXenos = (int) Math.Round(Math.Max(1, ev.PlayerPool.Count / _marinesPerXeno));
            var totalSurvivors = (int) Math.Round(ev.PlayerPool.Count / _marinesPerSurvivor);
            totalSurvivors = (int) Math.Clamp(totalSurvivors, _minimumSurvivors, _maximumSurvivors);
            var marines = ev.PlayerPool.Count - totalXenos - totalSurvivors;
            var jobSlotScaling = _config.GetCVar(RMCCVars.RMCJobSlotScaling);
            if (comp.DoJobSlotScaling && marines > 0 && jobSlotScaling)
            {
                var stations = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
                while (stations.MoveNext(out var stationId, out var stationJobs, out _))
                {
                    if (stationJobs.JobSlotScaling is not { } scalingProto)
                        continue;

                    // TODO RMC14 dont count survivors
                    if (!scalingProto.TryGet(out var scalingComp, _prototypes, _compFactory))
                        continue;

                    foreach (var (job, scaling) in scalingComp.Jobs)
                    {
                        var slots = _rmcStationJobs.GetSlots(marines, scaling.Factor, scaling.C, scaling.Min, scaling.Max);
                        if (scaling.Squad)
                            slots *= 4;

                        Log.Info($"Setting {job} to {slots} slots.");
                        var jobs = stationJobs.SetupAvailableJobs;
                        if (jobs.TryGetValue(job, out var available))
                        {
                            for (var i = 0; i < available.Length; i++)
                            {
                                available[i] = slots;
                            }
                        }

                        _stationJobs.TrySetJobSlot(stationId, job, slots, stationJobs: stationJobs);
                    }
                }
            }

            var priorities = Enum.GetValues<JobPriority>().Length;
            var xenoCandidates = new List<NetUserId>[priorities];
            for (var i = 0; i < xenoCandidates.Length; i++)
            {
                xenoCandidates[i] = [];
            }

            foreach (var (id, profile) in ev.Profiles)
            {
                if (!IsAllowed(id, comp.QueenJob))
                    continue;

                if (profile.JobPriorities.TryGetValue(comp.QueenJob, out var priority) &&
                    priority > JobPriority.Never)
                {
                    xenoCandidates[(int) priority].Add(id);
                }
            }

            if (comp.SpawnXenos)
            {
                NetUserId? queenSelected = null;
                for (var i = xenoCandidates.Length - 1; i >= 0; i--)
                {
                    var list = xenoCandidates[i];
                    while (list.Count > 0)
                    {
                        queenSelected = SpawnXeno(list, comp.QueenEnt);
                        if (queenSelected != null)
                            break;
                    }

                    if (queenSelected != null)
                    {
                        totalXenos--;
                        break;
                    }
                }

                foreach (var list in xenoCandidates)
                {
                    list.Clear();
                }

                foreach (var (id, profile) in ev.Profiles)
                {
                    if (id == queenSelected)
                        continue;

                    if (!IsAllowed(id, comp.XenoSelectableJob))
                        continue;

                    if (profile.JobPriorities.TryGetValue(comp.XenoSelectableJob, out var priority) &&
                        priority > JobPriority.Never)
                    {
                        xenoCandidates[(int) priority].Add(id);
                    }
                }

                var selectedXenos = 0;
                for (var i = xenoCandidates.Length - 1; i >= 0; i--)
                {
                    var list = xenoCandidates[i];
                    while (list.Count > 0 && selectedXenos < totalXenos)
                    {
                        if (SpawnXeno(list, comp.LarvaEnt, true) != null)
                            selectedXenos++;
                    }
                }

                // Any unfilled xeno slots become larva
                var unfilled = totalXenos - selectedXenos;
                if (unfilled > 0)
                    _hive.IncreaseBurrowedLarva(unfilled);
            }

            if (comp.SpawnSurvivors)
            {
                // Shuffle survivor jobs and ensure civilian survivor stays at the bottom, as civ survivor has infinite slots
                comp.SurvivorJobs = comp.SurvivorJobs
                    .Where(entry => entry.Job != comp.CivilianSurvivorJob)
                    .OrderBy(_ => _random.Next())
                    .Append(comp.SurvivorJobs.FirstOrDefault(entry => entry.Job == comp.CivilianSurvivorJob))
                    .ToList();

                var survivorCandidates = new Dictionary<ProtoId<JobPrototype>, List<NetUserId>[]>();
                foreach (var job in comp.SurvivorJobs)
                {
                    survivorCandidates[job.Job] = new List<NetUserId>[priorities];

                    var jobList = survivorCandidates[job.Job];
                    for (var i = 0; i < jobList.Length; i++)
                    {
                        jobList[i] = [];
                    }
                }

                foreach (var player in ev.PlayerPool)
                {
                    foreach (var (job, players) in survivorCandidates)
                    {
                        var id = player.UserId;
                        if (!IsAllowed(id, comp.CivilianSurvivorJob) || !IsAllowed(id, job))
                            continue;

                        if (!ev.Profiles.TryGetValue(id, out var profile))
                            continue;

                        var overriden = false;

                        if (comp.SurvivorJobOverrides != null)
                        { // Override the job
                            foreach (var (originalJob, overrideJob) in comp.SurvivorJobOverrides)
                            {
                                if (profile.JobPriorities.TryGetValue(originalJob, out var originalPriority) &&
                                    originalPriority > JobPriority.Never && overrideJob == job)
                                {
                                    players[(int)originalPriority].Add(id);
                                    overriden = true;
                                    break;
                                }
                            }
                        }

                        if (overriden)
                            continue;

                        if (profile.JobPriorities.TryGetValue(job, out var priority) &&
                            priority > JobPriority.Never)
                        {
                            players[(int)priority].Add(id);
                        }
                    }
                }

                var selectedSurvivors = 0;
                for (var i = priorities - 1; i >= 0; i--)
                {
                    foreach (var (job, players) in survivorCandidates)
                    {
                        var list = players[i];
                        var ignoreLimit = comp.IgnoreMaximumSurvivorJobs.Contains(job);
                        while (list.Count > 0 && (ignoreLimit || selectedSurvivors < totalSurvivors))
                        {
                            if (SpawnSurvivor(job, list, out var stop) is { } id)
                            {
                                foreach (var (_, otherPlayersLists) in survivorCandidates)
                                {
                                    foreach (var otherPlayers in otherPlayersLists)
                                    {
                                        otherPlayers.Remove(id);
                                    }
                                }

                                if (!ignoreLimit)
                                    selectedSurvivors++;
                            }

                            if (stop)
                                break;
                        }
                    }
                }
            }

            if (spawnedDropships)
                return;

            // don't open shitcode inside
            spawnedDropships = true;
            var dropshipMap = _mapManager.CreateMap();
            var dropshipPoints = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
            var ships = new[] { new ResPath("/Maps/_RMC14/alamo.yml"), new ResPath("/Maps/_RMC14/normandy.yml") };
            var shipIndex = 0;
            while (dropshipPoints.MoveNext(out var destinationId, out _, out var destTransform))
            {
                if (_mapSystem.TryGetMap(destTransform.MapID, out var destinationMapId) &&
                    comp.XenoMap == destinationMapId)
                {
                    continue;
                }

                if (!_mapLoader.TryLoadGrid(dropshipMap, ships[shipIndex], out var shipGrids, offset: new Vector2(shipIndex * 100, shipIndex * 100)))
                {
                    shipIndex++;

                    if (shipIndex >= ships.Length)
                        shipIndex = 0;

                    continue;
                }

                shipIndex++;
                if (shipIndex >= ships.Length)
                    shipIndex = 0;

                var computers = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
                while (computers.MoveNext(out var computerId, out var computer, out var xform))
                {
                    if (xform.GridUid != shipGrids.Value)
                        continue;

                    if (!_dropship.FlyTo((computerId, computer), destinationId, null, startupTime: 1f, hyperspaceTime: 1f))
                        continue;

                    break;
                }
            }

            var marineFactions = EntityQueryEnumerator<MarineIFFComponent>();
            while (marineFactions.MoveNext(out var iffId, out _))
            {
                _gunIFF.SetUserFaction(iffId, comp.MarineFaction);
            }

            var faxes = EntityQueryEnumerator<FaxMachineComponent>();
            while (faxes.MoveNext(out var faxId, out var faxComp))
            {
                _fax.Refresh(faxId, faxComp);
            }
        }
    }

    private void SpawnSquads(Entity<CMDistressSignalRuleComponent> rule)
    {
        foreach (var id in rule.Comp.SquadIds)
        {
            if (!rule.Comp.Squads.ContainsKey(id))
                rule.Comp.Squads[id] = Spawn(id);
        }

        foreach (var id in rule.Comp.ExtraSquadIds)
        {
            Spawn(id);
        }
    }

    public void SetCamoType(CamouflageType? ct = null)
    {
        if (ct != null)
        {
            _camo.CurrentMapCamouflage = ct.Value;
            return;
        }

        if (SelectedPlanetMap != null)
            _camo.CurrentMapCamouflage = SelectedPlanetMap.Value.Comp.Camouflage;
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.Job is not { } jobId ||
            !_prototypes.TryIndex(jobId, out var job) ||
            !job.IsCM)
        {
            return;
        }

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            var squadPreference = ev.HumanoidCharacterProfile?.SquadPreference;
            if (GetSpawner(comp, job, squadPreference) is not { } spawnerInfo)
                return;

            var (spawner, squad) = spawnerInfo;
            if (_hyperSleepChamberQuery.TryComp(spawner, out var hyperSleep) &&
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

            if (squad != null)
            {
                _squad.AssignSquad(ev.SpawnResult.Value, squad.Value, jobId);

                // TODO RMC14 add this to the map file
                if (TryComp(spawner, out TransformComponent? xform) &&
                    xform.GridUid != null)
                {
                    EnsureComp<AlmayerComponent>(xform.GridUid.Value);
                }

                if (comp.SetHunger && TryComp(ev.SpawnResult, out HungerComponent? hunger))
                    _hunger.SetHunger(ev.SpawnResult.Value, 50.0f, hunger);
            }

            return;
        }
    }

    private void OnRoundEndMessage(RoundEndMessageEvent ev)
    {
        var rules = QueryActiveRules();
        while (rules.MoveNext(out _, out var distress, out _))
        {
            if (distress.Result == DistressSignalRuleResult.None)
                continue;

            var audio = distress.Result switch
            {
                DistressSignalRuleResult.None => null,
                DistressSignalRuleResult.MajorMarineVictory => distress.MajorMarineAudio,
                DistressSignalRuleResult.MinorMarineVictory => distress.MinorMarineAudio,
                DistressSignalRuleResult.MajorXenoVictory => distress.MajorXenoAudio,
                DistressSignalRuleResult.MinorXenoVictory => distress.MinorXenoAudio,
                // DistressSignalRuleResult.AllDied => distress.AllDiedAudio,
                _ => null,
            };

            if (audio != null)
                _audio.PlayGlobal(_audio.GetSound(audio), Filter.Broadcast(), true, AudioParams.Default.WithVolume(-8));
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        StartPlanetVote();
        ResetSelectedPlanet();
        _config.SetCVar(CCVars.GameDisallowLateJoins, false);

        if (!_autoBalance)
            return;

        var rules = QueryAllRules();
        while (rules.MoveNext(out var comp, out _))
        {
            var adjust = comp.Result switch
            {
                DistressSignalRuleResult.None => 0,
                DistressSignalRuleResult.MajorMarineVictory => -1,
                DistressSignalRuleResult.MinorMarineVictory => -1,
                DistressSignalRuleResult.MajorXenoVictory => 1,
                DistressSignalRuleResult.MinorXenoVictory => 0, // hijack but all xenos die or timeout happens
                DistressSignalRuleResult.AllDied => 0,
                null => 0,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (adjust == 0)
                continue;

            var value = _marinesPerXeno;
            value += adjust * _autoBalanceStep;

            if (value > _autoBalanceMax)
                value = _autoBalanceMax;
            else if (value < _autoBalanceMin)
                value = _autoBalanceMin;

            _config.SetCVar(RMCCVars.CMMarinesPerXeno, value);
            break;
        }
    }

    private void OnDropshipHijackStart(ref DropshipHijackStartEvent ev)
    {
        var hiveStructures = EntityQueryEnumerator<HiveConstructionLimitedComponent, TransformComponent>();
        while (hiveStructures.MoveNext(out var hiveStructure, out _, out var transformComp))
        {
            if (transformComp.ParentUid != ev.Dropship && _rmcPlanet.IsOnPlanet(hiveStructure.ToCoordinates()))
            {
                _destruction.DestroyEntity(hiveStructure);
            }
        }
        var xenos = EntityQueryEnumerator<XenoComponent, MobStateComponent, TransformComponent>();
        var xenoAmount = 0;
        var larva = 0;
        //TODO RMC14 only do main hive
        while (xenos.MoveNext(out var xeno, out var comp, out var mobstate, out var transformComp))
        {
            if (_mobState.IsDead(xeno))
                continue;

            if (transformComp.ParentUid != ev.Dropship && _rmcPlanet.IsOnPlanet(xeno.ToCoordinates()))
            {
                if (comp.CountedInSlots)
                    larva++;

                //Ghost player and send message
                if (TryComp(xeno, out ActorComponent? actor))
                {
                    var session = actor.PlayerSession;
                    Entity<MindComponent> mind;

                    if (_mind.TryGetMind(session, out var mindId, out var mindComp))
                        mind = (mindId, mindComp);
                    else
                        mind = _mind.CreateMind(session.UserId);

                    var ghost = _ghost.SpawnGhost((mind.Owner, mind.Comp), xeno);
                    if (ghost != null)
                        EnsureComp<JoinXenoCooldownIgnoreComponent>(ghost.Value);

                    var origin = _transform.GetMoverCoordinates(xeno);
                    _popup.PopupCoordinates(Loc.GetString("rmc-xeno-hibernation"), origin, Filter.SinglePlayer(session), true, PopupType.MediumXeno);
                }

                QueueDel(xeno);
            }
            else
                xenoAmount++;
        }

        //Surge
        var shipQuery = EntityQueryEnumerator<MarineComponent, MobStateComponent, InfectableComponent, TransformComponent>();

        float totalHostWeights = 0;
        while (shipQuery.MoveNext(out var marine, out var mob, out var _, out var _, out var transformComp))
        {
            if (_mobState.IsDead(marine) || !_almayerMaps.Contains(transformComp.MapID))
                continue;

            if (!TryComp<MindContainerComponent>(marine, out var mindContainer))
                continue;

            if (mindContainer is null ||
                !TryComp<MindComponent>(mindContainer.Mind, out var mind))
                continue;

            foreach (var roleId in mind.MindRoles)
            {
                if (!TryComp<MindRoleComponent>(roleId, out var mindRole))
                    continue;

                if (mindRole.JobPrototype == null || !_prototypes.TryIndex(mindRole.JobPrototype, out var proto))
                    continue;

                totalHostWeights += proto.RoleWeight;
            }
        }

        //Get the maximum of either remaining marines or minimum amount
        var surgeAmount = Math.Max((int)Math.Ceiling(totalHostWeights * _hijackShipWeight) - xenoAmount, _hijackMinBurrowed);
        var rules = QueryActiveRules();
        while (rules.MoveNext(out _, out var rule, out _))
        {
            // Reset Hivecore Cooldown
            var hiveComp = EnsureComp<HiveComponent>(rule.Hive);
            //Add all the stranded xenos up
            _hive.IncreaseBurrowedLarva(larva); // TODO RMC14 should prob make sure it's only main hive
            _hive.ResetHiveCoreCooldown((rule.Hive, hiveComp));
            var surge = EnsureComp<HijackBurrowedSurgeComponent>(rule.Hive);
            surge.PooledLarva = surgeAmount;
        }
    }
    private void OnDropshipHijackLanded(ref DropshipHijackLandedEvent ev)
    {
        var rules = QueryActiveRules();
        var time = Timing.CurTime;
        while (rules.MoveNext(out _, out var rule, out _))
        {
            if (rule.HijackSongPlayed)
                break;

            rule.HijackSongPlayed = true;
            var song = _audio.PlayGlobal(rule.HijackSong, Filter.Broadcast(), true);
            if (song?.Entity is { } songEnt)
                EnsureComp<RMCHijackSongComponent>(songEnt);

            rule.ForceEndAt = time + _forceEndHijackTime;
            break;
        }

        var didCameraShake = false;
        var warshipQuery = EntityQueryEnumerator<AlmayerComponent, TransformComponent>();
        while (warshipQuery.MoveNext(out var uid, out _, out var xform)) // Stun everyone on the Almayer
        {
            if (!didCameraShake)
            {
                var map = _transform.GetMapId(uid);
                var sameMap = Filter.BroadcastMap(map);
                _rmcCameraShake.ShakeCamera(sameMap, 10, 2);
                didCameraShake = true;
            }

            StunAllMarinesOnAlmayer(xform);
        }
    }

    private void OnMobStateChanged<T>(Entity<T> ent, ref MobStateChangedEvent args) where T : IComponent?
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemCompDeferred<GhostRoleComponent>(ent);
            CheckRoundShouldEnd();
        }
    }

    private void OnCompRemove<T>(Entity<T> ent, ref ComponentRemove args) where T : IComponent?
    {
        CheckRoundShouldEnd();
    }

    private void OnMapInit(Entity<XenoEvolutionGranterComponent> ent, ref MapInitEvent args)
    {
        CheckRoundShouldEnd();
    }

    private void ReloadPrototypes()
    {
        _operationNames.Clear();
        _operationPrefixes.Clear();
        _operationSuffixes.Clear();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.TryGetComponent(out RMCDistressSignalNamesComponent? names, _compFactory))
                _operationNames.UnionWith(names.Names);

            if (prototype.TryGetComponent(out RMCDistressSignalPrefixesComponent? prefixes, _compFactory))
                _operationPrefixes.UnionWith(prefixes.Prefixes);

            if (prototype.TryGetComponent(out RMCDistressSignalSuffixesComponent? suffixes, _compFactory))
                _operationSuffixes.UnionWith(suffixes.Suffixes);
        }
    }

    protected override void OnStartAttempt(Entity<CMDistressSignalRuleComponent, GameRuleComponent> gameRule, RoundStartAttemptEvent ev)
    {
        if (ev.Forced || ev.Cancelled)
            return;

        if (!gameRule.Comp1.RequireXenoPlayers)
            return;

        var query = QueryAllRules();
        while (query.MoveNext(out _, out var distress, out _))
        {
            var xenoCandidates = 0;
            foreach (var player in ev.Players)
            {
                if (_prefsManager.TryGetCachedPreferences(player.UserId, out var preferences))
                {
                    var profile = (HumanoidCharacterProfile) preferences.GetProfile(preferences.SelectedCharacterIndex);
                    if (profile.JobPriorities.TryGetValue(distress.XenoSelectableJob, out var xenoPriority) &&
                        xenoPriority > JobPriority.Never)
                    {
                        xenoCandidates++;
                    }
                    else if (profile.JobPriorities.TryGetValue(distress.QueenJob, out var queenPriority) &&
                        queenPriority > JobPriority.Never)
                    {
                        xenoCandidates++;
                    }
                }
            }

            if (xenoCandidates >= _xenosMinimum)
                continue;

            var msg = $"Can't start distress signal. Requires at least {_xenosMinimum} xeno player but we have {xenoCandidates}.";
            ChatManager.SendAdminAnnouncement(msg);
            ChatManager.DispatchServerAnnouncement(msg);
            ev.Cancel();
        }
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var distress, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            distress.NextCheck ??= Timing.CurTime + distress.CheckEvery;

            if (distress.ForceEndAt != null && Timing.CurTime >= distress.ForceEndAt)
            {
                EndRound(distress, DistressSignalRuleResult.MinorXenoVictory, "rmc-distress-signal-minorxenovictory-timeout");
                continue;
            }

            var hijack = false;
            var dropshipQuery = EntityQueryEnumerator<DropshipComponent>();
            while (dropshipQuery.MoveNext(out var dropship))
            {
                if (dropship.Crashed)
                    hijack = true;
            }

            var time = Timing.CurTime;
            if (!distress.Hijack && hijack)
            {
                distress.Hijack = true;
                distress.AbandonedAt ??= time + distress.AbandonedDelay;
            }

            _almayerMaps.Clear();
            var almayerQuery = EntityQueryEnumerator<AlmayerComponent, TransformComponent>();
            while (almayerQuery.MoveNext(out _, out var xform))
            {
                _almayerMaps.Add(xform.MapID);
            }

            var xenosAlive = false;
            var xenos = EntityQueryEnumerator<ActorComponent, XenoComponent, MobStateComponent, TransformComponent>();
            while (xenos.MoveNext(out var xenoId, out _, out var xeno, out var mobState, out var xform))
            {
                if (!xeno.ContributesToVictory)
                    continue;

                if (HasComp<ThunderdomeMapComponent>(xform.MapUid))
                    continue;

                if (_mobState.IsAlive(xenoId, mobState) &&
                    (distress.AbandonedAt == null ||
                     time < distress.AbandonedAt ||
                     !distress.Hijack ||
                     _almayerMaps.Contains(xform.MapID)))
                {
                    xenosAlive = true;
                }

                if (xenosAlive)
                    break;
            }

            var marines = EntityQueryEnumerator<ActorComponent, MarineComponent, MobStateComponent, TransformComponent>();
            var marinesAlive = false;
            _marineList.Clear();
            while (marines.MoveNext(out var marineId, out _, out _, out var mobState, out var xform))
            {
                if (HasComp<VictimInfectedComponent>(marineId) ||
                    HasComp<VictimBurstComponent>(marineId) ||
                    _xenoNestedQuery.HasComp(marineId))
                {
                    continue;
                }

                if (_containers.IsEntityInContainer(marineId))
                    continue;

                if (HasComp<ThunderdomeMapComponent>(xform.MapUid))
                    continue;

                if (_mobState.IsAlive(marineId, mobState) &&
                    (distress.AbandonedAt == null ||
                     time < distress.AbandonedAt ||
                     !distress.Hijack ||
                     _almayerMaps.Contains(xform.MapID)))
                {
                    marinesAlive = true;
                    _marineList.Add(marineId);
                }

                if (marinesAlive && _marineList.Count > 1)
                    break;
            }

            if (xenosAlive && !marinesAlive)
            {
                EndRound(distress, DistressSignalRuleResult.MajorXenoVictory);
                continue;
            }

            if (!xenosAlive && marinesAlive)
            {
                // TODO RMC14 this should be when the dropship crashes, not if xenos ever boarded
                if (distress.Hijack)
                {
                    EndRound(distress, DistressSignalRuleResult.MinorXenoVictory);
                    continue;
                }
                else
                {
                    EndRound(distress, DistressSignalRuleResult.MajorMarineVictory);
                    continue;
                }
            }

            if (!xenosAlive && !marinesAlive)
            {
                EndRound(distress, DistressSignalRuleResult.AllDied);
                continue;
            }

            if (_marineList.Count == 1)
            {
                // TODO add ghost alert for last human
                var lastMarine = _marineList.Last();

                var cloaks = EntityQueryEnumerator<ThermalCloakComponent>();
                while (cloaks.MoveNext(out var cloakId, out var cloak))
                {
                    if (!cloak.Enabled)
                        continue;

                    _thermalCloak.SetInvisibility((cloakId, cloak), lastMarine, false, true);
                    _actions.SetCooldown(cloak.Action, Timing.CurTime, Timing.CurTime + TimeSpan.FromHours(2));
                    _actions.SetUseDelay(cloak.Action, TimeSpan.FromHours(2));
                }

                var ghillies = EntityQueryEnumerator<GhillieSuitComponent>();
                while (ghillies.MoveNext(out var ghillieId, out var ghillie))
                {
                    if (!ghillie.Enabled)
                        continue;

                    _ghillieSuit.ToggleInvisibility((ghillieId, ghillie), lastMarine, false);
                    _actions.SetCooldown(ghillie.Action, Timing.CurTime, Timing.CurTime + TimeSpan.FromHours(2));
                    _actions.SetUseDelay(ghillie.Action, TimeSpan.FromHours(2));
                }
            }

            if (_xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1))
            {
                distress.QueenDiedCheck = null;
                continue;
            }

            distress.QueenDiedCheck ??= Timing.CurTime + distress.QueenDiedDelay;
            if (distress.QueenDiedCheck == null)
                continue;

            if (Timing.CurTime >= distress.QueenDiedCheck)
            {
                if (_xenoEvolution.HasLiving<XenoComponent>(4))
                    EndRound(distress, DistressSignalRuleResult.MinorMarineVictory);
                else
                    EndRound(distress, DistressSignalRuleResult.MajorMarineVictory, "rmc-distress-signal-majormarinevictory-timeout");
            }
        }
    }

    private bool SpawnXenoMap(Entity<CMDistressSignalRuleComponent> rule)
    {
        //Just in case the planet was not selected before now
        var planet = SelectRandomPlanet();
        _lastPlanetMaps.Enqueue(planet.Proto.ID);
        while (_lastPlanetMaps.Count > 0 && _lastPlanetMaps.Count > _mapVoteExcludeLast)
        {
            _lastPlanetMaps.Dequeue();
        }

        if (!_mapLoader.TryLoadMap(planet.Comp.Map, out var mapNullable, out var grids))
            return false;

        var map = mapNullable.Value;
        EnsureComp<RMCPlanetComponent>(map);
        EnsureComp<TacticalMapComponent>(map);

        if (grids.Count == 0)
            return false;

        if (grids.Count > 1)
            Log.Error("Multiple planet-side grids found");

        rule.Comp.XenoMap = grids.First();

        _mapSystem.InitializeMap((map, map));

        //Process map inserts
        ActiveNightmareScenario = string.Empty;
        if (SelectedPlanetMap != null && SelectedPlanetMap.Value.Comp.NightmareScenarios != null)
        {
            ActiveNightmareScenario = _mapInsert.SelectMapScenario(SelectedPlanetMap.Value.Comp.NightmareScenarios);
        }
        var mapInsertQuery = EntityQueryEnumerator<MapInsertComponent>();
        while (mapInsertQuery.MoveNext(out var uid, out var mapInsert))
        {
            _mapInsert.ProcessMapInsert((uid, mapInsert));
        }

        // TODO RMC14 this should be delayed by 3 minutes + 13 second warning for immersion
        if (_landingZoneMiasmaEnabled &&
            rule.Comp.LandingZoneGas is { } gas &&
            TryComp(rule.Comp.XenoMap, out AreaGridComponent? areaGrid))
        {
            foreach (var (indices, areaProto) in areaGrid.Areas)
            {
                if (areaProto.TryGet(out var area, _prototypes, _compFactory) &&
                    area.LandingZone)
                {
                    var coordinates = _mapSystem.ToCoordinates(rule.Comp.XenoMap.Value, indices);
                    Spawn(gas, coordinates);
                }
            }
        }

        var tunnels = EntityQueryEnumerator<XenoTunnelComponent>();
        while (tunnels.MoveNext(out var uid, out _))
        {
            RemCompDeferred<DeletedByWeedKillerComponent>(uid);
        }

        var xenoMap = rule.Comp.XenoMap.Value;
        var rmcAmbientComp = EnsureComp<RMCAmbientLightComponent>(xenoMap);
        var rmcAmbientEffectComp = EnsureComp<RMCAmbientLightEffectsComponent>(xenoMap);
        var colorSequence = _rmcAmbientLight.ProcessPrototype(rmcAmbientEffectComp.Sunset);
        _rmcAmbientLight.SetColor((xenoMap, rmcAmbientComp), colorSequence, _sunsetDuration);

        return true;
    }

    private Spawners GetSpawners()
    {
        var spawners = new Spawners();
        var squadQuery = EntityQueryEnumerator<SquadSpawnerComponent>();
        while (squadQuery.MoveNext(out var uid, out var spawner))
        {
            bool IsHyperSleepFull(Entity<HyperSleepChamberComponent> chamber)
            {
                return _containers.TryGetContainer(chamber, chamber.Comp.ContainerId, out var container) &&
                       container.Count > 0;
            }

            if (_hyperSleepChamberQuery.TryComp(uid, out var hyperSleep) &&
                IsHyperSleepFull((uid, hyperSleep)))
            {
                if (spawner.Role == null)
                    spawners.SquadAnyFull.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.SquadFull.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
            }
            else
            {
                var found = false;
                foreach (var cardinal in _rmcMap.CardinalDirections)
                {
                    var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(uid, cardinal);
                    while (anchored.MoveNext(out var anchoredId))
                    {
                        if (_hyperSleepChamberQuery.TryComp(anchoredId, out hyperSleep))
                        {
                            found = true;
                            if (IsHyperSleepFull((anchoredId, hyperSleep)))
                            {
                                if (spawner.Role == null)
                                    spawners.SquadAnyFull.GetOrNew(spawner.Squad).Add(anchoredId);
                                else
                                    spawners.SquadFull.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(anchoredId);
                            }
                            else
                            {
                                if (spawner.Role == null)
                                    spawners.SquadAny.GetOrNew(spawner.Squad).Add(anchoredId);
                                else
                                    spawners.Squad.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(anchoredId);
                            }

                            break;
                        }
                    }

                    if (found)
                        break;
                }

                if (found)
                    continue;

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
                spawners.NonSquadFull.GetOrNew(spawner.Job.Value).Add(uid);
            }
            else
            {
                spawners.NonSquad.GetOrNew(spawner.Job.Value).Add(uid);
            }
        }

        return spawners;
    }

    private (EntProtoId Id, EntityUid Ent) NextSquad(
        ProtoId<JobPrototype> job,
        CMDistressSignalRuleComponent rule,
        EntProtoId<SquadTeamComponent>? preferred)
    {
        var squads = new List<(EntProtoId SquadId, EntityUid Squad, int Players)>();
        foreach (var (squadId, squad) in rule.Squads)
        {
            var players = 0;
            if (TryComp(squad, out SquadTeamComponent? team))
            {
                var roles = team.Roles;
                var maxRoles = team.MaxRoles;
                if (roles.TryGetValue(job, out var currentPlayers))
                    players = currentPlayers;

                if (preferred != null &&
                    preferred == squadId &&
                    (!maxRoles.TryGetValue(job, out var max) || players < max))
                {
                    return (squadId, squad);
                }
            }

            squads.Add((squadId, squad, players));
        }

        _random.Shuffle(squads);
        squads.Sort((a, b) => a.Players.CompareTo(b.Players));

        var chosen = squads[0];
        return (chosen.SquadId, chosen.Squad);
    }

    private (EntityUid Spawner, EntityUid? Squad)? GetSpawner(
        CMDistressSignalRuleComponent rule,
        JobPrototype job,
        EntProtoId<SquadTeamComponent>? preferred)
    {
        var allSpawners = GetSpawners();
        EntityUid? squad = null;

        if (job.HasSquad)
        {
            var (squadId, squadEnt) = NextSquad(job, rule, preferred);
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
                return (_random.Pick(spawners), squadEnt);

            if (allSpawners.NonSquadFull.TryGetValue(job.ID, out spawners))
                return (_random.Pick(spawners), squadEnt);

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
                if (point.Job?.Id == job.ID)
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

    protected override void AppendRoundEndText(EntityUid uid,
        CMDistressSignalRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var result = component.Result ??= DistressSignalRuleResult.None;
        if (component.CustomRoundEndMessage != null)
            args.AddLine($"{Loc.GetString(component.CustomRoundEndMessage)}");
        else
            args.AddLine($"{Loc.GetString($"cm-distress-signal-{result.ToString().ToLower()}")}");

        args.AddLine(string.Empty);

        if (_gameRulesExtras.MemorialEntry(ref args))
            args.AddLine(string.Empty);

        if (_gameRulesExtras.MarineAwards(ref args))
            args.AddLine(string.Empty);

        _gameRulesExtras.XenoAwards(ref args);
    }

    protected override void ActiveTick(EntityUid uid, CMDistressSignalRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var time = Timing.CurTime;
        component.StartTime ??= time;

        _scaling.TryStartScaling(component.MarineFaction);

        var announcementTime = time - component.StartTime;

        if (_queenBuildingBoostEnabled &&
            time - component.StartTime >= _queenBoostDuration &&
            !component.QueenBoostRemoved)
        {
            component.QueenBoostRemoved = true;
            RemoveQueenBuildingBoosts();
        }

        if (!component.RecalculatedPower)
        {
            component.RecalculatedPower = true;
            _rmcPower.RecalculatePower();
        }

        if (!component.AresGreetingDone && announcementTime >= component.AresGreetingDelay)
        {
            component.AresGreetingDone = true;

            if (component.StartARESAnnouncements)
                _marineAnnounce.AnnounceARES(default, "ARES. Online. Good morning, marines.", component.AresGreetingAudio,"rmc-announcement-ares-online");
        }

        if (!component.AresMapDone && announcementTime >= component.AresMapDelay)
        {
            component.AresMapDone = true;

            if (SelectedPlanetMap != null && component.StartARESAnnouncements)
            {
                var announcement = SelectedPlanetMap.Value.Comp.Announcement;
                _marineAnnounce.AnnounceARES(default, announcement, announcement: "rmc-announcement-ares-map");
            }
        }

        if (time >= component.NextCheck)
        {
            component.NextCheck = time + component.CheckEvery;
            CheckRoundShouldEnd();
        }

        if (component.EndAtAllClear != null &&
            time >= component.EndAtAllClear)
        {
            _roundEnd.EndRound();
            return;
        }

        if (_xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1))
            component.QueenDiedCheck = null;

        if (component.QueenDiedCheck == null)
            return;

        if (time >= component.QueenDiedCheck)
        {
            if (_xenoEvolution.HasLiving<XenoComponent>(4))
                EndRound(component, DistressSignalRuleResult.MinorMarineVictory);
            else
                EndRound(component, DistressSignalRuleResult.MajorMarineVictory, "rmc-distress-signal-majormarinevictory-timeout");
        }
    }

    private RMCPlanet SelectRandomPlanet()
    {
        if (SelectedPlanetMap != null)
            return SelectedPlanetMap.Value;

        var planet = _random.Pick(_rmcPlanet.GetCandidates());
        SelectedPlanetMap = planet;
        return planet;
    }

    private void ResetSelectedPlanet()
    {
        SelectedPlanetMap = null;
    }

    public void SetPlanet(RMCPlanet planet)
    {
        SelectedPlanetMap = planet;
    }

    public void SetCustomOperationName(string customname)
    {
        OperationName = customname;
        _usingCustomOperationName = true;
    }

    private void StartPlanetVote()
    {
        if (!_config.GetCVar(RMCCVars.RMCPlanetMapVote))
            return;

        var planets = _rmcPlanet.GetCandidates();
        if (!_useCarryoverVoting)
        {
            foreach (var planet in planets)
            {
                _carryoverVotes[planet.Proto.ID] = 0;
            }
        }

        planets.RemoveAll(p => _lastPlanetMaps.Contains(p.Proto.ID));

        var options = new List<(string text, object data)>();
        foreach (var planet in planets)
        {
            var name = planet.Proto.Name;
            var votes = _carryoverVotes.GetValueOrDefault(planet.Proto.ID);
            if (votes > 0)
                name = $"{name} [+{votes}]";

            options.Add((name, planet.Comp.Map.ToString()));
        }

        var vote = new VoteOptions
        {
            Title = Loc.GetString("rmc-distress-signal-next-map-title"),
            Options = options,
            Duration = TimeSpan.FromMinutes(2),
        };
        vote.SetInitiatorOrServer(null);

        _currentVote = _voteManager.CreateVote(vote);
        _currentVote.OnFinished += (_, args) =>
        {
            _currentVote = null;
            RMCPlanet picked;

            var voteResult = planets.Zip(args.Votes);
            var adjustedVotes = voteResult.Select(p => (p.Item1, p.Item2 + _carryoverVotes.GetValueOrDefault(p.First.Proto.ID))).ToList();
            var maxVotes = adjustedVotes.Max(v => v.Item2);
            var winningMaps = adjustedVotes.Where(item => item.Item2 == maxVotes).Select(item => item.Item1).ToList();

            if (winningMaps.Count > 1)
            {
                picked = _random.Pick(winningMaps);
                var msg = Loc.GetString("rmc-distress-signal-next-map-tie", ("picked", picked.Proto.Name));
                _chatManager.DispatchServerAnnouncement(msg);
            }
            else
            {
                picked = winningMaps.First();
                var msg = Loc.GetString("rmc-distress-signal-next-map-win", ("winner", picked.Proto.Name));
                _chatManager.DispatchServerAnnouncement(msg);
            }

            foreach (var (planet, votes) in planets.Zip(args.Votes))
            {
                var id = planet.Proto.ID;
                _carryoverVotes[id] = _useCarryoverVoting ? _carryoverVotes.GetValueOrDefault(id) + votes : 0;
            }

            _carryoverVotes[picked.Proto.ID] = 0;
            SelectedPlanetMap = picked;
        };
        _currentVote.OnCancelled += _ => _currentVote = null;
    }

    public bool HasPlanetVoteRunning()
    {
        return _currentVote != null;
    }

    public void CancelPlanetVote()
    {
        _currentVote?.Cancel();
    }

    private string GetRandomOperationName()
    {
        if (_usingCustomOperationName && OperationName != null)
        {
            _usingCustomOperationName = false;

            return OperationName;
        }

        var name = string.Empty;
        if (_operationNames.Count > 0)
            name += $"{_random.Pick(_operationNames)} ";

        if (_operationPrefixes.Count > 0)
            name += $"{_random.Pick(_operationPrefixes)}";

        if (_operationSuffixes.Count > 0)
            name += $"-{_random.Pick(_operationSuffixes)}";

        return name.Trim();
    }

    // TODO RMC14 this would be literally anywhere else if the code for loading maps wasn't dogshit and broken upstream
    private void SpawnAdminAreas(CMDistressSignalRuleComponent comp)
    {
        bool SpawnMap(ResPath path, [NotNullWhen(true)] out EntityUid? mapEnt)
        {
            mapEnt = default;

            try
            {
                if (string.IsNullOrWhiteSpace(path.ToString()))
                    return false;

                if (!_mapLoader.TryLoadMap(path, out var map, out _))
                    return false;

                _mapSystem.InitializeMap((map.Value, map.Value));
                mapEnt = map;
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Error loading admin fax area:\n{e}");
            }

            return false;
        }

        SpawnMap(new ResPath(_adminFaxAreaMap), out _);
        if (SpawnMap(comp.Thunderdome, out var mapEnt))
            EnsureComp<ThunderdomeMapComponent>(mapEnt.Value);
    }

    private void EndRound(CMDistressSignalRuleComponent rule, DistressSignalRuleResult result, LocId? customMessage = null)
    {
        if (!rule.AutoEnd)
            return;

        // you might be wondering what this check is doing here
        // the answer is simple
        // the absolute unit that wrote game rule system and game ticker made a conveniently named ActiveTick method
        // that gets ticked BEFORE THE FUCKING ROUND IS SETUP
        // so no marines and no xenos means instant mutual annihilation
        // therefore we wait an arbitrary 1 minute
        // i fucking hate my life dude
        // i slept 3 hours and have to stay up to manually end ANOTHER ROUND OF RMC14
        // TODO RMC14 why are we still here
        if (rule.StartTime == null || Timing.CurTime - rule.StartTime < rule.RoundEndCheckDelay)
            return;

        Log.Info($"Attempting to set {nameof(rule)} result to {result}");
        if (rule.Result != null)
            return;

        rule.Result = result;
        rule.CustomRoundEndMessage = customMessage;
        switch (rule.Result)
        {
            case DistressSignalRuleResult.MajorMarineVictory:
                if (rule.XenoMap is { } xenoMap)
                {
                    var rmcAmbientComp = EnsureComp<RMCAmbientLightComponent>(xenoMap);
                    var rmcAmbientEffectComp = EnsureComp<RMCAmbientLightEffectsComponent>(xenoMap);
                    var colorSequence = _rmcAmbientLight.ProcessPrototype(rmcAmbientEffectComp.Sunrise);
                    _rmcAmbientLight.SetColor((xenoMap, rmcAmbientComp), colorSequence, _sunriseDuration);
                }

                var ares = _ares.EnsureARES();
                _marineAnnounce.AnnounceRadio(ares,
                    "Bioscan complete. No unknown lifeform signature detected.",
                    rule.AllClearChannel);
                _marineAnnounce.AnnounceRadio(ares,
                    "Saving operational report to archive.",
                    rule.AllClearChannel);
                _marineAnnounce.AnnounceRadio(ares,
                    "Commencing final systems scan in 3 minutes.",
                    rule.AllClearChannel);
                rule.EndAtAllClear ??= Timing.CurTime + rule.AllClearEndDelay;
                break;
            default:
                _roundEnd.EndRound();
                break;
        }
    }

    /// <summary>
    /// Sets the hive of all loaded xeno friendly entities (e.g. weeds).
    /// Only makes sense for distress signal with 1 hive, with multiple hives you would need to determine which weeds belong to which hive
    /// </summary>
    private void SetFriendlyHives(EntityUid hive)
    {
        var query = EntityQueryEnumerator<XenoFriendlyComponent>();
        while (query.MoveNext(out var weeds, out _))
        {
            _hive.SetHive(weeds, hive);
        }

        var resinSlowdown = EntityQueryEnumerator<ResinSlowdownModifierComponent>();
        while (resinSlowdown.MoveNext(out var uid, out _))
        {
            _hive.SetHive(uid, hive);
        }

        var resinSpeedup = EntityQueryEnumerator<ResinSpeedupModifierComponent>();
        while (resinSpeedup.MoveNext(out var uid, out _))
        {
            _hive.SetHive(uid, hive);
        }

        var tunnelQuery = EntityQueryEnumerator<XenoTunnelComponent>();
        var tunnels = new List<EntityUid>();

        while (tunnelQuery.MoveNext(out var ent, out _))
        {
            tunnels.Add(ent);
        }
        // Replace all pre-mapped tunnels with a new tunnel with name and associated with the hive
        foreach (var tunnel in tunnels)
        {
            if (_xenoTunnel.TryPlaceTunnel(hive, null, tunnel.ToCoordinates(), out var newTunnel))
                RemCompDeferred<DeletedByWeedKillerComponent>(newTunnel.Value);

            QueueDel(tunnel);
        }
    }

    private void UnpowerFaxes(MapId map)
    {
        var faxes = EntityQueryEnumerator<FaxMachineComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (faxes.MoveNext(out _, out var power, out var xform))
        {
            if (xform.MapID != map)
                continue;

            power.Load = 5;
            power.NeedsPower = true;
        }
    }

    private void OnHiveChanged(Entity<HiveMemberComponent> ent, ref HiveChangedEvent args)
    {
        if (!_queenBuildingBoostEnabled)
            return;

        if (!HasComp<XenoEvolutionGranterComponent>(ent))
            return;

        if (args.Hive == null)
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (comp.QueenBoostRemoved)
                return;

            var withinBoostPeriod = comp.StartTime == null ||
                                (Timing.CurTime - comp.StartTime < _queenBoostDuration);

            if (withinBoostPeriod)
            {
                GiveQueenBoost(ent);
            }
            break;
        }
    }

    private void GiveQueenBoost(EntityUid queen)
    {
        var xenoConstruction = EntityManager.System<SharedXenoConstructionSystem>();
        xenoConstruction.GiveQueenBoost(queen, _queenBoostSpeedMultiplier, _queenBoostRemoteRange);

        _adminLog.Add(LogType.RMCXenoSpawn, $"Queen {ToPrettyString(queen):queen} received building boost");
    }

    private void OnXenoComponentInit(Entity<XenoComponent> ent, ref ComponentInit args)
    {
        if (!_queenBuildingBoostEnabled)
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (!TryComp<MetaDataComponent>(ent.Owner, out var metaData) ||
                metaData.EntityPrototype?.ID != comp.QueenEnt.Id)
                continue;

            var withinBoostPeriod = comp.StartTime == null ||
                                (Timing.CurTime - comp.StartTime < _queenBoostDuration);

            if (withinBoostPeriod)
            {
                GiveQueenBoost(ent.Owner);
            }
            break;
        }
    }

    private void RemoveQueenBuildingBoosts()
    {
        var xenoConstruction = EntityManager.System<SharedXenoConstructionSystem>();
        var queens = EntityQueryEnumerator<QueenBuildingBoostComponent, XenoEvolutionGranterComponent>();
        while (queens.MoveNext(out var queen, out var boost, out _))
        {
            xenoConstruction.RemoveQueenBoost(queen);
        }
    }

    /// <summary>
    /// Stuns all marines on the Almayer.
    /// </summary>
    private void StunAllMarinesOnAlmayer(TransformComponent xform)
    {
        // Get enumeration exceptions from people dropping things if we just paralyze as we go
        var toKnock = new ValueList<EntityUid>();
        GetMarinesOnAlmayer(xform, ref toKnock);

        foreach (var child in toKnock)
        {
            if (!TryComp<StatusEffectsComponent>(child, out var status))
                continue;

            _stuns.TryParalyze(child, _hijackStunTime, true, status);
        }
    }

    /// <summary>
    /// Gets all marines on the Almayer.
    /// </summary>
    private void GetMarinesOnAlmayer(TransformComponent xform, ref ValueList<EntityUid> reference)
    {
        // Not recursive because probably not necessary? If we need it to be that's why this method is separate.
        var childEnumerator = xform.ChildEnumerator;
        while (childEnumerator.MoveNext(out var child))
        {
            if (HasComp<XenoComponent>(child))
                continue;

            reference.Add(child);
        }
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
