using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

public sealed class SquadLeaderTrackerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TrackerSystem _tracker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, MapCoordinates> _squadLeaders = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<GrantSquadLeaderTrackerComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantSquadLeaderTrackerComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<SquadLeaderTrackerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SquadLeaderTrackerComponent, ComponentRemove>(OnRemove);
    }

    private void OnGotEquipped(Entity<GrantSquadLeaderTrackerComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        EnsureComp<SquadLeaderTrackerComponent>(args.Equipee);
    }

    private void OnGotUnequipped(Entity<GrantSquadLeaderTrackerComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        if (!_inventory.TryGetInventoryEntity<GrantSquadLeaderTrackerComponent>(args.Equipee, out _))
            RemCompDeferred<SquadLeaderTrackerComponent>(args.Equipee);
    }

    private void OnMapInit(Entity<SquadLeaderTrackerComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert, TrackerSystem.CenterSeverity);
    }

    private void OnRemove(Entity<SquadLeaderTrackerComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        _squadLeaders.Clear();
        var leaders = EntityQueryEnumerator<SquadLeaderComponent, SquadMemberComponent>();
        while (leaders.MoveNext(out var uid, out _, out var member))
        {
            if (member.Squad is not { } squad)
                continue;

            _squadLeaders.TryAdd(squad, _transform.GetMapCoordinates(uid));
        }

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<SquadLeaderTrackerComponent, SquadMemberComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var member))
        {
            if (time < tracker.UpdateAt)
                continue;

            tracker.UpdateAt = time + tracker.UpdateEvery;

            if (member.Squad is not { } squad ||
                !_squadLeaders.TryGetValue(squad, out var leader))
            {
                _alerts.ShowAlert(uid, tracker.Alert, TrackerSystem.CenterSeverity);
                continue;
            }

            var severity = _tracker.GetAlertSeverity(uid, leader);
            _alerts.ShowAlert(uid, tracker.Alert, severity);
        }
    }
}
