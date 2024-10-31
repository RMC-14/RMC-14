using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
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
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
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
        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        args.Handled = true;
        // TODO: if queen gets stored on the hive entity just use that instead of searching for it
        var granters = EntityQueryEnumerator<XenoEvolutionGranterComponent, HiveMemberComponent, XenoComponent>();
        while (granters.MoveNext(out var uid, out var granter, out var member, out var xeno))
        {
            if (member.Hive != hive.Owner)
                continue;

            _watchXeno.Watch(ent.Owner, (uid, member));
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        // TODO: replace this meme with a queen field on the hive that gets networked
        _hiveLeaders.Clear();
        var granters = EntityQueryEnumerator<XenoEvolutionGranterComponent, HiveMemberComponent>();
        while (granters.MoveNext(out var uid, out _, out var member))
        {
            if (member.Hive is not {} hive)
                continue;

            if (_hiveLeaders.ContainsKey(hive))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            _hiveLeaders[hive] = _transform.GetMapCoordinates(uid);
        }

        var time = _timing.CurTime;
        // not putting HiveMember in the query so it uses the center alert with no hive
        var query = EntityQueryEnumerator<HiveTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (time < tracker.UpdateAt)
                continue;

            tracker.UpdateAt = time + tracker.UpdateEvery;

            if (_hive.GetHive(uid) is not {} hive)
            {
                _alerts.ClearAlert(uid, tracker.Alert);
                continue;
            }

            if (!_hiveLeaders.TryGetValue(hive, out var leader))
            {
                _alerts.ShowAlert(uid, tracker.Alert, TrackerSystem.CenterSeverity);
                continue;
            }

            var severity = _tracker.GetAlertSeverity(uid, leader);
            _alerts.ShowAlert(uid, tracker.Alert, severity);
        }
    }
}
