using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.OrbitalCannon;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Overwatch;

public abstract class SharedOverwatchConsoleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly OrbitalCannonSystem _orbitalCannon = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly SharedSupplyDropSystem _supplyDrop = default!;
    [Dependency] private readonly SharedTacticalMapSystem _tacticalMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private EntityQuery<ActorComponent> _actor;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<OriginalRoleComponent> _originalRoleQuery;
    private EntityQuery<OverwatchDataComponent> _overwatchDataQuery;
    private EntityQuery<RMCPlanetComponent> _planetQuery;

    private readonly ProtoId<DamageGroupPrototype> _bruteGroup = "Brute";
    private readonly ProtoId<DamageGroupPrototype> _burnGroup = "Burn";
    private readonly ProtoId<DamageGroupPrototype> _toxinGroup = "Toxin";

    private TimeSpan _maxProcessTime;
    private TimeSpan _nextUpdateTime;
    private TimeSpan _updateEvery;
    private readonly Dictionary<Entity<SquadTeamComponent>, Queue<EntityUid>> _toProcess = new();
    private readonly HashSet<Entity<SquadTeamComponent>> _toRemove = new();

    public override void Initialize()
    {
        _actor = GetEntityQuery<ActorComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _originalRoleQuery = GetEntityQuery<OriginalRoleComponent>();
        _overwatchDataQuery = GetEntityQuery<OverwatchDataComponent>();
        _planetQuery = GetEntityQuery<RMCPlanetComponent>();

        SubscribeLocalEvent<OrbitalCannonChangedEvent>(OnOrbitalCannonChanged);
        SubscribeLocalEvent<OrbitalCannonLaunchEvent>(OnOrbitalCannonLaunch);

        SubscribeLocalEvent<OverwatchConsoleComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<OverwatchConsoleComponent, OverwatchTransferMarineSelectedEvent>(OnTransferMarineSelected);
        SubscribeLocalEvent<OverwatchConsoleComponent, OverwatchTransferMarineSquadEvent>(OnTransferMarineSquad);

        SubscribeLocalEvent<OverwatchWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);
        SubscribeLocalEvent<OverwatchWatchingComponent, DamageChangedEvent>(OnWatchingDamageChanged);

        Subs.BuiEvents<OverwatchConsoleComponent>(OverwatchConsoleUI.Key, subs =>
        {
            subs.Event<OverwatchConsoleSelectSquadBuiMsg>(OnOverwatchSelectSquadBui);
            subs.Event<OverwatchViewTacticalMapBuiMsg>(OnOverwatchViewTacticalMapBui);
            subs.Event<OverwatchConsoleTakeOperatorBuiMsg>(OnOverwatchTakeOperatorBui);
            subs.Event<OverwatchConsoleStopOverwatchBuiMsg>(OnOverwatchStopBui);
            subs.Event<OverwatchConsoleSetLocationBuiMsg>(OnOverwatchSetLocationBui);
            subs.Event<OverwatchConsoleShowDeadBuiMsg>(OnOverwatchShowDeadBui);
            subs.Event<OverwatchConsoleShowHiddenBuiMsg>(OnOverwatchShowHiddenBui);
            subs.Event<OverwatchConsoleTransferMarineBuiMsg>(OnOverwatchTransferMarineBui);
            subs.Event<OverwatchConsoleWatchBuiMsg>(OnOverwatchWatchBui);
            subs.Event<OverwatchConsoleHideBuiMsg>(OnOverwatchHideBui);
            subs.Event<OverwatchConsolePromoteLeaderBuiMsg>(OnOverwatchPromoteLeaderBui);
            subs.Event<OverwatchConsoleSupplyDropLongitudeBuiMsg>(OnOverwatchSupplyDropLongitudeBui);
            subs.Event<OverwatchConsoleSupplyDropLatitudeBuiMsg>(OnOverwatchSupplyDropLatitudeBui);
            subs.Event<OverwatchConsoleSupplyDropLaunchBuiMsg>(OnOverwatchSupplyDropLaunchBui);
            subs.Event<OverwatchConsoleSupplyDropSaveBuiMsg>(OnOverwatchSupplyDropSaveBui);
            subs.Event<OverwatchConsoleLocationCommentBuiMsg>(OnOverwatchSupplyDropCommentBui);
            subs.Event<OverwatchConsoleOrbitalLongitudeBuiMsg>(OnOverwatchOrbitalCoordinatesBui);
            subs.Event<OverwatchConsoleOrbitalLatitudeBuiMsg>(OnOverwatchOrbitalCoordinatesBui);
            subs.Event<OverwatchConsoleOrbitalLaunchBuiMsg>(OnOverwatchOrbitalLaunchBui);
            // subs.Event<OverwatchConsoleOrbitalSaveBuiMsg>(OnOverwatchOrbitalSaveBui);
            // subs.Event<OverwatchConsoleOrbitalCommentBuiMsg>(OnOverwatchOrbitalCommentBui);
            subs.Event<OverwatchConsoleSendMessageBuiMsg>(OnOverwatchSendMessageBui);
        });

        Subs.CVar(_config, RMCCVars.RMCOverwatchMaxProcessTimeMilliseconds, v => _maxProcessTime = TimeSpan.FromMilliseconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCOverwatchConsoleUpdateEverySeconds, v => _updateEvery = TimeSpan.FromSeconds(v), true);
    }

    private void OnOrbitalCannonChanged(ref OrbitalCannonChangedEvent ev)
    {
        var hasOrbital = ev.Cannon.Comp.Status == OrbitalCannonStatus.Chambered;
        var consoles = EntityQueryEnumerator<OverwatchConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            console.HasOrbital = hasOrbital;
            Dirty(uid, console);
        }
    }

    private void OnOrbitalCannonLaunch(ref OrbitalCannonLaunchEvent ev)
    {
        var consoles = EntityQueryEnumerator<OverwatchConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            console.NextOrbitalLaunch = _timing.CurTime + ev.Cooldown;
            Dirty(uid, console);
        }
    }

    private void OnBUIOpened(Entity<OverwatchConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (_net.IsClient)
            return;

        var state = GetOverwatchBuiState(ent);
        _ui.SetUiState(ent.Owner, OverwatchConsoleUI.Key, state);
    }

    private void OnTransferMarineSelected(Entity<OverwatchConsoleComponent> ent, ref OverwatchTransferMarineSelectedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Actor, out var actor))
            return;

        EntityUid? currentSquad = null;
        if (TryGetEntity(args.Marine, out var marine) &&
            _squad.TryGetMemberSquad(marine.Value, out var marineSquad))
        {
            currentSquad = marineSquad;
        }

        var state = GetOverwatchBuiState(ent);
        var options = new List<DialogOption>();
        foreach (var squad in state.Squads)
        {
            if (currentSquad == GetEntity(squad.Id))
                continue;

            options.Add(new DialogOption(squad.Name, new OverwatchTransferMarineSquadEvent(args.Actor, args.Marine, squad.Id)));
        }

        _dialog.OpenOptions(ent, actor.Value, "Squad Selection", options, "Choose the marine's new squad");
    }

    private void OnTransferMarineSquad(Entity<OverwatchConsoleComponent> ent, ref OverwatchTransferMarineSquadEvent args)
    {
        if (_net.IsClient)
            return;

        if (GetEntity(args.Actor) is not { Valid: true } actor)
            return;

        var squadId = args.Squad;
        var state = GetOverwatchBuiState(ent);
        if (!state.Squads.TryFirstOrNull(s => s.Id == squadId, out var squad))
        {
            _popup.PopupCursor("You can't transfer marines to that squad!", actor, PopupType.LargeCaution);
            return;
        }

        if (!TryGetEntity(args.Marine, out var marineId))
        {
            _popup.PopupCursor("That marine is KIA.", actor, PopupType.LargeCaution);
            return;
        }

        if (_mobState.IsDead(marineId.Value))
        {
            _popup.PopupCursor($"{Name(marineId.Value)} is KIA.", actor, PopupType.LargeCaution);
            return;
        }

        if (squad.Value.Leader != null && HasComp<SquadLeaderComponent>(marineId))
        {
            _popup.PopupCursor($"Transfer aborted. {squad.Value.Name} can't have another Squad Leader.", actor, PopupType.LargeCaution);
            return;
        }

        if (!TryGetEntity(squad.Value.Id, out var newSquadEnt))
        {
            _popup.PopupCursor("You can't transfer marines to that squad!", actor, PopupType.LargeCaution);
            return;
        }

        if (_squad.TryGetMemberSquad(marineId.Value, out var currentSquad) &&
            currentSquad.Owner == GetEntity(args.Squad))
        {
            _popup.PopupCursor($"{Name(marineId.Value)} is already in {Name(newSquadEnt.Value)}!", actor, PopupType.LargeCaution);
            return;
        }

        if (TryComp(newSquadEnt, out SquadTeamComponent? newSquadComp) &&
            _originalRoleQuery.TryComp(marineId, out var role) &&
            role.Job is { } job &&
            !_squad.HasSpaceForRole((newSquadEnt.Value, newSquadComp), job))
        {
            var jobName = job.Id;
            if (_prototypes.TryIndex(job, out var jobProto))
                jobName = Loc.GetString(jobProto.Name);

            _popup.PopupCursor($"Transfer aborted. {Name(newSquadEnt.Value)} can't have another {jobName}.", actor, PopupType.LargeCaution);
            return;
        }

        _squad.AssignSquad(marineId.Value, newSquadEnt.Value, null);

        var selfMsg = $"{Name(marineId.Value)} has been transfered from squad '{Name(currentSquad)}' to squad '{Name(newSquadEnt.Value)}'. Logging to enlistment file.";
        _marineAnnounce.AnnounceSingle(selfMsg, actor);
        _popup.PopupCursor(selfMsg, actor, PopupType.Large);

        var targetMsg = $"You've been transfered to {Name(newSquadEnt.Value)}!";
        _marineAnnounce.AnnounceSingle(targetMsg, marineId.Value);
        _popup.PopupEntity(targetMsg, marineId.Value, marineId.Value, PopupType.Large);
    }

    private void OnWatchingMoveInput(Entity<OverwatchWatchingComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        TryLocalUnwatch(ent);
    }

    private void OnWatchingDamageChanged(Entity<OverwatchWatchingComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is not { } delta)
            return;

        var damage = delta.GetDamagePerGroup(_prototypes);
        var bruteDamage = damage.GetValueOrDefault(_bruteGroup);
        var burnDamage = damage.GetValueOrDefault(_burnGroup);
        var toxinDamage = damage.GetValueOrDefault(_toxinGroup);
        if (bruteDamage + burnDamage <= FixedPoint2.Zero && toxinDamage <= 10)
            return;

        TryLocalUnwatch(ent);

        foreach (var (uiEnt, uiKey) in _ui.GetActorUis(ent.Owner).ToArray())
        {
            if (uiKey is OverwatchConsoleUI.Key)
                _ui.CloseUi(uiEnt, uiKey, ent);
        }

        if (_net.IsServer)
            _popup.PopupEntity("The pain kicked you out of the console!", ent, ent, PopupType.MediumCaution);
    }

    private void OnOverwatchSelectSquadBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleSelectSquadBuiMsg args)
    {
        if (_net.IsServer)
        {
            if (!TryGetEntity(args.Squad, out var squad) || !HasComp<SquadTeamComponent>(squad))
            {
                Log.Warning($"{ToPrettyString(args.Actor)} tried to select invalid squad id {ToPrettyString(squad)}");
                return;
            }

            if (TryComp(ent, out SupplyDropComputerComponent? supplyComputer))
                _supplyDrop.SetSquad((ent, supplyComputer), Prototype(squad.Value)?.ID);
        }

        ent.Comp.Squad = args.Squad;
        ent.Comp.Operator = Identity.Name(args.Actor, EntityManager);
        Dirty(ent);
    }

    private void OnOverwatchViewTacticalMapBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchViewTacticalMapBuiMsg args)
    {
        _tacticalMap.OpenComputerMap(ent.Owner, args.Actor);
    }

    private void OnOverwatchTakeOperatorBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleTakeOperatorBuiMsg args)
    {
        ent.Comp.Operator = Identity.Name(args.Actor, EntityManager);
        Dirty(ent);
    }

    private void OnOverwatchStopBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleStopOverwatchBuiMsg args)
    {
        ent.Comp.Squad = null;
        ent.Comp.Operator = null;
        Dirty(ent);
    }

    private void OnOverwatchSetLocationBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleSetLocationBuiMsg args)
    {
        if (args.Location < OverwatchLocation.Min || args.Location > OverwatchLocation.Max)
            return;

        ent.Comp.Location = args.Location;
        Dirty(ent);
    }

    private void OnOverwatchShowDeadBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleShowDeadBuiMsg args)
    {
        ent.Comp.ShowDead = args.Show;
        Dirty(ent);
    }

    private void OnOverwatchShowHiddenBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleShowHiddenBuiMsg args)
    {
        ent.Comp.ShowHidden = args.Show;
        Dirty(ent);
    }

    private void OnOverwatchTransferMarineBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleTransferMarineBuiMsg args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Squad is not { } selectedSquad)
            return;

        var state = GetOverwatchBuiState(ent);
        var options = new List<DialogOption>();
        if (state.Marines.TryGetValue(selectedSquad, out var marines))
        {
            foreach (var marine in marines)
            {
                var option = new DialogOption
                {
                    Text = $"{marine.Name}",
                    Event = new OverwatchTransferMarineSelectedEvent(GetNetEntity(args.Actor), marine.Id),
                };

                options.Add(option);
            }
        }

        _dialog.OpenOptions(ent, args.Actor, "Transfer Marine", options, "Choose marine to transfer");
    }

    private void OnOverwatchWatchBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleWatchBuiMsg args)
    {
        if (args.Target == default || !TryGetEntity(args.Target, out var target))
            return;

        if (!_inventory.TryGetInventoryEntity<OverwatchCameraComponent>(target.Value, out var camera))
            return;

        Watch(args.Actor, camera);
    }

    private void OnOverwatchHideBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleHideBuiMsg args)
    {
        if (_net.IsClient)
        {
            if (args.Hide)
                ent.Comp.Hidden.Add(args.Target);
            else
                ent.Comp.Hidden.Remove(args.Target);

            Dirty(ent);
            return;
        }

        if (args.Target == default || !TryGetEntity(args.Target, out var target))
            return;

        if (!HasComp<SquadMemberComponent>(target))
            return;

        if (args.Hide)
            ent.Comp.Hidden.Add(args.Target);
        else
            ent.Comp.Hidden.Remove(args.Target);

        Dirty(ent);

        var state = GetOverwatchBuiState(ent);
        _ui.SetUiState(ent.Owner, OverwatchConsoleUI.Key, state);
    }

    private void OnOverwatchPromoteLeaderBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsolePromoteLeaderBuiMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Target, out var target) ||
            !TryComp(target, out SquadMemberComponent? member))
        {
            return;
        }

        _squad.PromoteSquadLeader((target.Value, member), args.Actor, args.Icon);
        var state = GetOverwatchBuiState(ent);
        _ui.SetUiState(ent.Owner, OverwatchConsoleUI.Key, state);
    }

    private void OnOverwatchSupplyDropLongitudeBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleSupplyDropLongitudeBuiMsg args)
    {
        _supplyDrop.SetLongitude(ent.Owner, args.Longitude);
    }

    private void OnOverwatchSupplyDropLatitudeBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleSupplyDropLatitudeBuiMsg args)
    {
        _supplyDrop.SetLatitude(ent.Owner, args.Latitude);
    }

    private void OnOverwatchSupplyDropLaunchBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleSupplyDropLaunchBuiMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent, out SupplyDropComputerComponent? computer))
            return;

        _supplyDrop.TryLaunchSupplyDropPopup((ent, computer), args.Actor);

        var state = GetOverwatchBuiState(ent);
        _ui.SetUiState(ent.Owner, OverwatchConsoleUI.Key, state);
        Dirty(ent);
    }

    private void OnOverwatchSupplyDropSaveBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleSupplyDropSaveBuiMsg args)
    {
        var locations = ent.Comp.SavedLocations;
        if (locations.Length == 0)
            return;

        ref var last = ref ent.Comp.LastLocation;
        if (last >= locations.Length)
            last = 0;

        locations[last] = new OverwatchSavedLocation(args.Longitude, args.Latitude, string.Empty);

        last++;
        Dirty(ent);
    }

    private void OnOverwatchSupplyDropCommentBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleLocationCommentBuiMsg args)
    {
        var locations = ent.Comp.SavedLocations;
        if (args.Index < 0 || args.Index >= locations.Length)
            return;

        if (locations[args.Index] is not { } location)
            return;

        var comment = args.Comment;
        if (comment.Length > 50)
            comment = comment[..50];

        locations[args.Index] = location with { Comment = comment };
    }

    private void OnOverwatchOrbitalCoordinatesBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleOrbitalLongitudeBuiMsg args)
    {
        ent.Comp.OrbitalCoordinates = new Vector2i(args.Longitude, ent.Comp.OrbitalCoordinates.Y);
    }

    private void OnOverwatchOrbitalCoordinatesBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleOrbitalLatitudeBuiMsg args)
    {
        ent.Comp.OrbitalCoordinates = new Vector2i(ent.Comp.OrbitalCoordinates.X, args.Latitude);
    }

    private void OnOverwatchOrbitalLaunchBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleOrbitalLaunchBuiMsg args)
    {
        if (!ent.Comp.CanOrbitalBombardment)
            return;

        if (!_orbitalCannon.TryGetClosestCannon(ent, out var cannon))
            return;

        EntityUid squad = default;
        if (TryGetEntity(ent.Comp.Squad, out var squadNullable))
            squad = squadNullable.Value;

        _orbitalCannon.Fire(cannon, ent.Comp.OrbitalCoordinates, args.Actor, squad);
    }

    // private void OnOverwatchOrbitalSaveBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleOrbitalSaveBuiMsg args)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // private void OnOverwatchOrbitalCommentBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleOrbitalCommentBuiMsg args)
    // {
    //     throw new NotImplementedException();
    // }

    private void OnOverwatchSendMessageBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleSendMessageBuiMsg args)
    {
        if (!ent.Comp.CanMessageSquad)
            return;

        var time = _timing.CurTime;
        if (time < ent.Comp.LastMessage + ent.Comp.MessageCooldown)
            return;

        var message = args.Message;
        if (message.Length > 200)
            message = message[..200];

        if (!TryGetEntity(ent.Comp.Squad, out var squad) ||
            Prototype(squad.Value) is not { } squadProto)
        {
            return;
        }

        ent.Comp.LastMessage = time;
        Dirty(ent);

        _adminLog.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(args.Actor)} sent {squadProto.Name} squad message: {args.Message}");
        _marineAnnounce.AnnounceSquad($"[color=#3C70FF][bold]Overwatch:[/bold] {Name(args.Actor)} transmits: [font size=16][bold]{message}[/bold][/font][/color]", squadProto.ID);

        var coordinates = _transform.GetMapCoordinates(ent);
        var players = Filter.Empty().AddInRange(coordinates, 12, _player, EntityManager);
        players.RemoveWhereAttachedEntity(HasComp<XenoComponent>);

        var userMsg = $"[bold][color=#6685F5]'{Name(squad.Value)}' squad message sent: '{message}'.[/color][/bold]";
        var author = CompOrNull<ActorComponent>(args.Actor)?.PlayerSession.UserId;
        _rmcChat.ChatMessageToMany(userMsg, userMsg, players, ChatChannel.Local, author: author);
    }

    protected virtual void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<OverwatchCameraComponent?> toWatch)
    {
    }

    protected virtual void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        _eye.SetTarget(watcher, null);
    }

    private OverwatchConsoleBuiState GetOverwatchBuiState(Entity<OverwatchConsoleComponent> console)
    {
        return GetOverwatchBuiState(console.Comp);
    }

    private OverwatchConsoleBuiState GetOverwatchBuiState(OverwatchConsoleComponent console)
    {
        var squads = new List<OverwatchSquad>();
        var marines = new Dictionary<NetEntity, List<OverwatchMarine>>();
        var query = EntityQueryEnumerator<SquadTeamComponent>();
        while (query.MoveNext(out var uid, out var team))
        {
            if (console.Group != "ADMINISTRATOR" && team.Group != console.Group)
                continue;

            var netUid = GetNetEntity(uid);
            var squad = new OverwatchSquad(netUid, Name(uid), team.Color, null, team.CanSupplyDrop, team.LeaderIcon);
            var members = marines.GetOrNew(netUid);

            foreach (var member in team.Members)
            {
                if (_overwatchDataQuery.CompOrNull(member)?.Marine is { } data)
                    members.Add(data);
            }

            squads.Add(squad);
        }

        return new OverwatchConsoleBuiState(squads, marines);
    }

    public bool IsHidden(Entity<OverwatchConsoleComponent> console, NetEntity marine)
    {
        return console.Comp.Hidden.Contains(marine);
    }

    private void TryLocalUnwatch(Entity<OverwatchWatchingComponent> ent)
    {
        if (_net.IsClient && _player.LocalEntity == ent.Owner && _player.LocalSession != null)
            Unwatch(ent.Owner, _player.LocalSession);
        else if (TryComp(ent, out ActorComponent? actor))
            Unwatch(ent.Owner, actor.PlayerSession);
    }

    private void ProcessData()
    {
        if (_net.IsClient)
        {
            _toProcess.Clear();
            return;
        }

        try
        {
            var time = _timing.CurTime;
            if (_toProcess.Count > 0)
            {
                foreach (var (squadId, membersQueue) in _toProcess)
                {
                    if (TerminatingOrDeleted(squadId))
                    {
                        _toRemove.Add(squadId);
                        continue;
                    }

                    MapCoordinates? leaderCoords = null;
                    if (_squad.TryGetSquadLeader(squadId, out var leader))
                        leaderCoords = _transform.GetMapCoordinates(leader);

                    while (membersQueue.TryDequeue(out var member))
                    {
                        if (_timing.CurTime > time + _maxProcessTime)
                            break;

                        if (TerminatingOrDeleted(member))
                            continue;

                        // to ignore cryo'd marines
                        var xform = Transform(member);
                        if (!_map.TryGetMap(xform.MapID, out var mapId) ||
                            _map.IsPaused(mapId.Value))
                        {
                            continue;
                        }

                        var coords = _transform.GetMapCoordinates(member);
                        var name = Identity.Name(member, EntityManager);
                        var mobState = _mobStateQuery.CompOrNull(member)?.CurrentState ?? MobState.Alive;
                        var ssd = !_actor.HasComp(member);
                        var role = _originalRoleQuery.CompOrNull(member)?.Job;
                        var location = _planetQuery.HasComp(mapId) ? OverwatchLocation.Planet : OverwatchLocation.Ship;
                        var areaName = _area.TryGetArea(coords, out _, out var areaProto)
                            ? areaProto.Name
                            : string.Empty;
                        var netMember = GetNetEntity(member);

                        Vector2? leaderDistance = null;
                        if (member != leader.Owner &&
                            leaderCoords != null &&
                            leaderCoords.Value.MapId == coords.MapId)
                        {
                            leaderDistance = leaderCoords.Value.Position - coords.Position;
                        }

                        _inventory.TryGetInventoryEntity<OverwatchCameraComponent>(member, out var camera);

                        EnsureComp<OverwatchDataComponent>(member).Marine = new OverwatchMarine(
                            netMember,
                            GetNetEntity(camera),
                            name,
                            mobState,
                            ssd,
                            role,
                            location == OverwatchLocation.Planet,
                            location,
                            areaName,
                            leaderDistance
                        );
                    }

                    if (membersQueue.Count == 0)
                        _toRemove.Add(squadId);
                }

                foreach (var squad in _toRemove)
                {
                    _toProcess.Remove(squad);
                }

                return;
            }

            var query = EntityQueryEnumerator<SquadTeamComponent>();
            while (query.MoveNext(out var squadId, out var squadComp))
            {
                var queue = _toProcess.GetOrNew((squadId, squadComp));
                foreach (var member in squadComp.Members)
                {
                    queue.Enqueue(member);
                }
            }
        }
        catch
        {
            _toProcess.Clear();
            throw;
        }
    }

    private void UpdateConsoles()
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (time < _nextUpdateTime)
            return;

        _nextUpdateTime = time + _updateEvery;

        OverwatchConsoleBuiState? state = null;
        var query = EntityQueryEnumerator<OverwatchConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (!_ui.IsUiOpen(uid, OverwatchConsoleUI.Key))
                continue;

            state ??= GetOverwatchBuiState(console);
            _ui.SetUiState(uid, OverwatchConsoleUI.Key, state);
        }
    }

    public override void Update(float frameTime)
    {
        ProcessData();
        UpdateConsoles();
    }
}
