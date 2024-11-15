using Content.Shared.Coordinates;
using Content.Shared.Mobs;

namespace Content.Shared._RMC14.Xenonids.Deathcloud;

public sealed class XenoDeathcloudSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeathcloudComponent, MobStateChangedEvent>(OnStateChanged);
    }

    private void OnStateChanged(Entity<XenoDeathcloudComponent> xeno, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        SpawnAtPosition(xeno.Comp.Spawn, xeno.Owner.ToCoordinates());
    }
}
