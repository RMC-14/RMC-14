using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Alert;
using Content.Shared.Mobs;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Tracker.Xeno;

public sealed class HiveTrackerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TrackerSystem _tracker = default!;
    [Dependency] private readonly SquadLeaderTrackerSystem _squadLeaderTrackerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoWatchSystem _xenoWatch = default!;

    private const string HiveTrackerCategory = "HiveTracker";

    public override void Initialize()
    {
        // TODO RMC14 resin tracker
        SubscribeLocalEvent<HiveTrackerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<HiveTrackerComponent, HiveTrackerClickedAlertEvent>(OnClickedAlert);
        SubscribeLocalEvent<HiveTrackerComponent, HiveTrackerAltClickedAlertEvent>(OnAltClickedAlert);
        SubscribeLocalEvent<HiveTrackerComponent, HiveTrackerChangeModeEvent>(OnHiveTrackerChangeMode);
        SubscribeLocalEvent<HiveTrackerComponent, LeaderTrackerSelectTargetEvent>(OnHiveTrackerSelectTarget);

        SubscribeLocalEvent<RMCTrackableComponent, RequestTrackableNameEvent>(OnRequestTrackableName);
        SubscribeLocalEvent<RMCTrackableComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnRemove(Entity<HiveTrackerComponent> ent, ref ComponentRemove args)
    {
        _prototypeManager.TryIndex(ent.Comp.Mode, out var trackerMode);
        if(trackerMode == null)
            return;

        _alerts.ClearAlert(ent, trackerMode.Alert);
    }

    private void OnClickedAlert(Entity<HiveTrackerComponent> ent, ref HiveTrackerClickedAlertEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        EntityUid? target = null;

        // Watch the entity currently being tracked.
        if (TryComp(ent.Comp.Target, out HiveMemberComponent? targetHive) && targetHive.Hive == hive.Owner)
            target = ent.Comp.Target.Value;

        // Watch the queen if the tracking target is not a xeno.
        if (!HasComp<XenoComponent>(target))
            target = hive.Comp.CurrentQueen;

        if (target == null)
            return;

        args.Handled = true;
        _xenoWatch.Watch(ent.Owner, target.Value);
    }

    private void OnAltClickedAlert(Entity<HiveTrackerComponent> ent, ref HiveTrackerAltClickedAlertEvent args)
    {
        var options = new List<DialogOption> { };

        foreach (var mode in ent.Comp.TrackerModes)
        {
            options.Add(new DialogOption(
                Loc.GetString("rmc-xeno-tracker-target-" + mode),
                new HiveTrackerChangeModeEvent(mode)
            ));
        }

        _dialog.OpenOptions(
            ent,
            Loc.GetString("rmc-squad-info-tracking-selection"),
            options,
            Loc.GetString("rmc-squad-info-tracking-choose")
        );
    }

    private void OnHiveTrackerChangeMode(Entity<HiveTrackerComponent> ent, ref HiveTrackerChangeModeEvent args)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        if(!TryComp(ent.Owner, out HiveMemberComponent? member))
            return;

        _squadLeaderTrackerSystem.TryFindTargets(args.Mode, out var options, out var trackingOptions);

        // Remove targets that are not in the same hive as the tracking entity.
        var index = 0;
        while (index < trackingOptions.Count)
        {
            if (!TryComp(trackingOptions[index], out HiveMemberComponent? targetHiveMember) ||
                targetHiveMember.Hive != member.Hive)
            {
                options.RemoveAt(index);
                trackingOptions.RemoveAt(index);
                continue;
            }
            index++;
        }

        _dialog.OpenOptions(ent,
            Loc.GetString("rmc-squad-info-tracking-selection"),
            options,
            Loc.GetString("rmc-squad-info-tracking-choose")
        );
    }

    private void OnHiveTrackerSelectTarget(Entity<HiveTrackerComponent> ent, ref LeaderTrackerSelectTargetEvent args)
    {
        SetTarget(ent, GetEntity(args.Target));
        SetMode(ent, args.Mode);
        Dirty(ent);
    }

    private void SetTarget(Entity<HiveTrackerComponent> ent, EntityUid? target)
    {
        ent.Comp.Target = target;
        Dirty(ent);
    }

    private void SetMode(Entity<HiveTrackerComponent> ent, ProtoId<TrackerModePrototype> mode)
    {
        ent.Comp.Mode = mode;
        Dirty(ent);
    }

    private void UpdateDirection(Entity<HiveTrackerComponent> ent, MapCoordinates? coordinates = null)
    {
        _alerts.ClearAlertCategory(ent, HiveTrackerCategory);
        _prototypeManager.TryIndex(ent.Comp.Mode, out var trackerMode);
        if(trackerMode == null)
            return;

        var alert = trackerMode.Alert;
        var severity = TrackerSystem.CenterSeverity;

        if (coordinates != null)
            severity = _tracker.GetAlertSeverity(ent.Owner, coordinates.Value);

        _alerts.ShowAlert(ent.Owner, alert, severity);
    }

    private void OnRequestTrackableName(Entity<RMCTrackableComponent> ent, ref RequestTrackableNameEvent args)
    {
        if (args.Handled)
            return;

        var hive = _hive.GetHive(ent.Owner);

        if (hive == null)
            return;

        foreach (var item in hive.Value.Comp.HiveTunnels)
        {
            if (item.Value != ent.Owner)
                continue;

            args.Name = item.Key;
            break;
        }
        args.Handled = true;
    }

    private void OnMobStateChanged(Entity<RMCTrackableComponent> ent, ref MobStateChangedEvent args)
    {
        if (!HasComp<XenoComponent>(ent))
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        RemCompDeferred<RMCTrackableComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        // not putting HiveMember in the query so it uses the center alert with no hive
        var query = EntityQueryEnumerator<HiveTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (time < tracker.UpdateAt)
                continue;

            tracker.UpdateAt = time + tracker.UpdateEvery;

            // If the tracker is tracking an entity, point towards the target.
            if (tracker.Target != null)
            {
                if (!HasComp<RMCTrackableComponent>(tracker.Target.Value))
                {
                    SetTarget((uid, tracker), null);
                    continue;
                }

                UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value));
                continue;
            }

            // If the tracker is not tracking an entity, try to find a new target.
            var trackableQuery = EntityQueryEnumerator<RMCTrackableComponent, HiveMemberComponent>();
            while (trackableQuery.MoveNext(out var trackableUid, out _, out var targetMember))
            {
                if (!TryComp(uid, out HiveMemberComponent? member))
                    break;

                _prototypeManager.TryIndex(tracker.Mode, out var trackerMode);
                if (trackerMode?.Component == null)
                    break;

                if (member.Hive != targetMember.Hive)
                    continue;

                var trackingComponent = _factory.GetComponent(trackerMode.Component).GetType();
                if (EntityManager.TryGetComponent(trackableUid, trackingComponent, out _))
                {
                    SetTarget((uid, tracker), trackableUid);
                    if(tracker.Target != null)
                        UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value));
                }
                break;
            }
            UpdateDirection((uid, tracker));
        }
    }
}
