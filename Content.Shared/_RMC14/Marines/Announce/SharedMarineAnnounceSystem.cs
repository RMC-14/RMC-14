using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Announce;

public abstract class SharedMarineAnnounceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineCommunicationsOpenMapMsg>(OnMarineCommunicationsOpenMapMsg);
                subs.Event<MarineCommunicationsOverwatchMsg>(OnMarineCommunicationsOverwatchMsg);
            });
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
