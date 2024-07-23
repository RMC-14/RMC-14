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
        var hiveComp = Comp<HiveComponent>(args.Hive);
        var hive = (args.Hive, hiveComp);
        foreach (var (id, limit) in ent.Comp.Construction)
        {
            _hive.AdjustConstructLimit(hive, id, limit);
        }
    }

    private void OnShutdown(Entity<ModifyHiveLimitsComponent> ent, ref ComponentShutdown args)
    {
        if (CompOrNull<HiveMemberComponent>(ent)?.Hive is not {} hiveId ||
            TerminatingOrDeleted(hiveId) ||
            !TryComp<HiveComponent>(hiveId, out var hiveComp))
            return;

        // subtracting instead of adding here
        // if you manage to get a limit to 0 prior to this being removed
        var hive = (hiveId, hiveComp);
        foreach (var (id, limit) in ent.Comp.Construction)
        {
            _hive.AdjustConstructLimit(hive, id, -limit);
        }
    }
}
