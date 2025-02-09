using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Announce;

public abstract class SharedMarineAnnounceSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedMarineControlComputerSystem _marineControlComputer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, EchoSquadReasonEvent>(OnEchoSquadReason);
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, EchoSquadConfirmEvent>(OnEchoSquadConfirm);

        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineCommunicationsOpenMapMsg>(OnMarineCommunicationsOpenMapMsg);
                subs.Event<MarineCommunicationsEchoSquadMsg>(OnMarineCommunicationsEchoMsg);
                subs.Event<MarineCommunicationsOverwatchMsg>(OnMarineCommunicationsOverwatchMsg);
                subs.Event<MarineControlComputerMedalMsg>(OnMarineCommunicationsMedalMsg);
            });
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
        ProtoId<RadioChannelPrototype> channel
        )
    {
    }

    public virtual void AnnounceARES(
        EntityUid? source,
        string message,
        SoundSpecifier? sound = null,
        LocId? announcement = null
        )
    {
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
        SoundSpecifier? sound = null
    )
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
        SoundSpecifier? sound = null
    )
    {
    }
}
