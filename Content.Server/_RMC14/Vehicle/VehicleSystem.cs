using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared.Interaction;
using Robust.Server.Console.Commands;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Server._RMC14.Vehicle;
public class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnSpawnVehicleInterior);
    }

    private void OnSpawnVehicleInterior(Entity<VehicleComponent> ent, ref ComponentInit args)
    {
        if (HasComp<VehicleExitComponent>(ent))
        {


            return; // No recursive map spawning please.
        }

        var vehicleInteriorsMap = _mapSystem.CreateMap();
        var vehicleInteriorsMapTransform = Transform(vehicleInteriorsMap);
        var interior = new ResPath("/Maps/_RMC14/Vehicles/vehicle_testing_interior.yml");
        var interiorIndex = 0;

        if (!_mapLoader.TryLoadGrid(vehicleInteriorsMapTransform.MapID, interior, out var interiorGrid, offset: new Vector2(interiorIndex * 100, interiorIndex * 100)))
        {
            return;
        }

        var exitComp = EntityQueryEnumerator<VehicleExitComponent>();

        while (exitComp.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<VehicleComponent>(uid, out var vehicleComp))
                continue;

            if (vehicleComp.Other is { } other)
                continue;

            //if (vehicleComp.Id == ent.Comp.Id)
            //    continue;

            ent.Comp.Other = uid;
            vehicleComp.Other = ent.Owner;
        }
    }
}
