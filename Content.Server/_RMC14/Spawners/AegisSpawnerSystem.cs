using Content.Server.GameTicking.Events;
using Content.Shared.GameTicking;
using Robust.Shared.Spawners;

namespace Content.Server._RMC14.Spawners;

/// <summary>
/// Dedicated system for managing AEGIS spawner activation and persistence
/// </summary>
public sealed class AegisSpawnerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Global flag that determines if AEGIS spawners should be activated this round
    /// Once set, persists until round ends
    /// </summary>
    private bool _shouldActivateAegisSpawners = false;

    /// <summary>
    /// Flag to track if AEGIS crates have been spawned this round to prevent duplicates
    /// </summary>
    private bool _aegisSpawnedThisRound = false;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        // Reset AEGIS flags for new round
        _shouldActivateAegisSpawners = false;
        _aegisSpawnedThisRound = false;
    }

    public override void Update(float frameTime)
    {
        // Check for AEGIS spawners if flag is set and we haven't spawned yet this round
        if (_shouldActivateAegisSpawners && !_aegisSpawnedThisRound)
        {
            var aegisQuery = EntityQueryEnumerator<AegisSpawnerComponent>();
            var spawnedCount = 0;

            while (aegisQuery.MoveNext(out var uid, out var spawner))
            {
                if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                    continue;

                var coordinates = _transform.GetMoverCoordinates(uid);
                Spawn(spawner.Spawn, coordinates);

                if (spawner.DeleteAfterSpawn)
                    QueueDel(uid);

                spawnedCount++;
            }

            if (spawnedCount > 0)
            {
                _aegisSpawnedThisRound = true;
                // Keep _shouldActivateAegisSpawners = true until round ends
            }
        }
    }

    /// <summary>
    /// Sets the flag to activate AEGIS spawners for this round.
    /// Flag will persist until round ends.
    /// </summary>
    public void SetAegisSpawnersForThisRound()
    {
        _shouldActivateAegisSpawners = true;
    }

    /// <summary>
    /// Gets whether AEGIS spawners are scheduled to activate this round
    /// </summary>
    public bool AreAegisSpawnersScheduled()
    {
        return _shouldActivateAegisSpawners;
    }

    /// <summary>
    /// Gets whether AEGIS crates have been spawned this round
    /// </summary>
    public bool HaveAegisCratesSpawned()
    {
        return _aegisSpawnedThisRound;
    }

    /// <summary>
    /// Manually resets the AEGIS spawner flags (for debugging/testing purposes)
    /// </summary>
    public void ResetAegisSpawners()
    {
        _shouldActivateAegisSpawners = false;
        _aegisSpawnedThisRound = false;
    }
}
