using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Dropship;

/// <summary>
/// Stores the expected dock pairing for a restricted shuttle launch until verification completes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCExpectedDockComponent : Component
{
    /// <summary>
    /// Destination marker the shuttle is expected to dock with.
    /// </summary>
    [DataField]
    public EntityUid Destination;

    /// <summary>
    /// Grid containing the destination dock.
    /// </summary>
    [DataField]
    public EntityUid TargetGrid;

    /// <summary>
    /// Dock entity on the moving shuttle grid chosen for the launch.
    /// </summary>
    [DataField]
    public EntityUid ShuttleDock;

    /// <summary>
    /// Dock entity on the target grid chosen for the launch.
    /// </summary>
    [DataField]
    public EntityUid TargetDock;

    /// <summary>
    /// Expected post-dock coordinates for the shuttle.
    /// </summary>
    [DataField]
    public EntityCoordinates Coordinates;

    /// <summary>
    /// Expected post-dock shuttle rotation.
    /// </summary>
    [DataField]
    public Angle Angle;

    /// <summary>
    /// Dock pairs that must connect for the restricted docking to verify.
    /// </summary>
    [DataField]
    public List<RMCExpectedDockPair> DockPairs = [];

    /// <summary>
    /// Docking profile used when choosing and validating the target destination.
    /// </summary>
    [DataField]
    public RMCShuttleDockingClass DockingClass;

    /// <summary>
    /// ERT request id that owns this expected dock, when launched by ERT.
    /// </summary>
    [DataField]
    public Guid RequestId;

    /// <summary>
    /// ERT call prototype id that configured this launch, when available.
    /// </summary>
    [DataField]
    public string? Call;

    /// <summary>
    /// Whether the actual docked pair has already matched the expected pair.
    /// </summary>
    [DataField]
    public bool Confirmed;

    /// <summary>
    /// Actual shuttle dock found during verification.
    /// </summary>
    [DataField]
    public EntityUid? ActualShuttleDock;

    /// <summary>
    /// Actual destination dock found during verification.
    /// </summary>
    [DataField]
    public EntityUid? ActualTargetDock;

    /// <summary>
    /// Last verification failure reason for diagnostics.
    /// </summary>
    [DataField]
    public string? FailureReason;
}

public readonly record struct RMCExpectedDockPair(EntityUid ShuttleDock, EntityUid TargetDock);
