using Content.Shared.Camera;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleGunnerViewSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleGunnerViewUserComponent, GetEyePvsScaleEvent>(OnGetEyePvsScale);
        SubscribeLocalEvent<VehicleGunnerViewUserComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<VehicleGunnerViewUserComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VehicleGunnerViewUserComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnGetEyePvsScale(Entity<VehicleGunnerViewUserComponent> ent, ref GetEyePvsScaleEvent args)
    {
        args.Scale += ent.Comp.PvsScale + ent.Comp.CursorPvsIncrease;
    }

    private void OnHandleState(Entity<VehicleGunnerViewUserComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }

    private void OnStartup(Entity<VehicleGunnerViewUserComponent> ent, ref ComponentStartup args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }

    private void OnShutdown(Entity<VehicleGunnerViewUserComponent> ent, ref ComponentShutdown args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }
}
