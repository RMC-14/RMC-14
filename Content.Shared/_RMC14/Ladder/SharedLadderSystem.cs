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
        SubscribeLocalEvent<LadderComponent, GetVerbsEvent<AlternativeVerb>>(OnLadderGetAltVerbs);
        SubscribeLocalEvent<LadderComponent, CanDropDraggedEvent>(OnLadderCanDropDragged);
        SubscribeLocalEvent<LadderComponent, CanDragEvent>(OnLadderCanDrag);
        SubscribeLocalEvent<LadderComponent, DragDropDraggedEvent>(OnLadderDragDropDragged);

        SubscribeLocalEvent<LadderWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);
    }

    /// <summary>
    /// Check if the ladder <paramref name="ent"/> is connected to any other ladders.
    /// </summary>
    public bool LadderIsConnected(Entity<LadderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;
        return ent.Comp.Above.HasValue || ent.Comp.Below.HasValue;
    }

    /// <summary>
    /// Check if the ladder <paramref name="ent"/> is connected to <paramref name="checkConnected"/>.
    /// </summary>
    public bool LadderIsConnected(Entity<LadderComponent?> ent, Entity<LadderComponent?> checkConnected)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(checkConnected, ref checkConnected.Comp))
            return false;
        return ent.Comp.Above == checkConnected || ent.Comp.Below == checkConnected;
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
        if (SelectConnectedLadder(ent, args.User, SelectionReason.Climb) is { } connecedLadder)
            StartClimbing(ent, connecedLadder, args.User);
    }

    // Returns either the UID of the sole connected ladder, or opens a radial menu for the user to pick one and returns null.
    private EntityUid? SelectConnectedLadder(Entity<LadderComponent> ent, EntityUid user, SelectionReason reason)
    {
        switch (ent.Comp.Above, ent.Comp.Below)
        {
            // `Above` and `Below` are both set.
            case (not null, not null):
                OpenRadialMenu(ent, user, reason);
                // Return null since the radial menu handles it from here.
                return null;
            // Only `Above` is set.
            case (not null, null):
                return ent.Comp.Above;
            // Only `Below` is set.
            case (null, not null):
                return ent.Comp.Below;
            // None of the above.
            default:
                _popup.PopupClient(Loc.GetString("rmc-ladder-leads-nowhere"), ent, user, PopupType.SmallCaution);
                return null;
        }
    }

    private void OnRadialMenuSelected(Entity<LadderComponent> ent, ref LadderRadialSelectedMessage args)
    {
        switch (args.Reason)
        {
            case SelectionReason.Climb:
                StartClimbing(ent, GetEntity(args.DestinationLadder), args.Actor);
                break;
            case SelectionReason.Watch:
                Watch(args.Actor, GetEntity(args.DestinationLadder));
                break;
        }
    }

    private string? GetDirectionText(Entity<LadderComponent> ent, EntityUid destinationLadder)
    {
        if (destinationLadder == ent.Comp.Above)
            return Loc.GetString("rmc-ladder-direction-up");
        if (destinationLadder == ent.Comp.Below)
            return Loc.GetString("rmc-ladder-direction-down");
        return null;
    }

    private void StartClimbing(Entity<LadderComponent> ent, EntityUid destinationLadder, EntityUid user)
    {
        if (HasComp<GhostComponent>(user))
        {
            MoveToDestinationLadder(ent, destinationLadder, user, false);
            return;
        }

        if (ent.Comp.CurrentDoAfterUser is { } currentDoAfterUser &&
            currentDoAfterUser != user &&
            ent.Comp.CurrentDoAfterId is { } currentDoAfterId &&
            _doAfter.IsRunning(currentDoAfterUser, currentDoAfterId))
        {
            _popup.PopupClient(Loc.GetString("rmc-ladder-someone-else-climbing"), ent, user, PopupType.SmallCaution);
            return;
        }

        var direction = GetDirectionText(ent, destinationLadder);
        if (direction == null)
            throw new ArgumentException($"The provided destination ladder '{ToPrettyString(destinationLadder)}' is not connected to the source '{ToPrettyString(ent)}'!");

        var ev = new LadderDoAfterEvent(GetNetEntity(destinationLadder));
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, ev, ent, ent)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
            NeedHand = true,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            BreakOnMove = true,
            DistanceThreshold = ent.Comp.Range,
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var doAfterId))
            return;

        ent.Comp.CurrentDoAfterId = doAfterId.Value.Index;
        ent.Comp.CurrentDoAfterUser = user;
        Dirty(ent);

        var selfMessage = Loc.GetString("rmc-ladder-start-climbing-self", ("direction", direction));
        var othersMessage = Loc.GetString("rmc-ladder-start-climbing-others", ("user", user), ("direction", direction));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        if (_actorQuery.TryComp(user, out var actor))
            AddViewer(ent, actor.PlayerSession);
    }

    private void OnLadderDoAfterAttempt(Entity<LadderComponent> ent, ref DoAfterAttemptEvent<LadderDoAfterEvent> args)
    {
        if (args.Cancelled)
            return;

        // if the user anchors themself for some reason (e.g. defender fortify)
        if (Transform(args.DoAfter.Args.User).Anchored)
            args.Cancel();
    }

    private void OnLadderDoAfter(Entity<LadderComponent> ent, ref LadderDoAfterEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.CurrentDoAfterId = null;
        ent.Comp.CurrentDoAfterUser = null;
        Dirty(ent);

        if (args.Cancelled)
            return;

        if (_net.IsClient && args.User != _player.LocalEntity)
            return;

        args.Handled = true;

        var destination = GetEntity(args.DestinationLadder);
        MoveToDestinationLadder(ent, destination, args.User);
    }

    private void MoveToDestinationLadder(Entity<LadderComponent> source, EntityUid destination, EntityUid toMove, bool showMsg = true)
    {
        if (TerminatingOrDeleted(destination))
            return;

        if (_actorQuery.TryComp(toMove, out var actor))
            RemoveViewer(source, actor.PlayerSession);

        var destCoords = _transform.GetMapCoordinates(destination);
        if (destCoords.MapId == MapId.Nullspace)
            return;

        _transform.SetMapCoordinates(toMove, destCoords);
        _rmcTeleporter.HandlePulling(toMove, destCoords);

        if (showMsg)
        {
            var direction = GetDirectionText(source, destination) ?? Loc.GetString("rmc-ladder-direction-up");

            var selfMessage = Loc.GetString("rmc-ladder-finish-climbing-self", ("direction", direction));
            var othersMessage = Loc.GetString("rmc-ladder-finish-climbing-others", ("user", toMove), ("direction", direction));
            _popup.PopupPredicted(selfMessage, othersMessage, toMove, toMove);
        }
    }

    private void OnLadderGetAltVerbs(Entity<LadderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!LadderIsConnected(ent.AsNullable()))
            return;

        var user = args.User;
        if (!_interaction.InRangeUnobstructed(user, ent.Owner))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 100,
            Act = () =>
            {
                if (CanWatchPopup(ent, user) &&
                    SelectConnectedLadder(ent, user, SelectionReason.Watch) is { } otherLadder)
                {
                    Watch(user, otherLadder);
                }
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
        if (user != args.Target || !LadderIsConnected(ent.AsNullable()))
            return;

        if (!CanWatchPopup(ent, user))
            return;

        args.Handled = true;
        if (SelectConnectedLadder(ent, user, SelectionReason.Watch) is { } otherLadder)
            Watch(user, otherLadder);
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

    protected virtual void OpenRadialMenu(Entity<LadderComponent> ent, EntityUid user, SelectionReason reason)
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
