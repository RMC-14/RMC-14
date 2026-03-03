using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.HiveTeam;
using Content.Shared._RMC14.Xenonids.ManageHive;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.HiveTeam;

public sealed class HiveTeamSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, OpenHiveTeamsUIEvent>(OnOpenUI);

        Subs.BuiEvents<XenoComponent>(HiveTeamUIKey.Key, subs =>
        {
            subs.Event<HiveTeamSetLeaderMsg>(OnSetLeader);
            subs.Event<HiveTeamRemoveLeaderMsg>(OnRemoveLeader);
            subs.Event<HiveTeamAddMemberMsg>(OnAddMember);
            subs.Event<HiveTeamRemoveMemberMsg>(OnRemoveMember);
        });
    }

    private void OnOpenUI(Entity<XenoComponent> queen, ref OpenHiveTeamsUIEvent args)
    {
        OpenForQueen(queen);
    }

    public void OpenForQueen(EntityUid queen)
    {
        _ui.OpenUi(queen, HiveTeamUIKey.Key, queen);
        RefreshState(queen);
    }

    private void RefreshState(EntityUid queen)
    {
        if (_hive.GetHive(queen) is not { } hive)
            return;

        var teamsComp = EnsureTeams(hive);
        var allXenos = BuildXenoList(queen, hive);
        var teamData = BuildTeamData(teamsComp, allXenos);

        _ui.SetUiState(queen, HiveTeamUIKey.Key, new HiveTeamBuiState(teamData, allXenos));
    }

    private void OnSetLeader(Entity<XenoComponent> queen, ref HiveTeamSetLeaderMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);

        // Remove this xeno from any existing leader slot
        foreach (var t in teams.Teams)
        {
            if (t.Leader == xeno)
            {
                _hiveLeader.RevokeLeader(t.Leader.Value);
                t.Leader = null;
            }
        }

        team.Leader = xeno;
        _hiveLeader.GrantLeader(queen, xeno.Value);
        RefreshState(queen);
    }

    private void OnRemoveLeader(Entity<XenoComponent> queen, ref HiveTeamRemoveLeaderMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);
        if (team.Leader != null)
            _hiveLeader.RevokeLeader(team.Leader.Value);
        team.Leader = null;
        RefreshState(queen);
    }

    private void OnAddMember(Entity<XenoComponent> queen, ref HiveTeamAddMemberMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        var teams = EnsureTeams(hive);

        // Remove from any existing member slot
        foreach (var t in teams.Teams)
            t.Members.Remove(xeno.Value);

        var team = GetOrCreateTeam(teams, args.TeamIndex);
        if (!team.Members.Contains(xeno.Value))
            team.Members.Add(xeno.Value);

        RefreshState(queen);
    }

    private void OnRemoveMember(Entity<XenoComponent> queen, ref HiveTeamRemoveMemberMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);
        team.Members.Remove(xeno.Value);
        RefreshState(queen);
    }

    private HiveTeamsComponent EnsureTeams(Entity<HiveComponent> hive)
    {
        var comp = EnsureComp<HiveTeamsComponent>(hive);
        while (comp.Teams.Count < HiveTeamsComponent.TeamCount)
            comp.Teams.Add(new HiveTeamEntry());
        return comp;
    }

    private static HiveTeamEntry GetOrCreateTeam(HiveTeamsComponent teams, int index)
    {
        while (teams.Teams.Count <= index)
            teams.Teams.Add(new HiveTeamEntry());
        return teams.Teams[index];
    }

    private List<Xeno> BuildXenoList(EntityUid queen, Entity<HiveComponent> hive)
    {
        var xenos = new List<Xeno>();
        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var meta))
        {
            if (uid == queen || member.Hive != hive.Owner)
                continue;
            if (_mobState.IsDead(uid))
                continue;
            xenos.Add(new Xeno(GetNetEntity(uid), Name(uid, meta), meta.EntityPrototype?.ID));
        }
        xenos.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return xenos;
    }

    private List<HiveTeamData> BuildTeamData(HiveTeamsComponent teams, List<Xeno> allXenos)
    {
        var netMap = new Dictionary<EntityUid, Xeno>();
        foreach (var x in allXenos)
        {
            if (TryGetEntity(x.Entity, out var uid))
                netMap[uid.Value] = x;
        }

        var result = new List<HiveTeamData>();
        for (var i = 0; i < HiveTeamsComponent.TeamCount; i++)
        {
            HiveTeamEntry? entry = i < teams.Teams.Count ? teams.Teams[i] : null;

            Xeno? leader = null;
            if (entry?.Leader != null && netMap.TryGetValue(entry.Leader.Value, out var lx))
                leader = lx;

            var members = new List<Xeno>();
            if (entry != null)
            {
                foreach (var m in entry.Members)
                {
                    if (netMap.TryGetValue(m, out var mx))
                        members.Add(mx);
                }
            }

            result.Add(new HiveTeamData(i, leader, members));
        }
        return result;
    }
}
