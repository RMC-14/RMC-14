using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Waypoint;

public sealed class WaypointTrackerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TrackerAlertComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TrackerAlertComponent> ent, ref MapInitEvent args)
    {
        UpdateDirection((ent, ent.Comp));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TrackerAlertComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateDirection((uid, comp));
        }
    }

    private void UpdateDirection(Entity<TrackerAlertComponent?> ent, bool force = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.WorldDirection = GetTrackerDirection((ent, ent.Comp));

        if (ent.Comp.WorldDirection == ent.Comp.LastDirection && !force)
            return;

        if (ent.Comp.DirectionAlerts.TryGetValue(ent.Comp.WorldDirection, out var alertId))
        {
            _alerts.ShowAlert(ent, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(ent, ent.Comp.DirectionAlertCategory);
        }

        ent.Comp.LastDirection = ent.Comp.WorldDirection;

        Dirty(ent);
    }

    private Direction GetTrackerDirection(Entity<TrackerAlertComponent> ent)
    {
        if (ent.Comp.TrackedEntity is null)
            return Direction.Invalid;

        var pos = _transform.GetWorldPosition(ent);
        var targetPos = _transform.GetWorldPosition(ent.Comp.TrackedEntity.Value);

        var vec = pos - targetPos;
        return vec.ToWorldAngle().GetDir();
    }
}

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
