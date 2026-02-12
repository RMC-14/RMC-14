using Content.Shared.Camera;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleArtilleryViewSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleArtilleryViewUserComponent, GetEyePvsScaleEvent>(OnGetEyePvsScale);
        SubscribeLocalEvent<RMCVehicleArtilleryViewUserComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCVehicleArtilleryViewUserComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnGetEyePvsScale(Entity<RMCVehicleArtilleryViewUserComponent> ent, ref GetEyePvsScaleEvent args)
    {
        args.Scale += ent.Comp.PvsScale;
    }

    private void OnStartup(Entity<RMCVehicleArtilleryViewUserComponent> ent, ref ComponentStartup args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }

    private void OnShutdown(Entity<RMCVehicleArtilleryViewUserComponent> ent, ref ComponentShutdown args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }
}
