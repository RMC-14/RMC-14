using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Alert;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Waypoint;

public sealed partial class TrackerAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly Dictionary<TrackerDirection, short> AlertSeverity = new()
    {
        {TrackerDirection.Invalid, 0},
        {TrackerDirection.Center, 1},
        {TrackerDirection.South, 2},
        {TrackerDirection.SouthEast, 3},
        {TrackerDirection.East, 4},
        {TrackerDirection.NorthEast, 5},
        {TrackerDirection.North, 6},
        {TrackerDirection.NorthWest, 7},
        {TrackerDirection.West, 8},
        {TrackerDirection.SouthWest, 9},
    };

    private static readonly Dictionary<Direction, TrackerDirection> TrackerDirections = new()
    {
        {Direction.Invalid, TrackerDirection.Invalid},
        {Direction.South, TrackerDirection.South},
        {Direction.SouthEast, TrackerDirection.SouthEast},
        {Direction.East, TrackerDirection.East},
        {Direction.NorthEast, TrackerDirection.NorthEast},
        {Direction.North, TrackerDirection.North},
        {Direction.NorthWest, TrackerDirection.NorthWest},
        {Direction.West, TrackerDirection.West},
        {Direction.SouthWest, TrackerDirection.SouthWest},
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTrackerAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCTrackerAlertComponent, ComponentRemove>(OnComponentRemove);

        Subs.BuiEvents<RMCTrackerAlertComponent>(TrackerAlertUIKey.Key,
            subs =>
        {
            subs.Event<TrackerAlertBuiMsg>(OnTrackerAlertBui);
        });
    }

    private void OnMapInit(Entity<RMCTrackerAlertComponent> ent, ref MapInitEvent args)
    {
        foreach (var (_, trackerAlert) in ent.Comp.Alerts)
        {
            UpdateDirection(ent, trackerAlert);
        }
    }

    private void OnComponentRemove(Entity<RMCTrackerAlertComponent> ent, ref ComponentRemove args)
    {
        foreach (var (_, trackerAlert) in ent.Comp.Alerts)
        {
            _alerts.ClearAlertCategory(ent, trackerAlert.DirectionAlertCategory);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RMCTrackerAlertComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            foreach (var (_, trackerAlert) in tracker.Alerts)
            {
                if (_timing.CurTime < trackerAlert.NextUpdateTime)
                    continue;
                trackerAlert.NextUpdateTime = _timing.CurTime + trackerAlert.UpdateRate;

                UpdateDirection((uid, tracker), trackerAlert);
            }
        }
    }

    private void UpdateDirection(Entity<RMCTrackerAlertComponent> ent, RMCTrackerAlert alert,  bool force = false)
    {
        var alertId = alert.AlertPrototype;
        // queen_locator dm
        // If hud doesn't exist exit
        // Get type of tracker
            // Queen
            // Hive
            // Leader
            // Tunnel
        // else no tracker
            // Is tracking queen
            // reset tracker to empty
            // If not is tracking queen
                // Track queen
            // return
        // Point to interiors of vehicles
        // If zlevel doesn't match OR distance < 1 tile (adjacent) OR src == tracking
            // trackon-center
        // Else
            // If the self fake zlevel == target fake zlevel (e.g. almayer upper level vs lower level)
                // Directional tracker icon
            // else
                // trackon-center

        if (!_alerts.TryGet(alertId, out var alertPrototype))
        {
            Log.Error($"Invalid alert type {alertId}");
            return;
        }

        if (alert.TrackedEntity == null)
        {
            _alerts.ShowAlert(ent, alertId, 0, showCooldown: false);
            return;
        }

        var ev = new TrackerAlertVisibleAttemptEvent();
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
        {
            _alerts.ShowAlert(ent, alertId, 0, showCooldown: false);
            return;
        }

        alert.WorldDirection = GetTrackerDirection(ent, alert);

        if (alert.WorldDirection == alert.LastDirection && !force)
            return;

        if (alertPrototype.SupportsSeverity &&
            AlertSeverity.TryGetValue(alert.WorldDirection, out var severity))
        {
            _alerts.ShowAlert(ent, alertId, severity, showCooldown: false);
        }
        else
        {
            _alerts.ClearAlertCategory(ent, alert.DirectionAlertCategory);
        }

        alert.LastDirection = alert.WorldDirection;

        Dirty(ent);
    }

    private TrackerDirection GetTrackerDirection(EntityUid ent, RMCTrackerAlert alert)
    {
        if (alert.TrackedEntity is null)
            return TrackerDirection.Invalid;

        var pos = _transform.GetWorldPosition(ent);
        var targetPos = _transform.GetWorldPosition(GetEntity(alert.TrackedEntity.Value));

        var vec = targetPos - pos;
        return vec.Length() < 1
            ? TrackerDirection.Center
            : TrackerDirections[vec.ToWorldAngle().GetDir()];
    }

    private void OnTrackerAlertBui(EntityUid uid, RMCTrackerAlertComponent component, TrackerAlertBuiMsg args)
    {
        _ui.CloseUi(uid, TrackerAlertUIKey.Key, args.Actor);

        if (!component.Alerts.TryGetValue(args.AlertPrototype, out var trackerAlert))
            return;

        // TODO input validation
        trackerAlert.TrackedEntity = args.Target;
    }

    public void OpenSelectUI(EntityUid player, AlertPrototype alert)
    {
        var ev = new GetTrackerAlertEntriesEvent(alert);
        RaiseLocalEvent(player, ref ev);

        _ui.OpenUi(player, TrackerAlertUIKey.Key, player);
        _ui.SetUiState(player, TrackerAlertUIKey.Key, new TrackerAlertBuiState(ev.Entries));
    }
}

[ByRefEvent]
internal record struct TrackerAlertVisibleAttemptEvent
{
    public bool Cancelled;
}

[ByRefEvent]
public record struct GetTrackerAlertEntriesEvent(ProtoId<AlertPrototype> AlertPrototype)
{
    public readonly List<TrackerAlertEntry> Entries = [];
    public ProtoId<AlertPrototype> AlertPrototype = AlertPrototype;
}

[Serializable, NetSerializable]
public enum TrackerAlertUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct TrackerAlertEntry(NetEntity Entity, string Name, EntProtoId? Id, ProtoId<AlertPrototype> AlertPrototype);

[Serializable, NetSerializable]
public sealed class TrackerAlertBuiState(List<TrackerAlertEntry> entries) : BoundUserInterfaceState
{
    public readonly List<TrackerAlertEntry> Entries = entries;
}

[Serializable, NetSerializable]
public sealed class TrackerAlertBuiMsg(NetEntity target, ProtoId<AlertPrototype> alertPrototype) : BoundUserInterfaceMessage
{
    public readonly ProtoId<AlertPrototype> AlertPrototype = alertPrototype;

    public readonly NetEntity Target = target;
}
