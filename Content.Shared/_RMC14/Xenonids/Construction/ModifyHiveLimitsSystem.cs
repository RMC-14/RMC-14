using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Hive;

namespace Content.Shared._RMC14.Xenonids.Construction;

public sealed class ModifyHiveLimitsSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyHiveLimitsComponent, XenoConstructBuiltEvent>(OnBuilt);
        SubscribeLocalEvent<ModifyHiveLimitsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnBuilt(Entity<ModifyHiveLimitsComponent> ent, ref XenoConstructBuiltEvent args)
    {
        foreach (var (id, limit) in ent.Comp.Construction)
        {
            _hive.AdjustConstructLimit(args.Hive, id, limit);
        }
    }

    private void OnShutdown(Entity<ModifyHiveLimitsComponent> ent, ref ComponentShutdown args)
    {
        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        // subtracting instead of adding here
        // if you manage to get a limit to 0 prior to this being removed it goes negative
        // to prevent possible infinite construction stacking, youd have to pay off your exploited one before making more
        foreach (var (id, limit) in ent.Comp.Construction)
        {
            _hive.AdjustConstructLimit(hive, id, -limit);
        }
    }
}
