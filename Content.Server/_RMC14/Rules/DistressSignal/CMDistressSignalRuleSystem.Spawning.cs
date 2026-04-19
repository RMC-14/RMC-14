using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Bioscan;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Fax;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Spawners;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Nutrition.Components;
using Content.Shared.Preferences;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    /// <summary>
    /// Main handler for player spawning during the distress signal round.
    /// Initializes the xeno map, sets up survivor jobs, applies job slot scaling,
    /// selects and spawns xenos, and initializes dropships.
    /// </summary>
    private void OnRulePlayerSpawning(RulePlayerSpawningEvent ev)
    {
        var rule = TryGetActiveRuleEntity();
        if (!rule.HasValue)
            return;

        var comp = rule.Value.Comp;

        OperationName ??= GetRandomOperationName();

        if (!InitializeXenoMap(rule.Value, comp))
            return;

        SetupSurvivorJobs(comp);
        ApplyJobSlotScaling(comp, ev);

        var initialPlayerCount = ev.PlayerPool.Count;
        SelectAndSpawnXenos(comp, ev);
        SpawnSurvivors(comp, ev, initialPlayerCount);

        if (_spawnedDropships) return;

        _spawnedDropships = true;
        InitializeDropships(comp);
    }

    private bool InitializeXenoMap(Entity<CMDistressSignalRuleComponent> rule, CMDistressSignalRuleComponent comp)
    {
        // TODO: come up with random name like operation name, in a function that can be reused for hive v hive
        comp.Hive = _hive.CreateHive("xenonid hive", comp.HiveId);
        if (comp.SpawnPlanet && !SpawnXenoMap((rule.Owner, comp)))
        {
            Log.Error("Failed to load xeno map");
            // TODO: how should the gamemode handle failure? restart immediately or create an alert for admins
            return false;
        }

        _intel.RunSpawners();
        SetFriendlyHives(comp.Hive);

        if (comp.XenoMap != null)
            UnpowerFaxes(_transform.GetMapId(comp.XenoMap.Value));

        SetCamoType();
        SpawnSquads(rule);
        SpawnAdminAreas(comp);

        if (comp.Bioscan)
        {
            var bioscan = Spawn(null, MapCoordinates.Nullspace);
            EnsureComp<BioscanComponent>(bioscan);
        }

        return true;
    }

    private void SetupSurvivorJobs(CMDistressSignalRuleComponent comp)
    {
        if (SelectedPlanetMap == null)
            return;

        comp.SurvivorJobVariants = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobVariants);
        comp.SurvivorJobOverrides = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobOverrides);

        if (SelectedPlanetMap.Value.Comp.SurvivorJobs != null)
            comp.SurvivorJobs = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobs, notNullableOverride: true);

        if (ActiveNightmareScenario != null)
        {
            var activeScenarioSurvivors = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobScenarios);
            var activeScenarioSurvivorOverrides = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobOverrideScenarios);
            var activeScenarioSurvivorVariants = _serialization.CreateCopy(SelectedPlanetMap.Value.Comp.SurvivorJobVariantScenarios);

            if (activeScenarioSurvivors != null && activeScenarioSurvivors.TryGetValue(ActiveNightmareScenario, out var scenarioJobs))
                comp.SurvivorJobs = scenarioJobs;

            if (activeScenarioSurvivorOverrides != null)
                activeScenarioSurvivorOverrides.TryGetValue(ActiveNightmareScenario, out comp.SurvivorJobOverrides);

            if (activeScenarioSurvivorVariants != null)
                activeScenarioSurvivorVariants.TryGetValue(ActiveNightmareScenario, out comp.SurvivorJobVariantScenarios);
        }
    }

    private void ApplyJobSlotScaling(CMDistressSignalRuleComponent comp, RulePlayerSpawningEvent ev)
    {
        var totalXenos = (int) Math.Round(Math.Max(1, ev.PlayerPool.Count / _marinesPerXeno));
        // TODO RMC14 dont count survivors
        var totalSurvivors = (int) Math.Clamp((int)Math.Round(ev.PlayerPool.Count / _marinesPerSurvivor), _minimumSurvivors, _maximumSurvivors);
        var marines = ev.PlayerPool.Count - totalXenos - totalSurvivors;

        // TODO RMC14: Move to component
        if (!comp.DoJobSlotScaling || marines <= 0 || !_config.GetCVar(RMCCVars.RMCJobSlotScaling))
            return;

        var stations = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
        while (stations.MoveNext(out var stationId, out var stationJobs, out _))
        {
            if (stationJobs.JobSlotScaling is not { } scalingProto || !scalingProto.TryGet(out var scalingComp, _prototypes, _compFactory))
                continue;

            foreach (var (job, scaling) in scalingComp.Jobs)
            {
                var slots = _rmcStationJobs.GetSlots(marines, scaling.Factor, scaling.C, scaling.Min, scaling.Max);
                if (scaling.Squad)
                {
                    foreach (var squadId in comp.SquadIds)
                    {
                        if (comp.Squads.TryGetValue(squadId, out var squad) && TryComp(squad, out SquadTeamComponent? squadTeam))
                            _squad.SetSquadMaxRole((squad, squadTeam), job, slots);
                    }
                    slots *= 4;
                }

                var jobs = stationJobs.SetupAvailableJobs;
                if (jobs.TryGetValue(job, out var available))
                {
                    for (var i = 0; i < available.Length; i++)
                    {
                        available[i] = slots;
                    }
                }

                Log.Info($"Setting {job} to {slots} slots.");
                _stationJobs.TrySetJobSlot(stationId, job, slots, stationJobs: stationJobs);
            }
        }
    }

    /// <summary>
    /// Selects xeno players based on job priorities and spawns them as queen or larva.
    /// Handles burrowed larva calculation if there aren't enough xeno players.
    /// </summary>
    /// <param name="comp">The distress signal rule component.</param>
    /// <param name="ev">The rule player spawning event.</param>
    private void SelectAndSpawnXenos(CMDistressSignalRuleComponent comp, RulePlayerSpawningEvent ev)
    {
        if (!comp.SpawnXenos)
            return;

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
            var xenoEnt = SpawnXenoEnt(ent, player, doBurst, comp, xenoSpawnPoints, xenoLeaderSpawnPoints);

            if (!_mind.TryGetMind(playerId, out var mind))
                mind = _mind.CreateMind(playerId);

            _mind.TransferTo(mind.Value, xenoEnt);
            return playerId;
        }

        var totalXenos = (int) Math.Round(Math.Max(1, ev.PlayerPool.Count / _marinesPerXeno));
        var priorities = Enum.GetValues<JobPriority>().Length;
        var xenoCandidates = new List<NetUserId>[priorities];
        for (var i = 0; i < priorities; i++)
        {
            xenoCandidates[i] = new();
        }

        foreach (var (id, profile) in ev.Profiles)
        {
            if (IsJobAllowed(id, comp.QueenJob) && profile.JobPriorities.TryGetValue(comp.QueenJob, out var p) && p > JobPriority.Never)
                xenoCandidates[(int)p].Add(id);
        }

        NetUserId? queenSelected = null;
        for (var i = priorities - 1; i >= 0; i--)
        {
            while (xenoCandidates[i].Count > 0)
            {
                queenSelected = SpawnXeno(xenoCandidates[i], comp.QueenEnt);
                if (queenSelected != null) break;
            }
            if (queenSelected != null)
            {
                totalXenos--;
                break;
            }
        }

        for (var i = 0; i < priorities; i++)
        {
            xenoCandidates[i].Clear();
        }

        foreach (var (id, profile) in ev.Profiles)
        {
            if (id != queenSelected && IsJobAllowed(id, comp.XenoSelectableJob) && profile.JobPriorities.TryGetValue(comp.XenoSelectableJob, out var p) && p > JobPriority.Never)
                xenoCandidates[(int)p].Add(id);
        }

        var selectedXenos = 0;
        for (var i = priorities - 1; i >= 0; i--)
        {
            while (xenoCandidates[i].Count > 0 && selectedXenos < totalXenos)
            {
                if (SpawnXeno(xenoCandidates[i], comp.LarvaEnt, true) != null) selectedXenos++;
            }
        }

        if (totalXenos - selectedXenos > 0)
            _hive.IncreaseBurrowedLarva(totalXenos - selectedXenos);
    }

    private EntityUid SpawnXenoEnt(EntProtoId ent, ICommonSession player, bool doBurst,
        CMDistressSignalRuleComponent comp, List<EntityUid> xenoSpawnPoints, List<EntityUid> xenoLeaderSpawnPoints)
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

            var spawnEv = new PlayerSpawnCompleteEvent(corpseMob, player, comp.XenoSurvivorCorpseJob, false, true, 0, default, profile);
            RaiseLocalEvent(corpseMob, spawnEv, true);

            var victimInfected = EnsureComp<VictimInfectedComponent>(corpseMob);
            _parasite.SetBurstSpawn((corpseMob, victimInfected), ent);
            _parasite.SetHive((corpseMob, victimInfected), comp.Hive);
            _parasite.SpawnLarva((corpseMob, victimInfected), out var newXeno);
            _parasite.SetBurstDelay((corpseMob, victimInfected), comp.XenoSurvivorCorpseBurstDelay);

            RemCompDeferred<HiddenAppearanceComponent>(corpseMob);
            _xeno.MakeXeno(newXeno);

            _adminLog.Add(LogType.RMCXenoSpawn, $"Player {player} with mob {ToPrettyString(newXeno):xeno} spawned as a xeno from their corpse {ToPrettyString(corpseMob):corpse}");
            return newXeno;
        }

        var xenoEnt = SpawnAtPosition(ent, point.ToCoordinates());
        _xeno.MakeXeno(xenoEnt);
        _hive.SetHive(xenoEnt, comp.Hive);
        return xenoEnt;
    }

    /// <summary>
    /// Initializes dropships by loading ship grids, setting up navigation to destinations,
    /// and configuring marine IFF factions and fax machines.
    /// </summary>
    private void InitializeDropships(CMDistressSignalRuleComponent comp)
    {
        // don't open shitcode inside
        _mapSystem.CreateMap(out var dropshipMap);
        var dropshipPoints = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
        var shipIndex = 0;

        while (dropshipPoints.MoveNext(out var destinationId, out var destination, out var destTransform))
        {
            if (_mapSystem.TryGetMap(destTransform.MapID, out var destinationMapId) && comp.XenoMap == destinationMapId)
                continue;

            if (destination.Spawn == null)
                continue;

            var gridOffset = new Vector2(shipIndex * 100, shipIndex * 100);
            shipIndex++;

            if (!_mapLoader.TryLoadGrid(dropshipMap, destination.Spawn.Value, out var shipGrids, offset: gridOffset))
                continue;

            var computers = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
            while (computers.MoveNext(out var computerId, out var computer, out var xform))
            {
                if (xform.GridUid != shipGrids.Value) continue;

                if (!_dropship.FlyTo(
                    (computerId, computer),
                    destinationId,
                    user: null,
                    startupTime: 1f,
                    hyperspaceTime: 1f,
                    offset: true))
                {
                    continue;
                }

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

        if (SelectedPlanetMap == null)
            return;

        var specialFaxesList = SelectedPlanetMap.Value.Comp.SpecialFaxes;
        if (specialFaxesList == null)
            return;

        var specialFaxes = EntityQueryEnumerator<FaxMachineComponent, SpecialFaxComponent>();
        while (specialFaxes.MoveNext(out var faxId, out var faxComp, out var special))
        {
            foreach (var (targetFaxId, paper) in specialFaxesList)
            {
                if (special.FaxId != targetFaxId)
                    continue;

                if (!paper.TryGet(out var paperComponent, _prototypes, _compFactory))
                    continue;

                if (!_prototypes.TryIndex(paper.Id, out var entProto, logError: false))
                    continue;

                var content = Loc.GetString(paperComponent.Content);
                var printout = new FaxPrintout(content, entProto.Name, prototypeId: paper.Id, locked: true);
                _fax.Receive(faxId, printout, component: faxComp);
            }
        }
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.Job is not { } jobId ||
            !_prototypes.TryIndex(jobId, out var job) ||
            !job.IsCM)
        {
            return;
        }

        var comp = TryGetActiveRule();
        if (comp == null)
            return;

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
}
