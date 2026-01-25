using Content.Server.GameTicking.Events;
using Content.Shared._RMC14.AegisEvent;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;
using Content.Server.GameTicking;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Labels.Components;
using Content.Shared._RMC14.Requisitions;

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
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly FaxSystem _fax = default!;
    [Dependency] private readonly SharedRequisitionsSystem _req = default!;

    private bool _aegisScheduled = false;
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
        if (_aegisScheduled && _scheduledEventTime == null)
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
        _aegisScheduled = false;
        _scheduledMessage = string.Empty;
        _eventExecuted = false;
    }

    public override void Update(float frameTime)
    {
        // Check if we need to execute the scheduled event
        // Only execute if the time is positive (meaning round has started and timer is set)
        if (!_aegisScheduled || _scheduledEventTime == null || _eventExecuted)
            return;

        if (_timing.CurTime < _scheduledEventTime)
            return;

        ExecuteScheduledAegisEvent();
        _eventExecuted = true;
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
            _aegisScheduled = false;
            _scheduledEventTime = null; // Cancel scheduling
            return;
        }

        _aegisScheduled = true;
    }

    /// <summary>
    /// Resets/cancels any scheduled AEGIS lobby event
    /// </summary>
    public void ResetScheduledEvent()
    {
        _aegisScheduled = false;
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
        return _aegisScheduled;
    }

    /// <summary>
    /// Checks if the scheduled event has been executed
    /// </summary>
    public bool HasEventExecuted()
    {
        return _eventExecuted;
    }

    /// <summary>
    /// Executes the scheduled AEGIS event (announcements, fax, supply crate ASRS)
    /// AEGIS spawning and Id card spawning handled by AegisSpawnerSystem
    /// </summary>
    private void ExecuteScheduledAegisEvent()
    {
        Log.Info("Executing scheduled AEGIS lobby event");

        var systemManager = EntityManager.EntitySysManager;
        var entityManager = EntityManager;

        // Send announcements to both marines and xenos
        AegisSharedAnnouncement.AnnounceToBoth(systemManager, _scheduledMessage);

        // Send fax to Marine High Command
        if (!SendCICFax(systemManager, entityManager, _scheduledMessage, "RMCPaperAegisLobbyInfoFax", "UNS Oberon"))
            Log.Info("AEGIS event failed to send any faxes!");

        _req.CreateSpecialDelivery("RMCCrateAegisLobby");
        Log.Info("AEGIS delivery created and should be sent shortly.");
        //Unschedule after execution
        _aegisScheduled = false;
    }

    public bool SendCICFax(IEntitySystemManager systemManager, IEntityManager entityManager, string message, EntProtoId faxProto, string? sender = null)
    {
        if (!_proto.TryIndex(faxProto, out var faxPaper) ||
            !faxPaper.TryGetComponent<PaperComponent>(out var paper, EntityManager.ComponentFactory))
            return false;

        var label = string.Empty;

        if (faxPaper.TryGetComponent<LabelComponent>(out var labelComp, EntityManager.ComponentFactory))
            label = labelComp.CurrentLabel;

        var printout = new FaxPrintout(
            paper.Content,
            faxPaper.Name,
            label,
            faxProto,
            paper.StampState,
            paper.StampedBy
        );

        var faxQuery = entityManager.EntityQueryEnumerator<FaxMachineComponent>();
        bool sentFax = false;

        while (faxQuery.MoveNext(out var faxEnt, out var faxComp))
        {
            if (faxComp.FaxName == "CIC")
            {
                _fax.Receive(faxEnt, printout, sender, faxComp);
                sentFax = true;
            }
        }

        return sentFax;
    }
}
