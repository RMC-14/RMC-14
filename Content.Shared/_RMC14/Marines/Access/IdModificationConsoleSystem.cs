using System.Collections.Frozen;
using System.Linq;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.Marines.Access;

public sealed class IdModificationConsoleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly GunIFFSystem _iff = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;

    private FrozenDictionary<string, AccessGroupPrototype> _accessGroup =
        FrozenDictionary<string, AccessGroupPrototype>.Empty;

    private FrozenDictionary<string, AccessLevelPrototype> _accessLevel =
        FrozenDictionary<string, AccessLevelPrototype>.Empty;

    private EntityQuery<OriginalRoleComponent> _originalRoleQuery;

    public override void Initialize()
    {
        _originalRoleQuery = GetEntityQuery<OriginalRoleComponent>();

        Subs.BuiEvents<IdModificationConsoleComponent>(IdModificationConsoleUIKey.Key,
            subs =>
            {
                subs.Event<IdModificationConsoleAccessChangeBuiMsg>(OnAccessChangeMsg);
                subs.Event<IdModificationConsoleMultipleAccessChangeBuiMsg>(OnMultipleAccessChangeMsg);
                subs.Event<IdModificationConsoleSignInBuiMsg>(OnSignInMsg);
                subs.Event<IdModificationConsoleSignInTargetBuiMsg>(OnSignInTargetMsg);
                subs.Event<IdModificationConsoleIFFChangeBuiMsg>(OnIFFChangeMsg);
                subs.Event<IdModificationConsoleJobChangeBuiMsg>(OnJobChangeMsg);
                subs.Event<IdModificationConsoleTerminateConfirmBuiMsg>(OnTerminateConfirmMsg);
                subs.Event<IdModificationConsoleAssignSquadMsg>(OnTerminalAssignSquadMsg);
            });
        SubscribeLocalEvent<IdModificationConsoleComponent, MapInitEvent>(OnComponentInit);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<IdModificationConsoleComponent, InteractUsingEvent>(OnInteractHand);

        ReloadJobPrototypes();
        ReloadAccessPrototypes();
    }

    private void OnInteractHand(Entity<IdModificationConsoleComponent> ent, ref InteractUsingEvent args)
    {
        args.Handled = ContainerInHandler(ent, args.User);
    }

    private void OnJobChangeMsg(Entity<IdModificationConsoleComponent> ent,
        ref IdModificationConsoleJobChangeBuiMsg args)
    {
        if (!ent.Comp.Authenticated)
            return;

        if (!TryContainerEntity(ent, ent.Comp.TargetIdSlot, out var uid) || !TryComp(uid, out AccessComponent? access))
            return;

        access.Tags.Clear();

        if (!_prototype.TryIndex(args.AccessGroup, out var accessGroupPrototype))
            return;

        foreach (var tag in accessGroupPrototype.Tags)
        {
            access.Tags.Add(tag);
        }

        Dirty(uid.Value, access);

        _adminLogger.Add(LogType.RMCIdModify,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} has changed the accesses of {ToPrettyString(uid):entity} to {accessGroupPrototype.Name}");
    }

    private void OnTerminateConfirmMsg(Entity<IdModificationConsoleComponent> ent,
        ref IdModificationConsoleTerminateConfirmBuiMsg args)
    {
        if (!ent.Comp.Authenticated)
            return;

        if (!TryContainerEntity(ent, ent.Comp.TargetIdSlot, out var uid) || !TryComp(uid, out ItemIFFComponent? iff) ||
            !TryComp(uid, out IdCardComponent? idCard) || !TryComp(uid, out AccessComponent? access))
            return;

        _iff.SetIdFaction((uid.Value, iff), "FactionSurvivor");
        ent.Comp.HasIFF = false;

        foreach (var accessToRemove in ent.Comp.AccessList)
        {
            access.Tags.Remove(accessToRemove);
        }

        foreach (var accessToRemove in ent.Comp.HiddenAccessList)
        {
            access.Tags.Remove(accessToRemove);
        }

        idCard._jobTitle = "Civilian";
        Dirty(uid.Value, idCard);
        Dirty(uid.Value, access);
        if (idCard.OriginalOwner != null)
        {
            _rank.SetRank(idCard.OriginalOwner.Value, "RMCRankCivilian");
            _squad.RemoveSquad(idCard.OriginalOwner.Value, null);
            _metaData.SetEntityName(uid.Value,
                $"{MetaData(idCard.OriginalOwner.Value).EntityName} ({idCard._jobTitle})");
        }

        _adminLogger.Add(LogType.RMCIdModify,
            LogImpact.High,
            $"{ToPrettyString(args.Actor):player} has terminated {ToPrettyString(uid):entity} & {ToPrettyString(idCard.OriginalOwner):player}");
    }

    private void OnIFFChangeMsg(Entity<IdModificationConsoleComponent> ent,
        ref IdModificationConsoleIFFChangeBuiMsg args)
    {
        if (!ent.Comp.Authenticated)
            return;

        if (!TryContainerEntity(ent, ent.Comp.TargetIdSlot, out var uid) || uid == null)
            return;

        EnsureComp<ItemIFFComponent>(uid.Value, out var iff);
        var targetFaction = ent.Comp.Faction;

        if (!args.Revoke)
        {
            if (!iff.Factions.Contains(targetFaction))
                iff.Factions.Add(targetFaction);

            Dirty(uid.Value, iff);
            _adminLogger.Add(LogType.RMCIdModify,
                LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} has granted the {targetFaction} IFF for {ToPrettyString(uid):entity}");

            ent.Comp.HasIFF = true;
        }
        else
        {
            var removed = iff.Factions.Remove(targetFaction);

            if (iff.Factions.Count == 0)
            {
                _iff.SetIdFaction((uid.Value, iff), "FactionSurvivor");
            }
            else if (removed)
                Dirty(uid.Value, iff);

            ent.Comp.HasIFF = false;

            if (removed)
            {
                _adminLogger.Add(LogType.RMCIdModify,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} has revoked the {targetFaction} IFF for {ToPrettyString(uid):entity}");
            }
        }

        Dirty(ent);
    }

    private void OnSignInTargetMsg(Entity<IdModificationConsoleComponent> ent,
        ref IdModificationConsoleSignInTargetBuiMsg args)
    {
        if (TryContainerEntity(ent, ent.Comp.TargetIdSlot, out var id))
        {
            ContainerOutHandler(ent, args.Actor, ent.Comp.TargetIdSlot);
            _adminLogger.Add(LogType.RMCIdModify,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has ejected from {ent.Comp.TargetIdSlot} from: {ent.Owner:entity}");
        }
        else
            ContainerInHandler(ent, args.Actor, ent.Comp.TargetIdSlot);
    }

    private void OnSignInMsg(Entity<IdModificationConsoleComponent> ent, ref IdModificationConsoleSignInBuiMsg args)
    {
        if (TryContainerEntity(ent, ent.Comp.PrivilegedIdSlot, out var id))
        {
            ContainerOutHandler(ent, args.Actor, ent.Comp.PrivilegedIdSlot);
            _adminLogger.Add(LogType.RMCIdModify,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has ejected from {ent.Comp.PrivilegedIdSlot} from: {ToPrettyString(ent.Owner):entity}");
        }
        else
        {
            ContainerInHandler(ent, args.Actor, ent.Comp.PrivilegedIdSlot);
            if (ent.Comp.Authenticated)
                return;
            if (!_prototype.TryIndex(ent.Comp.Access, out var accessPrototype) || accessPrototype.Name == null)
                return;

            _popup.PopupClient($"This id is missing the {Loc.GetString(accessPrototype.Name)}",
                args.Actor,
                PopupType.MediumCaution);
        }
    }

    private void OnMultipleAccessChangeMsg(Entity<IdModificationConsoleComponent> ent,
        ref IdModificationConsoleMultipleAccessChangeBuiMsg args)
    {
        if (!ent.Comp.Authenticated)
            return;

        if (!TryContainerEntity(ent, ent.Comp.TargetIdSlot, out var uid) || !TryComp(uid, out AccessComponent? access))
            return;

        switch (args.Type)
        {
            case "GrantAll":
                foreach (var accessToAdd in ent.Comp.AccessList)
                {
                    if (!_prototype.TryIndex(accessToAdd, out var accessPrototype) ||
                        accessPrototype.AccessGroup != args.AccessList)
                        continue;
                    access.Tags.Add(accessPrototype);
                }

                _adminLogger.Add(LogType.RMCIdModify,
                    LogImpact.Medium,
                    $"{ToPrettyString(args.Actor):player} has granted all accesses for {args.AccessList} on {ToPrettyString(uid):entity}");
                break;
            case "RevokeAll":
                foreach (var accessToRemove in ent.Comp.AccessList)
                {
                    if (!_prototype.TryIndex(accessToRemove, out var accessPrototype) ||
                        accessPrototype.AccessGroup != args.AccessList)
                        continue;
                    access.Tags.Remove(accessToRemove);
                }

                _adminLogger.Add(LogType.RMCIdModify,
                    LogImpact.Medium,
                    $"{ToPrettyString(args.Actor):player} has revoked all accesses for {args.AccessList} on {ToPrettyString(uid):entity}");
                break;
            case "GrantAllGroup":
                foreach (var accessToAdd in ent.Comp.AccessList)
                {
                    access.Tags.Add(accessToAdd);
                }

                _adminLogger.Add(LogType.RMCIdModify,
                    LogImpact.Medium,
                    $"{ToPrettyString(args.Actor):player} has granted all accesses on {ToPrettyString(uid):entity}");
                break;
            case "RevokeAllGroup":
                foreach (var accessToRemove in ent.Comp.AccessList)
                {
                    access.Tags.Remove(accessToRemove);
                }

                _adminLogger.Add(LogType.RMCIdModify,
                    LogImpact.Medium,
                    $"{ToPrettyString(args.Actor):player} has revoked all accesses on {ToPrettyString(uid):entity}");
                break;
        }

        Dirty(uid.Value, access);
        Dirty(ent);
    }

    private void OnAccessChangeMsg(Entity<IdModificationConsoleComponent> ent,
        ref IdModificationConsoleAccessChangeBuiMsg args)
    {
        if (!ent.Comp.Authenticated)
            return;

        if (!TryContainerEntity(ent, ent.Comp.TargetIdSlot, out var uid) || !TryComp(uid, out AccessComponent? access))
            return;

        if (args.Add)
        {
            access.Tags.Add(args.Access);
            _adminLogger.Add(LogType.RMCIdModify,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has granted {args.Access} to {ToPrettyString(uid):entity}");
        }
        else
        {
            access.Tags.Remove(args.Access);
            _adminLogger.Add(LogType.RMCIdModify,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has revoked {args.Access} to {ToPrettyString(uid):entity}");
        }

        Dirty(uid.Value, access);
    }

    //TODO RMC14 add ranks tab

    // private void RankUpdate(Entity<IdCardComponent> card, RankPrototype Rank)
    // {
    //
    // }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<AccessLevelPrototype>())
            ReloadAccessPrototypes();
        if (ev.WasModified<AccessGroupPrototype>())
            ReloadJobPrototypes();
    }

    private void ReloadAccessPrototypes()
    {
        var dict = new Dictionary<string, AccessLevelPrototype>();
        foreach (var accessLevelProto in _prototype.EnumeratePrototypes<AccessLevelPrototype>())
        {
            object? accessLevelObj = new AccessLevelPrototype();
            _serialization.CopyTo(accessLevelProto, ref accessLevelObj);
            if (accessLevelObj is not AccessLevelPrototype accessLevel)
                continue;

            dict[accessLevelProto.ID] = accessLevel;
        }

        _accessLevel = dict.ToFrozenDictionary();
    }

    private void ReloadJobPrototypes()
    {
        var dict = new Dictionary<string, AccessGroupPrototype>();
        foreach (var accessLevelProto in _prototype.EnumeratePrototypes<AccessGroupPrototype>())
        {
            object? accessGroupObj = new AccessGroupPrototype();
            _serialization.CopyTo(accessLevelProto, ref accessGroupObj);
            if (accessGroupObj is not AccessGroupPrototype accessLevel)
                continue;

            dict[accessLevelProto.ID] = accessLevel;
        }

        _accessGroup = dict.ToFrozenDictionary();
    }

    private bool ContainerInHandler(Entity<IdModificationConsoleComponent> ent, EntityUid user)
    {
        if (!_hands.TryGetActiveItem(user, out var handItem) ||
            !TryComp(handItem, out IdCardComponent? idCardComponent) ||
            !TryComp(handItem, out AccessComponent? accessComponent))
            return false;

        if (accessComponent.Tags.Contains(ent.Comp.Access))
            return ContainerInHandler(ent, user, ent.Comp.PrivilegedIdSlot);

        return ContainerInHandler(ent, user, ent.Comp.TargetIdSlot);
    }

    private bool ContainerInHandler(Entity<IdModificationConsoleComponent> ent, EntityUid user, string containerType)
    {
        if (!_hands.TryGetActiveItem(user, out var handItem) ||
            !TryComp(handItem, out IdCardComponent? idCardComponent) ||
            !TryComp(handItem, out AccessComponent? accessComponent))
            return false;

        if (accessComponent.Tags.Contains(ent.Comp.Access) && containerType == ent.Comp.PrivilegedIdSlot)
            ent.Comp.Authenticated = true;

        ent.Comp.HasIFF = false;
        if (TryComp(handItem, out ItemIFFComponent? iff) && containerType == ent.Comp.TargetIdSlot)
            ent.Comp.HasIFF = iff.Factions.Contains(ent.Comp.Faction);

        var container = _container.EnsureContainer<ContainerSlot>(ent, containerType);
        _container.Insert(handItem.Value, container);

        var query = EntityQueryEnumerator<SquadTeamComponent>();
        ent.Comp.Squads = [];
        while (query.MoveNext(out var uid, out var team))
        {
            if (ent.Comp.SquadGroup != "ADMINISTRATOR" && team.Group != ent.Comp.SquadGroup)
                continue;

            ent.Comp.Squads.Add(new(GetNetEntity(uid), Name(uid), team.Color));
        }

        Dirty(ent);
        return true;
    }

    private bool ContainerOutHandler(Entity<IdModificationConsoleComponent> ent, EntityUid user, string containerType)
    {
        var container = _container.EnsureContainer<ContainerSlot>(ent, containerType);
        var contained = container.ContainedEntity;
        if (contained == null)
            return false;
        _container.Remove(contained.Value, container);
        if (containerType == ent.Comp.PrivilegedIdSlot)
            ent.Comp.Authenticated = false;
        if (containerType == ent.Comp.TargetIdSlot)
            ent.Comp.HasIFF = false;
        _hands.PickupOrDrop(user, contained.Value);
        Dirty(ent);
        return true;
    }

    private bool TryContainerEntity(Entity<IdModificationConsoleComponent> ent,
        string containerType,
        out EntityUid? contained)
    {
        var container = _container.EnsureContainer<ContainerSlot>(ent, containerType);
        contained = container.ContainedEntity;
        Dirty(ent);
        return contained != null;
    }

    private void OnComponentInit(Entity<IdModificationConsoleComponent> ent, ref MapInitEvent args)
    {
        UpdateAccessList(ent);
    }

    private void UpdateAccessList(Entity<IdModificationConsoleComponent> ent)
    {
        var accessList = new HashSet<ProtoId<AccessLevelPrototype>>();
        var accessListHidden = new HashSet<ProtoId<AccessLevelPrototype>>();
        var accessGroups = new HashSet<ProtoId<AccessLevelPrototype>>();

        foreach (var accessLevel in _accessLevel.Values)
        {
            if (accessLevel.Faction == ent.Comp.Faction && !accessLevel.Hidden)
            {
                if (accessLevel.Name != null && accessLevel.Name.Contains("protobaseaccess"))
                    accessGroups.Add(accessLevel);
                else
                    accessList.Add(accessLevel);
            }
            else if (accessLevel.Faction == ent.Comp.Faction && accessLevel.Hidden)
            {
                if (accessLevel.Name != null && !accessLevel.Name.Contains("protobaseaccess"))
                    accessListHidden.Add(accessLevel);
            }
        }

        ent.Comp.AccessGroups = accessGroups;
        ent.Comp.AccessList = accessList;
        ent.Comp.HiddenAccessList = accessListHidden;

        var groupList = new HashSet<ProtoId<AccessGroupPrototype>>();
        // var groupListHidden = new HashSet<ProtoId<AccessGroupPrototype>>();
        var groupGroups = new HashSet<ProtoId<AccessGroupPrototype>>();

        foreach (var accessGroup in _accessGroup.Values)
        {
            if (accessGroup.Faction == ent.Comp.Faction && !accessGroup.Hidden)
            {
                if (accessGroup.Name != null && accessGroup.Name.Contains("protobaseaccess"))
                    groupGroups.Add(accessGroup);
                else
                    groupList.Add(accessGroup);
            }
        }
        // else if (accessGroup.Faction == ent.Comp.Faction && accessGroup.Hidden)
        // {
        //     if(accessGroup.Name != null && !accessGroup.Name.Contains("protobaseaccess"))
        //         groupListHidden.Add(accessGroup);
        // }

        ent.Comp.JobGroups = groupGroups;
        ent.Comp.JobList = groupList;
    }

    private void OnTerminalAssignSquadMsg(Entity<IdModificationConsoleComponent> ent, ref IdModificationConsoleAssignSquadMsg args)
    {
        // Too many mispredictions.
        if (_net.IsClient)
            return;

        var actor = args.Actor;
        if (!ent.Comp.Authenticated)
            return;

        if (!TryContainerEntity(ent, ent.Comp.TargetIdSlot, out var uid) ||
            !TryComp(uid, out IdCardComponent? idCard) ||
            idCard.OriginalOwner is not { } marineId)
            return;

        if (args.Squad is not { } squadNetEnt)
        {
            _squad.RemoveSquad(marineId, null);
            _metaData.SetEntityName(uid.Value,
                $"{MetaData(idCard.OriginalOwner.Value).EntityName} ({idCard._jobTitle})");

            var selfMsgUnassign = $"{Name(marineId)} has been unassigned.";
            _marineAnnounce.AnnounceSingle(selfMsgUnassign, actor);
            _popup.PopupCursor(selfMsgUnassign, actor, PopupType.Large);

            var targetMsgUnassign = "You've been unassigned from your squad.";
            _marineAnnounce.AnnounceSingle(targetMsgUnassign, marineId);
            _popup.PopupEntity(targetMsgUnassign, marineId, marineId, PopupType.Large);

            _adminLogger.Add(LogType.RMCIdModify,
                LogImpact.Medium,
                $"{ToPrettyString(actor):player} has unassigned {ToPrettyString(marineId):player}");
            return;
        }

        if (!TryGetEntity(squadNetEnt, out var newSquadEnt))
        {
            _popup.PopupCursor($"There was an error assigning {Name(marineId)}.", actor, PopupType.LargeCaution);
            return;
        }

        if (TryComp(newSquadEnt, out SquadTeamComponent? newSquadComp) &&
            _originalRoleQuery.TryComp(marineId, out var role) &&
            role.Job is { } job &&
            !_squad.HasSpaceForRole((newSquadEnt.Value, newSquadComp), job))
        {
            var jobName = job.Id;
            if (_prototype.TryIndex(job, out var jobProto))
                jobName = Loc.GetString(jobProto.Name);

            _popup.PopupCursor($"{Name(newSquadEnt.Value)} can't have another {jobName}.", actor, PopupType.LargeCaution);
            return;
        }

        if (ent.Comp.EnlistmentRequirement is { } requirements && !_skills.HasAllSkills(marineId, requirements))
        {
            _popup.PopupCursor("You cannot assign untrained civilians to squads!", actor, PopupType.LargeCaution);
            return;
        }

        RemComp<SquadLeaderComponent>(marineId);
        _squad.AssignSquad(marineId, newSquadEnt.Value, null);
        _metaData.SetEntityName(uid.Value,
            $"{MetaData(idCard.OriginalOwner.Value).EntityName} ({Name(newSquadEnt.Value)} {idCard._jobTitle})");

        var selfMsg = $"{Name(marineId)} has been assigned to {Name(newSquadEnt.Value)}.";
        _marineAnnounce.AnnounceSingle(selfMsg, actor);
        _popup.PopupCursor(selfMsg, actor, PopupType.Large);

        var targetMsg = $"You've been transferred to {Name(newSquadEnt.Value)}!";
        _marineAnnounce.AnnounceSingle(targetMsg, marineId);
        _popup.PopupEntity(targetMsg, marineId, marineId, PopupType.Large);

        _adminLogger.Add(LogType.RMCIdModify,
            LogImpact.Medium,
            $"{ToPrettyString(actor):player} has assigned {ToPrettyString(marineId):player} to {Name(newSquadEnt.Value)}");
    }
}
