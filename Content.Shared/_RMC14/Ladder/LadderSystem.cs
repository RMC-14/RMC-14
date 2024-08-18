using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Ladder;

public sealed class LadderSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<LadderComponent>> _toUpdate = new();
    private readonly Dictionary<string, Entity<LadderComponent>> _toUpdateIds = new();

    private EntityQuery<LadderComponent> _ladderQuery;

    public override void Initialize()
    {
        _ladderQuery = GetEntityQuery<LadderComponent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<LadderComponent, MapInitEvent>(OnLadderMapInit);
        SubscribeLocalEvent<LadderComponent, ComponentRemove>(OnLadderRemove);
        SubscribeLocalEvent<LadderComponent, EntityTerminatingEvent>(OnLadderTerminating);
        SubscribeLocalEvent<LadderComponent, ActivateInWorldEvent>(OnLadderActivateInWorld);
        SubscribeLocalEvent<LadderComponent, DoAfterAttemptEvent<LadderDoAfterEvent>>(OnLadderDoAfterAttempt);
        SubscribeLocalEvent<LadderComponent, LadderDoAfterEvent>(OnLadderDoAfter);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _toUpdate.Clear();
    }

    private void OnLadderMapInit(Entity<LadderComponent> ent, ref MapInitEvent args)
    {
        _toUpdate.Add(ent);
    }

    private void OnLadderRemove(Entity<LadderComponent> ent, ref ComponentRemove args)
    {
        OnRemoveLadder(ent);
    }

    private void OnLadderTerminating(Entity<LadderComponent> ent, ref EntityTerminatingEvent args)
    {
        OnRemoveLadder(ent);
    }

    private void OnLadderActivateInWorld(Entity<LadderComponent> ent, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        if (ent.Comp.Other == null)
        {
            var msg = Loc.GetString("rmc-ladder-leads-nowhere");
            _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            return;
        }

        var time = _timing.CurTime;
        if (ent.Comp.LastDoAfterEnt is { } lastEnt &&
            ent.Comp.LastDoAfterId is { } lastId &&
            time - ent.Comp.LastDoAfterTime < ent.Comp.Delay * 5 &&
            _doAfter.GetStatus(new DoAfterId(lastEnt, lastId)) == DoAfterStatus.Running)
        {
            if (ent.Comp.LastDoAfterEnt != user)
            {
                var msg = Loc.GetString("rmc-ladder-someone-else-climbing");
                _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            }

            return;
        }

        var ev = new LadderDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, ev, ent, ent, ent)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var doAfterId))
            return;

        ent.Comp.LastDoAfterEnt = doAfterId.Value.Uid;
        ent.Comp.LastDoAfterId = doAfterId.Value.Index;
        ent.Comp.LastDoAfterTime = time;
        Dirty(ent);

        if (ent.Comp.Delay > TimeSpan.Zero)
        {
            var selfMessage = Loc.GetString("rmc-ladder-start-climbing-self");
            var othersMessage = Loc.GetString("rmc-ladder-start-climbing-others", ("user", user));
            _popup.PopupPredicted(selfMessage, othersMessage, user, user);
        }
    }

    private void OnLadderDoAfterAttempt(Entity<LadderComponent> ent, ref DoAfterAttemptEvent<LadderDoAfterEvent> args)
    {
        if (args.Cancelled)
            return;

        var user = args.DoAfter.Args.User.ToCoordinates();
        var target = ent.Owner.ToCoordinates();
        if (user.TryDistance(EntityManager, _transform, target, out var distance) &&
            distance > ent.Comp.Range)
        {
            args.Cancel();
        }
    }

    private void OnLadderDoAfter(Entity<LadderComponent> ent, ref LadderDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Other is not { } other || TerminatingOrDeleted(ent.Comp.Other))
            return;

        var user = args.User;
        _transform.SetCoordinates(user, _transform.GetMoverCoordinates(other));

        var selfMessage = Loc.GetString("rmc-ladder-finish-climbing-self");
        var othersMessage = Loc.GetString("rmc-ladder-finish-climbing-others", ("user", user));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        ent.Comp.LastDoAfterEnt = null;
        ent.Comp.LastDoAfterId = null;
        Dirty(ent);
    }

    private void OnRemoveLadder(Entity<LadderComponent> ent)
    {
        if (!TerminatingOrDeleted(ent.Comp.Other) &&
            _ladderQuery.TryComp(ent.Comp.Other, out var otherLadder))
        {
            otherLadder.Other = null;
            Dirty(ent.Comp.Other.Value, otherLadder);
        }

        ent.Comp.Other = null;
    }

    public override void Update(float frameTime)
    {
        if (_toUpdate.Count == 0)
            return;

        if (_net.IsClient)
        {
            _toUpdateIds.Clear();
            _toUpdate.Clear();
            return;
        }

        _toUpdateIds.Clear();
        foreach (var entity in _toUpdate)
        {
            if (entity.Comp.Id is not { } id)
                continue;

            if (_toUpdateIds.TryGetValue(id, out var old))
                Log.Error($"Found {ToPrettyString(entity)} with duplicate ID {id}, previous ladder: {ToPrettyString(old)}");

            _toUpdateIds[id] = entity;
        }

        _toUpdate.Clear();

        var ladders = EntityQueryEnumerator<LadderComponent>();
        while (ladders.MoveNext(out var uid, out var ladder))
        {
            if (ladder.Id == null)
                continue;

            if (!_toUpdateIds.TryGetValue(ladder.Id, out var toUpdate))
                continue;

            if (toUpdate.Owner == uid)
                continue;

            if (ladder.Other is { } old)
                Log.Error($"Found {ToPrettyString(toUpdate)} with duplicate ID {toUpdate.Comp.Id}, previous ladder: {ToPrettyString(old)}");

            ladder.Other = toUpdate;
            Dirty(uid, ladder);

            toUpdate.Comp.Other = uid;
            Dirty(toUpdate);
        }
    }
}
