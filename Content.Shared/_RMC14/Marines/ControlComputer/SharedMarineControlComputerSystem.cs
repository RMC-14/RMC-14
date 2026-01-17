using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using System.Globalization;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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

    private int _characterLimit = 1000;

    public override void Initialize()
    {
        base.Initialize();
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
                subs.Event<MarineControlComputerToggleEvacuationMsg>(OnToggleEvacuationMsg);
                subs.Event<MarineControlComputerOpenMedalsPanelMsg>(OnOpenMedalsPanel);
            });
        Subs.BuiEvents<MarineControlComputerComponent>(MarineControlComputerUi.MedalsPanel,
            subs =>
            {
                subs.Event<MarineControlComputerApproveRecommendationMsg>(OnApproveRecommendation);
                subs.Event<MarineControlComputerRejectRecommendationMsg>(OnRejectRecommendation);
            });
        SubscribeLocalEvent<MarineControlComputerComponent, MarineControlComputerMedalMsg>(OnMedal);
        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineControlComputerToggleEvacuationMsg>(OnMarineCommunicationsToggleEvacuation);
                subs.Event<MarineControlComputerOpenMedalsPanelMsg>(OnMarineCommunicationsOpenMedalsPanel);
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
        foreach (var medalEntityId in _commendation.GetAwardableMedalIds())
        {
            if (!_prototype.TryIndex(medalEntityId, out var medalProto))
                continue;

            var medalName = medalProto.Name;
            if (!string.IsNullOrEmpty(medalName))
            {
                medalName = medalName[0].ToString().ToUpper() + medalName.Substring(1);
            }
            var icon = new SpriteSpecifier.EntityPrototype(medalEntityId);

            options.Add(new DialogOption(medalName, new MarineControlComputerMedalNameEvent(args.Actor, args.Marine, medalEntityId, args.LastPlayerId), icon));
        }

        _dialog.OpenOptions(ent, actor.Value, Loc.GetString("rmc-medal-type"), options, Loc.GetString("rmc-medal-type-prompt"));
    }

    private void OnComputerMedalName(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerMedalNameEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Actor, out var actor))
            return;

        if (!_prototype.TryIndex(args.MedalEntityId, out var medalProto))
            return;

        var medalName = medalProto.Name;
        if (!string.IsNullOrEmpty(medalName))
        {
            medalName = medalName[0].ToString().ToUpper() + medalName.Substring(1);
        }
        var medalDescription = medalProto.Description;
        var prompt = $"[italic][bolditalic]{medalName}[/bolditalic] - {medalDescription}[/italic]\n\n{Loc.GetString("rmc-medal-citation-prompt")}";
        var ev = new MarineControlComputerMedalMessageEvent(args.Actor, args.Marine, medalName, args.MedalEntityId, LastPlayerId: args.LastPlayerId);
        _dialog.OpenInput(ent, actor.Value, prompt, ev, true, _commendation.CharacterLimit, _commendation.MinCharacterLimit, true);
    }

    private void OnComputerMedalMessage(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerMedalMessageEvent args)
    {
        if (!TryGetEntity(args.Actor, out var actor) ||
            !HasComp<CommendationGiverComponent>(actor) ||
            string.IsNullOrWhiteSpace(args.Message.Trim()))
        {
            return;
        }

        string? awardedLastPlayerId = null;

        // For not gibbed marines
        if (args.Marine != null)
        {
            if (!TryGetEntity(args.Marine, out var marine) ||
                !TryComp(marine, out CommendationReceiverComponent? receiver) ||
                receiver.LastPlayerId == null)
            {
                return;
            }
            awardedLastPlayerId = receiver.LastPlayerId;
            _commendation.GiveCommendation(actor.Value, marine.Value, args.Name, args.Message, CommendationType.Medal, args.CommendationPrototypeId);
        }
        // For gibbed marines
        else if (args.LastPlayerId != null)
        {
            var lastPlayerId = args.LastPlayerId;
            if (TryComp<MarineControlComputerComponent>(ent, out var computer) &&
                computer.GibbedMarines.FirstOrDefault(info => info.LastPlayerId == lastPlayerId) is { } info)
            {
                awardedLastPlayerId = lastPlayerId;
                // Format name with rank if available
                var receiverName = !string.IsNullOrEmpty(info.Rank) ? $"{info.Rank} {info.Name}" : info.Name;
                _commendation.GiveCommendationByLastPlayerId(actor.Value, lastPlayerId, receiverName, args.Name, args.Message, CommendationType.Medal, args.CommendationPrototypeId);
            }
        }
        else
        {
            return;
        }

        if (_net.IsClient)
            return;

        // Send messages to update UI if medals panel is open
        if (awardedLastPlayerId != null)
        {
            // Remove recommendation group
            var removeMsg = new MarineControlComputerRemoveRecommendationGroupMsg { LastPlayerId = awardedLastPlayerId };

            // Get the last added medal from commendation system
            var allEntries = _commendation.GetRoundCommendationEntries();
            var lastMedal = allEntries
                .LastOrDefault(e => e.Commendation.Type == CommendationType.Medal);

            var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
            while (computers.MoveNext(out var uid, out var comp))
            {
                if (_ui.IsUiOpen(uid, MarineControlComputerUi.MedalsPanel))
                {
                    _ui.ServerSendUiMessage(uid, MarineControlComputerUi.MedalsPanel, removeMsg);

                    // Send add message only if we found the medal
                    if (lastMedal != default)
                    {
                        var commendationId = GetCommendationId(lastMedal);
                        var canPrint = comp.CanPrintCommendations;
                        var isPrinted = comp.PrintedCommendationIds.Contains(commendationId);

                        var addMsg = new MarineControlComputerAddMedalMsg
                        {
                            MedalEntry = lastMedal,
                            CanPrint = canPrint,
                            IsPrinted = isPrinted
                        };
                        _ui.ServerSendUiMessage(uid, MarineControlComputerUi.MedalsPanel, addMsg);
                    }
                }
            }
        }

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
        // Handle messages from both Key and MedalsPanel UI keys
        if (!args.UiKey.Equals(MarineControlComputerUi.Key) &&
            !args.UiKey.Equals(MarineControlComputerUi.MedalsPanel))
            return;

        GiveMedal(ent, args.Actor);
    }

    private void OnApproveRecommendation(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerApproveRecommendationMsg args)
    {
        if (!HasComp<CommendationGiverComponent>(args.Actor))
        {
            _popup.PopupClient(Loc.GetString("rmc-medal-error-officer-only"), args.Actor, PopupType.MediumCaution);
            return;
        }

        if (_net.IsClient)
            return;

        // Copy LastPlayerId to local variable for use in lambda
        var targetLastPlayerId = args.LastPlayerId;

        // Try to find alive marine
        NetEntity? marineNetEntity = null;
        var receivers = EntityQueryEnumerator<CommendationReceiverComponent, MarineComponent>();
        while (receivers.MoveNext(out var uid, out var receiver, out _))
        {
            if (receiver.LastPlayerId == targetLastPlayerId)
            {
                marineNetEntity = GetNetEntity(uid);
                break;
            }
        }

        // If not found alive, check if it's a gibbed marine
        string? lastPlayerId = null;
        if (marineNetEntity == null)
        {
            var allGibbed = CollectGibbedMarines();
            if (allGibbed.Any(info => info.LastPlayerId == targetLastPlayerId))
            {
                lastPlayerId = targetLastPlayerId;
            }
            else
            {
                return; // Marine not found
            }
        }

        // Open medal type selection dialog (skip marine selection)
        var netActor = GetNetEntity(args.Actor);
        var evt = new MarineControlComputerMedalMarineEvent(netActor, marineNetEntity, lastPlayerId);
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnRejectRecommendation(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerRejectRecommendationMsg args)
    {
        if (!HasComp<CommendationGiverComponent>(args.Actor))
        {
            _popup.PopupClient(Loc.GetString("rmc-medal-error-officer-only"), args.Actor, PopupType.MediumCaution);
            return;
        }

        if (_net.IsClient)
            return;

        // Mark all recommendations for this marine as rejected
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            var updated = false;
            var toUpdate = new List<MarineAwardRecommendationInfo>();

            foreach (var recommendation in computer.AwardRecommendations)
            {
                if (recommendation.RecommendedLastPlayerId == args.LastPlayerId && !recommendation.IsRejected)
                {
                    toUpdate.Add(recommendation);
                }
            }

            foreach (var recommendation in toUpdate)
            {
                recommendation.IsRejected = true;
                updated = true;
            }

            if (updated)
                Dirty(uid, computer);
        }

        // Send message to remove the recommendation group from UI if medals panel is open
        var removeMsg = new MarineControlComputerRemoveRecommendationGroupMsg { LastPlayerId = args.LastPlayerId };
        computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var uid, out _))
        {
            if (_ui.IsUiOpen(uid, MarineControlComputerUi.MedalsPanel))
            {
                _ui.ServerSendUiMessage(uid, MarineControlComputerUi.MedalsPanel, removeMsg);
            }
        }
    }

    private void OnOpenMedalsPanel(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerOpenMedalsPanelMsg args)
    {
        if (!HasComp<CommendationGiverComponent>(args.Actor))
        {
            _popup.PopupClient(Loc.GetString("rmc-medal-error-officer-only"), args.Actor, PopupType.MediumCaution);
            return;
        }

        if (_net.IsClient)
            return;

        var state = BuildMedalsPanelState(ent, args.Actor);
        _ui.SetUiState(ent.Owner, MarineControlComputerUi.MedalsPanel, state);
        _ui.TryOpenUi(ent.Owner, MarineControlComputerUi.MedalsPanel, args.Actor);
    }

    protected virtual MarineMedalsPanelBuiState BuildMedalsPanelState(Entity<MarineControlComputerComponent> ent, EntityUid? viewerActor = null)
    {
        return new MarineMedalsPanelBuiState(
            new List<MarineRecommendationGroup>(),
            new List<RoundCommendationEntry>(),
            ent.Comp.CanPrintCommendations,
            ent.Comp.PrintedCommendationIds);
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
        var allGibbed = CollectGibbedMarines();
        foreach (var info in allGibbed)
        {
            if (info.LastPlayerId == string.Empty)
                continue;

            options.Add(new DialogOption(info.Name, new MarineControlComputerMedalMarineEvent(netActor, null, info.LastPlayerId)));
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

    private void OnMarineCommunicationsOpenMedalsPanel(Entity<MarineCommunicationsComputerComponent> ent, ref MarineControlComputerOpenMedalsPanelMsg args)
    {
        if (!TryComp<MarineControlComputerComponent>(ent.Owner, out var controlComp))
            return;

        OnOpenMedalsPanel(new Entity<MarineControlComputerComponent>(ent.Owner, controlComp), ref args);
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

    public bool TryAddAwardRecommendation(MarineAwardRecommendationInfo recommendation)
    {
        var added = false;
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            added = true;

            if (computer.AwardRecommendations.Add(recommendation))
                Dirty(uid, computer);
        }

        return added;
    }

    public HashSet<GibbedMarineInfo> CollectGibbedMarines()
    {
        var result = new HashSet<GibbedMarineInfo>();
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out _, out var computer))
        {
            result.UnionWith(computer.GibbedMarines);
        }

        return result;
    }

    public bool TryGetGibbedMarineInfo(string playerId, out GibbedMarineInfo info)
    {
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out _, out var computer))
        {
            if (computer.GibbedMarines.FirstOrDefault(info => info.LastPlayerId == playerId) is { } match)
            {
                info = match;
                return true;
            }
        }

        info = default!;
        return false;
    }

    protected static string GetCommendationId(RoundCommendationEntry entry)
    {
        var commendation = entry.Commendation;
        return $"{commendation.Receiver}|{commendation.Name}|{commendation.Round}|{commendation.Text}"; // Improvised hash
    }
}
