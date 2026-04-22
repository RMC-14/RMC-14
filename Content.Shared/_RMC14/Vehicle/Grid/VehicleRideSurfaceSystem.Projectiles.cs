using Content.Shared.Projectiles;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleRideSurfaceSystem
{
    private void OnSurfacePreventCollide(Entity<VehicleRideSurfaceComponent> ent, ref PreventCollideEvent args)
    {
        // targeted projectiles aimed at riders should not be blocked by the vehicle they are standing on
        if (args.Cancelled ||
            !HasComp<ProjectileComponent>(args.OtherEntity) ||
            !TryComp(args.OtherEntity, out TargetedProjectileComponent? targeted) ||
            !TryComp(targeted.Target, out VehicleRideSurfaceRiderComponent? rider) ||
            rider.Vehicle != ent.Owner)
        {
            return;
        }

        args.Cancelled = true;
    }

    private void OnRiderPreventCollide(Entity<VehicleRideSurfaceRiderComponent> ent, ref PreventCollideEvent args)
    {
        if (args.OtherEntity == ent.Comp.Vehicle)
            args.Cancelled = true;
    }

    public bool TryGetRiderAtCoordinates(EntityUid vehicle, MapCoordinates coordinates, out EntityUid rider)
    {
        rider = default;
        if (!TryComp(vehicle, out VehicleRideSurfaceComponent? surface) ||
            !_transformQuery.HasComp(vehicle))
        {
            return false;
        }

        var current = GetRideSurfaceTransform(_transformQuery.Comp(vehicle));
        if (coordinates.MapId != current.MapId || current.MapId == MapId.Nullspace)
            return false;

        var local = WorldToLocal(coordinates.Position, current);
        if (!Contains(surface, local, surface.SoftBorderPadding))
            return false;

        if (!_ridersByVehicle.TryGetValue(vehicle, out var riders))
            return false;

        var radiusSquared = RiderProjectileTargetRadius * RiderProjectileTargetRadius;
        var bestDistance = radiusSquared;
        var found = false;

        foreach (var uid in riders)
        {
            if (!TryComp(uid, out VehicleRideSurfaceRiderComponent? riderComp) ||
                riderComp.Vehicle != vehicle ||
                !_transformQuery.TryComp(uid, out var riderXform))
            {
                continue;
            }

            var riderMap = _transform.GetMapCoordinates((uid, riderXform));
            if (riderMap.MapId != coordinates.MapId)
                continue;

            var distance = (riderMap.Position - coordinates.Position).LengthSquared();
            if (distance > bestDistance)
                continue;

            bestDistance = distance;
            rider = uid;
            found = true;
        }

        return found;
    }
}
