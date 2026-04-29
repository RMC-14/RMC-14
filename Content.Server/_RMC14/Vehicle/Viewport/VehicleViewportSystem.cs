using Content.Server._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Viewport;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Server._RMC14.Vehicle.Viewport;

public sealed class VehicleViewportSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly VehicleSystem _vehicles = default!;
    [Dependency] private readonly VehicleViewToggleSystem _viewToggle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleViewportComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<VehicleViewportComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<VehicleViewportComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<VehicleEnterComponent, GetVerbsEvent<AlternativeVerb>>(OnVehicleEnterVerbs);
        SubscribeLocalEvent<VehicleViewportUserComponent, MoveInputEvent>(OnUserMove);
    }

    private void OnActivate(Entity<VehicleViewportComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!_vehicles.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is null)
            return;

        if (ToggleViewport(args.User, vehicle.Value, ent.Owner))
            args.Handled = true;
    }

    private void OnUserMove(Entity<VehicleViewportUserComponent> ent, ref MoveInputEvent args)
    {
        if (_net.IsClient || !args.HasDirectionalMovement)
            return;

        CloseViewport(ent.Owner, ent.Comp);
    }

    private void OnInteractUsing(Entity<VehicleViewportComponent> ent, ref InteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!_vehicles.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is null)
            return;

        if (ToggleViewport(args.User, vehicle.Value, ent.Owner))
            args.Handled = true;
    }

    private void OnInteractHand(Entity<VehicleViewportComponent> ent, ref InteractHandEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!_vehicles.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is null)
            return;

        if (ToggleViewport(args.User, vehicle.Value, ent.Owner))
            args.Handled = true;
    }

    private void OnVehicleEnterVerbs(Entity<VehicleEnterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (_net.IsClient ||
            !args.CanInteract ||
            !args.CanAccess ||
            args.Using != null ||
            !_vehicles.TryFindEntryPoint(ent.Owner, args.User, out _))
        {
            return;
        }

        var user = args.User;
        var vehicle = ent.Owner;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-vehicle-look-inside"),
            Act = () => ToggleInteriorPeek(user, vehicle),
        });
    }

    private bool ToggleViewport(EntityUid user, EntityUid vehicle, EntityUid source)
    {
        if (TryComp(user, out VehicleViewportUserComponent? existing))
        {
            CloseViewport(user, existing);
            return true;
        }

        var userState = EnsureComp<VehicleViewportUserComponent>(user);
        if (TryComp(user, out EyeComponent? newEye))
            userState.PreviousTarget = newEye.Target;
        userState.Source = source;

        _eye.SetTarget(user, vehicle);
        _viewToggle.EnableViewToggle(user, vehicle, source, userState.PreviousTarget, isOutside: true);
        return true;
    }

    private bool ToggleInteriorPeek(EntityUid user, EntityUid vehicle)
    {
        if (TryComp(user, out VehicleViewportUserComponent? existing))
        {
            if (existing.Source == vehicle)
            {
                CloseViewport(user, existing);
                return true;
            }

            CloseViewport(user, existing);
        }

        if (!_vehicles.TryGetInteriorPeekTarget(vehicle, out var peekTarget))
            return false;

        var userState = EnsureComp<VehicleViewportUserComponent>(user);
        if (TryComp(user, out EyeComponent? eye))
            userState.PreviousTarget = eye.Target;
        userState.Source = vehicle;

        _eye.SetTarget(user, peekTarget);
        return true;
    }

    private void CloseViewport(EntityUid user, VehicleViewportUserComponent? state = null)
    {
        state ??= CompOrNull<VehicleViewportUserComponent>(user);
        if (state == null)
            return;

        if (TryComp(user, out EyeComponent? eye))
            _eye.SetTarget(user, state.PreviousTarget, eye);

        if (state.Source != null)
            _viewToggle.DisableViewToggle(user, state.Source.Value);

        RemCompDeferred<VehicleViewportUserComponent>(user);
    }
}
