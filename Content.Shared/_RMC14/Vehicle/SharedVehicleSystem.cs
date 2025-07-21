using Content.Shared._RMC14.Teleporter;
using Content.Shared._RMC14.Vehicle.Events;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;
public abstract class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedRMCTeleporterSystem _rmcTeleporter = default!;

    private EntityQuery<ActorComponent> _actorQuery;
    public override void Initialize()
    {
        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeLocalEvent<VehicleComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<VehicleComponent, VehicleEnterDoAfterEvent>(OnVehicleEnterDoAfter);
        SubscribeLocalEvent<VehicleComponent, DoAfterAttemptEvent<VehicleEnterDoAfterEvent>>(OnVehicleEnterDoAfterAttempt);
    }

    private void OnActivateInWorld(Entity<VehicleComponent> ent, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        if (ent.Comp.Other == null)
        {
            var msg = Loc.GetString("rmc-vehicle-has-no-interior");
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
                var msg = Loc.GetString("rmc-vehicle-someone-else-is-entering");
                _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            }

            return;
        }

        var ev = new VehicleEnterDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, ev, ent, ent, ent)
        {
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var doAfterId))
            return;

        ent.Comp.LastDoAfterEnt = doAfterId.Value.Uid;
        ent.Comp.LastDoAfterId = doAfterId.Value.Index;
        ent.Comp.LastDoAfterTime = time;
        Dirty(ent);

        if (ent.Comp.Delay > TimeSpan.Zero)
        {
            var selfMessage = Loc.GetString("rmc-vehicle-start-entering");
            var othersMessage = Loc.GetString("rmc-vehicle-start-entering-others", ("user", user));
            _popup.PopupPredicted(selfMessage, othersMessage, user, user);
        }
    }

    private void OnVehicleEnterDoAfterAttempt(Entity<VehicleComponent> ent, ref DoAfterAttemptEvent<VehicleEnterDoAfterEvent> args)
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
    }

    private void OnVehicleEnterDoAfter(Entity<VehicleComponent> ent, ref VehicleEnterDoAfterEvent args)
    {
        var user = args.User;
        if (_net.IsClient && user != _player.LocalEntity)
            return;

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Other is not { } other || TerminatingOrDeleted(ent.Comp.Other))
            return;

        var coordinates = _transform.GetMapCoordinates(other);
        if (coordinates.MapId == MapId.Nullspace)
            return;

        _transform.SetMapCoordinates(user, coordinates);

        var selfMessage = Loc.GetString("rmc-vehicle-finish-entering-self");
        var othersMessage = Loc.GetString("rmc-vehicle-finish-entering-others", ("user", user));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        ent.Comp.LastDoAfterEnt = null;
        ent.Comp.LastDoAfterId = null;
        Dirty(ent);

        //_rmcTeleporter.HandlePulling(user, coordinates);
    }
}
