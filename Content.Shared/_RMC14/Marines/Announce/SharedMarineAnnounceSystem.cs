using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Announce;

public abstract class SharedMarineAnnounceSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedMarineControlComputerSystem _marineControlComputer = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRankSystem _rankSystem = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private int _characterLimit = 1000;

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, EchoSquadReasonEvent>(OnEchoSquadReason);
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, EchoSquadConfirmEvent>(OnEchoSquadConfirm);

        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineCommunicationsComputerMsg>(OnMarineCommunicationsComputerMsg);
                subs.Event<MarineCommunicationsOpenMapMsg>(OnMarineCommunicationsOpenMapMsg);
                subs.Event<MarineCommunicationsEchoSquadMsg>(OnMarineCommunicationsEchoMsg);
                subs.Event<MarineCommunicationsOverwatchMsg>(OnMarineCommunicationsOverwatchMsg);
                subs.Event<MarineControlComputerMedalMsg>(OnMarineCommunicationsMedalMsg);
            });

        Subs.CVar(_config, CCVars.ChatMaxMessageLength, limit => _characterLimit = limit, true);
    }

    private void OnEchoSquadReason(Entity<MarineCommunicationsComputerComponent> ent, ref EchoSquadReasonEvent args)
    {
        if (!ent.Comp.CanCreateEcho)
            return;

        if (!TryGetEntity(args.User, out var user))
            return;

        var ev = new EchoSquadConfirmEvent(args.User, args.Message);
        _dialog.OpenConfirmation(
            ent,
            user.Value,
            "Confirm Activation",
            $"Confirm activation of Echo Squad for {args.Message}",
            ev
        );
    }

    private void OnEchoSquadConfirm(Entity<MarineCommunicationsComputerComponent> ent, ref EchoSquadConfirmEvent args)
    {
        if (!ent.Comp.CanCreateEcho)
            return;

        if (!TryGetEntity(args.User, out var user))
            return;

        ent.Comp.CanCreateEcho = false;
        Dirty(ent);

        if (_squad.HasSquad(SquadSystem.EchoSquadId))
            return;

        _squad.TryEnsureSquad(SquadSystem.EchoSquadId, out _);
        _adminLog.Add(LogType.RMCSquadCreated, $"Echo squad was created by {ToPrettyString(user)} with reason {args.Message}");
    }

    private void OnMarineCommunicationsComputerMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsComputerMsg args)
    {
        if (!_skills.HasSkill(args.Actor, ent.Comp.AnnounceSkill, ent.Comp.AnnounceSkillLevel))
        {
            _popup.PopupClient(Loc.GetString("rmc-skills-no-training", ("target", ent)), args.Actor, PopupType.MediumCaution);
            return;
        }

        var time = _timing.CurTime;
        if (_timing.CurTime < ent.Comp.LastAnnouncement + ent.Comp.Cooldown)
        {
            var cooldownMessage = Loc.GetString("rmc-announcement-cooldown", ("seconds", (int) ent.Comp.Cooldown.TotalSeconds));
            _popup.PopupClient(cooldownMessage, args.Actor, PopupType.SmallCaution);
            return;
        }

        _ui.CloseUi(ent.Owner, MarineCommunicationsComputerUI.Key);
        var text = args.Text;
        if (text.Length > _characterLimit)
            text = text[.._characterLimit].Trim();

        AnnounceSigned(args.Actor, text, name: ent.Comp.AnnounceName);

        ent.Comp.LastAnnouncement = time;
        Dirty(ent);
    }

    private void OnMarineCommunicationsOpenMapMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsOpenMapMsg args)
    {
        _ui.TryOpenUi(ent.Owner, TacticalMapComputerUi.Key, args.Actor);
    }

    private void OnMarineCommunicationsEchoMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsEchoSquadMsg args)
    {
        if (!ent.Comp.CanCreateEcho)
            return;

        if (_squad.HasSquad(SquadSystem.EchoSquadId))
            return;

        var ev = new EchoSquadReasonEvent(GetNetEntity(args.Actor));
        _dialog.OpenInput(ent, args.Actor, "What is the purpose of Echo Squad?", ev);
    }

    private void OnMarineCommunicationsOverwatchMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsOverwatchMsg args)
    {
        if (!_skills.HasSkill(args.Actor, ent.Comp.OverwatchSkill, ent.Comp.OverwatchSkillLevel))
        {
            _popup.PopupClient("You are not trained in overwatch!", args.Actor, PopupType.LargeCaution);
            return;
        }

        _ui.TryOpenUi(ent.Owner, OverwatchConsoleUI.Key, args.Actor);
    }

    private void OnMarineCommunicationsMedalMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineControlComputerMedalMsg args)
    {
        if (!ent.Comp.CanGiveMedals)
            return;

        _marineControlComputer.GiveMedal(ent, args.Actor);
    }

    public virtual void AnnounceRadio(
        EntityUid sender,
        string message,
        ProtoId<RadioChannelPrototype> channel)
    {
    }

    public virtual void AnnounceARESStaging(
        EntityUid? source,
        string message,
        SoundSpecifier? sound = null,
        LocId? announcement = null)
    {
    }

    public void AnnounceARES(
        EntityUid? source,
        string message,
        SoundSpecifier? sound = null)
    {
        AnnounceARESStaging(source, message, sound, "rmc-announcement-ares-command");
    }

    public virtual void AnnounceSquad(
        string message,
        EntProtoId<SquadTeamComponent> squad,
        SoundSpecifier? sound = null)
    {
    }

    public virtual void AnnounceSquad(
        string message,
        EntityUid squad,
        SoundSpecifier? sound = null)
    {
    }

    public virtual void AnnounceSingle(
        string message,
        EntityUid receiver,
        SoundSpecifier? sound = null)
    {
    }

    /// <summary>
    /// Dispatches already wrapped announcement to Marines.
    /// </summary>
    /// <param name="message">The content of the announcement.</param>
    /// <param name="sound">GlobalSound for announcement.</param>
    public virtual void AnnounceToMarines(
        string message,
        SoundSpecifier? sound = null)
    {
    }

    /// <summary>
    /// Dispatches an unsigned announcement to Marines.
    /// </summary>
    /// <param name="message">The content of the announcement.</param>
    /// <param name="author">The author of the message, UNMC High Command by default.</param>
    /// <param name="sound">GlobalSound for announcement.</param>
    public virtual void AnnounceHighCommand(
        string message,
        string? author = null,
        SoundSpecifier? sound = null)
    {
    }

    /// <summary>
    /// Dispatches a signed announcement to Marines.
    /// </summary>
    /// <param name="sender">EntityUid of sender, for job and name params.</param>
    /// <param name="message">The content of the announcement.</param>
    /// <param name="author">The author of the message, Command by default.</param>
    /// <param name="name">The name to sign the message with, defaults to the name of <see cref="author"/>.</param>
    /// <param name="sound">GlobalSound for announcement.</param>
    public void AnnounceSigned(
        EntityUid sender,
        string message,
        string? author = null,
        string? name = null,
        SoundSpecifier? sound = null)
    {
        if (_net.IsClient)
            return;

        author ??= Loc.GetString("rmc-announcement-author"); // Get "Command" fluent string if author==null
        name ??= _rankSystem.GetSpeakerFullRankName(sender) ?? Name(sender);
        var wrappedMessage = Loc.GetString("rmc-announcement-message-signed", ("author", author), ("message", message), ("name", name));

        // TODO RMC14 receivers
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e =>
                HasComp<MarineComponent>(e) ||
                HasComp<GhostComponent>(e)
            );

        AnnounceToMarines(wrappedMessage);
        _adminLog.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced message: {message}");
    }

    public string FormatHighCommand(string? author, string message)
    {
        author ??= Loc.GetString("rmc-announcement-author-highcommand");
        return Loc.GetString("rmc-announcement-message", ("author", author), ("message", message));
    }

    public string FormatARESStaging(LocId? author, string message)
    {
        author ??= "rmc-announcement-ares-message";
        return Loc.GetString(author, ("message", FormattedMessage.EscapeText(message)));
    }

    public string FormatARES(string message)
    {
        return FormatARESStaging("rmc-announcement-ares-command", message);
    }
}
