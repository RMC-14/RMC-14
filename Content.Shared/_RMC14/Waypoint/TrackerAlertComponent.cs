using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Waypoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TrackerAlertComponent : Component
{
    [DataField]
    public EntityUid? TrackedEntity;

    [DataField]
    public Direction WorldDirection;

    public Direction LastDirection;

    /// <summary>
    /// A dictionary relating hunger thresholds to corresponding alerts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Direction, ProtoId<AlertPrototype>> DirectionAlerts = new()
    {
        { Direction.Invalid, "QueenTrackerOff" },
        { Direction.South, "QueenTrackerOn" },
        { Direction.SouthEast, "QueenTrackerOn" },
        { Direction.East, "QueenTrackerOn" },
        { Direction.NorthEast, "QueenTrackerOn" },
        { Direction.North, "QueenTrackerOn" },
        { Direction.NorthWest, "QueenTrackerOn" },
        { Direction.West, "QueenTrackerOn" },
        { Direction.SouthWest, "QueenTrackerOn" },
    };

    [DataField, AutoNetworkedField]
    public Dictionary<Direction, string> AlertSeverity = new()
    {
        { Direction.South, "South" },
        { Direction.SouthEast, "SouthEast" },
        { Direction.East, "East" },
        { Direction.NorthEast, "NorthEast" },
        { Direction.North, "North" },
        { Direction.NorthWest, "NorthWest" },
        { Direction.West, "West" },
        { Direction.SouthWest, "SouthWest" },
    };

    [DataField]
    public ProtoId<AlertCategoryPrototype> DirectionAlertCategory = "Tracker";
}
