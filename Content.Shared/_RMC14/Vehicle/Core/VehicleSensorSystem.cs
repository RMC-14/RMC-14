using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleSensorSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapCollectRevealsEvent>(OnTacticalMapCollectReveals);
    }

    private void OnTacticalMapCollectReveals(ref TacticalMapCollectRevealsEvent ev)
    {
        var query = EntityQueryEnumerator<VehicleSensorComponent>();
        while (query.MoveNext(out var uid, out var sensor))
        {
            if (!TryGetActiveVehicle(uid, sensor, out var vehicle))
                continue;

            if (!TryComp(vehicle, out TransformComponent? xform) ||
                xform.GridUid != ev.Map ||
                !TryComp(ev.Map, out MapGridComponent? gridComp) ||
                !_transform.TryGetGridTilePosition((vehicle, xform), out var indices, gridComp))
            {
                continue;
            }

            ev.Reveals.Add(new TacticalMapReveal(sensor.RevealFrom, sensor.RevealTo, indices, sensor.Range));
        }
    }

    private bool TryGetActiveVehicle(EntityUid uid, VehicleSensorComponent sensor, out EntityUid vehicle)
    {
        vehicle = uid;

        if (sensor.RequiresDeployed)
        {
            if (!TryComp(uid, out VehicleDeployableComponent? deployable) || !deployable.Deployed)
                return false;
        }
        else if (_container.TryGetContainingContainer(uid, out var container))
        {
            vehicle = container.Owner;
        }
        else
        {
            return false;
        }

        if (TryComp(vehicle, out HardpointIntegrityComponent? frame) && frame.Integrity <= 0f)
            return false;

        return true;
    }
}
