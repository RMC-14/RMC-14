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
using Content.Shared.Actions;
using Content.Shared.Body.Events;
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
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly HiveTrackerSystem _tracker = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier TeamAnnounceSound = new SoundCollectionSpecifier("XenoQueenCommand", AudioParams.Default.WithVolume(-6));
    private static readonly ResPath FireteamRsi = new("/Textures/_RMC14/Interface/fireteam.rsi");

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, OpenHiveTeamsUIEvent>(OnOpenUI);
        SubscribeLocalEvent<HiveLeaderComponent, HiveLeaderSquadActionEvent>(OnSquadAction);
        SubscribeLocalEvent<HiveTeamMemberComponent, HiveTeamMemberViewActionEvent>(OnMemberViewAction);
        SubscribeLocalEvent<HiveTeamMemberComponent, ComponentShutdown>(OnMemberShutdown);
        SubscribeLocalEvent<HiveMemberComponent, NewXenoEvolvedEvent>(OnXenoEvolved);
        SubscribeLocalEvent<HiveMemberComponent, XenoDevolvedEvent>(OnXenoDevolved);
        SubscribeLocalEvent<XenoComponent, HiveLeaderRemovedEvent>(OnLeaderRemoved);
        SubscribeLocalEvent<HiveTeamMemberComponent, MobStateChangedEvent>(OnMemberMobStateChanged);
        SubscribeLocalEvent<HiveTeamMemberComponent, BeingGibbedEvent>(OnMemberGibbed);

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
            subs.Event<HiveLeaderSquadAnnounceMsg>(OnSquadAnnounce);
            subs.Event<HiveLeaderAddMemberMsg>(OnLeaderAddMember);
            subs.Event<HiveLeaderRemoveMemberMsg>(OnLeaderRemoveMember);
        });
    }

    private void DirtyTeams(Entity<HiveComponent> hive)
    {
        if (!TryComp(hive.Owner, out HiveTeamsComponent? teams))
            return;

        Dirty(hive.Owner, teams);
        SendHiveTeamUIUpdates(hive, teams);
    }

    private static string GetRoleName(int role)
    {
        return role >= 0 && role < HiveTeamsComponent.RoleNames.Length
            ? HiveTeamsComponent.RoleNames[role]
            : "?";
    }

    private static List<HiveTeamEntryState> ToEntryStates(HiveTeamsComponent teams)
    {
        return teams.Teams.Select(t => new HiveTeamEntryState(t.Leader, t.Members.ToList(), t.Role)).ToList();
    }

    /// <summary>
    /// Returns the index of the team led by <paramref name="leader"/>, or -1 if they don't lead one.
    /// </summary>
    private static int FindLeaderTeam(HiveTeamsComponent teams, NetEntity leader)
    {
        for (var i = 0; i < teams.Teams.Count; i++)
        {
            if (teams.Teams[i].Leader == leader)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Builds a list of all living xenos in the hive for UI display.
    /// </summary>
    private List<HiveTeamXeno> BuildAllXenos(EntityUid hiveOwner)
    {
        var xenos = new List<HiveTeamXeno>();
        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var meta))
        {
            if (member.Hive != hiveOwner)
                continue;
            if (_mobState.IsDead(uid))
                continue;
            xenos.Add(new HiveTeamXeno(GetNetEntity(uid), Name(uid, meta), meta.EntityPrototype?.ID));
        }

        xenos.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return xenos;
    }

    /// <summary>
    /// Sends updated UI state to all open hive team UIs.
    /// </summary>
    private void SendHiveTeamUIUpdates(Entity<HiveComponent> hive, HiveTeamsComponent teams)
    {
        var allXenos = BuildAllXenos(hive.Owner);
        var teamStates = ToEntryStates(teams);

        // Update Queen's hive team UI
        var queenState = new HiveTeamBuiState(allXenos, teamStates);
        var queenQuery = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, HiveLeaderGranterComponent>();
        while (queenQuery.MoveNext(out var queenUid, out _, out var hiveMember, out _))
        {
            if (hiveMember.Hive != hive.Owner)
                continue;
            if (_ui.IsUiOpen(queenUid, HiveTeamUIKey.Key))
                _ui.SetUiState(queenUid, HiveTeamUIKey.Key, queenState);
        }

        // Update hive leader squad UIs
        for (var i = 0; i < teams.Teams.Count; i++)
        {
            var team = teams.Teams[i];
            if (team.Leader == null || !TryGetEntity(team.Leader.Value, out var leaderUid))
                continue;

            if (!_ui.IsUiOpen(leaderUid.Value, HiveLeaderSquadUIKey.Key))
                continue;

            var leaderState = new HiveLeaderSquadBuiState(allXenos, teamStates, teamStates[i], i, GetRoleName(team.Role));
            _ui.SetUiState(leaderUid.Value, HiveLeaderSquadUIKey.Key, leaderState);
        }

        // Update member view UIs
        var memberQuery = EntityQueryEnumerator<HiveTeamMemberComponent, HiveMemberComponent>();
        while (memberQuery.MoveNext(out var memberUid, out var teamMember, out var hiveMember))
        {
            if (hiveMember.Hive != hive.Owner)
                continue;
            if (!_ui.IsUiOpen(memberUid, HiveTeamMemberUIKey.Key))
                continue;

            var teamIndex = teamMember.TeamNumber - 1;
            if (teamIndex < 0 || teamIndex >= teams.Teams.Count)
                continue;

            var team = teams.Teams[teamIndex];
            var memberState = BuildMemberViewState(allXenos, team, teamMember.TeamNumber);
            _ui.SetUiState(memberUid, HiveTeamMemberUIKey.Key, memberState);
        }
    }

    private static HiveTeamMemberBuiState BuildMemberViewState(List<HiveTeamXeno> allXenos, HiveTeamEntry team, int teamNumber)
    {
        var byEntity = allXenos.ToDictionary(x => x.Entity);

        HiveTeamXeno? leader = null;
        if (team.Leader is { } leaderNet && byEntity.TryGetValue(leaderNet, out var leaderXeno))
            leader = leaderXeno;

        var members = new List<HiveTeamXeno>();
        foreach (var memberNet in team.Members)
        {
            if (byEntity.TryGetValue(memberNet, out var memberXeno))
                members.Add(memberXeno);
        }

        return new HiveTeamMemberBuiState(members, leader, teamNumber, GetRoleName(team.Role));
    }

    /// <summary>
    /// Syncs <see cref="HiveTeamMemberComponent"/> with the team data. Leaders and members both get the component
    /// for the HUD icon, but only members get the view action, since leaders use the hive leader squad action.
    /// Existing components are updated in place so the view action isn't recreated on every change.
    /// </summary>
    private void RefreshTeamMemberComponents(HiveTeamsComponent teams)
    {
        var assignments = new Dictionary<EntityUid, (int Number, bool IsLeader)>();
        for (var i = 0; i < teams.Teams.Count; i++)
        {
            var team = teams.Teams[i];
            var number = i + 1;

            if (team.Leader != null && TryGetEntity(team.Leader.Value, out var leaderUid) && !TerminatingOrDeleted(leaderUid))
                assignments[leaderUid.Value] = (number, true);

            foreach (var memberNet in team.Members)
            {
                if (TryGetEntity(memberNet, out var memberUid) && !TerminatingOrDeleted(memberUid))
                    assignments[memberUid.Value] = (number, false);
            }
        }

        // Anyone no longer on a team loses the component; OnMemberShutdown cleans up their action and UI
        var query = EntityQueryEnumerator<HiveTeamMemberComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!assignments.ContainsKey(uid))
                RemCompDeferred<HiveTeamMemberComponent>(uid);
        }

        foreach (var (uid, (number, isLeader)) in assignments)
        {
            var comp = EnsureComp<HiveTeamMemberComponent>(uid);
            comp.TeamNumber = number;
            comp.Icon = new SpriteSpecifier.Rsi(FireteamRsi, $"hudsquad_ft{number}");

            if (isLeader)
                RemoveViewAction((uid, comp));
            else if (comp.ViewAction == null)
                _actions.AddAction(uid, ref comp.ViewAction, comp.ViewActionId);

            Dirty(uid, comp);
        }
    }

    private void RemoveViewAction(Entity<HiveTeamMemberComponent> ent)
    {
        _ui.CloseUi(ent.Owner, HiveTeamMemberUIKey.Key);

        if (ent.Comp.ViewAction is not { } action)
            return;

        _actions.RemoveAction(ent.Owner, action);
        QueueDel(action);
        ent.Comp.ViewAction = null;
    }

    private void OnMemberShutdown(Entity<HiveTeamMemberComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemoveViewAction(ent);
    }

    private void OnOpenUI(Entity<XenoComponent> queen, ref OpenHiveTeamsUIEvent args)
    {
        OpenForQueen(queen);
    }

    public void OpenForQueen(EntityUid queen)
    {
        if (_hive.GetHive(queen) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        _ui.OpenUi(queen, HiveTeamUIKey.Key, queen);
        _ui.SetUiState(queen, HiveTeamUIKey.Key, new HiveTeamBuiState(BuildAllXenos(hive.Owner), ToEntryStates(teams)));
    }

    private void RemoveMemberFromTeams(EntityUid uid, Entity<HiveComponent> hive, HiveTeamsComponent teams)
    {
        var netEnt = GetNetEntity(uid);
        foreach (var team in teams.Teams)
        {
            team.Members.Remove(netEnt);
            if (team.Leader == netEnt)
                team.Leader = null;
        }

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void OnMemberMobStateChanged(Entity<HiveTeamMemberComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        if (!TryComp(hive.Owner, out HiveTeamsComponent? teams))
            return;

        RemoveMemberFromTeams(ent.Owner, hive, teams);
    }

    private void OnMemberGibbed(Entity<HiveTeamMemberComponent> ent, ref BeingGibbedEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        if (!TryComp(hive.Owner, out HiveTeamsComponent? teams))
            return;

        RemoveMemberFromTeams(ent.Owner, hive, teams);
    }

    private void OnLeaderRemoved(Entity<XenoComponent> xeno, ref HiveLeaderRemovedEvent args)
    {
        // Skip if the entity is terminating due to evolution — OnXenoEvolved will swap the UID
        if (TerminatingOrDeleted(args.Leader))
            return;

        if (_hive.GetHive(xeno.Owner) is not { } hive)
            return;

        if (!TryComp(hive.Owner, out HiveTeamsComponent? teams))
            return;

        var netLeader = GetNetEntity(args.Leader);
        var changed = false;
        foreach (var team in teams.Teams)
        {
            if (team.Leader != netLeader)
                continue;

            team.Leader = null;
            changed = true;
        }

        if (!changed)
            return;

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void OnXenoEvolved(Entity<HiveMemberComponent> xeno, ref NewXenoEvolvedEvent args)
    {
        var hive = _hive.GetHive(args.NewXeno) ?? _hive.GetHive(xeno.Owner);
        if (hive == null)
            return;
        if (!TryComp(hive.Value.Owner, out HiveTeamsComponent? teams))
            return;
        UpdateTeamNetEntities(teams, args.OldXeno.Owner, args.NewXeno);
        RefreshTeamMemberComponents(teams);

        // Reopen the hive leader squad UI on the new entity if it was open on the old one
        if (_ui.IsUiOpen(args.OldXeno.Owner, HiveLeaderSquadUIKey.Key) &&
            TryComp(args.NewXeno, out ActorComponent? actor))
        {
            _ui.CloseUi(args.OldXeno.Owner, HiveLeaderSquadUIKey.Key);
            _ui.OpenUi(args.NewXeno, HiveLeaderSquadUIKey.Key, actor.PlayerSession);
        }

        DirtyTeams(hive.Value);
    }

    private void OnXenoDevolved(Entity<HiveMemberComponent> xeno, ref XenoDevolvedEvent args)
    {
        var hive = _hive.GetHive(args.NewXeno) ?? _hive.GetHive(xeno.Owner);
        if (hive == null)
            return;
        if (!TryComp(hive.Value.Owner, out HiveTeamsComponent? teams))
            return;
        UpdateTeamNetEntities(teams, args.OldXeno, args.NewXeno);
        RefreshTeamMemberComponents(teams);

        // Reopen the hive leader squad UI on the new entity if it was open on the old one
        if (_ui.IsUiOpen(args.OldXeno, HiveLeaderSquadUIKey.Key) &&
            TryComp(args.NewXeno, out ActorComponent? actor))
        {
            _ui.CloseUi(args.OldXeno, HiveLeaderSquadUIKey.Key);
            _ui.OpenUi(args.NewXeno, HiveLeaderSquadUIKey.Key, actor.PlayerSession);
        }

        DirtyTeams(hive.Value);
    }

    private void UpdateTeamNetEntities(HiveTeamsComponent teams, EntityUid oldUid, EntityUid newUid)
    {
        var oldNet = GetNetEntity(oldUid);
        var newNet = GetNetEntity(newUid);
        foreach (var team in teams.Teams)
        {
            if (team.Leader == oldNet)
                team.Leader = newNet;

            for (var i = 0; i < team.Members.Count; i++)
            {
                if (team.Members[i] == oldNet)
                    team.Members[i] = newNet;
            }
        }
    }

    private void OnSquadAction(Entity<HiveLeaderComponent> leader, ref HiveLeaderSquadActionEvent args)
    {
        if (_hive.GetHive(leader.Owner) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        var index = FindLeaderTeam(teams, GetNetEntity(leader.Owner));
        if (index < 0)
            return;

        _ui.OpenUi(leader.Owner, HiveLeaderSquadUIKey.Key, leader.Owner);

        var teamStates = ToEntryStates(teams);
        var state = new HiveLeaderSquadBuiState(BuildAllXenos(hive.Owner), teamStates, teamStates[index], index, GetRoleName(teams.Teams[index].Role));
        _ui.SetUiState(leader.Owner, HiveLeaderSquadUIKey.Key, state);
    }

    private void OnMemberViewAction(Entity<HiveTeamMemberComponent> member, ref HiveTeamMemberViewActionEvent args)
    {
        if (_hive.GetHive(member.Owner) is not { } hive)
            return;

        if (!TryComp(hive.Owner, out HiveTeamsComponent? teams))
            return;

        var teamIndex = member.Comp.TeamNumber - 1;
        if (teamIndex < 0 || teamIndex >= teams.Teams.Count)
            return;

        var team = teams.Teams[teamIndex];
        var allXenos = BuildAllXenos(hive.Owner);
        var memberState = BuildMemberViewState(allXenos, team, member.Comp.TeamNumber);

        _ui.OpenUi(member.Owner, HiveTeamMemberUIKey.Key, member.Owner);
        _ui.SetUiState(member.Owner, HiveTeamMemberUIKey.Key, memberState);
    }

    private void OnSquadAnnounce(Entity<XenoComponent> xeno, ref HiveLeaderSquadAnnounceMsg args)
    {
        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (_hive.GetHive(xeno.Owner) is not { } hive)
            return;

        var teamsComp = EnsureTeams(hive);
        var index = FindLeaderTeam(teamsComp, GetNetEntity(xeno.Owner));
        if (index < 0)
            return;

        var myEntry = teamsComp.Teams[index];

        var recipients = new HashSet<EntityUid> { xeno.Owner };
        foreach (var memberNet in myEntry.Members)
        {
            if (TryGetEntity(memberNet, out var memberUid))
                recipients.Add(memberUid.Value);
        }

        var queenQuery = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, HiveLeaderGranterComponent>();
        while (queenQuery.MoveNext(out var queenUid, out _, out var hiveMember, out _))
        {
            if (hiveMember.Hive == hive.Owner)
                recipients.Add(queenUid);
        }

        var message = $"{Name(xeno.Owner)} [{GetRoleName(myEntry.Role)}]: {args.Message}";
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
        var netXeno = GetNetEntity(xeno.Value);

        var index = FindLeaderTeam(teams, GetNetEntity(leaderUid));
        if (index < 0)
            return;

        var myEntry = teams.Teams[index];

        if (teams.Teams.Any(t => t.Leader == netXeno))
            return;

        foreach (var t in teams.Teams)
            t.Members.Remove(netXeno);

        if (!myEntry.Members.Contains(netXeno))
            myEntry.Members.Add(netXeno);

        if (myEntry.Leader != null && TryGetEntity(myEntry.Leader.Value, out var leaderEnt))
            _tracker.SetTrackerTarget(xeno.Value, leaderEnt.Value, "HiveLeader");

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void RemoveMemberFromLeaderTeam(EntityUid leaderUid, NetEntity xenoNet)
    {
        if (_hive.GetHive(leaderUid) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        foreach (var t in teams.Teams)
            t.Members.Remove(xenoNet);

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void OnLeaderAddMember(Entity<XenoComponent> leader, ref HiveLeaderAddMemberMsg args)
    {
        AddMemberToLeaderTeam(leader.Owner, args.Xeno);
    }

    private void OnLeaderRemoveMember(Entity<XenoComponent> leader, ref HiveLeaderRemoveMemberMsg args)
    {
        RemoveMemberFromLeaderTeam(leader.Owner, args.Xeno);
    }

    private void OnSetLeader(Entity<XenoComponent> queen, ref HiveTeamSetLeaderMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);
        var netXeno = GetNetEntity(xeno.Value);

        foreach (var t in teams.Teams)
        {
            if (t.Leader == netXeno && TryGetEntity(t.Leader.Value, out var oldLeader))
            {
                _hiveLeader.RevokeLeader(oldLeader.Value);
                t.Leader = null;
            }
        }

        foreach (var t in teams.Teams)
            t.Members.Remove(netXeno);

        team.Leader = netXeno;
        _hiveLeader.GrantLeader(queen, xeno.Value);

        foreach (var memberNet in team.Members)
        {
            if (TryGetEntity(memberNet, out var memberUid))
                _tracker.SetTrackerTarget(memberUid.Value, xeno.Value, "HiveLeader");
        }

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void OnRemoveLeader(Entity<XenoComponent> queen, ref HiveTeamRemoveLeaderMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);
        if (team.Leader != null && TryGetEntity(team.Leader.Value, out var leaderUid))
            _hiveLeader.RevokeLeader(leaderUid.Value);
        team.Leader = null;

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void OnAddMember(Entity<XenoComponent> queen, ref HiveTeamAddMemberMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        var teams = EnsureTeams(hive);
        var netXeno = GetNetEntity(xeno.Value);

        if (teams.Teams.Any(t => t.Leader == netXeno))
            return;

        foreach (var t in teams.Teams)
            t.Members.Remove(netXeno);

        var team = GetOrCreateTeam(teams, args.TeamIndex);
        if (!team.Members.Contains(netXeno))
            team.Members.Add(netXeno);

        if (team.Leader != null && TryGetEntity(team.Leader.Value, out var leaderUid))
            _tracker.SetTrackerTarget(xeno.Value, leaderUid.Value, "HiveLeader");

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void OnRemoveMember(Entity<XenoComponent> queen, ref HiveTeamRemoveMemberMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);
        team.Members.Remove(args.Xeno);

        RefreshTeamMemberComponents(teams);
        DirtyTeams(hive);
    }

    private void OnSetRole(Entity<XenoComponent> queen, ref HiveTeamSetRoleMsg args)
    {
        if (_hive.GetHive(queen.Owner) is not { } hive)
            return;

        var teams = EnsureTeams(hive);
        var team = GetOrCreateTeam(teams, args.TeamIndex);
        team.Role = Math.Clamp(args.Role, 0, HiveTeamsComponent.RoleNames.Length - 1);
        DirtyTeams(hive);
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
}
