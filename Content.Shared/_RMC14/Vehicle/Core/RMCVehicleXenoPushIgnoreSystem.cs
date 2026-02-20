using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleXenoPushIgnoreSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleXenoPushIgnoreComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<RMCVehicleXenoPushIgnoreComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<RMCVehicleXenoPushIgnoreComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnPreventCollide(Entity<RMCVehicleXenoPushIgnoreComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<ThrownItemComponent>(ent))
            args.Cancelled = true;
    }

    private void OnLand(Entity<RMCVehicleXenoPushIgnoreComponent> ent, ref LandEvent args)
    {
        if (_net.IsClient)
            return;

        RemCompDeferred<RMCVehicleXenoPushIgnoreComponent>(ent);
    }

    private void OnStopThrow(Entity<RMCVehicleXenoPushIgnoreComponent> ent, ref StopThrowEvent args)
    {
        if (_net.IsClient)
            return;

        RemCompDeferred<RMCVehicleXenoPushIgnoreComponent>(ent);
    }
}
