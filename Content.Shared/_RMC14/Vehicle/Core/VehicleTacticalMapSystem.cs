using Content.Shared._RMC14.TacticalMap;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleTacticalMapSystem : EntitySystem
{
    [Dependency] private readonly VehicleSystem _vehicle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapTrackedComponent, TacticalMapGetTrackedPositionEvent>(OnGetTrackedPosition);
    }

    private void OnGetTrackedPosition(Entity<TacticalMapTrackedComponent> ent, ref TacticalMapGetTrackedPositionEvent args)
    {
        if (args.Target != null)
            return;

        if (_vehicle.TryGetVehicleFromInterior(ent, out var vehicle) && vehicle is { } vehicleUid)
            args.Target = vehicleUid;
    }
}
