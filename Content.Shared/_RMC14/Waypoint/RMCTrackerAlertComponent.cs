using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Waypoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RMCTrackerAlertComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? TrackedEntity;

    [ViewVariables, AutoNetworkedField]
    public TrackerDirection WorldDirection;

    [ViewVariables]
    public TrackerDirection LastDirection;

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<AlertPrototype> AlertPrototype;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertCategoryPrototype> DirectionAlertCategory = "Tracker";

    /// <summary>
    /// The time when the tracker alert will update next.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// The time between each update.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);
}

public enum TrackerDirection
{
    Invalid = -1,
    Center = 0,
    South = 1,
    SouthEast = 2,
    East = 3,
    NorthEast = 4,
    North = 5,
    NorthWest = 6,
    West = 7,
    SouthWest = 8,
}
