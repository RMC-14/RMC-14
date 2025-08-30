using Content.Server._RMC14.Rules;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Radio;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines;

public sealed class MarineAnnounceSystem : SharedMarineAnnounceSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public readonly SoundSpecifier DefaultAnnouncementSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg");
    public readonly SoundSpecifier DefaultSquadSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/tech_notification.ogg");
    public readonly SoundSpecifier AresAnnouncementSound = new SoundPathSpecifier("/Audio/_RMC14/AI/announce.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineCommunicationsComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);

        SubscribeLocalEvent<RMCPlanetComponent, RMCPlanetAddedEvent>(OnPlanetAdded);

        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineCommunicationsDesignatePrimaryLZMsg>(OnMarineCommunicationsDesignatePrimaryLZMsg);
            });
    }

    private void OnMapInit(Entity<MarineCommunicationsComputerComponent> computer, ref MapInitEvent args)
    {
        UpdatePlanetMap(computer);
    }

    private void OnBUIOpened(Entity<MarineCommunicationsComputerComponent> computer, ref BoundUIOpenedEvent args)
    {
        UpdatePlanetMap(computer);
    }

    private void OnPlanetAdded(Entity<RMCPlanetComponent> ent, ref RMCPlanetAddedEvent args)
    {
        var computers = EntityQueryEnumerator<MarineCommunicationsComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            UpdatePlanetMap((uid, computer));
        }
    }

    private void OnMarineCommunicationsDesignatePrimaryLZMsg(
        Entity<MarineCommunicationsComputerComponent> computer,
        ref MarineCommunicationsDesignatePrimaryLZMsg args)
    {
        var user = args.Actor;
        if (!TryGetEntity(args.LZ, out var lz))
        {
            Log.Warning($"{ToPrettyString(user)} tried to designate invalid entity {args.LZ} as primary LZ!");
            return;
        }

        _dropship.TryDesignatePrimaryLZ(user, lz.Value);
    }

    private void UpdatePlanetMap(Entity<MarineCommunicationsComputerComponent> computer)
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
    public override void AnnounceToMarines(
        string message,
        SoundSpecifier? sound = null)
    {
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e =>
                HasComp<MarineComponent>(e) ||
                HasComp<GhostComponent>(e)
            );

        filter.RemoveWhereAttachedEntity(HasComp<RMCSurvivorComponent>);

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, message, default, false, true, null);
        _audio.PlayGlobal(sound ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
    }

    public override void AnnounceHighCommand(
        string message,
        string? author = null,
        SoundSpecifier? sound = null)
    {
        var wrappedMessage = FormatHighCommand(author, message);
        AnnounceToMarines(wrappedMessage);
    }

    public override void AnnounceRadio(
        EntityUid sender,
        string message,
        ProtoId<RadioChannelPrototype> channel)
    {
        base.AnnounceRadio(sender, message, channel);

        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced radio message: {message}");
        _radio.SendRadioMessage(sender, message, channel, sender);
    }

    public override void AnnounceARESStaging(
        EntityUid? source,
        string message,
        SoundSpecifier? sound = null,
        LocId? announcement = null)
    {
        base.AnnounceARESStaging(source, message, sound, announcement);

        message = FormatARESStaging(announcement, message);

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

    public override void AnnounceSquad(string message, EntityUid squad, SoundSpecifier? sound = null)
    {
        base.AnnounceSquad(message, squad, sound);

        var filter = Filter.Empty().AddWhereAttachedEntity(e => _squad.IsInSquad(e, squad));

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, message, default, false, true, null);
        _audio.PlayGlobal(sound ?? DefaultSquadSound, filter, true, AudioParams.Default.WithVolume(-2f));
    }

    public override void AnnounceSingle(string message, EntityUid receiver, SoundSpecifier? sound = null)
    {
        base.AnnounceSingle(message, receiver, sound);

        if (TryComp(receiver, out ActorComponent? actor))
            _chatManager.ChatMessageToOne(ChatChannel.Radio, message, message, default, false, actor.PlayerSession.Channel);

        _audio.PlayEntity(sound, receiver, receiver, AudioParams.Default.WithVolume(-2f));
    }
}
