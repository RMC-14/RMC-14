using System.Numerics;
using Content.Server.Medical.SuitSensors;
using Content.Shared._RMC14.Medical.CrewMonitoring;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.CrewMonitoring;

public sealed class RMCPortableCrewMonitorSystem : SharedRMCPortableCrewMonitorSystem
{
    [Dependency] private readonly RMCCrewMonitorDataSystem _data = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly HashSet<EntityUid> _open = new();
    private readonly HashSet<EntityUid> _scanning = new();
    private readonly HashSet<EntityUid> _remove = new();
    private readonly Dictionary<EntityUid, Entity<SuitSensorComponent>> _sensors = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPortableCrewMonitorComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<RMCPortableCrewMonitorComponent, BoundUIClosedEvent>(OnUIClosed);
        SubscribeLocalEvent<RMCPortableCrewMonitorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RMCPortableCrewMonitorComponent, RMCPortableCrewMonitorScanBuiMsg>(OnScan);
        SubscribeLocalEvent<RMCPortableCrewMonitorComponent, RMCPortableCrewMonitorSelectBuiMsg>(OnSelect);
    }

    private void OnUIOpened(Entity<RMCPortableCrewMonitorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(RMCPortableCrewMonitorUIKey.Key))
            return;

        _open.Add(ent);
        ent.Comp.NextTrackAt = TimeSpan.Zero;
        UpdateTracking(ent);
    }

    private void OnUIClosed(Entity<RMCPortableCrewMonitorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(RMCPortableCrewMonitorUIKey.Key) ||
            _ui.IsUiOpen(ent.Owner, RMCPortableCrewMonitorUIKey.Key))
        {
            return;
        }

        _open.Remove(ent);
        ClearTracking(ent.Owner);
    }

    private void OnShutdown(Entity<RMCPortableCrewMonitorComponent> ent, ref ComponentShutdown args)
    {
        _open.Remove(ent);
        _scanning.Remove(ent);
    }

    private void OnScan(Entity<RMCPortableCrewMonitorComponent> ent, ref RMCPortableCrewMonitorScanBuiMsg args)
    {
        if (ent.Comp.Scanning || _hands.GetActiveItem(args.Actor) != ent.Owner)
            return;

        ent.Comp.Scanning = true;
        ent.Comp.ScanEndsAt = _timing.CurTime + ent.Comp.ScanDuration;
        _scanning.Add(ent);
        Dirty(ent);
    }

    private void OnSelect(Entity<RMCPortableCrewMonitorComponent> ent, ref RMCPortableCrewMonitorSelectBuiMsg args)
    {
        var targetNet = args.Target;
        if (_hands.GetActiveItem(args.Actor) != ent.Owner ||
            !TryGetEntity(targetNet, out var target) ||
            !ent.Comp.Sensors.TryGetValue(target.Value, out var sensor) ||
            !ent.Comp.Signals.Exists(entry => entry.Id == targetNet))
        {
            return;
        }

        ent.Comp.Selected = targetNet;
        ent.Comp.SelectedTarget = target;
        ent.Comp.SelectedSensor = sensor;
        ent.Comp.NextTrackAt = TimeSpan.Zero;
        Dirty(ent);
        UpdateTracking(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        _remove.Clear();

        foreach (var uid in _scanning)
        {
            if (!TryComp(uid, out RMCPortableCrewMonitorComponent? monitor))
            {
                _remove.Add(uid);
                continue;
            }

            if (time < monitor.ScanEndsAt)
                continue;

            CompleteScan((uid, monitor));
            _remove.Add(uid);
        }

        _scanning.ExceptWith(_remove);
        _remove.Clear();

        foreach (var uid in _open)
        {
            if (!TryComp(uid, out RMCPortableCrewMonitorComponent? monitor))
            {
                _remove.Add(uid);
                continue;
            }

            if (time < monitor.NextTrackAt)
                continue;

            monitor.NextTrackAt = time + monitor.TrackEvery;
            UpdateTracking((uid, monitor));
        }

        _open.ExceptWith(_remove);
    }

    private void CompleteScan(Entity<RMCPortableCrewMonitorComponent> ent)
    {
        ent.Comp.Scanning = false;
        ent.Comp.HasScanned = true;
        ent.Comp.Signals.Clear();
        ent.Comp.Sensors.Clear();

        var coordinates = _transform.GetMapCoordinates(ent.Owner);
        if (_data.IsValidMap(coordinates.MapId))
        {
            _data.CollectSensors(
                ent.Comp.NpcFactions,
                ent.Comp.IffFactions,
                SuitSensorMode.SensorCords,
                _sensors,
                coordinates.MapId);

            foreach (var (user, sensor) in _sensors)
            {
                if (!TryComp(user, out MobStateComponent? state))
                    continue;

                var identity = _data.GetIdentity(user);
                ent.Comp.Signals.Add(new RMCPortableCrewMonitorEntry(
                    GetNetEntity(user),
                    identity.Name,
                    identity.JobTitle,
                    identity.JobIcon,
                    state.CurrentState));
                ent.Comp.Sensors[user] = sensor.Owner;
            }
        }

        ent.Comp.Signals.Sort(static (left, right) =>
        {
            var name = string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
            return name != 0
                ? name
                : string.Compare(left.JobTitle, right.JobTitle, StringComparison.CurrentCultureIgnoreCase);
        });

        if (ent.Comp.SelectedTarget is { } selected && ent.Comp.Sensors.TryGetValue(selected, out var selectedSensor))
            ent.Comp.SelectedSensor = selectedSensor;
        else
            ent.Comp.SelectedSensor = null;

        Dirty(ent);
        UpdateTracking(ent);
    }

    private void UpdateTracking(Entity<RMCPortableCrewMonitorComponent> ent)
    {
        Vector2? offset = null;
        var directionOnly = false;
        if (ent.Comp.SelectedTarget is { } target &&
            ent.Comp.SelectedSensor is { } sensor &&
            TryComp(sensor, out SuitSensorComponent? suitSensor) &&
            suitSensor.Mode >= SuitSensorMode.SensorCords &&
            !TerminatingOrDeleted(target) &&
            _data.IsTrackedSensor(
                (sensor, suitSensor),
                target,
                ent.Comp.NpcFactions,
                ent.Comp.IffFactions,
                _transform.GetMapCoordinates(ent.Owner).MapId))
        {
            var holderCoordinates = _transform.GetMapCoordinates(ent.Owner);
            var targetCoordinates = _transform.GetMapCoordinates(target);
            if (holderCoordinates.MapId == targetCoordinates.MapId)
            {
                offset = targetCoordinates.Position - holderCoordinates.Position;
                if (offset.Value.Length() > ent.Comp.RadarRange)
                {
                    offset = Vector2.Normalize(offset.Value);
                    directionOnly = true;
                }
            }
        }

        if (!TryComp(ent, out RMCPortableCrewMonitorTrackingComponent? tracking) ||
            tracking.Offset == offset && tracking.DirectionOnly == directionOnly)
        {
            return;
        }

        tracking.Offset = offset;
        tracking.DirectionOnly = directionOnly;
        Dirty(ent.Owner, tracking);
    }

    private void ClearTracking(EntityUid uid)
    {
        if (!TryComp(uid, out RMCPortableCrewMonitorTrackingComponent? tracking) ||
            tracking.Offset == null && !tracking.DirectionOnly)
        {
            return;
        }

        tracking.Offset = null;
        tracking.DirectionOnly = false;
        Dirty(uid, tracking);
    }
}
