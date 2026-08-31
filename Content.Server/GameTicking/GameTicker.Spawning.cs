using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking.Events;
using Content.Server.Spawners.Components;
using Content.Server.Speech.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server._RMC14.Announce;

namespace Content.Server.GameTicking
{
    using JobAssignmentsDict = Dictionary<ProtoId<JobPrototype>, List<JobAssignment>>;

    public sealed partial class GameTicker
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly AdminSystem _admin = default!;
        [Dependency] private readonly MarinePresenceAnnounceSystem _marinePresenceAnnounce = default!;

        public static readonly EntProtoId ObserverPrototypeName = "MobObserver";
        public static readonly EntProtoId AdminObserverPrototypeName = "RMCAdminObserver";

        /// <summary>
        /// How many players have joined the round through normal methods.
        /// Useful for game rules to look at. Doesn't count observers, people in lobby, etc.
        /// </summary>
        public int PlayersJoinedRoundNormally;

        // Mainly to avoid allocations.
        private readonly List<EntityCoordinates> _possiblePositions = new();

        private List<EntityUid> GetSpawnableStations()
        {
            var spawnableStations = new List<EntityUid>();
            var query = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
            while (query.MoveNext(out var uid, out _, out _))
            {
                spawnableStations.Add(uid);
            }

            return spawnableStations;
        }

        private void RMCSpawnPlayers(List<ICommonSession> readyPlayers,
            Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
            bool force)
        {
            var (playerAssignments, jobAssignments) = GetPlayerAssignments(readyPlayers, profiles, force);

            // Calculate extended access for stations.
            var spawnableStations = GetSpawnableStations();
            var stationJobCounts = spawnableStations.ToDictionary(e => e, _ => 0);
            foreach (var player in playerAssignments)
            {
                if (player.AssignedJob == null)
                {
                    var playerSession = _playerManager.GetSessionById(player.Session.UserId);
                    var evNoJobs = new NoJobsAvailableSpawningEvent(playerSession); // Used by gamerules to wipe their antag slot, if they got one
                    RaiseLocalEvent(evNoJobs);

                    _chatManager.DispatchServerMessage(playerSession, Loc.GetString("job-not-available-wait-in-lobby"));
                }
                else if (player.AssignedJob.StationId is { } stationId)
                {
                    stationJobCounts[stationId] += 1;
                }
            }

            _stationJobs.CalcExtendedAccess(stationJobCounts);

            // Spawn everybody in!
            foreach (var player in playerAssignments)
            {
                if (player.AssignedJob == null)
                    continue;

                RMCSpawnPlayer(player);
            }

            RefreshLateJoinAllowed();

            // RMC version of the event to allow systems to react to player assignments,
            // as well as unassigned roles
            RaiseLocalEvent(new RoundstartPlayersSpawnedEvent(
                playerAssignments,
                jobAssignments));

            // Allow rules to add roles to players who have been spawned in. (For example, on-station traitors)
            RaiseLocalEvent(new RulePlayerJobsAssignedEvent(
                playerAssignments.Select(player => player.Session).ToArray(),
                profiles,
                force));
        }

