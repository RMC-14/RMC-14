using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Waypoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TrackerAlertComponent : Component
{
    [DataField]
    public EntityUid? TrackedEntity;

    [DataField]
    public Direction WorldDirection;

    public Direction LastDirection;

    [DataField]
    public ProtoId<AlertPrototype> AlertPrototype;

    [DataField, AutoNetworkedField]
    public Dictionary<Direction, short> AlertSeverity = new()
    {
        { Direction.Invalid, 1 },
        { Direction.South, 2 },
        { Direction.SouthEast, 3 },
        { Direction.East, 4 },
        { Direction.NorthEast, 5 },
        { Direction.North, 6 },
        { Direction.NorthWest, 7 },
        { Direction.West, 8 },
        { Direction.SouthWest, 9 },
    };

    [DataField]
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
