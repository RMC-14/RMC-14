using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Marines.ControlComputer;

public abstract class SharedMarineControlComputerSystem : EntitySystem
{
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly SharedCommendationSystem _commendation = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedEvacuationSystem _evacuation = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly WarshipSystem _warship = default!;

    private LocalizedDatasetPrototype _medalsDataset = default!;
    private int _characterLimit = 1000;

    public override void Initialize()
    {
        base.Initialize();
        _medalsDataset = _prototype.Index<LocalizedDatasetPrototype>("RMCMarineMedals");
        SubscribeLocalEvent<EvacuationEnabledEvent>(OnRefreshComputers);
        SubscribeLocalEvent<EvacuationDisabledEvent>(OnRefreshComputers);
        SubscribeLocalEvent<EvacuationProgressEvent>(OnRefreshComputers);
        SubscribeLocalEvent<DropshipHijackStartEvent>(OnRefreshComputers);
        SubscribeLocalEvent<RMCAlertLevelChangedEvent>(OnRefreshComputers);

        SubscribeLocalEvent<MarineControlComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeUIOpen);
        SubscribeLocalEvent<MarineControlComputerComponent, MarineControlComputerMedalMarineEvent>(OnComputerMedalMarine);
        SubscribeLocalEvent<MarineControlComputerComponent, MarineControlComputerMedalNameEvent>(OnComputerMedalName);
        SubscribeLocalEvent<MarineControlComputerComponent, MarineControlComputerMedalMessageEvent>(OnComputerMedalMessage);
        SubscribeLocalEvent<MarineControlComputerComponent, MarineControlComputerAlertEvent>(OnComputerAlert);
        SubscribeLocalEvent<MarineControlComputerComponent, MarineControlComputerShipAnnouncementDialogEvent>(OnShipAnnouncementDialog);