        private (List<PlayerSpawnInfo>, JobAssignmentsDict) GetPlayerAssignments(List<ICommonSession> readyPlayers,
            Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
            bool force)
        {
            var players = readyPlayers.Select(p => new PlayerSpawnInfo(p, profiles[p.UserId])).ToList();
            Random.Shared.Shuffle(players);

            var high = new List<ProtoId<JobPrototype>>();
            var medium = new List<ProtoId<JobPrototype>>();
            var low = new List<ProtoId<JobPrototype>>();

            // populate player info with their job priorities
            foreach (var player in players)
            {
                high.Clear();
                medium.Clear();
                low.Clear();
                foreach (var (job, priority) in player.Profile.JobPriorities)
                {
                    var jobBans = _banManager.GetJobBans(player.Session.UserId);
                    if (jobBans == null || jobBans.Contains(job))
                        continue; // player banned from this job

                    var ev = new IsJobAllowedEvent(player.Session, job);
                    RaiseLocalEvent(ref ev);
                    if (ev.Cancelled)
                        continue; // player not whitelisted for this job

                    switch (priority)
                    {
                        case JobPriority.High:
                            high.Add(job);
                            break;
                        case JobPriority.Medium:
                            medium.Add(job);
                            break;
                        case JobPriority.Low:
                            low.Add(job);
                            break;
                        default:
                            break;
                    }
                }

                // In order to make sure jobs with equal preference have an equal chance of being
                // selected on average, randomize their order.
                Random.Shared.Shuffle(high);
                Random.Shared.Shuffle(medium);
                Random.Shared.Shuffle(low);

                player.JobPreferenceOrder.AddRange(high);
                player.JobPreferenceOrder.AddRange(medium);
                player.JobPreferenceOrder.AddRange(low);
            }

            var processingPlayers = new List<PlayerSpawnInfo>();
            var jobAssignments = new JobAssignmentsDict();
            var weightedJobs = _stationJobs.GetWeightedJobs();
            var metaJobAssignments = new HashSet<MetaJobAssignment>();
            var metaPlayerAssignments = new HashSet<MetaPlayerAssignment>();

            RaiseLocalEvent(new InitializingAssignmentsEvent(jobAssignments, metaJobAssignments, metaPlayerAssignments));

            foreach (var newPlayer in players)
            {
                // Game rules or systems add slots that can be assigned to players.
                RaiseLocalEvent(new CollectingAssignmentsEvent(processingPlayers, jobAssignments, metaJobAssignments, metaPlayerAssignments));

                processingPlayers.Add(newPlayer);

                AssignAll();
            }
            ;
            Log.Debug($"""
                Collected job assignments:
                  {string.Join("\n  ", jobAssignments.Select(x => $"{x.Key}: {x.Value[0].AssignmentLimit}"))}

                Player assigmnets:
                  {string.Join("\n  ", processingPlayers.Select(x => $"{x.Session}: {x.AssignedJob?.JobID}"))}
                """);

            return (processingPlayers, jobAssignments);

            void AssignAll()
            {
                // Re-process all processed players so they can get more preferrable jobs if they were added to the pool.
                foreach (var player in processingPlayers)
                {
                    AssignPreferredAvailableJob(player, false);
                }

                // Now assign un-taken priority jobs (such as heads of departments)
                foreach (var weightedJob in weightedJobs)
                {
                    if (!jobAssignments.ContainsKey(weightedJob))
                        continue;

                    var reprocessStartIndex = processingPlayers.Count;
                    foreach (var unassigned in jobAssignments[weightedJob].Where(item => item.IsAssignable))
                    {
                        if (AssignWeightedJobs(unassigned) is { } assignedIndex
                            && assignedIndex < reprocessStartIndex)
                        {
                            reprocessStartIndex = assignedIndex;
                        }
                    }

                    // If someone was re-assigned we have to reprocess all players that came after,
                    // except we must not re-assign players who were assigned higher weight jobs.
                    foreach (var player in processingPlayers.Skip(reprocessStartIndex + 1))
                    {
                        AssignPreferredAvailableJob(player, true);
                    }
                }
            }

            int? AssignWeightedJobs(JobAssignment assignment)
            {
                if (assignment.AssignmentLimit is not { } assignmentLimit)
                {
                    Log.Error($"Attempted to assign a weighted job \"{assignment.JobID}\" with no assignment limit.");
                    return null;
                }
                var playersNeeded = assignment.AssignedPlayers.Count() - assignmentLimit;
                int? lowestAssigned = null;
                // Forcefully assigned weighted jobs are assigned in reverse lottery order
                for (var i = processingPlayers.Count - 1; i >= 0; --i)
                {
                    var player = processingPlayers[i];
                    if (player.AssignedJob is { } playerAssignment
                        && playerAssignment.JobPrototype.Weight > assignment.JobPrototype.Weight)
                        continue; // player has a higher weight job already

                    var preferenceIndex = player.JobPreferenceOrder.IndexOf(assignment.JobID);
                    if (preferenceIndex < 0)
                        continue; // player has this job set as Never

                    AssignToPlayer(player, assignment, preferenceIndex);
                    lowestAssigned = i;
                    --playersNeeded;
                    if (playersNeeded <= 0)
                        break;
                }
                return lowestAssigned;
            }

            void AssignPreferredAvailableJob(PlayerSpawnInfo player, bool keepHigherWeight)
            {
                // TODO add functionality that assigns players to desired meta assignments like Traitors (not needed in RMC)
                for (var i = 0; i < (player.AssignedPreferenceIndex ?? player.JobPreferenceOrder.Count); ++i)
                {
                    var preferredJob = player.JobPreferenceOrder[i];
                    foreach (var assignment in jobAssignments[preferredJob])
                    {
                        if (!assignment.IsAssignable)
                            continue;

                        if (keepHigherWeight
                            && player.AssignedJob != null
                            && assignment.JobPrototype.Weight < player.AssignedJob.JobPrototype.Weight)
                            continue;

                        AssignToPlayer(player, assignment, i);
                        return;
                    }
                }
            }

            void AssignToPlayer(PlayerSpawnInfo player, JobAssignment newAssignment, int preferenceIndex = 0)
            {
                if (!newAssignment.IsAssignable)
                {
                    Log.Error($"Tried to assign player {player.Session} to unassignable job {newAssignment.JobID}");
                    return;
                }

                var newPreferenceIndex = player.JobPreferenceOrder.IndexOf(newAssignment.JobID, preferenceIndex);
                if (newPreferenceIndex < 0)
                {
                    Log.Error($"Tried to assign player {player.Session} to Never job {newAssignment.JobID}");
                    return;
                }

                if (player.AssignedJob is { } oldAssignment)
                {
                    oldAssignment.AssignedPlayers.Remove(player);
                }

                newAssignment.AssignedPlayers.Add(player);
                player.AssignedJob = newAssignment;
                player.AssignedPreferenceIndex = newPreferenceIndex;
            }
        }

