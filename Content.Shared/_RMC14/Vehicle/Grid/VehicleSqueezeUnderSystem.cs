using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleSqueezeUnderSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleSqueezeUnderComponent, PreventCollideEvent>(OnVehiclePreventCollide);
    }

    private void OnVehiclePreventCollide(Entity<VehicleSqueezeUnderComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp(args.OtherEntity, out VehicleSqueezingUnderComponent? squeezing) && squeezing.Vehicle == ent.Owner)
        {
            args.Cancelled = true;
            return;
        }

        if (HasComp<MobStateComponent>(args.OtherEntity) && _standing.IsDown(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (!CanSqueezeUnder(ent, args.OtherEntity))
            return;

        args.Cancelled = true;
        TryMarkUnder(args.OtherEntity, ent);
    }

    public bool CanSqueezeUnder(Entity<VehicleSqueezeUnderComponent> vehicle, EntityUid xeno)
    {
        if (!HasComp<XenoComponent>(xeno))
            return false;

        if (!_size.TryGetSize(xeno, out var size))
            return false;

        return size < vehicle.Comp.MinBlockingSize;
    }

    public void TryMarkUnder(EntityUid xeno, Entity<VehicleSqueezeUnderComponent> vehicle)
    {
        var squeezing = EnsureComp<VehicleSqueezingUnderComponent>(xeno);
        if (squeezing.Vehicle == vehicle.Owner)
            return;

        squeezing.Vehicle = vehicle.Owner;
        Dirty(xeno, squeezing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VehicleSqueezingUnderComponent>();
        while (query.MoveNext(out var uid, out var squeezing))
        {
            if (StillUnder(uid, squeezing.Vehicle))
                continue;

            RemCompDeferred<VehicleSqueezingUnderComponent>(uid);
        }
    }

    private bool StillUnder(EntityUid xeno, EntityUid vehicle)
    {
        if (!vehicle.IsValid() || TerminatingOrDeleted(vehicle) || !HasComp<VehicleSqueezeUnderComponent>(vehicle))
            return false;

        var vehicleAabb = _physics.GetWorldAABB(vehicle);
        var xenoAabb = _physics.GetWorldAABB(xeno);
        return vehicleAabb.Intersects(xenoAabb);
    }
}
