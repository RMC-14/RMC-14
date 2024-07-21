using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Alert;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Waypoint;

public sealed class TrackerAlertSystem : EntitySystem
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
        SubscribeLocalEvent<TrackerAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCTrackerAlertTargetComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCTrackerAlertTargetComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<RMCTrackerAlertTargetComponent, XenoAddedToHiveEvent>(OnXenoAddedToHive);
        SubscribeLocalEvent<RMCTrackerAlertTargetComponent, XenoRemovedFromHiveEvent>(OnXenoRemovedFromHive);

        Subs.BuiEvents<TrackerAlertComponent>(TrackerAlertUIKey.Key,
            subs =>
        {
            subs.Event<TrackerAlertBuiMsg>(OnTrackerAlertBui);
        });
    }

    private void OnTrackerAlertBui(EntityUid uid, TrackerAlertComponent component, TrackerAlertBuiMsg args)
    {
        _ui.CloseUi(uid, TrackerAlertUIKey.Key, args.Actor);

        if (!TryGetEntity(args.Target, out var target))
            return;

        component.TrackedEntity = target;
    }

    private void OnXenoRemovedFromHive(Entity<RMCTrackerAlertTargetComponent> ent, ref XenoRemovedFromHiveEvent args)
    {
        if (!TryComp(args.Hive, out HiveComponent? hive))
            return;

        hive.Trackers.Remove(ent);
    }

    private void OnXenoAddedToHive(Entity<RMCTrackerAlertTargetComponent> ent, ref XenoAddedToHiveEvent args)
    {
        if (!TryComp(args.Hive, out HiveComponent? hive))
            return;

        hive.Trackers.Add(ent);
    }

    private void OnComponentStartup(Entity<RMCTrackerAlertTargetComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out XenoComponent? xeno) || !TryComp(xeno.Hive, out HiveComponent? hive))
            return;

        hive.Trackers.Add(ent);
    }

    private void OnMobStateChanged(Entity<RMCTrackerAlertTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryComp(ent, out XenoComponent? xeno) || !TryComp(xeno.Hive, out HiveComponent? hive))
            return;

        hive.Trackers.Remove(ent);
    }

    private void OnMapInit(Entity<TrackerAlertComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out XenoComponent? xeno))
            return;

        UpdateDirection((ent, ent.Comp, xeno));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TrackerAlertComponent, XenoComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var xeno))
        {
            if (_timing.CurTime < tracker.NextUpdateTime)
                continue;
            tracker.NextUpdateTime = _timing.CurTime + tracker.UpdateRate;

            UpdateDirection((uid, tracker, xeno));
        }
    }

    private void UpdateDirection(Entity<TrackerAlertComponent?, XenoComponent?> ent, bool force = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        var alertId = ent.Comp1.AlertPrototype;
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

        if (ent.Comp1.TrackedEntity == null)
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

        ent.Comp1.WorldDirection = GetTrackerDirection((ent, ent.Comp1));

        if (ent.Comp1.WorldDirection == ent.Comp1.LastDirection && !force)
            return;

        if (alertPrototype.SupportsSeverity &&
            AlertSeverity.TryGetValue(ent.Comp1.WorldDirection, out var severity))
        {
            _alerts.ShowAlert(ent, alertId, severity, showCooldown: false);
        }
        else
        {
            _alerts.ClearAlertCategory(ent, ent.Comp1.DirectionAlertCategory);
        }

        ent.Comp1.LastDirection = ent.Comp1.WorldDirection;

        Dirty(ent, ent.Comp1);
    }

    private TrackerDirection GetTrackerDirection(Entity<TrackerAlertComponent> ent)
    {
        if (ent.Comp.TrackedEntity is null)
            return TrackerDirection.Invalid;

        var pos = _transform.GetWorldPosition(ent);
        var targetPos = _transform.GetWorldPosition(ent.Comp.TrackedEntity.Value);

        var vec = targetPos - pos;
        return vec.Length() < 1
            ? TrackerDirection.Center
            : TrackerDirections[vec.ToWorldAngle().GetDir()];
    }

    public void OpenSelectUI(EntityUid player)
    {
        var ev = new GetTrackerAlertEntriesEvent();
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
public record struct GetTrackerAlertEntriesEvent()
{
    public readonly List<TrackerAlertEntry> Entries = [];
}

[RegisterComponent]
public sealed partial class RMCTrackerAlertTargetComponent : Component
{
    [DataField]
    public int Priority;
}

[Serializable, NetSerializable]
public enum TrackerAlertUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct TrackerAlertEntry(NetEntity Entity, string Name, EntProtoId? Id);

[Serializable, NetSerializable]
public sealed class TrackerAlertBuiState(List<TrackerAlertEntry> entries) : BoundUserInterfaceState
{
    public readonly List<TrackerAlertEntry> Entries = entries;
}

[Serializable, NetSerializable]
public sealed class TrackerAlertBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}