        private void RMCSpawnPlayer(PlayerSpawnInfo player)
        {
            if (player.AssignedJob == null)
                return;

            var ev = new RMCPlayerSpawningEvent(player);
            RaiseLocalEvent(ev);

            if (ev.Handled)
            {
                PlayerJoinGame(player.Session);
                return;
            }

            SpawnPlayer(player.Session, player.Profile, player.AssignedJob.StationId ?? EntityUid.Invalid, player.AssignedJob.JobID, false);
        }

        private void SpawnPlayers(List<ICommonSession> readyPlayers,
            Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
            bool force)
        {
            // Allow game rules to spawn players by themselves if needed. (For example, nuke ops or wizard)
            RaiseLocalEvent(new RulePlayerSpawningEvent(readyPlayers, profiles, force));

            var playerNetIds = readyPlayers.Select(o => o.UserId).ToHashSet();

            // RulePlayerSpawning feeds a readonlydictionary of profiles.
            // We need to take these players out of the pool of players available as they've been used.
            if (readyPlayers.Count != profiles.Count)
            {
                var toRemove = new RemQueue<NetUserId>();

                foreach (var (player, _) in profiles)
                {
                    if (playerNetIds.Contains(player))
                        continue;

                    toRemove.Add(player);
                }

                foreach (var player in toRemove)
                {
                    profiles.Remove(player);
                }
            }

            var spawnableStations = GetSpawnableStations();
            var assignedJobs = _stationJobs.AssignJobs(profiles, spawnableStations);

            _stationJobs.AssignOverflowJobs(ref assignedJobs, playerNetIds, profiles, spawnableStations);

            // Calculate extended access for stations.
            var stationJobCounts = spawnableStations.ToDictionary(e => e, _ => 0);
            foreach (var (netUser, (job, station)) in assignedJobs)
            {
                if (job == null)
                {
                    var playerSession = _playerManager.GetSessionById(netUser);
                    var evNoJobs = new NoJobsAvailableSpawningEvent(playerSession); // Used by gamerules to wipe their antag slot, if they got one
                    RaiseLocalEvent(evNoJobs);

                    _chatManager.DispatchServerMessage(playerSession, Loc.GetString("job-not-available-wait-in-lobby"));
                }
                else
                {
                    stationJobCounts[station] += 1;
                }
            }

            _stationJobs.CalcExtendedAccess(stationJobCounts);

            // Spawn everybody in!
            foreach (var (player, (job, station)) in assignedJobs)
            {
                if (job == null)
                    continue;

                SpawnPlayer(_playerManager.GetSessionById(player), profiles[player], station, job, false);
            }

            RefreshLateJoinAllowed();

            // Allow rules to add roles to players who have been spawned in. (For example, on-station traitors)
            RaiseLocalEvent(new RulePlayerJobsAssignedEvent(
                assignedJobs.Keys.Select(x => _playerManager.GetSessionById(x)).ToArray(),
                profiles,
                force));
        }

