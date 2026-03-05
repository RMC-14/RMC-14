using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.ResinMark;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Alert;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Content.Shared._RMC14.Tracker.Xeno;

public sealed class ResinMarkerTrackerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TrackerSystem _tracker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoWatchSystem _xenoWatch = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<AlertPrototype> ResinMarkerAlert = "ResinMarkerTracker";
    private static readonly ProtoId<AlertCategoryPrototype> ResinMarkerTrackerCategory = "ResinMarkerTracker";

    public override void Initialize()
    {
        SubscribeLocalEvent<ResinMarkerTrackerComponent, NewXenoEvolvedEvent>(OnNewXenoEvolved);
        SubscribeLocalEvent<ResinMarkerTrackerComponent, XenoDevolvedEvent>(OnXenoDevolved);
        SubscribeLocalEvent<ResinMarkerTrackerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ResinMarkerTrackerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ResinMarkerTrackerComponent, ResinMarkerTrackerClickedAlertEvent>(OnClickedAlert);
        SubscribeLocalEvent<ResinMarkerTrackerComponent, ResinMarkerTrackerAltClickedAlertEvent>(OnAltClickedAlert);
        SubscribeLocalEvent<ResinMarkerTrackerComponent, ResinMarkerTrackerSelectTargetEvent>(OnSelectTarget);
    }

    private void OnNewXenoEvolved(Entity<ResinMarkerTrackerComponent> newXeno, ref NewXenoEvolvedEvent args)
    {
        if (!TryComp<ResinMarkerTrackerComponent>(args.OldXeno, out var oldTracker))
            return;

        KeepTrackingOnEvolveDevolve(newXeno, (args.OldXeno.Owner, oldTracker));
    }

    private void OnXenoDevolved(Entity<ResinMarkerTrackerComponent> newXeno, ref XenoDevolvedEvent args)
    {
        if (!TryComp<ResinMarkerTrackerComponent>(args.OldXeno, out var oldTracker))
            return;

        KeepTrackingOnEvolveDevolve(newXeno, (args.OldXeno, oldTracker));
    }

    private void KeepTrackingOnEvolveDevolve(Entity<ResinMarkerTrackerComponent> newXeno, Entity<ResinMarkerTrackerComponent> oldTracker)
    {
        SetTarget(newXeno, oldTracker.Comp.Target);
        UpdateDirection(newXeno, oldTracker.Comp.Target == null ? null : _transform.GetMapCoordinates(oldTracker.Comp.Target.Value));

        var query = EntityQueryEnumerator<ResinMarkerTrackerComponent>();
        while (query.MoveNext(out var trackerUid, out var tracker))
        {
            if (tracker.Target == oldTracker.Owner)
                SetTarget((trackerUid, tracker), newXeno.Owner);
        }
    }

    private void OnMapInit(Entity<ResinMarkerTrackerComponent> ent, ref MapInitEvent args)
    {
        UpdateDirection(ent);
    }

    private void OnRemove(Entity<ResinMarkerTrackerComponent> ent, ref ComponentRemove args)
    {
        if (_net.IsClient)
            return;

        _alerts.ClearAlert(ent.Owner, ResinMarkerAlert);
    }

    private void OnClickedAlert(Entity<ResinMarkerTrackerComponent> ent, ref ResinMarkerTrackerClickedAlertEvent args)
    {
        if (ent.Comp.Target == null ||
            !TryComp(ent.Owner, out HiveMemberComponent? member) ||
            !TryComp(ent.Comp.Target, out XenoResinMarkerComponent? marker) ||
            marker.Hive != member.Hive)
        {
            return;
        }

        args.Handled = true;

        if (HasComp<XenoWatchingComponent>(ent.Owner) && TryComp(ent.Owner, out ActorComponent? actor))
            _xenoWatch.Unwatch(ent.Owner, actor.PlayerSession);
        else
            _xenoWatch.Watch(ent.Owner, ent.Comp.Target.Value);
    }

    private void OnAltClickedAlert(Entity<ResinMarkerTrackerComponent> ent, ref ResinMarkerTrackerAltClickedAlertEvent args)
    {
        if (!TryComp(ent.Owner, out HiveMemberComponent? member))
            return;

        var options = new List<DialogOption>();
        var query = EntityQueryEnumerator<XenoResinMarkerComponent>();
        while (query.MoveNext(out var markerUid, out var marker))
        {
            if (marker.Hive != member.Hive)
                continue;

            var targetName = _net.IsClient ? string.Empty : Name(markerUid);
            var nameEv = new RequestTrackableNameEvent();
            RaiseLocalEvent(markerUid, ref nameEv);
            if (nameEv.Name != null)
                targetName = nameEv.Name;

            options.Add(new DialogOption(
                targetName,
                new ResinMarkerTrackerSelectTargetEvent(GetNetEntity(markerUid))
            ));
        }

        if (options.Count == 0)
            return;

        args.Handled = true;
        _dialog.OpenOptions(
            ent,
            Loc.GetString("rmc-squad-info-tracking-selection"),
            options,
            Loc.GetString("rmc-squad-info-tracking-choose")
        );
    }

    private void OnSelectTarget(Entity<ResinMarkerTrackerComponent> ent, ref ResinMarkerTrackerSelectTargetEvent args)
    {
        if (!_timing.IsFirstTimePredicted ||
            !TryGetEntity(args.Target, out var targetUidNullable) ||
            targetUidNullable == null ||
            !TryComp(ent.Owner, out HiveMemberComponent? member) ||
            !TryComp(targetUidNullable.Value, out XenoResinMarkerComponent? marker) ||
            marker.Hive != member.Hive)
        {
            return;
        }

        SetTarget(ent, targetUidNullable.Value);
        UpdateDirection(ent, _transform.GetMapCoordinates(targetUidNullable.Value));
    }

    private void SetTarget(Entity<ResinMarkerTrackerComponent> ent, EntityUid? target)
    {
        ent.Comp.Target = target;
        Dirty(ent);
    }

    public void ForceTrackTarget(EntityUid trackerUid, EntityUid target)
    {
        if (!TryComp<ResinMarkerTrackerComponent>(trackerUid, out var tracker))
            return;

        var ent = (trackerUid, tracker);
        SetTarget(ent, target);
        UpdateDirection(ent, _transform.GetMapCoordinates(target));
    }

    private void UpdateDirection(Entity<ResinMarkerTrackerComponent> ent, MapCoordinates? coordinates = null)
    {
        if (_net.IsClient)
            return;

        _alerts.ClearAlertCategory(ent.Owner, ResinMarkerTrackerCategory);

        short severity = 0;
        if (coordinates != null)
            severity = _tracker.GetAlertSeverity(ent.Owner, coordinates.Value);

        _alerts.ShowAlert(ent.Owner, ResinMarkerAlert, severity);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ResinMarkerTrackerComponent, HiveMemberComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var member))
        {
            if (now < tracker.UpdateAt)
                continue;

            tracker.UpdateAt = now + tracker.UpdateEvery;

            if (tracker.Target == null)
            {
                UpdateDirection((uid, tracker));
                continue;
            }

            if (!TryComp(tracker.Target, out XenoResinMarkerComponent? marker) ||
                marker.Hive != member.Hive)
            {
                SetTarget((uid, tracker), null);
                UpdateDirection((uid, tracker));
                continue;
            }

            UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value));
        }
    }
}
