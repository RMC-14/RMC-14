using Content.Server._RMC14.Rules;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Radio.EntitySystems;
using Content.Server.Roles.Jobs;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Survivor;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines;

public sealed class MarineAnnounceSystem : SharedMarineAnnounceSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private int _characterLimit = 1000;
    public readonly SoundSpecifier DefaultAnnouncementSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg");
    public readonly SoundSpecifier DefaultSquadSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/tech_notification.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineCommunicationsComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);

        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineCommunicationsComputerMsg>(OnMarineCommunicationsComputerMsg);
                subs.Event<MarineCommunicationsDesignatePrimaryLZMsg>(OnMarineCommunicationsDesignatePrimaryLZMsg);
            });

        Subs.CVar(_config, CCVars.ChatMaxMessageLength, limit => _characterLimit = limit, true);
    }

    private void OnMapInit(
        Entity<MarineCommunicationsComputerComponent> computer,
        ref MapInitEvent args
        )
    {
        UpdatePlanetMap(computer);
    }

    private void OnBUIOpened(
        Entity<MarineCommunicationsComputerComponent> computer,
        ref BoundUIOpenedEvent args
        )
    {
        UpdatePlanetMap(computer);
    }

    private void OnMarineCommunicationsComputerMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsComputerMsg args)
    {
        _ui.CloseUi(ent.Owner, MarineCommunicationsComputerUI.Key);

        var time = _timing.CurTime;
        if (_timing.CurTime < ent.Comp.LastAnnouncement + ent.Comp.Cooldown)
        {
            var cooldownMessage = Loc.GetString("rmc-announcement-cooldown", ("seconds", (int) ent.Comp.Cooldown.TotalSeconds));
            _popup.PopupClient(cooldownMessage, args.Actor);
            return;
        }

        var text = args.Text;
        if (text.Length > _characterLimit)
            text = text[.._characterLimit].Trim();

        AnnounceSigned(args.Actor, text);

        ent.Comp.LastAnnouncement = time;
        Dirty(ent);
    }

    private void OnMarineCommunicationsDesignatePrimaryLZMsg(
        Entity<MarineCommunicationsComputerComponent> computer,
        ref MarineCommunicationsDesignatePrimaryLZMsg args
        )
    {
        var user = args.Actor;
        if (!TryGetEntity(args.LZ, out var lz))
        {
            Log.Warning($"{ToPrettyString(user)} tried to designate invalid entity {args.LZ} as primary LZ!");
            return;
        }

        _dropship.TryDesignatePrimaryLZ(user, lz.Value);
    }

    private void UpdatePlanetMap(
        Entity<MarineCommunicationsComputerComponent> computer
        )
    {
        var planet = _distressSignal.SelectedPlanetMapName ?? string.Empty;
        var operation = _distressSignal.OperationName ?? string.Empty;
        var landingZones = new List<LandingZone>();

        foreach (var (id, metaData) in _dropship.GetPrimaryLZCandidates())
        {
            landingZones.Add(new LandingZone(GetNetEntity(id), metaData.EntityName));
        }

        landingZones.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        var state = new MarineCommunicationsComputerBuiState(planet, operation, landingZones);
        _ui.SetUiState(computer.Owner, MarineCommunicationsComputerUI.Key, state);
    }


    /// <summary>
    /// Dispatches already wrapped announcement to Marines.
    /// </summary>
    /// <param name="message">The content of the announcement.</param>
    /// <param name="sound">GlobalSound for announcement.</param>
    public void AnnounceToMarines(
        string message,
        SoundSpecifier? sound = null
        )
    {
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e =>
                HasComp<MarineComponent>(e) ||
                HasComp<GhostComponent>(e)
            );

        filter.RemoveWhereAttachedEntity(HasComp<SurvivorComponent>);

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, message, default, false, true, null);
        _audio.PlayGlobal(sound ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
    }

    /// <summary>
    /// Dispatches an unsigned announcement to Marines.
    /// </summary>
    /// <param name="message">The content of the announcement.</param>
    /// <param name="author">The author of the message, UNMC High Command by default.</param>
    /// <param name="sound">GlobalSound for announcement.</param>
    public void AnnounceHighCommand(
        string message,
        string? author = null,
        SoundSpecifier? sound = null
        )
    {
        author ??= Loc.GetString("rmc-announcement-author-highcommand");
        var wrappedMessage = Loc.GetString("rmc-announcement-message", ("author", author), ("message", message));

        AnnounceToMarines(wrappedMessage);
    }

    /// <summary>
    /// Dispatches a signed announcement to Marines.
    /// </summary>
    /// <param name="sender">EntityUid of sender, for job and name params.</param>
    /// <param name="message">The content of the announcement.</param>
    /// <param name="author">The author of the message, Command by default.</param>
    /// <param name="sound">GlobalSound for announcement.</param>
    public void AnnounceSigned(
        EntityUid sender,
        string message,
        string? author = null,
        SoundSpecifier? sound = null
        )
    {
        // TODO RMC14 rank
        var job = string.Empty;
        if (_mind.TryGetMind(sender, out var mindId, out _) &&
            _job.MindTryGetJobName(mindId, out var jobName))
        {
            job = jobName;
        }

        author ??= Loc.GetString("rmc-announcement-author"); // Get "Command" fluent string if author==null
        var name = Name(sender);
        var wrappedMessage = Loc.GetString("rmc-announcement-message-signed", ("author", author), ("message", message), ("job", job), ("name", name));

        // TODO RMC14 receivers
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e =>
                HasComp<MarineComponent>(e) ||
                HasComp<GhostComponent>(e)
            );

        AnnounceToMarines(wrappedMessage);
        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced message: {message}");
    }

    public override void AnnounceRadio(
        EntityUid sender,
        string message,
        ProtoId<RadioChannelPrototype> channel
        )
    {
        base.AnnounceRadio(sender, message, channel);

        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced radio message: {message}");
        _radio.SendRadioMessage(sender, message, channel, sender);
    }

    public override void AnnounceARES(
        EntityUid? source,
        string message,
        SoundSpecifier? sound = null,
        LocId? announcement = null
        )
    {
        base.AnnounceARES(source, message, sound, announcement);

        announcement ??= "rmc-announcement-ares-message";
        message = Loc.GetString(announcement, ("message", FormattedMessage.EscapeText(message)));

        AnnounceToMarines(message, sound);
        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(source):player} ARES announced message: {message}");
    }

    public override void AnnounceSquad(string message, EntProtoId<SquadTeamComponent> squad, SoundSpecifier? sound = null)
    {
        base.AnnounceSquad(message, squad, sound);

        var filter = Filter.Empty().AddWhereAttachedEntity(e => _squad.IsInSquad(e, squad));

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, message, default, false, true, null);
        _audio.PlayGlobal(sound ?? DefaultSquadSound, filter, true, AudioParams.Default.WithVolume(-2f));
    }
}