        Subs.BuiEvents<MarineControlComputerComponent>(MarineControlComputerUi.Key,
            subs =>
            {
                subs.Event<MarineControlComputerAlertLevelMsg>(OnAlertLevel);
                subs.Event<MarineControlComputerShipAnnouncementMsg>(OnShipAnnouncement);
                subs.Event<MarineControlComputerMedalMsg>(OnMedal);
                subs.Event<MarineControlComputerToggleEvacuationMsg>(OnToggleEvacuationMsg);
            });
        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineControlComputerToggleEvacuationMsg>(OnMarineCommunicationsToggleEvacuation);
            });

        Subs.CVar(_config, CCVars.ChatMaxMessageLength, limit => _characterLimit = limit, true);
    }

    private void OnRefreshComputers<T>(ref T ev)
    {
        RefreshComputers();
    }

    private void OnComputerBeforeUIOpen(Entity<MarineControlComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        RefreshComputers();
    }

    private void OnComputerMedalMarine(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerMedalMarineEvent args)
    {
        if (!TryGetEntity(args.Actor, out var actor))
            return;

        // For not gibbed marines
        if (args.Marine != null)
        {
            if (!TryGetEntity(args.Marine, out var marine))
                return;

            if (marine == actor)
            {
                _popup.PopupClient(Loc.GetString("rmc-medal-error-self-award"), actor, PopupType.MediumCaution);
                return;
            }
        }
        // For gibbed marines
        else if (args.LastPlayerId == null)
        {
            return;
        }

        if (_net.IsClient)
            return;

        var options = new List<DialogOption>();
        foreach (var name in _medalsDataset.Values)
        {
            options.Add(new DialogOption(Loc.GetString(name), new MarineControlComputerMedalNameEvent(args.Actor, args.Marine, Loc.GetString(name), args.LastPlayerId)));
        }

        _dialog.OpenOptions(ent, actor.Value, Loc.GetString("rmc-medal-type"), options, Loc.GetString("rmc-medal-type-prompt"));
    }

    private void OnComputerMedalName(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerMedalNameEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Actor, out var actor))
            return;

        var ev = new MarineControlComputerMedalMessageEvent(args.Actor, args.Marine, args.Name, LastPlayerId: args.LastPlayerId);
        _dialog.OpenInput(ent, actor.Value, Loc.GetString("rmc-medal-citation-prompt"), ev, true, _commendation.CharacterLimit);
    }

    private void OnComputerMedalMessage(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerMedalMessageEvent args)
    {
        if (!TryGetEntity(args.Actor, out var actor) ||
            !HasComp<CommendationGiverComponent>(actor) ||
            string.IsNullOrWhiteSpace(args.Message.Trim()))
        {
            return;
        }

        // For not gibbed marines
        if (args.Marine != null)
        {
            if (!TryGetEntity(args.Marine, out var marine) ||
                !TryComp(marine, out CommendationReceiverComponent? receiver) ||
                receiver.LastPlayerId == null)
            {
                return;
            }
            _commendation.GiveCommendation(actor.Value, marine.Value, args.Name, args.Message, CommendationType.Medal);
        }
        // For gibbed marines
        else if (args.LastPlayerId != null)
        {
            if (TryComp<MarineControlComputerComponent>(ent, out var computer) &&
                computer.GibbedMarines.TryGetValue(args.LastPlayerId, out var info))
            {
                _commendation.GiveCommendationByLastPlayerId(actor.Value, args.LastPlayerId, info.Name, args.Name, args.Message, CommendationType.Medal);
            }
        }
        else
        {
            return;
        }

        if (_net.IsClient)
            return;

        _popup.PopupCursor(Loc.GetString("rmc-medal-awarded"), actor.Value, PopupType.Large);
    }

    private void OnComputerAlert(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerAlertEvent args)
    {
        _alertLevel.Set(args.Level, GetEntity(args.User));
    }

    private void OnAlertLevel(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerAlertLevelMsg args)
    {
        var current = _alertLevel.Get();
        var options = new List<DialogOption>();
        foreach (var level in Enum.GetValues<RMCAlertLevels>())
        {
            if (level == current)
                continue;

            if (level >= RMCAlertLevels.Red)
                continue;

            var text = Loc.GetString($"rmc-alert-{level.ToString().ToLowerInvariant()}");
            options.Add(new DialogOption(text, new MarineControlComputerAlertEvent(GetNetEntity(args.Actor), level)));
        }

        _dialog.OpenOptions(ent,
            args.Actor,
            Loc.GetString("rmc-alert-level"),
            options,
            Loc.GetString("rmc-alert-level-which")
        );
    }

    private void OnShipAnnouncement(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerShipAnnouncementMsg args)
    {
        if (!CanUseShipAnnouncementPopup(ent, args.Actor))
            return;

        var ev = new MarineControlComputerShipAnnouncementDialogEvent(GetNetEntity(args.Actor));
        _dialog.OpenInput(
            ent,
            args.Actor,
            Loc.GetString("rmc-announcement-shipside-header"),
            ev,
            true,
            _characterLimit
        );
    }

    private void OnShipAnnouncementDialog(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerShipAnnouncementDialogEvent args)
    {
        if (GetEntity(args.User) is not { Valid: true } user)
            return;

        if (!CanUseShipAnnouncementPopup(ent, user))
            return;

        ent.Comp.LastShipAnnouncement = _timing.CurTime;
        var map = _warship.TryGetWarshipMap(ent, out var warshipMap) ? warshipMap : _transform.GetMapId(ent.Owner);
        _marineAnnounce.AnnounceSigned(
            user,
            args.Message,
            Loc.GetString("rmc-announcement-author-shipside"),
            sound: SharedMarineAnnounceSystem.AresAnnouncementSound,
            filter: Filter.BroadcastMap(map).RemoveWhereAttachedEntity(e => !HasComp<MarineComponent>(e) && !HasComp<GhostComponent>(e)),
            excludeSurvivors: false
        );
    }

    private void OnMedal(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerMedalMsg args)
    {
        GiveMedal(ent, args.Actor);
    }

    public void GiveMedal(EntityUid computer, EntityUid actor)
    {
        if (!TryComp(actor, out ActorComponent? actorComp))
            return;

        if (!HasComp<CommendationGiverComponent>(actor))
        {
            _popup.PopupClient(Loc.GetString("rmc-medal-error-officer-only"), actor, PopupType.MediumCaution);
            return;
        }

        if (_net.IsClient)
            return;

        var netActor = GetNetEntity(actor);
        var options = new List<DialogOption>();

        // Add not gibbed marines
        var receivers = EntityQueryEnumerator<CommendationReceiverComponent, MarineComponent>();
        while (receivers.MoveNext(out var uid, out var receiver, out _))
        {
            if (receiver.LastPlayerId == null ||
                Guid.Parse(receiver.LastPlayerId) == actorComp.PlayerSession.UserId)
            {
                continue;
            }

            if (HasComp<RMCSurvivorComponent>(uid))
                continue;

            if (uid == actor)
                continue;

            options.Add(new DialogOption(Name(uid), new MarineControlComputerMedalMarineEvent(netActor, GetNetEntity(uid))));
        }

        // Add gibbed marines regardless of the entity itself, will always be added
        var allGibbed = new Dictionary<string, GibbedMarineInfo>();
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var _, out var comp))  // all components must be synchronized with each other, but this is just in case
        {
            foreach (var (playerId, info) in comp.GibbedMarines)
            {
                if (info.LastPlayerId == null)
                    continue;
                allGibbed[playerId] = info;
            }
        }
        foreach (var (playerId, info) in allGibbed)
        {
            options.Add(new DialogOption(info.Name, new MarineControlComputerMedalMarineEvent(netActor, null, playerId)));
        }

        _dialog.OpenOptions(computer, actor, Loc.GetString("rmc-medal-recipient"), options, Loc.GetString("rmc-medal-recipient-prompt"));
    }

    private void OnToggleEvacuationMsg(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerToggleEvacuationMsg args)
    {
        if (_ui.HasUi(ent.Owner, MarineControlComputerUi.Key))
            _ui.CloseUi(ent.Owner, MarineControlComputerUi.Key, args.Actor);

        if (_ui.HasUi(ent.Owner, MarineCommunicationsComputerUI.Key))
            _ui.CloseUi(ent.Owner, MarineCommunicationsComputerUI.Key, args.Actor);

        if (!ent.Comp.CanEvacuate)
            return;

        var time = _timing.CurTime;
        if (time < ent.Comp.LastToggle + ent.Comp.ToggleCooldown)
            return;

        ent.Comp.LastToggle = time;

        // TODO RMC14 evacuation start sound
        _evacuation.ToggleEvacuation(null, ent.Comp.EvacuationCancelledSound, _transform.GetMap(ent.Owner));
        RefreshComputers();
    }

    private void OnMarineCommunicationsToggleEvacuation(Entity<MarineCommunicationsComputerComponent> ent, ref MarineControlComputerToggleEvacuationMsg args)
    {
        if (TryComp<MarineControlComputerComponent>(ent.Owner, out var controlComp))
        {
            OnToggleEvacuationMsg(new Entity<MarineControlComputerComponent>(ent.Owner, controlComp), ref args);
        }
    }

    private void RefreshComputers()
    {
        if (_net.IsClient)
            return;

        var canEvacuate = _alertLevel.IsRedOrDeltaAlert() || _evacuation.IsEvacuationEnabled();
        var evacuationEnabled = _evacuation.IsEvacuationEnabled();
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            computer.Evacuating = evacuationEnabled;
            computer.CanEvacuate = canEvacuate;
            Dirty(uid, computer);
        }
    }

    private bool CanUseShipAnnouncementPopup(Entity<MarineControlComputerComponent> ent, EntityUid user)
    {
        var cooldown = ent.Comp.ShipAnnouncementCooldown;
        if (ent.Comp.LastShipAnnouncement != null &&
            _timing.CurTime < ent.Comp.LastShipAnnouncement + cooldown)
        {
            var msg = Loc.GetString("rmc-announcement-cooldown", ("seconds", (int) cooldown.TotalSeconds));
            _popup.PopupClient(msg, user);
            return false;
        }

        return true;
    }
}
