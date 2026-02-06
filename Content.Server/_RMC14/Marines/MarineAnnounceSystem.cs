using Content.Server._RMC14.Comms;
using Content.Server._RMC14.Rules;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Shared._RMC14.Comms;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Prototypes;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Marines;

public sealed class MarineAnnounceSystem : SharedMarineAnnounceSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly CommsEncryptionSystem _encryption = default!;

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

    public override void AnnounceToMarines(
        string message,
        SoundSpecifier? sound = null,
        Filter? filter = null,
        bool excludeSurvivors = true)
    {
        filter ??= Filter.Empty()
            .AddWhereAttachedEntity(e =>
                HasComp<MarineComponent>(e) ||
                HasComp<GhostComponent>(e)
            );

        // TODO RMC14
        if (excludeSurvivors)
            filter.RemoveWhereAttachedEntity(HasComp<RMCSurvivorComponent>);

        // Get all recipient entities
        var recipients = new List<EntityUid>();
        var marineQuery = EntityQueryEnumerator<MarineComponent>();
        while (marineQuery.MoveNext(out var uid, out _))
        {
            if (excludeSurvivors && HasComp<RMCSurvivorComponent>(uid))
                continue;
            recipients.Add(uid);
        }

        var ghostQuery = EntityQueryEnumerator<GhostComponent>();
        while (ghostQuery.MoveNext(out var uid, out _))
        {
            recipients.Add(uid);
        }

        // Find encryption component for garbling
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out _, out var encryptionComp))
        {
            // No encryption system, send clear messages to all
            foreach (var recipient in recipients)
            {
                if (TryComp(recipient, out ActorComponent? actor))
                    _chatManager.ChatMessageToOne(ChatChannel.Radio, message, message, default, false, actor.PlayerSession.Channel);
            }
            _audio.PlayGlobal(sound ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
            return;
        }

        // Send messages to each recipient, garbling based on location
        foreach (var recipient in recipients)
        {
            var recipientMessage = message;

            // Only garble for groundside recipients when encryption is active
            if (encryptionComp.IsGroundside && IsRecipientGroundside(recipient))
            {
                var garblePercent = _encryption.GetGarblePercentage(encryptionComp);
                recipientMessage = _encryption.GarbleMessage(message, garblePercent);
            }

            if (TryComp(recipient, out ActorComponent? actor))
                _chatManager.ChatMessageToOne(ChatChannel.Radio, recipientMessage, recipientMessage, default, false, actor.PlayerSession.Channel);
        }

        _audio.PlayGlobal(sound ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
    }

    private bool IsRecipientGroundside(EntityUid recipient)
    {
        // Check if recipient is on the planet map by comparing map names
        if (TryComp<TransformComponent>(recipient, out var xform) &&
            xform.MapUid is { } mapUid &&
            TryComp<MetaDataComponent>(mapUid, out var metadata))
        {
            return metadata.EntityName == _distressSignal.SelectedPlanetMapName;
        }
        return false;
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

    private string ApplyGarbling(string message)
    {
        // Find the encryption component
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out _, out var comp))
            return message;

        if (!comp.IsGroundside)
            return message;

        var garblePercent = _encryption.GetGarblePercentage(comp);
        return _encryption.GarbleMessage(message, garblePercent);
    }
}
