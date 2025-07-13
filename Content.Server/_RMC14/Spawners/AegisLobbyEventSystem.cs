using Content.Server.GameTicking.Events;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Xenonids;
using Content.Shared._RMC14.AegisEvent;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Server.GameTicking;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server._RMC14.Spawners;

/// <summary>
/// System to handle delayed AEGIS event execution when scheduled from lobby
/// </summary>
public sealed class AegisLobbyEventSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private TimeSpan? _scheduledEventTime = null;
    private string _scheduledMessage = string.Empty;
    private bool _eventExecuted = false;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        if (_scheduledEventTime != null && _scheduledEventTime.Value < TimeSpan.Zero)
        {
            _scheduledEventTime = _timing.CurTime + TimeSpan.FromMinutes(1);
            _eventExecuted = false;
            Log.Info($"AEGIS lobby event scheduled to execute at {_scheduledEventTime} (1 minute after round start)");
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        // Reset for new round
        _scheduledEventTime = null;
        _scheduledMessage = string.Empty;
        _eventExecuted = false;
    }

    public override void Update(float frameTime)
    {
        // Check if we need to execute the scheduled event
        // Only execute if the time is positive (meaning round has started and timer is set)
        if (_scheduledEventTime != null && !_eventExecuted &&
            _scheduledEventTime.Value > TimeSpan.Zero &&
            _timing.CurTime >= _scheduledEventTime)
        {
            ExecuteScheduledAegisEvent();
            _eventExecuted = true;
        }
    }

    /// <summary>
    /// Schedules an AEGIS event to be executed 1 minute after round start
    /// </summary>
    public void ScheduleAegisEvent(string message)
    {
        _scheduledMessage = message;
        _eventExecuted = false;

        // Check if we're currently in a round vs in lobby
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
        {
            // We're already in a round, don't schedule anything - this should not happen with proper usage
            Log.Warning("AEGIS lobby event called during active round - this should only be used in lobby");
            _scheduledEventTime = null; // Cancel scheduling
            return;
        }

        // We're in lobby, schedule for next round start + 1 minute
        // Use a marker value that indicates scheduling is pending (not executed yet)
        _scheduledEventTime = TimeSpan.FromSeconds(-1); // Will be set to actual time when round starts
    }

    /// <summary>
    /// Resets/cancels any scheduled AEGIS lobby event
    /// </summary>
    public void ResetScheduledEvent()
    {
        _scheduledEventTime = null;
        _scheduledMessage = string.Empty;
        _eventExecuted = false;
        Log.Info("AEGIS lobby event schedule has been reset");
    }

    /// <summary>
    /// Checks if there is a scheduled AEGIS lobby event
    /// </summary>
    public bool IsEventScheduled()
    {
        return _scheduledEventTime != null;
    }

    /// <summary>
    /// Checks if the scheduled event has been executed
    /// </summary>
    public bool HasEventExecuted()
    {
        return _eventExecuted;
    }

    /// <summary>
    /// Executes the scheduled AEGIS event (announcements, fax, ASRS)
    /// </summary>
    private void ExecuteScheduledAegisEvent()
    {
        Log.Info("Executing scheduled AEGIS lobby event");

        var systemManager = EntityManager.EntitySysManager;
        var entityManager = EntityManager;

        // Send announcements to both marines and xenos
        AegisSharedAnnouncement.AnnounceToBoth(systemManager, _scheduledMessage);

        // Send fax to Marine High Command
        SendAegisLobbyFax(systemManager, entityManager, _scheduledMessage);

        // Find and spawn at a random AEGIS spawner location, just like AegisSpawnerSystem does
        var aegisSpawners = new List<(EntityUid Uid, AegisSpawnerComponent Component)>();

        // Collect all valid AEGIS spawners
        var aegisQuery = EntityQueryEnumerator<AegisSpawnerComponent>();
        while (aegisQuery.MoveNext(out var uid, out var spawner))
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                continue;

            aegisSpawners.Add((uid, spawner));
        }

        // Spawn the AEGIS lobby crate at a random spawner location
        if (aegisSpawners.Count > 0)
        {
            var selectedSpawner = _random.Pick(aegisSpawners);
            var coordinates = _transform.GetMoverCoordinates(selectedSpawner.Uid);
            var aegisCrate = entityManager.SpawnEntity("RMCCrateAegisLobby", coordinates);
            entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(aegisCrate);

            Log.Info($"AEGIS lobby event executed: announcements sent, fax delivered, and AEGIS intelligence crate spawned at {_transform.GetWorldPosition(selectedSpawner.Uid)} (selected 1 out of {aegisSpawners.Count} spawners)");
        }
        else
        {
            var aegisCrate = entityManager.SpawnEntity("RMCCrateAegisLobby", MapCoordinates.Nullspace);
            entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(aegisCrate);
            Log.Warning("AEGIS lobby event executed but no AEGIS spawners found on map - crate spawned via ASRS delivery as fallback");
        }
    }

    private void SendAegisLobbyFax(IEntitySystemManager systemManager, IEntityManager entityManager, string message)
    {
        var faxSystem = systemManager.GetEntitySystem<FaxSystem>();

        var faxQuery = entityManager.EntityQueryEnumerator<FaxMachineComponent>();
        while (faxQuery.MoveNext(out var faxEnt, out var faxComp))
        {
            if (faxComp.FaxName == "CIC")
            {
                var aegisPaper = entityManager.SpawnEntity("CMPaperAegisLobbyInfoFax", MapCoordinates.Nullspace);

                if (entityManager.TryGetComponent<PaperComponent>(aegisPaper, out var paperComp) &&
                    entityManager.TryGetComponent<MetaDataComponent>(aegisPaper, out var metaComp))
                {
                    var printout = new FaxPrintout(
                        paperComp.Content,
                        metaComp.EntityName,
                        null, // No label
                        "CMPaperAegisLobbyInfoFax",
                        paperComp.StampState,
                        paperComp.StampedBy
                    );

                    faxSystem.Receive(faxEnt, printout, null, faxComp);
                }

                entityManager.DeleteEntity(aegisPaper);
                break;
            }
        }
    }
}
