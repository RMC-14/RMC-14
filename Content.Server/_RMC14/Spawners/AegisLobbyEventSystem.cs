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

namespace Content.Server._RMC14.Spawners;

/// <summary>
/// System to handle delayed AEGIS event execution when scheduled from lobby
/// </summary>
public sealed class AegisLobbyEventSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

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
        // If we have a scheduled event (marked with negative value), set the execution time for 1 minute from now
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
        Log.Info($"AEGIS event scheduled for 1 minute after next round start with message: {message}");
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

        // Spawn and send the Aegis ID card
        var idItem = entityManager.SpawnEntity("RMCIDCardAegis", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(idItem);

        // Spawn and send the Powerloader pamphlet
        var pamphletItem = entityManager.SpawnEntity("CMPamphletPowerloader", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(pamphletItem);

        Log.Info("AEGIS lobby event executed: announcements sent, fax delivered, AEGIS ID card and pamphlet sent through ASRS");
    }

    private void SendAegisLobbyFax(IEntitySystemManager systemManager, IEntityManager entityManager, string message)
    {
        var faxSystem = systemManager.GetEntitySystem<FaxSystem>();

        var faxQuery = entityManager.EntityQueryEnumerator<FaxMachineComponent>();
        while (faxQuery.MoveNext(out var faxEnt, out var faxComp))
        {
            if (faxComp.FaxName == "CIC")
            {
                var aegisPaper = entityManager.SpawnEntity("CMPaperAegisInfoFax", MapCoordinates.Nullspace);

                if (entityManager.TryGetComponent<PaperComponent>(aegisPaper, out var paperComp) &&
                    entityManager.TryGetComponent<MetaDataComponent>(aegisPaper, out var metaComp))
                {
                    var printout = new FaxPrintout(
                        paperComp.Content,
                        metaComp.EntityName,
                        null, // No label
                        "CMPaperAegisInfoFax",
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