        private void SpawnPlayer(ICommonSession player,
            EntityUid station,
            string? jobId = null,
            bool lateJoin = true,
            bool silent = false)
        {
            var character = GetPlayerProfile(player);

            var jobBans = _banManager.GetJobBans(player.UserId);
            if (jobBans == null || jobId != null && jobBans.Contains(jobId))
                return;

            if (jobId != null)
            {
                var ev = new IsJobAllowedEvent(player, new ProtoId<JobPrototype>(jobId));
                RaiseLocalEvent(ref ev);
                if (ev.Cancelled)
                    return;
            }

            SpawnPlayer(player, character, station, jobId, lateJoin, silent);
        }

        private void SpawnPlayer(ICommonSession player,
            HumanoidCharacterProfile character,
            EntityUid station,
            string? jobId = null,
            bool lateJoin = true,
            bool silent = false)
        {
            // Can't spawn players with a dummy ticker!
            if (DummyTicker)
                return;

            if (station == EntityUid.Invalid)
            {
                var stations = GetSpawnableStations();
                _robustRandom.Shuffle(stations);
                if (stations.Count == 0)
                    station = EntityUid.Invalid;
                else
                    station = stations[0];
            }

            if (lateJoin && DisallowLateJoin)
            {
                JoinAsObserver(player);
                return;
            }

            string speciesId;
            if (_randomizeCharacters)
            {
                var weightId = _cfg.GetCVar(CCVars.ICRandomSpeciesWeights);

                // If blank, choose a round start species.
                if (string.IsNullOrEmpty(weightId))
                {
                    var roundStart = new List<ProtoId<SpeciesPrototype>>();

                    var speciesPrototypes = _prototypeManager.EnumeratePrototypes<SpeciesPrototype>();
                    foreach (var proto in speciesPrototypes)
                    {
                        if (proto.RoundStart)
                            roundStart.Add(proto.ID);
                    }

                    speciesId = roundStart.Count == 0
                        ? SharedHumanoidAppearanceSystem.DefaultSpecies
                        : _robustRandom.Pick(roundStart);
                }
                else
                {
                    var weights = _prototypeManager.Index<WeightedRandomSpeciesPrototype>(weightId);
                    speciesId = weights.Pick(_robustRandom);
                }

                character = HumanoidCharacterProfile.RandomWithSpecies(speciesId);
            }

            // We raise this event to allow other systems to handle spawning this player themselves. (e.g. late-join wizard, etc)
            var bev = new PlayerBeforeSpawnEvent(player, character, jobId, lateJoin, station);
            RaiseLocalEvent(bev);

            // Do nothing, something else has handled spawning this player for us!
            if (bev.Handled)
            {
                PlayerJoinGame(player, silent);
                return;
            }

            // Figure out job restrictions
            var restrictedRoles = new HashSet<ProtoId<JobPrototype>>();
            var ev = new GetDisallowedJobsEvent(player, restrictedRoles);
            RaiseLocalEvent(ref ev);

            var jobBans = _banManager.GetJobBans(player.UserId);
            if (jobBans != null)
                restrictedRoles.UnionWith(jobBans);

            // Pick best job best on prefs.
            jobId ??= _stationJobs.PickBestAvailableJobWithPriority(station,
                character.JobPriorities,
                true,
                restrictedRoles);
            // If no job available, stay in lobby, or if no lobby spawn as observer
            if (jobId is null)
            {
                if (!LobbyEnabled)
                {
                    JoinAsObserver(player);
                }

                var evNoJobs = new NoJobsAvailableSpawningEvent(player); // Used by gamerules to wipe their antag slot, if they got one
                RaiseLocalEvent(evNoJobs);

                _chatManager.DispatchServerMessage(player,
                    Loc.GetString("game-ticker-player-no-jobs-available-when-joining"));
                return;
            }

            PlayerJoinGame(player, silent);

            var data = player.ContentData();

            DebugTools.AssertNotNull(data);

            var newMind = _mind.CreateMind(data!.UserId, character.Name);
            _mind.SetUserId(newMind, data.UserId);

            var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);

