using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Hive;

namespace Content.Client._RMC14.Xenonids.Hive;

public sealed class HiveAllianceRefreshSystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HiveComponent, AfterAutoHandleStateEvent>(OnHiveStateChanged);
    }

    private void OnHiveStateChanged(Entity<HiveComponent> hive, ref AfterAutoHandleStateEvent args)
    {
        if (hive.Comp.CurrentQueen is { } queen)
            _rmcUI.RefreshUIs<HiveAllianceBui>(queen);
    }
}
