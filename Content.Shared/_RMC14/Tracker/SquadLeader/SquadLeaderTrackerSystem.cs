using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Roles;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

public sealed class SquadLeaderTrackerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TrackerSystem _tracker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<EntityUid, MapCoordinates> _squadLeaders = new();
    private readonly Dictionary<EntityUid, MapCoordinates>?[] _fireteamLeaders =
        new Dictionary<EntityUid, MapCoordinates>?[3];

    private EntityQuery<FireteamLeaderComponent> _fireteamLeaderQuery;
    private EntityQuery<FireteamMemberComponent> _fireteamMemberQuery;
    private EntityQuery<OriginalRoleComponent> _originalRoleQuery;
    private EntityQuery<SquadLeaderTrackerComponent> _squadLeaderTrackerQuery;
    private EntityQuery<SquadMemberComponent> _squadMemberQuery;

    private const string SquadLeader = "CMSquadLeader";
    private const string FireteamLeader = "CMFireteamLeader";
    private const string SquadTrackerCategory = "SquadTracker";

    public override void Initialize()
    {
        _fireteamLeaderQuery = GetEntityQuery<FireteamLeaderComponent>();
        _fireteamMemberQuery = GetEntityQuery<FireteamMemberComponent>();
        _originalRoleQuery = GetEntityQuery<OriginalRoleComponent>();
        _squadLeaderTrackerQuery = GetEntityQuery<SquadLeaderTrackerComponent>();
        _squadMemberQuery = GetEntityQuery<SquadMemberComponent>();

        SubscribeLocalEvent<SquadMemberAddedEvent>(OnSquadMemberAdded);
        SubscribeLocalEvent<SquadMemberRemovedEvent>(OnSquadMemberRemoved);

        SubscribeLocalEvent<GrantSquadLeaderTrackerComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantSquadLeaderTrackerComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<SquadLeaderTrackerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SquadLeaderTrackerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<SquadLeaderTrackerComponent, SquadLeaderTrackerClickedEvent>(OnSquadLeaderTrackerClicked);
        SubscribeLocalEvent<SquadLeaderTrackerComponent, SquadLeaderTrackerChangeModeEvent>(OnSquadLeaderTrackerChangeMode);
        SubscribeLocalEvent<SquadLeaderTrackerComponent, LeaderTrackerSelectTargetEvent>(OnLeaderTrackerSelectTargetEvent);

        Subs.BuiEvents<SquadLeaderTrackerComponent>(SquadLeaderTrackerUI.Key,
            subs =>
            {
                subs.Event<SquadLeaderTrackerAssignFireteamMsg>(OnAssignFireteamMsg);
                subs.Event<SquadLeaderTrackerUnassignFireteamMsg>(OnUnassignFireteamMsg);
                subs.Event<SquadLeaderTrackerPromoteFireteamLeaderMsg>(OnPromoteFireteamLeaderMsg);
                subs.Event<SquadLeaderTrackerDemoteFireteamLeaderMsg>(OnDemoteFireteamLeaderMsg);
                subs.Event<SquadLeaderTrackerChangeTrackedMsg>(OnChangeTrackedMsg);
            });
    }

    private void OnSquadMemberAdded(ref SquadMemberAddedEvent ev)
    {
        AddFireteamMember(ev.Squad.Comp.Fireteams, ev.Member);

        if (_squad.TryGetSquadLeader(ev.Squad, out var leader))
        {
            ev.Squad.Comp.Fireteams.SquadLeader = Name(leader);
            ev.Squad.Comp.Fireteams.SquadLeaderId = GetNetEntity(leader);
        }
        else
        {
            ev.Squad.Comp.Fireteams.SquadLeader = null;
            ev.Squad.Comp.Fireteams.SquadLeaderId = null;
        }

        SyncMemberFireteams(ev.Member);
    }

    private void OnSquadMemberRemoved(ref SquadMemberRemovedEvent ev)
    {
        var netEnt = GetNetEntity(ev.Member);
        RemoveFireteamMember(ev.Squad.Comp.Fireteams, netEnt);
    }

    private void OnGotEquipped(Entity<GrantSquadLeaderTrackerComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        var leaderTracker = EnsureComp<SquadLeaderTrackerComponent>(args.Equipee);
        leaderTracker.TrackableRoles = ent.Comp.TrackableRoles;
        leaderTracker.ExtraModes = ent.Comp.ExtraModes;
        if(ent.Comp.DefaultRole != null)
            SetRole((args.Equipee, leaderTracker), ent.Comp.DefaultRole.Value);
        Dirty(args.Equipee, leaderTracker);
    }

    private void OnGotUnequipped(Entity<GrantSquadLeaderTrackerComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        if (!_inventory.TryGetInventoryEntity<GrantSquadLeaderTrackerComponent>(args.Equipee, out _))
            RemCompDeferred<SquadLeaderTrackerComponent>(args.Equipee);
    }

    private void OnMapInit(Entity<SquadLeaderTrackerComponent> ent, ref MapInitEvent args)
    {
        UpdateDirection(ent);
        if (!_squad.TryGetMemberSquad(ent.Owner, out var squad))
            return;

        UpdateDirection(ent, squad: Name(squad));
        ent.Comp.Fireteams = squad.Comp.Fireteams;
        Dirty(ent);
    }

    private void OnRemove(Entity<SquadLeaderTrackerComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void OnSquadLeaderTrackerClicked(Entity<SquadLeaderTrackerComponent> ent, ref SquadLeaderTrackerClickedEvent args)
    {
        _ui.TryOpenUi(ent.Owner, SquadLeaderTrackerUI.Key, ent);
    }

    private void OnSquadLeaderTrackerChangeMode(Entity<SquadLeaderTrackerComponent> ent, ref SquadLeaderTrackerChangeModeEvent args)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        var options = new List<DialogOption>();
        var trackingOptions = new List<EntityUid>();

        // Try to find all entities that fit the selected role.
        var query = EntityQueryEnumerator<RMCTrackableComponent>();
        while (query.MoveNext(out var trackableUid, out _))
        {
            var role = "NoOriginalRole";
            var targetSquadName = "";

            if (_originalRoleQuery.TryComp(trackableUid, out var originalRole))
                role = originalRole.Job;

            if (role != args.Role &&
                (args.Role != SquadLeader || !HasComp<SquadLeaderComponent>(trackableUid)))
                continue;

            // Can't call Name() clientside
            if (!_net.IsClient)
            {
                if (_squadMemberQuery.TryComp(trackableUid, out var targetSquadMember) && targetSquadMember.Squad is { } targetSquad)
                    targetSquadName = Name(targetSquad);
            }

            // Populate the dialogue window with all trackable entities of the selected role.
            options.Add(new DialogOption("(" + targetSquadName + ") " + _rank.GetSpeakerFullRankName(trackableUid),
                new LeaderTrackerSelectTargetEvent(GetNetEntity(trackableUid), args.Role)
            ));

            trackingOptions.Add(trackableUid);
        }

        // Remove targets that are not in the same squad as the tracking entity.
        if (_squadMemberQuery.TryComp(ent, out var squadMember))
        {
            var index = 0;
            while (index < trackingOptions.Count)
            {
                if (_squadMemberQuery.TryComp(trackingOptions[index], out var targetSquadMember) &&
                    squadMember.Squad != targetSquadMember.Squad &&
                    !HasComp<SquadLeaderComponent>(ent))
                {
                    options.RemoveAt(index);
                    trackingOptions.RemoveAt(index);
                    continue;
                }
                index++;
            }
        }

        // Set the target directly without first needing to confirm as there is only 1 or no target.
        if (options.Count <= 1)
        {
            if (trackingOptions.TryGetValue(0, out var target))
                SetTarget(ent, target);
            else
                SetTarget(ent, null);

            SetRole(ent, args.Role);
        }
        // There are multiple entities of the selected role, open a new ui window to choose which entity should be tracked.
        else
        {
            _dialog.OpenOptions(ent,
                Loc.GetString("rmc-squad-info-tracking-selection"),
                options,
                Loc.GetString("rmc-squad-info-tracking-choose")
            );
            return;
        }

        // Check if an extra mode is selected that doesn't target a role.
        if(args.Mode != null)
        {
            if (args.Mode == SquadLeaderTrackerMode.PrimaryLandingZone)
            {
                var landingZoneQuery = EntityQueryEnumerator<PrimaryLandingZoneComponent>();
                while (landingZoneQuery.MoveNext(out var landingZoneUid, out _))
                {
                    SetTarget(ent, landingZoneUid);
                    SetRole(ent, null);
                    break;
                }
            }
        }

        // Can't call Name() clientside
        if (_net.IsClient)
            return;

        MapCoordinates? location = null;
        var squadName = "";

        // Update this here so there is no need to wait for the next delayed update for a smoother experience.
        if (ent.Comp.Target != null)
        {
            location = _transform.GetMapCoordinates(ent.Comp.Target.Value);
            if (_squadMemberQuery.TryComp(ent.Comp.Target.Value, out var squad) &&
                squad.Squad is { } memberSquadName)
                squadName = Name(memberSquadName);
        }

        UpdateDirection(ent, location, squadName);
    }

    private void OnLeaderTrackerSelectTargetEvent(Entity<SquadLeaderTrackerComponent> ent, ref LeaderTrackerSelectTargetEvent args)
    {
        SetTarget(ent, GetEntity(args.Target));
        SetRole(ent, args.Role);
        Dirty(ent);
    }

    private void OnAssignFireteamMsg(Entity<SquadLeaderTrackerComponent> ent, ref SquadLeaderTrackerAssignFireteamMsg args)
    {
        if (_net.IsClient)
            return;

        if (args.Fireteam < 0 || args.Fireteam >= ent.Comp.Fireteams.Fireteams.Length)
            return;

        if (!TryGetEntity(args.Marine, out var marine))
            return;

        if (!CanChangeFireteamMember(args.Actor, marine.Value, true))
            return;

        RemoveFireteamMember(ent.Comp.Fireteams, args.Marine);

        var member = EnsureComp<FireteamMemberComponent>(marine.Value);
        member.Fireteam = args.Fireteam;
        Dirty(marine.Value, member);

        AddFireteamMember(ent.Comp.Fireteams, marine.Value);

        _adminLog.Add(LogType.RMCFireteam, $"{ToPrettyString(args.Actor)} assigned {ToPrettyString(marine)} to fireteam {args.Fireteam}");

        Dirty(ent);
        SyncMemberFireteams(ent.Owner);
    }

    private void OnUnassignFireteamMsg(Entity<SquadLeaderTrackerComponent> ent, ref SquadLeaderTrackerUnassignFireteamMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Marine, out var marine))
            return;

        if (!CanChangeFireteamMember(args.Actor, marine.Value, false))
            return;

        if (!TryComp(marine.Value, out FireteamMemberComponent? member))
            return;

        RemoveFireteamMember(ent.Comp.Fireteams, args.Marine);
        _adminLog.Add(LogType.RMCFireteam, $"{ToPrettyString(args.Actor)} unassigned {ToPrettyString(marine)} from fireteam {member.Fireteam}");

        Dirty(ent);
        SyncMemberFireteams(ent.Owner);
    }

    private void OnPromoteFireteamLeaderMsg(Entity<SquadLeaderTrackerComponent> ent, ref SquadLeaderTrackerPromoteFireteamLeaderMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Marine, out var marineId))
            return;

        if (!CanChangeFireteamMember(args.Actor, marineId.Value, true))
            return;

        if (!TryComp(marineId, out FireteamMemberComponent? member))
            return;

        if (member.Fireteam < 0 || member.Fireteam >= ent.Comp.Fireteams.Fireteams.Length)
            return;

        var netMember = GetNetEntity(marineId.Value);
        var job = _originalRoleQuery.CompOrNull(marineId.Value)?.Job;
        var marine = new SquadLeaderTrackerMarine(netMember, job, Name(marineId.Value));
        ref var fireteam = ref ent.Comp.Fireteams.Fireteams[member.Fireteam];
        fireteam ??= new SquadLeaderTrackerFireteam();

        DemoteFireteamLeader(fireteam, args.Actor);

        fireteam.Leader = marine;
        EnsureComp<FireteamLeaderComponent>(marineId.Value);
        _adminLog.Add(LogType.RMCFireteam, $"{ToPrettyString(args.Actor)} promoted {ToPrettyString(marineId)} to fireteam leader");

        Dirty(ent);
        SyncMemberFireteams(ent.Owner);
    }

    private void OnDemoteFireteamLeaderMsg(Entity<SquadLeaderTrackerComponent> ent, ref SquadLeaderTrackerDemoteFireteamLeaderMsg args)
    {
        if (_net.IsClient)
            return;

        if (args.Fireteam < 0 || args.Fireteam >= ent.Comp.Fireteams.Fireteams.Length)
            return;

        ref var fireteam = ref ent.Comp.Fireteams.Fireteams[args.Fireteam];
        if (!TryGetEntity(fireteam?.Leader?.Id, out var marineId))
            return;

        if (!CanChangeFireteamMember(args.Actor, marineId.Value, false))
            return;

        DemoteFireteamLeader(fireteam, args.Actor);

        Dirty(ent);
        SyncMemberFireteams(ent.Owner);
    }

    private void OnChangeTrackedMsg(Entity<SquadLeaderTrackerComponent> ent, ref SquadLeaderTrackerChangeTrackedMsg args)
    {
        var options = new List<DialogOption> { };

        foreach (var role in ent.Comp.TrackableRoles)
        {
            options.Add(new DialogOption(
                Loc.GetString("rmc-squad-info-" + role),
                new SquadLeaderTrackerChangeModeEvent(role)
            ));
        }

        foreach (var mode in ent.Comp.ExtraModes)
        {
            options.Add(new DialogOption(
                Loc.GetString("rmc-squad-info-" + mode),
                new SquadLeaderTrackerChangeModeEvent(null, mode)
            ));
        }

        _dialog.OpenOptions(
            ent,
            Loc.GetString("rmc-squad-info-tracking-selection"),
            options,
            Loc.GetString("rmc-squad-info-tracking-choose")
        );
    }

    private bool CanChangeFireteamMember(EntityUid user, EntityUid target, bool add)
    {
        if (!HasComp<SquadLeaderComponent>(user))
            return false;

        if (!_squad.AreInSameSquad(user, target))
            return false;

        if (add && HasComp<SquadLeaderComponent>(target))
            return false;

        return true;
    }

    private void SyncMemberFireteams(Entity<SquadMemberComponent?> member)
    {
        if (Resolve(member, ref member.Comp, false) &&
            member.Comp.Squad != null)
        {
            SyncFireteams(member.Comp.Squad.Value);
        }
    }

    private void SyncFireteams(Entity<SquadTeamComponent?> squad)
    {
        if (!Resolve(squad, ref squad.Comp, false))
            return;

        Array.Clear(squad.Comp.Fireteams.Fireteams);
        squad.Comp.Fireteams.Unassigned.Clear();
        if (_squad.TryGetSquadLeader((squad, squad.Comp), out var leader))
        {
            squad.Comp.Fireteams.SquadLeader = Name(leader);
            squad.Comp.Fireteams.SquadLeaderId = GetNetEntity(leader);
        }
        else
        {
            squad.Comp.Fireteams.SquadLeader = null;
            squad.Comp.Fireteams.SquadLeaderId = null;
        }

        foreach (var member in squad.Comp.Members)
        {
            AddFireteamMember(squad.Comp.Fireteams, member);
        }
    }

    private void AddFireteamMember(FireteamData fireteamData, EntityUid member)
    {
        var netMember = GetNetEntity(member);
        var job = _originalRoleQuery.CompOrNull(member)?.Job;
        var marine = new SquadLeaderTrackerMarine(netMember, job, Name(member));
        if (_fireteamMemberQuery.TryComp(member, out var fireteamMember) &&
            fireteamMember.Fireteam >= 0 &&
            fireteamMember.Fireteam < fireteamData.Fireteams.Length)
        {
            ref var fireteam = ref fireteamData.Fireteams[fireteamMember.Fireteam];
            fireteam ??= new SquadLeaderTrackerFireteam();
            fireteam.Members ??= new Dictionary<NetEntity, SquadLeaderTrackerMarine>();
            fireteam.Members[netMember] = marine;

            if (_fireteamLeaderQuery.HasComp(member))
                fireteam.Leader = marine;
        }
        else
        {
            fireteamData.Unassigned[netMember] = marine;
        }

        if (!_squadLeaderTrackerQuery.TryComp(member, out var tracker))
            return;

        tracker.Fireteams = fireteamData;
        Dirty(member, tracker);
    }

    private void RemoveFireteamMember(FireteamData fireteamData, NetEntity member)
    {
        foreach (var fireteam in fireteamData.Fireteams)
        {
            if (fireteam?.Leader?.Id == member)
                fireteam.Leader = null;

            fireteam?.Members?.Remove(member);
        }

        fireteamData.Unassigned.Remove(member);

        if (TryGetEntity(member, out var memberId))
            RemComp<FireteamMemberComponent>(memberId.Value);
    }

    private void DemoteFireteamLeader(SquadLeaderTrackerFireteam? fireteam, EntityUid user)
    {
        if (fireteam != null &&
            fireteam.Leader?.Id is { } oldLeaderNet &&
            TryGetEntity(oldLeaderNet, out var oldLeader) &&
            !TerminatingOrDeleted(oldLeader))
        {
            RemComp<FireteamLeaderComponent>(oldLeader.Value);
            fireteam.Leader = null;

            _adminLog.Add(LogType.RMCFireteam,
                $"{ToPrettyString(user)} demoted {ToPrettyString(oldLeader)} from fireteam leader");
        }
    }

    private void UpdateDirection(Entity<SquadLeaderTrackerComponent> ent, MapCoordinates? coordinates = null, string squad = "")
    {
        _alerts.ClearAlertCategory(ent, SquadTrackerCategory);

        var alert = ent.Comp.Alert;
        var severity = TrackerSystem.CenterSeverity;

        if (ent.Comp.Role != null)
        {
            if (ent.Comp.RoleTrackerAlerts.TryGetValue(ent.Comp.Role.Value, out var newAlert))
                alert = newAlert + squad;
        }

        if (HasComp<PrimaryLandingZoneComponent>(ent.Comp.Target) &&
            ent.Comp.ExtraModeTrackerAlerts.TryGetValue(SquadLeaderTrackerMode.PrimaryLandingZone, out var lzAlert))
            alert = lzAlert;

        if (coordinates != null)
            severity = _tracker.GetAlertSeverity(ent.Owner, coordinates.Value);

        _alerts.ShowAlert(ent.Owner, alert, severity);
    }

    private void SetTarget(Entity<SquadLeaderTrackerComponent> ent, EntityUid? target)
    {
        ent.Comp.Target = target;
        Dirty(ent);
    }

    private void SetRole(Entity<SquadLeaderTrackerComponent> ent, ProtoId<JobPrototype>? role)
    {
        ent.Comp.Role = role;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        _squadLeaders.Clear();
        var squadLeaders = EntityQueryEnumerator<SquadLeaderComponent, SquadMemberComponent>();
        while (squadLeaders.MoveNext(out var uid, out _, out var member))
        {
            if (member.Squad is not { } squad)
                continue;

            _squadLeaders.TryAdd(squad, _transform.GetMapCoordinates(uid));
        }

        Array.Clear(_fireteamLeaders);
        var fireteamLeaders = EntityQueryEnumerator<FireteamLeaderComponent, FireteamMemberComponent, SquadMemberComponent>();
        while (fireteamLeaders.MoveNext(out var uid, out _, out var fireteamMember, out var squadMember))
        {
            if (squadMember.Squad is not { } squad)
                continue;

            if (fireteamMember.Fireteam < 0 || fireteamMember.Fireteam >= _fireteamLeaders.Length)
                continue;

            ref var leaders = ref _fireteamLeaders[fireteamMember.Fireteam];
            leaders ??= new Dictionary<EntityUid, MapCoordinates>();
            leaders[squad] = _transform.GetMapCoordinates(uid);
        }

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<SquadLeaderTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (time < tracker.UpdateAt)
                continue;

            tracker.UpdateAt = time + tracker.UpdateEvery;
            var targetSquadName = "";

            // Swap target to the new SquadLeader after it is swapped.
            if (tracker.Target != null && tracker.Role == SquadLeader && !HasComp<SquadLeaderComponent>(tracker.Target))
            {
                if (_squadMemberQuery.TryComp(tracker.Target.Value, out var target) &&
                    target.Squad is { } targetSquad &&
                    TryComp(targetSquad, out SquadTeamComponent? team) &&
                    _squad.TryGetSquadLeader((targetSquad, team), out var leader))
                {
                    SetTarget((uid,tracker),leader );
                    targetSquadName = Name(targetSquad);
                    var targetCoordinates = _transform.GetMapCoordinates(tracker.Target.Value);

                    UpdateDirection((uid, tracker), targetCoordinates, targetSquadName);
                    continue;
                }
            }

            if (_squadMemberQuery.TryComp(uid, out var squadMember) && squadMember.Squad is { } squad)
            {
                if (_fireteamMemberQuery.TryComp(uid, out var fireteamMember) && tracker.Role == FireteamLeader)
                {
                    var fireteamIndex = fireteamMember.Fireteam;
                    if (fireteamIndex >= 0 &&
                        fireteamIndex < _fireteamLeaders.Length &&
                        _fireteamLeaders[fireteamIndex] is { } fireteam &&
                        fireteam.TryGetValue(squad, out var leader))
                    {
                        UpdateDirection((uid, tracker), leader, Name(squad));
                        continue;
                    }
                }
                else if(tracker.Role == SquadLeader &&
                        _squadLeaders.TryGetValue(squad, out var squadLeader))
                {
                    targetSquadName = Name(squad);

                    if (HasComp<SquadLeaderComponent>(uid) &&
                        tracker.Target != null)
                    {
                        if (_squadMemberQuery.TryComp(tracker.Target.Value, out var target) &&
                            target.Squad is { } targetSquad)
                        {
                            targetSquadName = Name(targetSquad);
                        }
                        squadLeader = _transform.GetMapCoordinates(tracker.Target.Value);
                    }

                    UpdateDirection((uid, tracker), squadLeader, targetSquadName);
                    continue;
                }
            }

            // If the tracker is tracking an entity, point towards the target.
            if (tracker.Target != null)
            {
                if (_squadMemberQuery.TryComp(tracker.Target, out var targetSquad) && targetSquad.Squad != null)
                    targetSquadName = Name(targetSquad.Squad.Value);

                UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value), targetSquadName);
                continue;
            }

            // If the tracker is not tracking an entity, try to find a new target.
            var trackableQuery = EntityQueryEnumerator<RMCTrackableComponent, OriginalRoleComponent>();
            while (trackableQuery.MoveNext(out var trackableUid, out _, out var originalRole))
            {
                if (originalRole.Job != tracker.Role)
                    continue;

                if (_squadMemberQuery.TryComp(tracker.Target, out var targetSquad) && targetSquad.Squad != null)
                    targetSquadName = Name(targetSquad.Squad.Value);

                tracker.Target = trackableUid;
                UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value), targetSquadName);
                break;
            }

            UpdateDirection((uid, tracker));
        }
    }
}
