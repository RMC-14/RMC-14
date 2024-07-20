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
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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

        if (!_alerts.TryGet(alertId, out var alertPrototype))
        {
            Log.Error($"Invalid alert type {alertId}");
            return;
        }

        // Has a queen
        if (ent.Comp2.Hive == null ||
            !_xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1,
                entity => TryComp(entity, out XenoComponent? queenXeno) && queenXeno.Hive == ent.Comp2.Hive))
        {
            _alerts.ShowAlert(ent, alertId, 0, showCooldown: false);
            return;
        }

        ent.Comp1.WorldDirection = GetTrackerDirection((ent, ent.Comp1));

        if (ent.Comp1.WorldDirection == ent.Comp1.LastDirection && !force)
            return;

        if (alertPrototype.SupportsSeverity &&
            ent.Comp1.AlertSeverity.TryGetValue(ent.Comp1.WorldDirection, out var severity))
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

    private Direction GetTrackerDirection(Entity<TrackerAlertComponent> ent)
    {
        if (ent.Comp.TrackedEntity is null)
            return Direction.Invalid;

        var pos = _transform.GetWorldPosition(ent);
        var targetPos = _transform.GetWorldPosition(ent.Comp.TrackedEntity.Value);

        var vec = targetPos - pos;
        return vec.ToWorldAngle().GetDir();
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
