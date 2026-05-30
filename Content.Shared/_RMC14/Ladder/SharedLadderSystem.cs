using System.Linq;
using Content.Shared._RMC14.Teleporter;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ladder;

public abstract class SharedLadderSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCTeleporterSystem _rmcTeleporter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<LadderComponent>> _toUpdate = new();
    private readonly Dictionary<string, HashSet<Entity<LadderComponent>>> _toUpdateIds = new();

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<LadderComponent> _ladderQuery;

    public override void Initialize()
    {
        _actorQuery = GetEntityQuery<ActorComponent>();
        _ladderQuery = GetEntityQuery<LadderComponent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<LadderComponent, MapInitEvent>(OnLadderMapInit);
        SubscribeLocalEvent<LadderComponent, ComponentRemove>(OnLadderRemove);
        SubscribeLocalEvent<LadderComponent, EntityTerminatingEvent>(OnLadderRemove);
        SubscribeLocalEvent<LadderComponent, ActivateInWorldEvent>(OnLadderActivateInWorld);
        SubscribeLocalEvent<LadderComponent, DoAfterAttemptEvent<LadderDoAfterEvent>>(OnLadderDoAfterAttempt);
        SubscribeLocalEvent<LadderComponent, LadderDoAfterEvent>(OnLadderDoAfter);
        SubscribeLocalEvent<LadderComponent, GetVerbsEvent<AlternativeVerb>>(OnLadderGetAltVerbs);
        SubscribeLocalEvent<LadderComponent, CanDropDraggedEvent>(OnLadderCanDropDragged);
        SubscribeLocalEvent<LadderComponent, CanDragEvent>(OnLadderCanDrag);
        SubscribeLocalEvent<LadderComponent, DragDropDraggedEvent>(OnLadderDragDropDragged);

        SubscribeLocalEvent<LadderWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _toUpdate.Clear();
        _toUpdateIds.Clear();
    }

    private void OnLadderMapInit(Entity<LadderComponent> ent, ref MapInitEvent args)
    {
        _toUpdate.Add(ent);
    }

    public bool LadderIdInUse(string id)
    {
        if (_toUpdateIds.ContainsKey(id))
            return true;
        return false;
    }

    public void ReassignLadderId(Entity<LadderComponent> ent, string? newId)
    {
        if (ent.Comp.Other != null)
        {
            if (TryComp<LadderComponent>(ent.Comp.Other, out var ladder))
            {
                var other = ent.Comp.Other.Value;
                //Remove other ladder connect
                ent.Comp.Other = null;
                ladder.Id = null;
                ladder.Other = null;
                Dirty(other, ladder);
            }
        }

        if (ent.Comp.Id != null)
            _toUpdateIds.Remove(ent.Comp.Id);

        ent.Comp.Id = newId;
        Dirty(ent);
        _toUpdate.Add(ent);
    }

    private void OnLadderRemove<T>(Entity<LadderComponent> ent, ref T args)
    {
        foreach (var watching in ent.Comp.Watching)
        {
            if (TerminatingOrDeleted(watching))
                continue;

            RemCompDeferred<LadderWatchingComponent>(watching);
        }

        if (!TerminatingOrDeleted(ent.Comp.Other) &&
            _ladderQuery.TryComp(ent.Comp.Other, out var otherLadder))
        {
            otherLadder.Other = null;
            Dirty(ent.Comp.Other.Value, otherLadder);
        }

        ent.Comp.Other = null;
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
            _doAfter.GetStatus(new DoAfterId(lastEnt, lastId)) == DoAfterStatus.Running &&
            !HasComp<GhostComponent>(user))
        {
            if (ent.Comp.LastDoAfterEnt != user)
            {
                var msg = Loc.GetString("rmc-ladder-someone-else-climbing");
                _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            }

            return;
        }

        var ev = new LadderDoAfterEvent();
        var delay = ent.Comp.Delay;
        if (HasComp<GhostComponent>(args.User))
            delay = TimeSpan.Zero;

        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, ent, ent)
        {
            AttemptFrequency = delay == TimeSpan.Zero ? AttemptFrequency.Never : AttemptFrequency.EveryTick,
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

        if (_actorQuery.TryComp(user, out var actor))
            AddViewer(ent, actor.PlayerSession);
    }

    private void OnLadderDoAfterAttempt(Entity<LadderComponent> ent, ref DoAfterAttemptEvent<LadderDoAfterEvent> args)
    {
        if (args.Cancelled)
            return;

        var user = args.DoAfter.Args.User;
        var target = ent.Owner.ToCoordinates();
        if (user.ToCoordinates().TryDistance(EntityManager, _transform, target, out var distance) &&
            distance > ent.Comp.Range)
        {
            args.Cancel();
        }

        if (Transform(user).Anchored)
            args.Cancel();
    }

    private void OnLadderDoAfter(Entity<LadderComponent> ent, ref LadderDoAfterEvent args)
    {
        var user = args.User;
        if (_net.IsClient && user != _player.LocalEntity)
            return;

        if (_actorQuery.TryComp(user, out var actor))
            RemoveViewer(ent, actor.PlayerSession);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Other is not { } other || TerminatingOrDeleted(ent.Comp.Other))
            return;

        var coordinates = _transform.GetMapCoordinates(other);
        if (coordinates.MapId == MapId.Nullspace)
            return;

        _transform.SetMapCoordinates(user, coordinates);

        var selfMessage = Loc.GetString("rmc-ladder-finish-climbing-self");
        var othersMessage = Loc.GetString("rmc-ladder-finish-climbing-others", ("user", user));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        ent.Comp.LastDoAfterEnt = null;
        ent.Comp.LastDoAfterId = null;
        Dirty(ent);

        _rmcTeleporter.HandlePulling(user, coordinates);
    }

    private void OnLadderGetAltVerbs(Entity<LadderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (ent.Comp.Other is not { } other)
            return;

        var user = args.User;
        if (!CanWatchPopup(ent, user))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 100,
            Act = () =>
            {
                if (!CanWatchPopup(ent, user))
                    return;

                Watch(user, other);
            },
            Text = Loc.GetString("rmc-ladder-look-through"),
        });
    }

    private void OnLadderCanDropDragged(Entity<LadderComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.User != args.Target)
            return;

        args.Handled = true;
        args.CanDrop = true;
    }

    private void OnLadderCanDrag(Entity<LadderComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnLadderDragDropDragged(Entity<LadderComponent> ent, ref DragDropDraggedEvent args)
    {
        var user = args.User;
        if (ent.Comp.Other is not { } other ||
            user != args.Target)
        {
            return;
        }

        if (!CanWatchPopup(ent, user))
            return;

        args.Handled = true;
        Watch(user, other);
    }

    private void OnWatchingMoveInput(Entity<LadderWatchingComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_net.IsClient && _player.LocalEntity == ent.Owner && _player.LocalSession != null)
            Unwatch(ent.Owner, _player.LocalSession);
        else if (TryComp(ent, out ActorComponent? actor))
            Unwatch(ent.Owner, actor.PlayerSession);
    }

    protected virtual void AddViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
    }

    protected virtual void RemoveViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
    }

    protected virtual void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<LadderComponent?> toWatch)
    {
    }

    protected virtual void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        _eye.SetTarget(watcher, null);
    }

    protected bool CanWatchPopup(Entity<LadderComponent> ladder, EntityUid user)
    {
        if (!_interaction.InRangeUnobstructed(user, ladder.Owner, popup: true))
            return false;

        return true;
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

            var ids = _toUpdateIds.GetOrNew(id);
            if (ids.Count > 2)
            {
                var idsString = string.Join(",", ids.Select(e => ToPrettyString(e)));
                Log.Error($"Found more than 2 ladders with id {id}, current ladder: {ToPrettyString(entity)}, previous ladders: {idsString}");
            }

            ids.Add(entity);
        }

        _toUpdate.Clear();

        var ladders = EntityQueryEnumerator<LadderComponent>();
        while (ladders.MoveNext(out var uid, out var ladder))
        {
            if (ladder.Id == null)
                continue;

            if (!_toUpdateIds.TryGetValue(ladder.Id, out var ids))
                continue;

            if (ids.FirstOrNull(e => e.Owner != uid) is not { } toUpdate)
                continue;

            if (toUpdate.Owner == uid)
                continue;

            if (ladder.Other is { } old && old != toUpdate.Owner)
                Log.Error($"Found {ToPrettyString(toUpdate)} with duplicate ID {toUpdate.Comp.Id}, previous ladder: {ToPrettyString(old)}");

            ladder.Other = toUpdate;
            Dirty(uid, ladder);

            toUpdate.Comp.Other = uid;
            Dirty(toUpdate);
        }
    }
}
