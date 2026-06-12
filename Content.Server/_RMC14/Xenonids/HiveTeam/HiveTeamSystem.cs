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
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly HiveTrackerSystem _tracker = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier TeamAnnounceSound = new SoundCollectionSpecifier("XenoQueenCommand", AudioParams.Default.WithVolume(-6));

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, OpenHiveTeamsUIEvent>(OnOpenUI);
        SubscribeLocalEvent<HiveLeaderComponent, HiveLeaderSquadActionEvent>(OnSquadAction);
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
        if (TryComp(hive.Owner, out HiveTeamsComponent? teams))
            Dirty(hive.Owner, teams);
    }

    private void RefreshTeamMemberComponents(HiveTeamsComponent teams)
    {
        var query = EntityQueryEnumerator<HiveTeamMemberComponent>();
        while (query.MoveNext(out var uid, out _))
            RemCompDeferred<HiveTeamMemberComponent>(uid);

        for (var i = 0; i < teams.Teams.Count; i++)
        {
            var team = teams.Teams[i];
            var number = i + 1;

            if (team.Leader != null && TryGetEntity(team.Leader.Value, out var leaderUid) && !TerminatingOrDeleted(leaderUid))
            {
                var comp = EnsureComp<HiveTeamMemberComponent>(leaderUid.Value);
                comp.TeamNumber = number;
                comp.Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Interface/marine_hud.rsi"), $"hudsquad_ft{number}");
                Dirty(leaderUid.Value, comp);
            }

            foreach (var memberNet in team.Members)
            {
                if (!TryGetEntity(memberNet, out var memberUid) || TerminatingOrDeleted(memberUid.Value))
                    continue;
                var comp = EnsureComp<HiveTeamMemberComponent>(memberUid.Value);
                comp.TeamNumber = number;
                comp.Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Interface/marine_hud.rsi"), $"hudsquad_ft{number}");
                Dirty(memberUid.Value, comp);
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
        if (_hive.GetHive(queen) is { } hive)
        {
            EnsureTeams(hive);
            DirtyTeams(hive);
        }
    }

    private void RemoveMemberFromTeams(EntityUid uid, Entity<HiveComponent> hive, HiveTeamsComponent teams)
    {
        var netEnt = GetNetEntity(uid);
        foreach (var team in teams.Teams)
        {
            team.Members.Remove(netEnt);
            if (team.Leader == netEnt)
            {
                team.Leader = null;
                RemCompDeferred<HiveTeamMemberComponent>(uid);
            }
        }
        RemCompDeferred<HiveTeamMemberComponent>(uid);
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
        foreach (var team in teams.Teams)
        {
            if (team.Leader == netLeader)
            {
                RemCompDeferred<HiveTeamMemberComponent>(args.Leader);
                team.Leader = null;
            }
        }

        Dirty(hive.Owner, teams);
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
        Dirty(hive.Value.Owner, teams);

        // Reopen the hive leader squad UI on the new entity if it was open on the old one
        if (_ui.IsUiOpen(args.OldXeno.Owner, HiveLeaderSquadUIKey.Key) &&
            TryComp(args.NewXeno, out ActorComponent? actor))
        {
            _ui.CloseUi(args.OldXeno.Owner, HiveLeaderSquadUIKey.Key);
            _ui.OpenUi(args.NewXeno, HiveLeaderSquadUIKey.Key, actor.PlayerSession);
        }
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
        Dirty(hive.Value.Owner, teams);

        // Reopen the hive leader squad UI on the new entity if it was open on the old one
        if (_ui.IsUiOpen(args.OldXeno, HiveLeaderSquadUIKey.Key) &&
            TryComp(args.NewXeno, out ActorComponent? actor))
        {
            _ui.CloseUi(args.OldXeno, HiveLeaderSquadUIKey.Key);
            _ui.OpenUi(args.NewXeno, HiveLeaderSquadUIKey.Key, actor.PlayerSession);
        }
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
        _ui.OpenUi(leader.Owner, HiveLeaderSquadUIKey.Key, leader.Owner);
        if (_hive.GetHive(leader.Owner) is { } hive)
        {
            EnsureTeams(hive);
            DirtyTeams(hive);
        }
    }

    private void OnSquadAnnounce(Entity<XenoComponent> xeno, ref HiveLeaderSquadAnnounceMsg args)
    {
        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (_hive.GetHive(xeno.Owner) is not { } hive)
            return;

        var teamsComp = EnsureTeams(hive);
        var netLeader = GetNetEntity(xeno.Owner);

        HiveTeamEntry? myEntry = null;
        foreach (var entry in teamsComp.Teams)
        {
            if (entry.Leader == netLeader)
            {
                myEntry = entry;
                break;
            }
        }

        if (myEntry == null)
            return;

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
        var netLeader = GetNetEntity(leaderUid);
        var netXeno = GetNetEntity(xeno.Value);

        HiveTeamEntry? myEntry = null;
        for (var i = 0; i < teams.Teams.Count; i++)
        {
            if (teams.Teams[i].Leader == netLeader)
            {
                myEntry = teams.Teams[i];
                break;
            }
        }

        if (myEntry == null)
            return;

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

        if (TryGetEntity(xenoNet, out var xeno))
            RemCompDeferred<HiveTeamMemberComponent>(xeno.Value);

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

        if (TryGetEntity(args.Xeno, out var xeno))
            RemCompDeferred<HiveTeamMemberComponent>(xeno.Value);

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
