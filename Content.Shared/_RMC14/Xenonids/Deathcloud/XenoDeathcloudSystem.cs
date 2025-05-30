using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Deathcloud;

public sealed class XenoDeathcloudSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeathcloudComponent, MobStateChangedEvent>(OnStateChanged);
    }

    private void OnStateChanged(Entity<XenoDeathcloudComponent> xeno, ref MobStateChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        SpawnAtPosition(xeno.Comp.Spawn, xeno.Owner.ToCoordinates());
        QueueDel(xeno);
    }
}
