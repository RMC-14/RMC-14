using Content.Server.GameTicking.Events;
using Content.Shared.GameTicking;
using Robust.Shared.Spawners;
using Robust.Shared.Random;
using System.Collections.Generic;
using Content.Server.Humanoid.Systems;
using Content.Shared._RMC14.Intel;

namespace Content.Server._RMC14.Spawners;

/// <summary>
/// Dedicated system for managing AEGIS corpse spawner activation and persistence
/// </summary>
public sealed class AegisCorpseSpawnerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;

    /// <summary>
    /// Global flag that determines if AEGIS corpse spawners should be activated this round
    /// Once set, persists until round ends
    /// </summary>
    private bool _shouldActivateAegisCorpseSpawners = false;

    /// <summary>
    /// Flag to track if AEGIS corpse spawners have been spawned this round to prevent duplicates
    /// </summary>
    private bool _aegisCorpseSpawnedThisRound = false;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        // Reset AEGIS corpse flags for new round
        _shouldActivateAegisCorpseSpawners = false;
        _aegisCorpseSpawnedThisRound = false;
    }

    public override void Update(float frameTime)
    {
        // Check for AEGIS corpse spawners if flag is set and we haven't spawned yet this round
        if (_shouldActivateAegisCorpseSpawners && !_aegisCorpseSpawnedThisRound)
        {
            var aegisCorpseSpawners = new List<(EntityUid Uid, AegisCorpseSpawnerComponent Component)>();
            // Collect all valid AEGIS corpse spawners
            var aegisQuery = EntityQueryEnumerator<AegisCorpseSpawnerComponent>();
            while (aegisQuery.MoveNext(out var uid, out var spawner))
            {
                if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                    continue;

                aegisCorpseSpawners.Add((uid, spawner));
            }

            // Only spawn ONE randomly selected spawner
            if (aegisCorpseSpawners.Count > 0)
            {
                var selectedSpawner = _random.Pick(aegisCorpseSpawners);
                var coordinates = _transform.GetMoverCoordinates(selectedSpawner.Uid);

                // Use RandomHumanoidSystem to spawn the corpse properly, just like RMCSpawnerSystem does
                // This spawns one random humanoid based on the RandomHumanoidSettingsPrototype
                var corpse = _randomHumanoid.SpawnRandomHumanoid(selectedSpawner.Component.Spawn, coordinates, MetaData(selectedSpawner.Uid).EntityName);
                EnsureComp<IntelRecoverCorpseObjectiveComponent>(corpse);

                if (selectedSpawner.Component.DeleteAfterSpawn)
                    QueueDel(selectedSpawner.Uid);

                _aegisCorpseSpawnedThisRound = true;
                Log.Info($"AEGIS corpse spawned at {_transform.GetWorldPosition(selectedSpawner.Uid)} (selected 1 out of {aegisCorpseSpawners.Count} spawners)");
                // Keep _shouldActivateAegisCorpseSpawners = true until round ends
            }
        }
    }

    /// <summary>
    /// Sets the flag to activate AEGIS corpse spawners for this round.
    /// Flag will persist until round ends.
    /// </summary>
    public void SetAegisCorpseSpawnersForThisRound()
    {
        _shouldActivateAegisCorpseSpawners = true;
    }

    /// <summary>
    /// Gets whether AEGIS corpse spawners are scheduled to activate this round
    /// </summary>
    public bool AreAegisCorpseSpawnersScheduled()
    {
        return _shouldActivateAegisCorpseSpawners;
    }

    /// <summary>
    /// Gets whether AEGIS corpse spawners have been spawned this round
    /// </summary>
    public bool HaveAegisCorpseSpawnersSpawned()
    {
        return _aegisCorpseSpawnedThisRound;
    }

    /// <summary>
    /// Manually resets the AEGIS corpse spawner flags (for debugging/testing purposes)
    /// </summary>
    public void ResetAegisCorpseSpawners()
    {
        _shouldActivateAegisCorpseSpawners = false;
        _aegisCorpseSpawnedThisRound = false;
    }
}
