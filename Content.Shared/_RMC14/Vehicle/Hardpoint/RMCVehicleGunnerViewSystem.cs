using Content.Shared.Camera;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleGunnerViewSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleGunnerViewUserComponent, GetEyePvsScaleEvent>(OnGetEyePvsScale);
        SubscribeLocalEvent<RMCVehicleGunnerViewUserComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCVehicleGunnerViewUserComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnGetEyePvsScale(Entity<RMCVehicleGunnerViewUserComponent> ent, ref GetEyePvsScaleEvent args)
    {
        args.Scale += ent.Comp.PvsScale;
    }

    private void OnStartup(Entity<RMCVehicleGunnerViewUserComponent> ent, ref ComponentStartup args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }

    private void OnShutdown(Entity<RMCVehicleGunnerViewUserComponent> ent, ref ComponentShutdown args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }
}
