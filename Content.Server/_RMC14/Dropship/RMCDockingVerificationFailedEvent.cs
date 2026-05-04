using Content.Shared._RMC14.Dropship;

namespace Content.Server._RMC14.Dropship;

/// <summary>
/// Raised when a restricted shuttle docks somewhere other than the expected destination pair.
/// </summary>
/// <param name="Shuttle">Shuttle grid that failed verification.</param>
/// <param name="Destination">Destination marker selected for the launch.</param>
/// <param name="TargetGrid">Grid containing the expected target dock.</param>
/// <param name="ShuttleDock">Expected dock entity on the shuttle grid.</param>
/// <param name="TargetDock">Expected dock entity on the destination grid.</param>
/// <param name="ActualShuttleDock">Actual shuttle dock found after docking, if any.</param>
/// <param name="ActualTargetDock">Actual destination dock found after docking, if any.</param>
/// <param name="RequestId">ERT request id that owns the launch, when launched by ERT.</param>
/// <param name="Call">ERT call prototype id that configured the launch, when available.</param>
/// <param name="DockingClass">Docking profile used during destination selection.</param>
/// <param name="Reason">Human-readable verification failure reason.</param>
[ByRefEvent]
public readonly record struct RMCDockingVerificationFailedEvent(
    EntityUid Shuttle,
    EntityUid Destination,
    EntityUid TargetGrid,
    EntityUid ShuttleDock,
    EntityUid TargetDock,
    EntityUid? ActualShuttleDock,
    EntityUid? ActualTargetDock,
    Guid RequestId,
    string? Call,
    RMCShuttleDockingClass DockingClass,
    string Reason);
