using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using System;

namespace Content.Shared._RMC14.Vehicle;
public abstract class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(Entity<VehicleComponent> ent, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        _popup.PopupClient("Vehicle interaction!", user);
    }
}
