using Content.Shared._RMC14.ERT;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server._RMC14.Dropship;

[RegisterComponent]
public sealed partial class RMCERTExpectedDockComponent : Component
{
    [ViewVariables]
    public EntityUid Destination;

    [ViewVariables]
    public EntityUid TargetGrid;

    [ViewVariables]
    public EntityUid ShuttleDock;

    [ViewVariables]
    public EntityUid TargetDock;

    [ViewVariables]
    public EntityCoordinates Coordinates;

    [ViewVariables]
    public Angle Angle;

    [ViewVariables]
    public RMCERTShuttleDockingClass DockingClass;

    [ViewVariables]
    public Guid RequestId;

    [ViewVariables]
    public string? Call;

    [ViewVariables]
    public bool Confirmed;

    [ViewVariables]
    public EntityUid? ActualShuttleDock;

    [ViewVariables]
    public EntityUid? ActualTargetDock;

    [ViewVariables]
    public string? FailureReason;
}

[ByRefEvent]
public readonly record struct RMCERTDockingVerificationFailedEvent(
    EntityUid Shuttle,
    EntityUid Destination,
    EntityUid TargetGrid,
    EntityUid ShuttleDock,
    EntityUid TargetDock,
    EntityUid? ActualShuttleDock,
    EntityUid? ActualTargetDock,
    Guid RequestId,
    string? Call,
    RMCERTShuttleDockingClass DockingClass,
    string Reason);
