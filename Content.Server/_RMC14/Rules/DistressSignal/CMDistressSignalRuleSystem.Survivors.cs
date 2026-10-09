using System.Linq;
using Content.Server.Database;
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
    /// Spawns a player as the survivor they're assigned as.
    /// </summary>
    /// <param name="player"></param>
    private void SpawnSurvivor(PlayerSpawnInfo player, CMDistressSignalRuleComponent comp)
    {
        if (player.AssignedJob is not { } assignment)
            return;

        Log.Debug($"Trying to spawn {player.Session} as survivor {player.AssignedJob.JobID}");

        var playerId = player.Session.UserId;

        var actualJob = DetermineSurvivorJob(assignment.JobID, player.Session.UserId, comp, out var _, out var _);

        if (!_survivorSpawners.TryGetValue(actualJob, out var spawners))
        {
            // No spawners exist for their actual job. Use civilian instead.
            if (!_survivorSpawners.TryGetValue(comp.CivilianSurvivorJob, out spawners))
            {
                // No spawners exist for civilian jobs either. Probably a mapping error?
                // Don't spawn the surv.
                Log.Error($"Failed to find spawners for {actualJob} or {comp.CivilianSurvivorJob}. Could not spawn survivor {player.Session}.");
                return;
            }

            if (spawners.Count <= 0)
            {
                // Ran out of civilian spawn locations. Repopulate them.
                var spawnerQuery = EntityQueryEnumerator<SpawnPointComponent>();
                while (spawnerQuery.MoveNext(out var spawnId, out var spawnComp))
                {
                    if (spawnComp.Job == comp.CivilianSurvivorJob)
                        spawners.Add(spawnId);
                }
            }
        }
        else if (spawners.Count <= 0)
        {
            // Spawners exist for their actual job but we ran out of spawners. Repopulate them.
            var spawnerQuery = EntityQueryEnumerator<SpawnPointComponent>();
            while (spawnerQuery.MoveNext(out var spawnId, out var spawnComp))
            {
                if (spawnComp.Job == actualJob)
                    spawners.Add(spawnId);
            }
        }

        if (spawners.Count <= 0)
        {
            // Even after trying to repopulate spawners, we still ended up with none. Something went wrong.
            Log.Error($"Failed to repopulate spawners for either {actualJob} or {comp.CivilianSurvivorJob}. Could not spawn survivor {player.Session}.");
            return;
        }

        var spawner = _random.PickAndTake(spawners);

        var survivorMob = _stationSpawning.SpawnPlayerMob(
            _transform.GetMoverCoordinates(spawner),
            actualJob,
            player.Profile,
            null);

        if (!_mind.TryGetMind(playerId, out var mind))
            mind = _mind.CreateMind(playerId);

        RemCompDeferred<TacticalMapUserComponent>(survivorMob);
        _mind.TransferTo(mind.Value, survivorMob);
        _roles.MindAddJobRole(mind.Value, jobPrototype: actualJob);
        _playTime.PlayerRolesChanged(player.Session);

        RaiseLocalEvent(survivorMob, new PlayerSpawnCompleteEvent(survivorMob, player.Session, actualJob, false, true, 0, default, player.Profile), true);
    }

    private ProtoId<JobPrototype> DetermineSurvivorJob(
        ProtoId<JobPrototype> job,
        NetUserId playerId,
        CMDistressSignalRuleComponent comp,
        out bool scenarioSuccess,
        out bool stop)
    {
        stop = false;
        var spawnAsJob = job;

        scenarioSuccess = TryGetScenarioJob(job, playerId, comp, ref spawnAsJob, ref stop);
        if (stop)
            return spawnAsJob;

        if (!scenarioSuccess)
        {
            CheckVariantJob(job, playerId, comp, ref spawnAsJob, ref stop);
        }

        return spawnAsJob;
    }

    private bool TryGetScenarioJob(
        ProtoId<JobPrototype> job,
        NetUserId playerId,
        CMDistressSignalRuleComponent comp,
        ref ProtoId<JobPrototype> spawnAsJob,
        ref bool stop)
    {
        if (comp.SurvivorJobVariantScenarios == null ||
            !comp.SurvivorJobVariantScenarios.TryGetValue(job, out var scenarioJobsList))
        {
            return false;
        }

        for (var i = 0; i < scenarioJobsList.Count; i++)
        {
            var (scenarioJob, amount) = scenarioJobsList[i];
            if (!IsJobAllowed(playerId, scenarioJob))
                continue;

            if (amount == -1)
            {
                spawnAsJob = scenarioJob;
                return true;
            }

            if (amount <= 0)
                continue;

            scenarioJobsList[i] = (scenarioJob, amount - 1);
            spawnAsJob = scenarioJob;
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
        ref ProtoId<JobPrototype> spawnAsJob,
        ref bool stop)
    {
        if (comp.SurvivorJobVariants == null ||
            !comp.SurvivorJobVariants.TryGetValue(job, out var variants))
        {
            return;
        }

        for (var i = 0; i < variants.Count; i++)
        {
            var (variantJob, amount) = variants[i];
            if (!IsJobAllowed(playerId, variantJob))
                continue;

            if (amount == -1)
            {
                spawnAsJob = variantJob;
                return;
            }

            if (amount <= 0)
                continue;

            variants[i] = (variantJob, amount - 1);
            spawnAsJob = variantJob;
            return;
        }

        stop = true;
    }
}
