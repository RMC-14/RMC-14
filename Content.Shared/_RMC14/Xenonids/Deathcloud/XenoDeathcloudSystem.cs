using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Deathcloud;

public sealed partial class XenoDeathcloudSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedXenoHiveSystem _hive = default!;

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

        var spawn = SpawnAtPosition(xeno.Comp.Spawn, xeno.Owner.ToCoordinates());
        _hive.SetSameHive(xeno.Owner, spawn);
    }
}
