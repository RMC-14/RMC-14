using Content.Server._RMC14.Rules;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Roles.Jobs;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
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
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private int _characterLimit = 1000;

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

    private void OnMapInit(Entity<MarineCommunicationsComputerComponent> computer, ref MapInitEvent args)
    {
        UpdatePlanetMap(computer);
    }

    private void OnBUIOpened(Entity<MarineCommunicationsComputerComponent> computer, ref BoundUIOpenedEvent args)
    {
        UpdatePlanetMap(computer);
    }

    private void OnMarineCommunicationsComputerMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsComputerMsg args)
    {
        _ui.CloseUi(ent.Owner, MarineCommunicationsComputerUI.Key);

        var time = _timing.CurTime;
        if (_timing.CurTime < ent.Comp.LastAnnouncement + ent.Comp.Cooldown)
        {
            // TODO RMC14 localize
            _popup.PopupClient($"Please allow at least {(int) ent.Comp.Cooldown.TotalSeconds} seconds to pass between announcements", args.Actor);
            return;
        }

        var text = args.Text;
        if (text.Length > _characterLimit)
            text = text[.._characterLimit].Trim();

        Announce(args.Actor, text, ent.Comp.Sound);

        ent.Comp.LastAnnouncement = time;
        Dirty(ent);
    }

    private void OnMarineCommunicationsDesignatePrimaryLZMsg(Entity<MarineCommunicationsComputerComponent> computer, ref MarineCommunicationsDesignatePrimaryLZMsg args)
    {
        var user = args.Actor;
        if (!TryGetEntity(args.LZ, out var lz))
        {
            Log.Warning($"{ToPrettyString(user)} tried to designate invalid entity {args.LZ} as primary LZ!");
            return;
        }

        _dropship.TryDesignatePrimaryLZ(user, lz.Value, computer.Comp.Sound);
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

    public void Announce(EntityUid sender, string message, SoundSpecifier sound)
    {
        // TODO RMC14 localize this
        // TODO RMC14 rank
        var job = string.Empty;
        if (_mind.TryGetMind(sender, out var mindId, out _) &&
            _job.MindTryGetJobName(mindId, out var jobName))
        {
            job = jobName;
        }

        var name = Name(sender);
        var wrappedMessage =
            $"[font size=14][bold][color=white]Command Announcement[/color][/bold][/font]\n[font size=12][color=red]\n{message}\n\nSigned by,\n{job} {name}[/color][/font]";

        // TODO RMC14 receivers
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e =>
                HasComp<MarineComponent>(e) ||
                HasComp<GhostComponent>(e)
            );
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, default, false, true, null);
        _audio.PlayGlobal(sound, filter, true, AudioParams.Default.WithVolume(-2f));
        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced message: {message}");
    }

    public override void AnnounceRadio(EntityUid sender, string message, ProtoId<RadioChannelPrototype> channel)
    {
        base.AnnounceRadio(sender, message, channel);

        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced radio message: {message}");
        _radio.SendRadioMessage(sender, message, channel, sender);
    }

    public override void AnnounceARES(EntityUid? source, string message, SoundSpecifier sound)
    {
        base.AnnounceARES(source, message, sound);

        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(source):player} ARES announced message: {message}");

        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e => HasComp<MarineComponent>(e) || HasComp<GhostComponent>(e));
        var headerText = "[color=white][font size=16][bold]ARES v3.2 Operation Staging Order[/bold][/font][/color]\n\n";
        var wrapped = FormattedMessage.EscapeText(message);
        message = $"{headerText}[color=red][font size=14][bold]{wrapped}[/bold][/font][/color]\n";
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, message, default, false, true, null);

        _audio.PlayGlobal(sound, filter, true, AudioParams.Default.WithVolume(-2f));
    }
}
