using Content.Shared._RMC14.Xenonids.Hive;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Hive;

public sealed class HiveAllianceRefreshSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HiveComponent, AfterAutoHandleStateEvent>(OnHiveStateChanged);
    }

    private void OnHiveStateChanged(Entity<HiveComponent> hive, ref AfterAutoHandleStateEvent args)
    {
        var query = EntityQueryEnumerator<HiveMemberComponent>();
        while (query.MoveNext(out var uid, out var member))
        {
            if (member.Hive != hive.Owner)
                continue;

            if (_ui.TryGetOpenUi<HiveAllianceBui>(uid, HiveAllianceUIKey.Key, out var bui))
                bui.Refresh();
        }
    }
}
