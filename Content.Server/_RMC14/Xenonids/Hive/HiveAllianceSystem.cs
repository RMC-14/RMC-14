using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.ManageHive;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class HiveAllianceSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, OpenHiveAllianceUIEvent>(OnOpenUI);

        Subs.BuiEvents<XenoComponent>(HiveAllianceUIKey.Key, subs =>
        {
            subs.Event<HiveAllianceSetFactionMsg>(OnSetFactionAlly);
            subs.Event<HiveAllianceSetHiveMsg>(OnSetHiveAlly);
        });
    }

    private void OnOpenUI(Entity<XenoComponent> queen, ref OpenHiveAllianceUIEvent args)
    {
        _ui.OpenUi(queen.Owner, HiveAllianceUIKey.Key, queen);
    }

    private void OnSetFactionAlly(Entity<XenoComponent> queen, ref HiveAllianceSetFactionMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive || hive.Comp.CurrentQueen != queen.Owner)
            return;

        var faction = args.Faction;
        if (!Array.Exists(HiveAlliableFactions.All, f => f.Id == faction.Id))
            return;

        _hive.SetFactionAlly(hive, faction, args.Allied);
    }

    private void OnSetHiveAlly(Entity<XenoComponent> queen, ref HiveAllianceSetHiveMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive || hive.Comp.CurrentQueen != queen.Owner)
            return;

        if (GetEntity(args.Hive) is not { Valid: true } otherHive ||
            otherHive == hive.Owner ||
            !HasComp<HiveComponent>(otherHive))
        {
            return;
        }

        _hive.SetHiveAlly(hive, otherHive, args.Allied);
    }
}
