using Content.Server.Shuttles;
using Content.Shared._RMC14.Dropship;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server._RMC14.Dropship;

/// <summary>
/// Stores the expected dock pairing for a restricted shuttle launch until verification completes.
/// </summary>
[RegisterComponent]
public sealed partial class RMCExpectedDockComponent : Component
{
    /// <summary>
    /// Destination marker the shuttle is expected to dock with.
    /// </summary>
    [ViewVariables]
    public EntityUid Destination;

    /// <summary>
    /// Grid containing the destination dock.
    /// </summary>
    [ViewVariables]
    public EntityUid TargetGrid;

    /// <summary>
    /// Dock entity on the moving shuttle grid chosen for the launch.
    /// </summary>
    [ViewVariables]
    public EntityUid ShuttleDock;

    /// <summary>
    /// Dock entity on the target grid chosen for the launch.
    /// </summary>
    [ViewVariables]
    public EntityUid TargetDock;

    /// <summary>
    /// Expected post-dock coordinates for the shuttle.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates Coordinates;

    /// <summary>
    /// Expected post-dock shuttle rotation.
    /// </summary>
    [ViewVariables]
    public Angle Angle;

    /// <summary>
    /// Docking configuration used to perform the launch.
    /// </summary>
    [ViewVariables]
    public DockingConfig? Config;

    /// <summary>
    /// Docking profile used when choosing and validating the target destination.
    /// </summary>
    [ViewVariables]
    public RMCShuttleDockingClass DockingClass;

    /// <summary>
    /// ERT request id that owns this expected dock, when launched by ERT.
    /// </summary>
    [ViewVariables]
    public Guid RequestId;

    /// <summary>
    /// ERT call prototype id that configured this launch, when available.
    /// </summary>
    [ViewVariables]
    public string? Call;

    /// <summary>
    /// Whether the actual docked pair has already matched the expected pair.
    /// </summary>
    [ViewVariables]
    public bool Confirmed;

    /// <summary>
    /// Actual shuttle dock found during verification.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActualShuttleDock;

    /// <summary>
    /// Actual destination dock found during verification.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActualTargetDock;

    /// <summary>
    /// Last verification failure reason for diagnostics.
    /// </summary>
    [ViewVariables]
    public string? FailureReason;
}
