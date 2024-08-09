using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Hive;

﻿namespace Content.Shared._RMC14.Xenonids.Construction;

public abstract class SharedXenoHiveCoreSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoAnnounceSystem _announce = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiveCoreComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<HiveCoreComponent> ent, ref ComponentShutdown args)
    {
        // TODO: when OB is a thing add this popup:
        // "The resin roof withers away as {THE($core)} dies!"

        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        _hive.StartCoreDeathCooldown(hive, ent.Comp.NewConstructCooldown);
        var msg = Loc.GetString("cm-xeno-hive-core-death", ("core", ent));
        _announce.AnnounceToHive(ent, hive, msg);
    }
}
