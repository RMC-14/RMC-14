using System.Linq;
using Content.Shared._RMC14.Teleporter;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

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

    private EntityQuery<ActorComponent> _actorQuery;
    protected EntityQuery<LadderComponent> LadderQuery;

    public override void Initialize()
    {
        _actorQuery = GetEntityQuery<ActorComponent>();
        LadderQuery = GetEntityQuery<LadderComponent>();

        SubscribeLocalEvent<LadderComponent, ComponentRemove>(OnLadderRemove);
        SubscribeLocalEvent<LadderComponent, EntityTerminatingEvent>(OnLadderRemove);
        SubscribeLocalEvent<LadderComponent, ActivateInWorldEvent>(OnLadderActivateInWorld);
        SubscribeLocalEvent<LadderComponent, LadderRadialSelectedMessage>(OnRadialMenuSelected);
        SubscribeLocalEvent<LadderComponent, DoAfterAttemptEvent<LadderDoAfterEvent>>(OnLadderDoAfterAttempt);
        SubscribeLocalEvent<LadderComponent, LadderDoAfterEvent>(OnLadderDoAfter);
        //SubscribeLocalEvent<LadderComponent, GetVerbsEvent<AlternativeVerb>>(OnLadderGetAltVerbs);
        SubscribeLocalEvent<LadderComponent, CanDropDraggedEvent>(OnLadderCanDropDragged);
        SubscribeLocalEvent<LadderComponent, CanDragEvent>(OnLadderCanDrag);
        //SubscribeLocalEvent<LadderComponent, DragDropDraggedEvent>(OnLadderDragDropDragged);

        SubscribeLocalEvent<LadderWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);
    }

    private void OnLadderRemove<T>(Entity<LadderComponent> ent, ref T args)
    {
        foreach (var watching in ent.Comp.Watching)
        {
            if (TerminatingOrDeleted(watching))
                continue;

            RemCompDeferred<LadderWatchingComponent>(watching);
        }

        if (ent.Comp.Above is { } above &&
            !TerminatingOrDeleted(above) &&
            LadderQuery.TryComp(above, out var aboveComp))
        {
            aboveComp.Below = null;
            Dirty(above, aboveComp);
        }

        if (ent.Comp.Below is { } below &&
            !TerminatingOrDeleted(below) &&
            LadderQuery.TryComp(below, out var belowComp))
        {
            belowComp.Above = null;
            Dirty(below, belowComp);
        }
    }

    private void OnLadderActivateInWorld(Entity<LadderComponent> ent, ref ActivateInWorldEvent args)
    {
        // todo: make this less stupid
        if (ent.Comp.Above != null && ent.Comp.Below != null)
        {
            OpenRadialMenu(ent, args.User);
        }
        else if (ent.Comp.Above != null)
        {
            StartClimbing(ent, ent.Comp.Above.Value, args.User);
        }
        else if (ent.Comp.Below != null)
        {
            StartClimbing(ent, ent.Comp.Below.Value, args.User);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-ladder-leads-nowhere"), ent, args.User, PopupType.SmallCaution);
        }
    }

    private void OnRadialMenuSelected(Entity<LadderComponent> ent, ref LadderRadialSelectedMessage args)
    {
        StartClimbing(ent, GetEntity(args.DestinationLadder), args.Actor);
    }

    private void StartClimbing(Entity<LadderComponent> ent, EntityUid destinationLadder, EntityUid user)
    {
        var time = _timing.CurTime;
        if (ent.Comp.LastDoAfterEnt is { } lastEnt &&
            ent.Comp.LastDoAfterId is { } lastId &&
            time - ent.Comp.LastDoAfterTime < ent.Comp.Delay * 5 && // todo: check why `LastDoAfterTime` exists
            _doAfter.GetStatus(new DoAfterId(lastEnt, lastId)) == DoAfterStatus.Running &&
            !HasComp<GhostComponent>(user))
        {
            if (ent.Comp.LastDoAfterEnt != user) // todo: rename to `LastDoAfterUser`?
                _popup.PopupClient(Loc.GetString("rmc-ladder-someone-else-climbing"), ent, user, PopupType.SmallCaution);

            return;
        }

        var ev = new LadderDoAfterEvent(GetNetEntity(destinationLadder));
        var delay = ent.Comp.Delay;
        if (HasComp<GhostComponent>(user))
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

        var destination = GetEntity(args.DestinationLadder);
        if (TerminatingOrDeleted(destination))
            return;

        var coordinates = _transform.GetMapCoordinates(destination);
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

    // TODO: Use the same selection radial menu when you click the verb
    // private void OnLadderGetAltVerbs(Entity<LadderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    // {
    //     if (ent.Comp.Other is not { } other)
    //         return;

    //     var user = args.User;
    //     if (!CanWatchPopup(ent, user))
    //         return;

    //     args.Verbs.Add(new AlternativeVerb
    //     {
    //         Priority = 100,
    //         Act = () =>
    //         {
    //             if (CanWatchPopup(ent, user))
    //                 Watch(user, other);
    //         },
    //         Text = Loc.GetString("rmc-ladder-look-through"),
    //     });
    // }

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

    // todo
    // private void OnLadderDragDropDragged(Entity<LadderComponent> ent, ref DragDropDraggedEvent args)
    // {
    //     var user = args.User;
    //     if (ent.Comp.Other is not { } other ||
    //         user != args.Target)
    //     {
    //         return;
    //     }

    //     if (!CanWatchPopup(ent, user))
    //         return;

    //     args.Handled = true;
    //     Watch(user, other);
    // }

    private void OnWatchingMoveInput(Entity<LadderWatchingComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_net.IsClient && _player.LocalEntity == ent.Owner && _player.LocalSession != null)
            Unwatch(ent.Owner, _player.LocalSession);
        else if (TryComp(ent, out ActorComponent? actor))
            Unwatch(ent.Owner, actor.PlayerSession);
    }

    protected virtual void OpenRadialMenu(Entity<LadderComponent> ent, EntityUid user)
    { }

    protected virtual void AddViewer(Entity<LadderComponent> ent, ICommonSession player)
    { }

    protected virtual void RemoveViewer(Entity<LadderComponent> ent, ICommonSession player)
    { }

    protected virtual void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<LadderComponent?> toWatch)
    { }

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
}
