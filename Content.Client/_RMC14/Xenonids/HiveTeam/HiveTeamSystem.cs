using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveTeam;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.HiveTeam;

public sealed class HiveTeamSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HiveTeamsComponent, AfterAutoHandleStateEvent>(OnTeamsStateChanged);
    }

    private void OnTeamsStateChanged(Entity<HiveTeamsComponent> hive, ref AfterAutoHandleStateEvent args)
    {
        // Find any xeno in this hive that has a HiveTeam or HiveLeaderSquad BUI open
        var query = EntityQueryEnumerator<HiveMemberComponent>();
        while (query.MoveNext(out var uid, out var member))
        {
            if (member.Hive != hive.Owner)
                continue;

            if (_ui.TryGetOpenUi<HiveTeamBui>(uid, HiveTeamUIKey.Key, out var queenBui))
                queenBui.Refresh();

            if (_ui.TryGetOpenUi<HiveLeaderSquadBui>(uid, HiveLeaderSquadUIKey.Key, out var leaderBui))
                leaderBui.Refresh();
        }
    }
}
