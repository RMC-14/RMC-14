using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleRunoverSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleRunoverComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<RMCVehicleRunoverComponent> ent, ref PreventCollideEvent args)
    {
        if (args.OtherEntity == ent.Comp.Vehicle)
            args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCVehicleRunoverComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(comp.Vehicle))
            {
                RemCompDeferred<RMCVehicleRunoverComponent>(uid);
                continue;
            }

            if (IsOverlapping(uid, comp.Vehicle))
            {
                if (comp.Duration == TimeSpan.Zero)
                    comp.Duration = TimeSpan.FromSeconds(1.5);

                comp.ExpiresAt = time + comp.Duration;
                _stun.TryKnockdown(uid, comp.Duration, true);
                continue;
            }

            if (comp.ExpiresAt <= time)
                RemCompDeferred<RMCVehicleRunoverComponent>(uid);
        }
    }

    private bool IsOverlapping(EntityUid mob, EntityUid vehicle)
    {
        var mobAabb = _lookup.GetWorldAABB(mob);
        var vehicleAabb = _lookup.GetWorldAABB(vehicle);
        return mobAabb.Intersects(vehicleAabb);
    }
}
