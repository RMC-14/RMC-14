using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Tracker.Xeno;

public sealed class HiveTrackerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TrackerSystem _tracker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWatchXenoSystem _watchXeno = default!;

    private readonly Dictionary<EntityUid, MapCoordinates> _hiveLeaders = new();

    public override void Initialize()
    {
        // TODO RMC14 resin tracker, hive leader tracker
        SubscribeLocalEvent<HiveTrackerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HiveTrackerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<HiveTrackerComponent, HiveTrackerClickedAlertEvent>(OnClickedAlert);
    }

    private void OnMapInit(Entity<HiveTrackerComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert, TrackerSystem.CenterSeverity);
    }

    private void OnRemove(Entity<HiveTrackerComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void OnClickedAlert(Entity<HiveTrackerComponent> ent, ref HiveTrackerClickedAlertEvent args)
    {
        if (!TryComp(ent, out XenoComponent? selfXeno) ||
            selfXeno.Hive is not { } selfHive)
        {
            return;
        }

        args.Handled = true;
        var granters = EntityQueryEnumerator<XenoEvolutionGranterComponent, XenoComponent>();
        while (granters.MoveNext(out var uid, out var granter, out var granterXeno))
        {
            if (granterXeno.Hive is not { } granterHive ||
                selfHive != granterHive)
            {
                continue;
            }

            _watchXeno.Watch((ent, selfXeno), (uid, granterXeno));
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        _hiveLeaders.Clear();
        var granters = EntityQueryEnumerator<XenoEvolutionGranterComponent, XenoComponent>();
        while (granters.MoveNext(out var uid, out _, out var xeno))
        {
            if (xeno.Hive is not { } hive)
                continue;

            if (_hiveLeaders.ContainsKey(hive))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            _hiveLeaders[hive] = _transform.GetMapCoordinates(uid);
        }

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<HiveTrackerComponent, XenoComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var xeno))
        {
            if (time < tracker.UpdateAt)
                continue;

            tracker.UpdateAt = time + tracker.UpdateEvery;

            if (xeno.Hive is not { } hive ||
                !_hiveLeaders.TryGetValue(hive, out var leader))
            {
                _alerts.ShowAlert(uid, tracker.Alert, TrackerSystem.CenterSeverity);
                continue;
            }

            var severity = _tracker.GetAlertSeverity(uid, leader);
            _alerts.ShowAlert(uid, tracker.Alert, severity);
        }
    }
}
