using Content.Server._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Viewport;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Server._RMC14.Vehicle.Viewport;

public sealed class RMCVehicleViewportSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly RMCVehicleSystem _vehicles = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleViewportComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<RMCVehicleViewportComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RMCVehicleViewportComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCVehicleViewportUserComponent, MoveInputEvent>(OnUserMove);
    }

    private void OnActivate(Entity<RMCVehicleViewportComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!_vehicles.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is null)
            return;

        if (ToggleViewport(args.User, vehicle.Value))
            args.Handled = true;
    }

    private void OnUserMove(Entity<RMCVehicleViewportUserComponent> ent, ref MoveInputEvent args)
    {
        if (_net.IsClient || !args.HasDirectionalMovement)
            return;

        CloseViewport(ent.Owner, ent.Comp);
    }

    private void OnInteractUsing(Entity<RMCVehicleViewportComponent> ent, ref InteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!_vehicles.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is null)
            return;

        if (ToggleViewport(args.User, vehicle.Value))
            args.Handled = true;
    }

    private void OnInteractHand(Entity<RMCVehicleViewportComponent> ent, ref InteractHandEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!_vehicles.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is null)
            return;

        if (ToggleViewport(args.User, vehicle.Value))
            args.Handled = true;
    }

    private bool ToggleViewport(EntityUid user, EntityUid vehicle)
    {
        if (TryComp(user, out RMCVehicleViewportUserComponent? existing))
        {
            CloseViewport(user, existing);
            return true;
        }

        var userState = EnsureComp<RMCVehicleViewportUserComponent>(user);
        if (TryComp(user, out EyeComponent? newEye))
            userState.PreviousTarget = newEye.Target;

        _eye.SetTarget(user, vehicle);
        return true;
    }

    private void CloseViewport(EntityUid user, RMCVehicleViewportUserComponent? state = null)
    {
        state ??= CompOrNull<RMCVehicleViewportUserComponent>(user);
        if (state == null)
            return;

        if (TryComp(user, out EyeComponent? eye))
            _eye.SetTarget(user, state.PreviousTarget, eye);

        RemCompDeferred<RMCVehicleViewportUserComponent>(user);
    }
}