            _playTimeTrackings.PlayerRolesChanged(player);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(station, jobId, character);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);

            _roles.MindAddJobRole(newMind, silent: silent, jobPrototype: jobId);
            var jobName = _jobs.MindTryGetJobName(newMind);
            _admin.UpdatePlayerList(player);

            if (lateJoin && !silent && false) // RMC14
            {
                if (jobPrototype.JoinNotifyCrew)
                {
                    _chatSystem.DispatchStationAnnouncement(station,
                        Loc.GetString("latejoin-arrival-announcement-special",
                            ("character", MetaData(mob).EntityName),
                            ("entity", mob),
                            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                        Loc.GetString("latejoin-arrival-sender"),
                        playDefaultSound: false,
                        colorOverride: Color.Gold);
                }
                else
                {
                    _chatSystem.DispatchStationAnnouncement(station,
                        Loc.GetString("latejoin-arrival-announcement",
                            ("character", MetaData(mob).EntityName),
                            ("entity", mob),
                            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                        Loc.GetString("latejoin-arrival-sender"),
                        playDefaultSound: false);
                }
            }

            if (player.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            {
                AddComp<OwOAccentComponent>(mob);
            }

            _stationJobs.TryAssignJob(station, jobPrototype, player.UserId);

            if (lateJoin)
            {
                _adminLogger.Add(LogType.LateJoin,
                    LogImpact.Medium,
                    $"Player {player.Name} late joined as {character.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");
            }
            else
            {
                _adminLogger.Add(LogType.RoundStartJoin,
                    LogImpact.Medium,
                    $"Player {player.Name} joined as {character.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");
            }

            // Make sure they're aware of extended access.
            if (Comp<StationJobsComponent>(station).ExtendedAccess
                && (jobPrototype.ExtendedAccess.Count > 0 || jobPrototype.ExtendedAccessGroups.Count > 0))
            {
                _chatManager.DispatchServerMessage(player, Loc.GetString("job-greet-crew-shortages"));
            }

            if (!silent && TryComp(station, out MetaDataComponent? metaData))
            {
                _chatManager.DispatchServerMessage(player,
                    Loc.GetString("job-greet-station-name", ("stationName", metaData.EntityName)));
            }

            if (_distressSignal?.SelectedPlanetMapName != null)
            {
                _chatManager.DispatchServerMessage(player,
                    Loc.GetString("job-greet-planet-name", ("planetName",_distressSignal.SelectedPlanetMapName)));
            }

            // We raise this event directed to the mob, but also broadcast it so game rules can do something now.
            PlayersJoinedRoundNormally++;
            var aev = new PlayerSpawnCompleteEvent(mob,
                player,
                jobId,
                lateJoin,
                silent,
                PlayersJoinedRoundNormally,
                station,
                character);
            RaiseLocalEvent(mob, aev, true);

            _marinePresenceAnnounce.AnnounceLateJoin(lateJoin, silent, mob, jobId, jobName, jobPrototype); // RMC14
        }

        public void Respawn(ICommonSession player)
        {
            _mind.WipeMind(player);
            _adminLogger.Add(LogType.Respawn, LogImpact.Medium, $"Player {player} was respawned.");

            if (LobbyEnabled)
                PlayerJoinLobby(player);
            else
                SpawnPlayer(player, EntityUid.Invalid);
        }

        /// <summary>
        /// Makes a player join into the game and spawn on a station.
        /// </summary>
        /// <param name="player">The player joining</param>
        /// <param name="station">The station they're spawning on</param>
        /// <param name="jobId">An optional job for them to spawn as</param>
        /// <param name="silent">Whether or not the player should be greeted upon joining</param>
        public void MakeJoinGame(ICommonSession player, EntityUid station, string? jobId = null, bool silent = false)
        {
            if (!_playerGameStatuses.ContainsKey(player.UserId))
                return;

            if (!_userDb.IsLoadComplete(player))
                return;

            SpawnPlayer(player, station, jobId, silent: silent);
        }

        /// <summary>
        /// Causes the given player to join the current game as observer ghost. See also <see cref="SpawnObserver"/>
        /// </summary>
        public void JoinAsObserver(ICommonSession player)
        {
            // Can't spawn players with a dummy ticker!
            if (DummyTicker)
                return;

            PlayerJoinGame(player);
            SpawnObserver(player);
        }

        /// <summary>
        /// Spawns an observer ghost and attaches the given player to it. If the player does not yet have a mind, the
        /// player is given a new mind with the observer role. Otherwise, the current mind is transferred to the ghost.
        /// </summary>
        public void SpawnObserver(ICommonSession player)
        {
            if (DummyTicker)
                return;

            var makeObserver = false;
            Entity<MindComponent?>? mind = player.GetMind();
            if (mind == null)
            {
                var name = GetPlayerProfile(player).Name;
                var (mindId, mindComp) = _mind.CreateMind(player.UserId, name);
                mind = (mindId, mindComp);
                _mind.SetUserId(mind.Value, player.UserId);
                makeObserver = true;
            }

            var ghost = _ghost.SpawnGhost(mind.Value);
            if (makeObserver)
                _roles.MindAddRole(mind.Value, "MindRoleObserver");

            _adminLogger.Add(LogType.LateJoin,
                LogImpact.Low,
                $"{player.Name} late joined the round as an Observer with {ToPrettyString(ghost):entity}.");
        }

        #region Spawn Points

        public EntityCoordinates GetObserverSpawnPoint()
        {
            _possiblePositions.Clear();
            var spawnPointQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (spawnPointQuery.MoveNext(out var uid, out var point, out var transform))
            {
                if (point.SpawnType != SpawnPointType.Observer
                   || TerminatingOrDeleted(uid)
                   || transform.MapUid == null
                   || TerminatingOrDeleted(transform.MapUid.Value))
                {
                    continue;
                }

                _possiblePositions.Add(transform.Coordinates);
            }

            var metaQuery = GetEntityQuery<MetaDataComponent>();

            // Fallback to a random grid.
            if (_possiblePositions.Count == 0)
            {
                var query = AllEntityQuery<MapGridComponent>();
                while (query.MoveNext(out var uid, out var grid))
                {
                    if (!metaQuery.TryGetComponent(uid, out var meta) || meta.EntityPaused || TerminatingOrDeleted(uid))
                    {
                        continue;
                    }

                    _possiblePositions.Add(new EntityCoordinates(uid, Vector2.Zero));
                }
            }

            if (_possiblePositions.Count != 0)
            {
                // TODO: This is just here for the eye lerping.
                // Ideally engine would just spawn them on grid directly I guess? Right now grid traversal is handling it during
                // update which means we need to add a hack somewhere around it.
                var spawn = _robustRandom.Pick(_possiblePositions);
                var toMap = _transform.ToMapCoordinates(spawn);

                if (_mapManager.TryFindGridAt(toMap, out var gridUid, out _))
                {
                    var gridXform = Transform(gridUid);

                    return new EntityCoordinates(gridUid, Vector2.Transform(toMap.Position, _transform.GetInvWorldMatrix(gridXform)));
                }

                return spawn;
            }

            if (_map.MapExists(DefaultMap))
            {
                var mapUid = _map.GetMapOrInvalid(DefaultMap);
                if (!TerminatingOrDeleted(mapUid))
                    return new EntityCoordinates(mapUid, Vector2.Zero);
            }

            // Just pick a point at this point I guess.
            foreach (var map in _map.GetAllMapIds())
            {
                var mapUid = _map.GetMapOrInvalid(map);

                if (!metaQuery.TryGetComponent(mapUid, out var meta)
                    || meta.EntityPaused
                    || TerminatingOrDeleted(mapUid))
                {
                    continue;
                }

                return new EntityCoordinates(mapUid, Vector2.Zero);
            }

            // AAAAAAAAAAAAA
            // This should be an error, if it didn't cause tests to start erroring when they delete a player.
            _sawmill.Warning("Found no observer spawn points!");
            return EntityCoordinates.Invalid;
        }

        #endregion
    }

    // RMC start
    /// <summary>
    /// A job that a player can start as at round start.
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="stationId"></param>
    public sealed class JobAssignment(JobPrototype jobPrototype, EntityUid? stationId)
    {
        public JobPrototype JobPrototype = jobPrototype;
        public EntityUid? StationId = stationId;
        public HashSet<PlayerSpawnInfo> AssignedPlayers = new HashSet<PlayerSpawnInfo>();
        public HashSet<MetaJobAssignment> MetaAssignments = new HashSet<MetaJobAssignment>();

        /// <summary>
        /// The max amount of assigned players this job can have. Null if unlimited.
        /// </summary>
        public int? AssignmentLimit = null;

        /// <summary>
        /// If this assignment is at max capacity
        /// </summary>
        public bool IsFull
        {
            get
            {
                return AssignmentLimit != null && AssignedPlayers.Count >= AssignmentLimit.Value;
            }
        }

        /// <summary>
        /// If this assignment and all its meta assignments have space for another player.
        /// </summary>
        public bool IsAssignable
        {
            get
            {
                return !IsFull && MetaAssignments.All(m => !m.IsFull);
            }
        }

        public ProtoId<JobPrototype> JobID
        {
            get
            {
                return JobPrototype.ID;
            }
        }
    }

    /// <summary>
    /// A category for job assignments. Allows job assignments to be "linked" so that there is a player limit across multiple jobs,
    /// or allows classifying jobs so that a player can specify a preference for a broader category.
    ///
    /// For an RMC example, "Survivor" is a meta job assignment. Although there is a max of say 4 engineers, 3 doctors, 2 security,
    /// and no limit for civilian survs, the maximum amount of "Survivor" roles total may be 7. You can't have 4 engineers, 3 doctors,
    /// and 2 security all together, even if that is the maximum amount of each role individually.
    /// </summary>
    /// <param name="name"></param>
    public sealed class MetaJobAssignment(string name)
    {
        public string Name = name;
        public HashSet<JobAssignment> Assignments = new HashSet<JobAssignment>();
        public HashSet<string> Tags = new HashSet<string>();

        /// <summary>
        /// The max amount of assigned players this meta assignment can have. Null if unlimited.
        /// </summary>
        public int? AssignmentLimit = null;

        /// <summary>
        /// If this meta assignment is at max capacity.
        /// </summary>
        public bool IsFull
        {
            get
            {
                if (AssignmentLimit is not { } limit)
                    return false;

                var filledCount = 0;
                foreach (var assignment in Assignments)
                {
                    filledCount += assignment.AssignedPlayers.Count;
                }
                return filledCount >= limit;
            }
        }
    }

    /// <summary>
    /// A category for player assignments. Allows players assignments to be categorized further than just by the job they have,
    /// potentially having a limit that isn't linked to any particular job.
    ///
    /// For example, "Traitor" is a meta player assignment. There are only so many traitor slots at the start of the round
    /// that can be filled, but if the traitor assignments are maxed out that does not limit which jobs can be taken.
    /// </summary>
    /// <param name="name"></param>
    public sealed class MetaPlayerAssignment(string name)
    {
        public string Name = name;
        public HashSet<PlayerSpawnInfo> AssignedPlayers = new HashSet<PlayerSpawnInfo>();

        /// <summary>
        /// The only jobs that are allowed to be assigned to players with this meta assignment.
        /// </summary>
        public HashSet<ProtoId<JobPrototype>> JobWhitelist = new HashSet<ProtoId<JobPrototype>>();

        /// <summary>
        /// Jobs that are not allowed to be assigned to players with this meta assignment.
        /// </summary>
        public HashSet<ProtoId<JobPrototype>> JobBlacklist = new HashSet<ProtoId<JobPrototype>>();

        /// <summary>
        /// The max amount of assigned players this meta assignment can have. Null if unlimited.
        /// </summary>
        public int? AssignmentLimit = null;

        /// <summary>
        /// If this meta assignment is at max capacity.
        /// </summary>
        public bool IsFull
        {
            get
            {
                return AssignmentLimit != null && AssignedPlayers.Count >= AssignmentLimit.Value;
            }
        }
    }

    public sealed class PlayerSpawnInfo(ICommonSession player, HumanoidCharacterProfile profile)
    {
        public ICommonSession Session = player;
        public HumanoidCharacterProfile Profile = profile;
        public JobAssignment? AssignedJob;
        public HashSet<MetaPlayerAssignment> MetaAssignments = new HashSet<MetaPlayerAssignment>();

        // The list of jobs this player prefers, in preference order.
        public List<ProtoId<JobPrototype>> JobPreferenceOrder = new List<ProtoId<JobPrototype>>();

        // The index of the job the player is currently assigned.
        public int? AssignedPreferenceIndex;
    }

    /// <summary>
    /// Event raised before any players are assigned, to allow systems to set up initial job assignments.
    /// </summary>
    /// <param name="jobAssignments"></param>
    /// <param name="metaJobAssignments"></param>
    /// <param name="metaPlayerAssignments"></param>
    public sealed class InitializingAssignmentsEvent(
        JobAssignmentsDict jobAssignments,
        HashSet<MetaJobAssignment> metaJobAssignments,
        HashSet<MetaPlayerAssignment> metaPlayerAssignments)
    {
        public readonly JobAssignmentsDict JobAssignments = jobAssignments;
        public readonly HashSet<MetaJobAssignment> MetaJobAssignments = metaJobAssignments;
        public readonly HashSet<MetaPlayerAssignment> MetaPlayerAssignments = metaPlayerAssignments;
    }

    /// <summary>
    ///     Event raised before each player is assigned, to allow systems and rules to add
    ///     assignments that players can be assigned to.
    ///     This event is not involved in actually assigning players, only for collecting the available assignments.
    /// </summary>
    public sealed class CollectingAssignmentsEvent(
        List<PlayerSpawnInfo> processedPlayers,
        JobAssignmentsDict jobAssignments,
        HashSet<MetaJobAssignment> metaJobAssignments,
        HashSet<MetaPlayerAssignment> metaPlayerAssignments)
    {
        public readonly List<PlayerSpawnInfo> ProcessedPlayers = processedPlayers;
        public readonly JobAssignmentsDict JobAssignments = jobAssignments;
        public readonly HashSet<MetaJobAssignment> MetaJobAssignments = metaJobAssignments;
        public readonly HashSet<MetaPlayerAssignment> MetaPlayerAssignments = metaPlayerAssignments;
    }

    /// <summary>
    ///     Event raised after all players have been assigned and spawned in.
    ///     Note that processed players may not necessarily have been assigned,
    ///     due to not getting one of their preferred roles.
    /// </summary>
    public sealed class RoundstartPlayersSpawnedEvent(
        List<PlayerSpawnInfo> processedPlayers,
        JobAssignmentsDict jobAssignments)
    {
        public readonly List<PlayerSpawnInfo> ProcessedPlayers = processedPlayers;
        public readonly JobAssignmentsDict JobAssignments = jobAssignments;
    }

    public sealed class RMCPlayerSpawningEvent(
        PlayerSpawnInfo player) : HandledEntityEventArgs
    {
        public readonly PlayerSpawnInfo Player = player;
    }
    // RMC end
}
