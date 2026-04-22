using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    /// <summary>
    /// Main survivor spawning handler. Delegates to helper methods for job collection,
    /// candidate assignment, and individual survivor spawning.
    /// </summary>
    /// <param name="comp">The distress signal rule component.</param>
    /// <param name="ev">The rule player spawning event.</param>
    /// <param name="initialPlayerCount">Player count before any players were removed from the pool (for correct totalSurvivors calculation).</param>
    private void SpawnSurvivors(CMDistressSignalRuleComponent comp, RulePlayerSpawningEvent ev, int initialPlayerCount)
    {
        if (!comp.SpawnSurvivors || comp.SurvivorJobs.Count == 0)
            return;

        if (ActiveNightmareScenario == null)
        {
            var compCopy = comp;
            IEnumerable<(ProtoId<JobPrototype> Job, int Amount)> jobs = comp.SurvivorJobs
                .Where(entry => entry.Job != compCopy.CivilianSurvivorJob)
                .OrderBy(_ => _random.Next());

            if (comp.SurvivorJobs.TryFirstOrNull(entry => entry.Job == compCopy.CivilianSurvivorJob, out var civJob))
                jobs = jobs.Append(civJob.Value);

            comp.SurvivorJobs = jobs.ToList();
        }

        var possibleJobs = CollectPossibleSurvivorJobs(comp);
        var (spawners, spawnersLeft) = FindSurvivorSpawners(possibleJobs);
        var candidates = CollectSurvivorCandidates(comp, ev);

        SpawnSurvivorsFromCandidates(comp, ev, candidates, spawners, spawnersLeft, initialPlayerCount);
    }

    private List<ProtoId<JobPrototype>> CollectPossibleSurvivorJobs(CMDistressSignalRuleComponent comp)
    {
        var jobs = new List<ProtoId<JobPrototype>>();
        jobs.AddRange(comp.SurvivorJobs.Select(x => x.Job));
        if (comp.SurvivorJobOverrides != null)
            jobs.AddRange(comp.SurvivorJobOverrides.Values);
        if (comp.SurvivorJobVariants != null)
            jobs.AddRange(comp.SurvivorJobVariants.Values.SelectMany(x => x).Select(x => x.Variant));
        if (comp.SurvivorJobVariantScenarios != null)
            jobs.AddRange(comp.SurvivorJobVariantScenarios.Values.SelectMany(x => x).Select(x => x.Special));
        return jobs;
    }

    private (Dictionary<ProtoId<JobPrototype>, List<EntityUid>> Spawners,
             Dictionary<ProtoId<JobPrototype>, List<EntityUid>> SpawnersLeft)
        FindSurvivorSpawners(List<ProtoId<JobPrototype>> possibleJobs)
    {
        var spawners = new Dictionary<ProtoId<JobPrototype>, List<EntityUid>>();
        var spawnerQuery = EntityQueryEnumerator<SpawnPointComponent>();
        while (spawnerQuery.MoveNext(out var spawnId, out var spawnComp))
        {
            if (spawnComp.Job is { } job && possibleJobs.Contains(job))
                spawners.GetOrNew(job).Add(spawnId);
        }

        var spawnersLeft = spawners.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        return (spawners, spawnersLeft);
    }

    private Dictionary<ProtoId<JobPrototype>, List<NetUserId>[]> CollectSurvivorCandidates(
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev)
    {
        var priorities = Enum.GetValues<JobPriority>().Length;
        var candidates = new Dictionary<ProtoId<JobPrototype>, List<NetUserId>[]>();

        foreach (var job in comp.SurvivorJobs)
        {
            var jobList = new List<NetUserId>[priorities];
            for (var i = 0; i < jobList.Length; i++)
            {
                jobList[i] = [];
            }
            candidates[job.Job] = jobList;
        }

        foreach (var player in ev.PlayerPool)
        {
            foreach (var (job, players) in candidates)
            {
                TryAddSurvivorCandidate(player.UserId, job, players, comp, ev);
            }
        }

        return candidates;
    }

    private void TryAddSurvivorCandidate(
        NetUserId id,
        ProtoId<JobPrototype> job,
        List<NetUserId>[] players,
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev)
    {
        if (!IsJobAllowed(id, comp.CivilianSurvivorJob) || !IsJobAllowed(id, job))
            return;

        if (comp.SurvivorJobOverrides != null)
        {
            foreach (var (originalJob, overrideJob) in comp.SurvivorJobOverrides)
            {
                if (overrideJob != job)
                    continue;

                var hasOverridePriority = TryGetSurvivorPriority(id, originalJob, comp, ev, out var overridePrio);
                if (TryGetPriority(id, overrideJob, ev, out var directOverridePrio) &&
                    directOverridePrio > overridePrio)
                {
                    hasOverridePriority = true;
                    overridePrio = directOverridePrio;
                }

                if (!hasOverridePriority)
                    continue;

                players[(int)overridePrio].Add(id);
                return;
            }
        }

        if (TryGetSurvivorPriority(id, job, comp, ev, out var prio))
        {
            players[(int)prio].Add(id);
        }
    }

    private bool TryGetSurvivorPriority(
        NetUserId id,
        ProtoId<JobPrototype> job,
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev,
        out JobPriority priority)
    {
        priority = JobPriority.Never;

        if (TryGetPriority(id, job, ev, out priority))
            return true;

        if (!_prefsManager.TryGetCachedPreferences(id, out var preferences))
            return false;

        return preferences.JobPriorities.TryGetValue(job, out priority) &&
               priority > JobPriority.Never &&
               HasSurvivorProfileForJob(preferences, job, comp);
    }

    private void SpawnSurvivorsFromCandidates(
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev,
        Dictionary<ProtoId<JobPrototype>, List<NetUserId>[]> candidates,
        Dictionary<ProtoId<JobPrototype>, List<EntityUid>> spawners,
        Dictionary<ProtoId<JobPrototype>, List<EntityUid>> spawnersLeft,
        int initialPlayerCount)
    {
        var priorities = Enum.GetValues<JobPriority>().Length;
        var totalSurvivors = (int)Math.Clamp((int)Math.Round(initialPlayerCount / _marinesPerSurvivor), _minimumSurvivors, _maximumSurvivors);
        var selected = 0;

        for (var i = priorities - 1; i >= 0; i--)
        {
            foreach (var (job, players) in candidates)
            {
                var ignoreLimit = comp.IgnoreMaximumSurvivorJobs.Contains(job);
                var playerNames = players[i].Select(p => ev.Profiles.TryGetValue(p, out var prof) ? prof.Name : p.ToString());
                Log.Info($"Rolling survivor job {job} with priority {i} and players {string.Join(", ", playerNames)}");
                while (players[i].Count > 0 && (ignoreLimit || selected < totalSurvivors))
                {
                    if (TrySpawnSingleSurvivor(job, players[i], comp, ev, spawnersLeft, spawners, out var playerId))
                    {
                        var name = ev.Profiles.TryGetValue(playerId, out var prof) ? prof.Name : playerId.ToString();
                        Log.Info($"Spawned survivor job {job} with name/id {name}, ignore limit: {ignoreLimit}");
                        RemoveCandidateFromAllLists(candidates, playerId);
                        if (!ignoreLimit)
                            selected++;
                    }
                    else
                    {
                        Log.Info($"Stopped rolling survivor job {job}");
                        break;
                    }
                }
            }
        }
    }

    private void RemoveCandidateFromAllLists(
        Dictionary<ProtoId<JobPrototype>, List<NetUserId>[]> candidates,
        NetUserId playerId)
    {
        foreach (var (_, priorityLists) in candidates)
        {
            foreach (var list in priorityLists)
            {
                list.Remove(playerId);
            }
        }
    }

    private bool TrySpawnSingleSurvivor(
        ProtoId<JobPrototype> job,
        List<NetUserId> list,
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev,
        Dictionary<ProtoId<JobPrototype>, List<EntityUid>> spawnersLeft,
        Dictionary<ProtoId<JobPrototype>, List<EntityUid>> spawners,
        out NetUserId spawnedId)
    {
        var playerId = _random.Pick(list);
        if (!_player.TryGetSessionById(playerId, out var player))
        {
            list.Remove(playerId);
            spawnedId = default;
            return false;
        }

        var spawnAsJob = DetermineSurvivorJob(job, playerId, comp, ev, out var scenarioSuccess, out var stop);
        if (stop)
        {
            spawnedId = default;
            return false;
        }

        var selectRandomVariant = SelectedPlanetMap?.Comp.SelectRandomSurvivorVariant ?? false;
        if (!DecrementOriginalJobSlot(job, comp, selectRandomVariant, scenarioSuccess, ref spawnAsJob))
        {
            spawnedId = default;
            return false;
        }

        var spawner = FindSurvivorSpawner(spawnAsJob, comp, spawnersLeft, spawners, out stop);
        if (stop)
        {
            spawnedId = default;
            return false;
        }

        list.Remove(playerId);
        ev.PlayerPool.Remove(player);
        GameTicker.PlayerJoinGame(player);

        var profile = SelectSpawnProfile(player, spawnAsJob, job);
        var coordinates = _transform.GetMoverCoordinates(spawner);
        var survivorMob = _stationSpawning.SpawnPlayerMob(coordinates, spawnAsJob, profile, null);

        if (!_mind.TryGetMind(playerId, out var mind))
            mind = _mind.CreateMind(playerId);

        RemCompDeferred<TacticalMapUserComponent>(survivorMob);
        _mind.TransferTo(mind.Value, survivorMob);
        _roles.MindAddJobRole(mind.Value, jobPrototype: spawnAsJob);
        _playTime.PlayerRolesChanged(player);

        RaiseLocalEvent(survivorMob, new PlayerSpawnCompleteEvent(survivorMob, player, spawnAsJob, false, true, 0, default, profile), true);

        spawnedId = playerId;
        return true;
    }

    private ProtoId<JobPrototype> DetermineSurvivorJob(
        ProtoId<JobPrototype> job,
        NetUserId playerId,
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev,
        out bool scenarioSuccess,
        out bool stop)
    {
        stop = false;
        var spawnAsJob = job;

        scenarioSuccess = TryGetScenarioJob(job, playerId, comp, ev, ref spawnAsJob, ref stop);
        if (stop)
            return spawnAsJob;

        if (!scenarioSuccess)
        {
            CheckVariantJob(job, playerId, comp, ev, ref spawnAsJob, ref stop);
        }

        return spawnAsJob;
    }

    private bool TryGetScenarioJob(
        ProtoId<JobPrototype> job,
        NetUserId playerId,
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev,
        ref ProtoId<JobPrototype> spawnAsJob,
        ref bool stop)
    {
        if (comp.SurvivorJobVariantScenarios == null ||
            !comp.SurvivorJobVariantScenarios.TryGetValue(job, out var scenarioJobsList))
        {
            return false;
        }

        if (TryGetPreferredSurvivorJobIndex(playerId, scenarioJobsList, ev, out var preferredIndex) &&
            TryUseSurvivorJobAtIndex(playerId, scenarioJobsList, preferredIndex, ref spawnAsJob))
        {
            return true;
        }

        for (var i = 0; i < scenarioJobsList.Count; i++)
        {
            if (TryUseSurvivorJobAtIndex(playerId, scenarioJobsList, i, ref spawnAsJob))
                return true;
        }

        stop = true;
        return true;
    }

    /// <summary>
    /// Checks if a variant job can be assigned. Matches legacy: iterates in order, picks first allowed.
    /// Does NOT use random selection — selectRandomVariant only affects DecrementOriginalJobSlot.
    /// </summary>
    private void CheckVariantJob(
        ProtoId<JobPrototype> job,
        NetUserId playerId,
        CMDistressSignalRuleComponent comp,
        RulePlayerSpawningEvent ev,
        ref ProtoId<JobPrototype> spawnAsJob,
        ref bool stop)
    {
        if (comp.SurvivorJobVariants == null ||
            !comp.SurvivorJobVariants.TryGetValue(job, out var variants))
        {
            return;
        }

        if (TryGetPreferredSurvivorJobIndex(playerId, variants, ev, out var preferredIndex) &&
            TryUseSurvivorJobAtIndex(playerId, variants, preferredIndex, ref spawnAsJob))
        {
            return;
        }

        for (var i = 0; i < variants.Count; i++)
        {
            if (TryUseSurvivorJobAtIndex(playerId, variants, i, ref spawnAsJob))
                return;
        }

        stop = true;
    }

    private bool TryGetPreferredSurvivorJobIndex(
        NetUserId playerId,
        List<(ProtoId<JobPrototype> Job, int Amount)> jobs,
        RulePlayerSpawningEvent ev,
        out int selectedIndex)
    {
        selectedIndex = -1;

        for (var i = 0; i < jobs.Count; i++)
        {
            var (job, amount) = jobs[i];
            if (amount != -1 && amount <= 0)
                continue;

            if (!IsJobAllowed(playerId, job))
                continue;

            if (!HasProfileForJob(playerId, job, ev))
                continue;

            selectedIndex = i;
            return true;
        }

        return false;
    }

    private bool HasProfileForJob(
        NetUserId playerId,
        ProtoId<JobPrototype> job,
        RulePlayerSpawningEvent ev)
    {
        if (_prefsManager.TryGetCachedPreferences(playerId, out var preferences))
            return preferences.SelectProfileForJob(job, SelectedPlanetMapId) != null;

        return ev.Profiles.TryGetValue(playerId, out var profile) &&
               profile.JobPreferences.Contains(job);
    }

    private bool HasSurvivorProfileForJob(
        PlayerPreferences preferences,
        ProtoId<JobPrototype> job,
        CMDistressSignalRuleComponent comp)
    {
        if (preferences.SelectProfileForJob(job, SelectedPlanetMapId) != null)
            return true;

        if (comp.SurvivorJobVariants != null &&
            comp.SurvivorJobVariants.TryGetValue(job, out var variants))
        {
            foreach (var (variant, _) in variants)
            {
                if (preferences.SelectProfileForJob(variant, SelectedPlanetMapId) != null)
                    return true;
            }
        }

        if (comp.SurvivorJobVariantScenarios != null &&
            comp.SurvivorJobVariantScenarios.TryGetValue(job, out var scenarioVariants))
        {
            foreach (var (variant, _) in scenarioVariants)
            {
                if (preferences.SelectProfileForJob(variant, SelectedPlanetMapId) != null)
                    return true;
            }
        }

        if (comp.SurvivorJobOverrides != null &&
            comp.SurvivorJobOverrides.TryGetValue(job, out var overrideJob))
        {
            return preferences.SelectProfileForJob(overrideJob, SelectedPlanetMapId) != null;
        }

        return false;
    }

    private bool TryUseSurvivorJobAtIndex(
        NetUserId playerId,
        List<(ProtoId<JobPrototype> Job, int Amount)> jobs,
        int index,
        ref ProtoId<JobPrototype> spawnAsJob)
    {
        var (job, amount) = jobs[index];
        if (!IsJobAllowed(playerId, job))
            return false;

        if (amount == -1)
        {
            spawnAsJob = job;
            return true;
        }

        if (amount <= 0)
            return false;

        jobs[index] = (job, amount - 1);
        spawnAsJob = job;
        return true;
    }

    /// <summary>
    /// Decrements the original job's slot count. Also, handles random variant override when enabled and the scenario didn't succeed.
    /// This matches legacy behavior where the decrement always targets the original job, not the variant.
    /// </summary>
    private bool DecrementOriginalJobSlot(
        ProtoId<JobPrototype> job,
        CMDistressSignalRuleComponent comp,
        bool selectRandomVariant,
        bool scenarioSuccess,
        ref ProtoId<JobPrototype> spawnAsJob)
    {
        for (var i = 0; i < comp.SurvivorJobs.Count; i++)
        {
            var (survJob, amount) = comp.SurvivorJobs[i];
            if (survJob != job)
                continue;

            if (!scenarioSuccess && spawnAsJob == job && selectRandomVariant &&
                comp.SurvivorJobVariants != null &&
                comp.SurvivorJobVariants.TryGetValue(job, out var randomInsertList) &&
                randomInsertList.Count > 0)
            {
                spawnAsJob = _random.Pick(randomInsertList).Variant;
            }

            if (amount == -1)
                return true;

            if (amount <= 0)
                return false;

            comp.SurvivorJobs[i] = (survJob, amount - 1);
        }
        return true;
    }

    private EntityUid FindSurvivorSpawner(
        ProtoId<JobPrototype> spawnAsJob,
        CMDistressSignalRuleComponent comp,
        Dictionary<ProtoId<JobPrototype>, List<EntityUid>> spawnersLeft,
        Dictionary<ProtoId<JobPrototype>, List<EntityUid>> spawners,
        out bool stop)
    {
        stop = false;

        if (!spawnersLeft.TryGetValue(spawnAsJob, out var jobSpawners) &&
            !spawnersLeft.TryGetValue(comp.CivilianSurvivorJob, out jobSpawners))
        {
            stop = true;
            return default;
        }

        if (jobSpawners.Count == 0)
        {
            if (spawners.TryGetValue(comp.CivilianSurvivorJob, out var fallbackSpawners))
            {
                jobSpawners.Clear();
                jobSpawners.AddRange(fallbackSpawners);
            }

            if (jobSpawners.Count == 0)
            {
                stop = true;
                return default;
            }
        }

        return _random.PickAndTake(jobSpawners);
    }
}
