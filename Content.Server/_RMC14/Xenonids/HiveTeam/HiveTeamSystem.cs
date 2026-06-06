using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.Tracker.Xeno;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.HiveTeam;
using Content.Shared._RMC14.Xenonids.ManageHive;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Xenonids.HiveTeam;

public sealed class HiveTeamSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly HiveTrackerSystem _tracker = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, OpenHiveTeamsUIEvent>(OnOpenUI);
        SubscribeLocalEvent<HiveLeaderComponent, HiveLeaderSquadActionEvent>(OnSquadAction);
        SubscribeLocalEvent<HiveTeamsComponent, NewXenoEvolvedEvent>(OnXenoEvolved);
        SubscribeLocalEvent<HiveTeamsComponent, XenoDevolvedEvent>(OnXenoDevolved);
        SubscribeLocalEvent<XenoComponent, HiveLeaderRemovedEvent>(OnLeaderRemoved);
        SubscribeLocalEvent<HiveTeamMemberComponent, MobStateChangedEvent>(OnMemberMobStateChanged);

        Subs.BuiEvents<XenoComponent>(HiveTeamUIKey.Key, subs =>
        {
            subs.Event<HiveTeamSetLeaderMsg>(OnSetLeader);
            subs.Event<HiveTeamRemoveLeaderMsg>(OnRemoveLeader);
            subs.Event<HiveTeamAddMemberMsg>(OnAddMember);
            subs.Event<HiveTeamRemoveMemberMsg>(OnRemoveMember);
            subs.Event<HiveTeamSetRoleMsg>(OnSetRole);
        });

        Subs.BuiEvents<XenoComponent>(HiveLeaderSquadUIKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnSquadBuiOpened);
            subs.Event<HiveLeaderSquadAnnounceMsg>(OnSquadAnnounce);
            subs.Event<HiveLeaderAddMemberMsg>(OnLeaderAddMember);
            subs.Event<HiveLeaderRemoveMemberMsg>(OnLeaderRemoveMember);
        });
    }

    private void RefreshTeamMemberComponents(HiveTeamsComponent teams)
    {
        // Clear all existing team member components first
        var query = EntityQueryEnumerator<HiveTeamMemberComponent>();
        while (query.MoveNext(out var uid, out _))
            RemCompDeferred<HiveTeamMemberComponent>(uid);

        for (var i = 0; i < teams.Teams.Count; i++)
        {
            var team = teams.Teams[i];
            var number = i + 1;

            if (team.Leader != null && !TerminatingOrDeleted(team.Leader.Value))
            {
                var comp = EnsureComp<HiveTeamMemberComponent>(team.Leader.Value);
                comp.TeamNumber = number;
                Dirty(team.Leader.Value, comp);
            }

            foreach (var member in team.Members)
            {
                if (TerminatingOrDeleted(member))
                    continue;
                var comp = EnsureComp<HiveTeamMemberComponent>(member);
                comp.TeamNumber = number;
                Dirty(member, comp);
            }
        }
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
        RefreshTeamMemberComponents(teamsComp);
    }

    private void OnMemberMobStateChanged(Entity<HiveTeamMemberComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        if (!TryComp(hive.Owner, out HiveTeamsComponent? teams))
            return;

        foreach (var team in teams.Teams)
            team.Members.Remove(ent.Owner);

        RemCompDeferred<HiveTeamMemberComponent>(ent.Owner);
        RefreshTeamMemberComponents(teams);
        RefreshAllQueens(hive);

        // Refresh any open leader squad windows in this hive
        var leaderQuery = EntityQueryEnumerator<HiveLeaderComponent, HiveMemberComponent>();
        while (leaderQuery.MoveNext(out var leaderUid, out _, out var member))
        {
            if (member.Hive != hive.Owner)
                continue;
            if (_ui.IsUiOpen(leaderUid, HiveLeaderSquadUIKey.Key))
                RefreshSquadState(leaderUid);
        }
    }

    private void OnLeaderRemoved(Entity<XenoComponent> xeno, ref HiveLeaderRemovedEvent args)
    {
        if (_hive.GetHive(xeno.Owner) is not { } hive)
            return;

        if (!TryComp(hive.Owner, out HiveTeamsComponent? teams))
            return;

        foreach (var team in teams.Teams)
        {
            if (team.Leader == args.Leader)
            {
                RemCompDeferred<HiveTeamMemberComponent>(args.Leader);
                team.Leader = null;
            }
        }
    }

    // --- Evolve/Devolve: update stored EntityUids in team entries ---

    private void OnXenoEvolved(Entity<HiveTeamsComponent> hive, ref NewXenoEvolvedEvent args)
    {
        UpdateTeamUids(hive.Comp, args.OldXeno, args.NewXeno);
    }

    private void OnXenoDevolved(Entity<HiveTeamsComponent> hive, ref XenoDevolvedEvent args)
    {
        UpdateTeamUids(hive.Comp, args.OldXeno, args.NewXeno);
    }

    private void UpdateTeamUids(HiveTeamsComponent teams, EntityUid oldUid, EntityUid newUid)
    {
        foreach (var team in teams.Teams)
        {
            if (team.Leader == oldUid)
                team.Leader = newUid;

            for (var i = 0; i < team.Members.Count; i++)
            {
                if (team.Members[i] == oldUid)
                    team.Members[i] = newUid;
            }
        }
    }

    // --- Squad window ---

    private void OnSquadAction(Entity<HiveLeaderComponent> leader, ref HiveLeaderSquadActionEvent args)
    {
        _ui.OpenUi(leader.Owner, HiveLeaderSquadUIKey.Key, leader.Owner);
        RefreshSquadState(leader.Owner);
    }

    private void OnSquadBuiOpened(Entity<XenoComponent> xeno, ref BoundUIOpenedEvent args)
    {
        RefreshSquadState(xeno.Owner);
    }

    private void RefreshSquadState(EntityUid leader)
    {
        if (_hive.GetHive(leader) is not { } hive)
            return;

        var teamsComp = EnsureTeams(hive);
        var allXenos = BuildXenoListFull(hive);
        var netMap = new Dictionary<EntityUid, Xeno>();
        foreach (var x in allXenos)
        {
            if (TryGetEntity(x.Entity, out var uid))
                netMap[uid.Value] = x;
        }

        HiveTeamEntry? myEntry = null;
        var myIndex = 0;
        for (var i = 0; i < teamsComp.Teams.Count; i++)
        {
            if (teamsComp.Teams[i].Leader == leader)
            {
                myEntry = teamsComp.Teams[i];
                myIndex = i;
                break;
            }
        }

        if (myEntry == null)
            return;

        Xeno? leaderXeno = netMap.TryGetValue(leader, out var lx) ? lx : null;
        var members = new List<Xeno>();
        foreach (var m in myEntry.Members)
        {
            if (netMap.TryGetValue(m, out var mx))
                members.Add(mx);
        }

        var roleName = myEntry.Role >= 0 && myEntry.Role < HiveTeamsComponent.RoleNames.Length
            ? HiveTeamsComponent.RoleNames[myEntry.Role]
            : "?";
        var teamData = new HiveTeamData(myIndex, leaderXeno, members, myEntry.Role);
        _ui.SetUiState(leader, HiveLeaderSquadUIKey.Key, new HiveLeaderSquadBuiState(teamData, roleName, allXenos));
    }

    private List<Xeno> BuildXenoListFull(Entity<HiveComponent> hive)
    {
        var xenos = new List<Xeno>();
        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var meta))
        {
            if (member.Hive != hive.Owner)
                continue;
            if (_mobState.IsDead(uid))
                continue;
            xenos.Add(new Xeno(GetNetEntity(uid), Name(uid, meta), meta.EntityPrototype?.ID));
        }
        return xenos;
    }

    private static readonly SoundSpecifier TeamAnnounceSound = new SoundCollectionSpecifier("XenoQueenCommand", AudioParams.Default.WithVolume(-6));
    private void OnSquadAnnounce(Entity<XenoComponent> xeno, ref HiveLeaderSquadAnnounceMsg args)
    {
        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (_hive.GetHive(xeno.Owner) is not { } hive)
            return;

        var teamsComp = EnsureTeams(hive);

        HiveTeamEntry? myEntry = null;
        foreach (var entry in teamsComp.Teams)
        {
            if (entry.Leader == xeno.Owner)
            {
                myEntry = entry;
                break;
            }
        }

        if (myEntry == null)
            return;

        var recipients = new HashSet<EntityUid> { xeno.Owner };
        foreach (var member in myEntry.Members)
            recipients.Add(member);

        // Also include the hive queen
        if (_hive.GetHive(xeno.Owner) is { } hiveForQueen)
        {
            var queenQuery = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, HiveLeaderGranterComponent>();
            while (queenQuery.MoveNext(out var queenUid, out _, out var hiveMember, out _))
            {
                if (hiveMember.Hive == hiveForQueen.Owner)
                    recipients.Add(queenUid);
            }
        }

        var roleName = myEntry.Role >= 0 && myEntry.Role < HiveTeamsComponent.RoleNames.Length
            ? HiveTeamsComponent.RoleNames[myEntry.Role]
            : "?";
        var message = $"{Name(xeno.Owner)} [{roleName}]: {args.Message}";
        var escapedMessage = FormattedMessage.EscapeText(message);
        var wrapped = $"[color=#FFFFFF][font size=16][bold]{escapedMessage}[/bold][/font][/color]";

        var filter = Filter.Empty();
        foreach (var recipient in recipients)
        {
            if (TryComp(recipient, out ActorComponent? actor))
                filter.AddPlayer(actor.PlayerSession);
        }

        _adminLog.Add(LogType.RMCXenoAnnounce, $"{ToPrettyString(xeno.Owner):source} hive team announced: {args.Message}");
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrapped, default, false, true, null);
        _audio.PlayGlobal(TeamAnnounceSound, filter, true, AudioParams.Default.WithVolume(-6f));
    }

    private void AddMemberToLeaderTeam(EntityUid leaderUid, NetEntity xenoNet)
    {
        if (_hive.GetHive(leaderUid) is not { } hive)
            return;

        if (!TryGetEntity(xenoNet, out var xeno))
            return;

        var teams = EnsureTeams(hive);

        HiveTeamEntry? myEntry = null;
        for (var i = 0; i < teams.Teams.Count; i++)
        {
            if (teams.Teams[i].Leader == leaderUid)
            {
                myEntry = teams.Teams[i];
                break;
            }
        }

        if (myEntry == null)
            return;

        if (teams.Teams.Any(t => t.Leader == xeno.Value))
            return;

        foreach (var t in teams.Teams)
            t.Members.Remove(xeno.Value);

        if (!myEntry.Members.Contains(xeno.Value))
            myEntry.Members.Add(xeno.Value);

        if (myEntry.Leader != null)
            _tracker.SetTrackerTarget(xeno.Value, myEntry.Leader, "HiveLeader");

        RefreshTeamMemberComponents(teams);
        RefreshSquadState(leaderUid);
        RefreshAllQueens(hive);
    }

    private void RemoveMemberFromLeaderTeam(EntityUid leaderUid, NetEntity xenoNet)
    {
        if (_hive.GetHive(leaderUid) is not { } hive)
            return;

        if (!TryGetEntity(xenoNet, out var xeno))
            return;

        var teams = EnsureTeams(hive);
        foreach (var t in teams.Teams)
            t.Members.Remove(xeno.Value);

        RefreshTeamMemberComponents(teams);
        RefreshSquadState(leaderUid);
        RefreshAllQueens(hive);
    }

    private void RefreshAllQueens(Entity<HiveComponent> hive)
    {
        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
        while (query.MoveNext(out var uid, out _, out var member))
        {
            if (member.Hive != hive.Owner)
                continue;
            if (!_ui.IsUiOpen(uid, HiveTeamUIKey.Key))
                continue;
            RefreshState(uid);
        }
    }

    // --- Queen UI message handlers ---

    private void OnSetLeader(Entity<XenoComponent> queen, ref HiveTeamSetLeaderMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);

        // Revoke leadership if already a leader of another team
        foreach (var t in teams.Teams)
        {
            if (t.Leader == xeno)
            {
                _hiveLeader.RevokeLeader(t.Leader.Value);
                t.Leader = null;
            }
        }

        // Remove from any team's member list
        foreach (var t in teams.Teams)
            t.Members.Remove(xeno.Value);

        team.Leader = xeno;
        _hiveLeader.GrantLeader(queen, xeno.Value);

        foreach (var member in team.Members)
            _tracker.SetTrackerTarget(member, xeno.Value, "HiveLeader");

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

    private void OnLeaderAddMember(Entity<XenoComponent> leader, ref HiveLeaderAddMemberMsg args)
    {
        AddMemberToLeaderTeam(leader.Owner, args.Xeno);
    }

    private void OnLeaderRemoveMember(Entity<XenoComponent> leader, ref HiveLeaderRemoveMemberMsg args)
    {
        RemoveMemberFromLeaderTeam(leader.Owner, args.Xeno);
    }

    private void OnAddMember(Entity<XenoComponent> queen, ref HiveTeamAddMemberMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        var teams = EnsureTeams(hive);

        if (teams.Teams.Any(t => t.Leader == xeno.Value))
            return;

        foreach (var t in teams.Teams)
            t.Members.Remove(xeno.Value);

        var team = GetOrCreateTeam(teams, args.TeamIndex);
        if (!team.Members.Contains(xeno.Value))
            team.Members.Add(xeno.Value);

        if (team.Leader != null)
            _tracker.SetTrackerTarget(xeno.Value, team.Leader, "HiveLeader");

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

    private void OnSetRole(Entity<XenoComponent> queen, ref HiveTeamSetRoleMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);
        team.Role = Math.Clamp(args.Role, 0, HiveTeamsComponent.RoleNames.Length - 1);
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

            result.Add(new HiveTeamData(i, leader, members, entry?.Role ?? 0));
        }
        return result;
    }
}
