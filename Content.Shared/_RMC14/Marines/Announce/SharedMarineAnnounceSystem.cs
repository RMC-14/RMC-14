using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Marines.Announce;

public abstract class SharedMarineAnnounceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, ActivatableUIOpenAttemptEvent>(OnMarineCommunicationsComputerOpenAttempt);

        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineCommunicationsOpenMapMsg>(OnMarineCommunicationsOpenMapMsg);
                subs.Event<MarineCommunicationsOverwatchMsg>(OnMarineCommunicationsOverwatchMsg);
            });
    }

    private void OnMarineCommunicationsComputerOpenAttempt(
        Entity<MarineCommunicationsComputerComponent> ent,
        ref ActivatableUIOpenAttemptEvent args
        )
    {
        if (args.Cancelled)
            return;

        if (_timing.CurTime < ent.Comp.LastAnnouncement + ent.Comp.Cooldown)
        {
            // TODO RMC14 localize
            _popup.PopupClient($"Please allow at least {(int) ent.Comp.Cooldown.TotalSeconds} seconds to pass between announcements", args.User);
            args.Cancel();
        }
    }

    private void OnMarineCommunicationsOpenMapMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsOpenMapMsg args)
    {
        _ui.TryOpenUi(ent.Owner, TacticalMapComputerUi.Key, args.Actor);
    }

    private void OnMarineCommunicationsOverwatchMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsOverwatchMsg args)
    {
        if (!_skills.HasSkill(args.Actor, ent.Comp.OverwatchSkill, ent.Comp.OverwatchSkillLevel))
        {
            _popup.PopupCursor("You are not trained in overwatch!", args.Actor, PopupType.LargeCaution);
            return;
        }

        _ui.TryOpenUi(ent.Owner, OverwatchConsoleUI.Key, args.Actor);
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
}
