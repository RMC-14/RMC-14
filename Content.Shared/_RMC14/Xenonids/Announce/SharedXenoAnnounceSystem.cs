using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Bioscan;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Announce;

public abstract class SharedXenoAnnounceSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;

    [Dependency] protected readonly SharedXenoHiveSystem Hive = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAnnounceDeathComponent, MobStateChangedEvent>(OnAnnounceDeathMobStateChanged);
    }

    private void OnAnnounceDeathMobStateChanged(Entity<XenoAnnounceDeathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var locationName = "Unknown";
        if (_areas.TryGetArea(ent, out _, out var areaProto))
            locationName = areaProto.Name;

        if (HasComp<ParasiteSpentComponent>(ent))
            AnnounceSameHive(ent.Owner, Loc.GetString("rmc-xeno-parasite-announce-infect", ("xeno", ent.Owner), ("location", locationName)), color: ent.Comp.Color);
        else
        {
            if (HasComp<XenoEvolutionGranterComponent>(ent) || _xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1))
                AnnounceSameHive(ent.Owner, Loc.GetString(ent.Comp.Message, ("xeno", ent.Owner), ("location", locationName)), color: ent.Comp.Color);
        }
    }

    public string WrapHive(string message, Color? color = null)
    {
        color ??= Color.FromHex("#921992");
        return $"[color={color.Value.ToHex()}][font size=16][bold]{message}[/bold][/font][/color]\n\n";
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="source"></param>
    /// <param name="filter"></param>
    /// <param name="message">Message to send into chat</param>
    /// <param name="wrapped"></param>
    /// <param name="sound"></param>
    /// <param name="popup"></param>
    /// <param name="needsQueen">Whether the message can only be sent if the hive has an active queen</param>
    public virtual void Announce(EntityUid source,
        Filter filter,
        string message,
        string wrapped,
        SoundSpecifier? sound = null,
        PopupType? popup = null,
        bool needsQueen = false)
    {
    }

    public void AnnounceToHive(EntityUid source,
        EntityUid hive,
        string message,
        SoundSpecifier? sound = null,
        PopupType? popup = null,
        Color? color = null,
        bool needsQueen = false)
    {
        var filter = Filter.Empty().AddWhereAttachedEntity(e => Hive.IsMember(e, hive));
        Announce(source, filter, message, WrapHive(message, color), sound, popup, needsQueen);
    }

    public void AnnounceSameHive(Entity<HiveMemberComponent?> xeno,
        string message,
        SoundSpecifier? sound = null,
        PopupType? popup = null,
        Color? color = null,
        bool needsQueen = false)
    {
        if (Hive.GetHive(xeno) is not {} hive)
            return;

        AnnounceToHive(xeno, hive, message, sound, popup, color, needsQueen);
    }

    public void AnnounceAll(EntityUid source,
        string message,
        SoundSpecifier? sound = null,
        PopupType? popup = null,
        bool needsQueen = false)
    {
        Announce(
            source,
            Filter.Empty().AddWhereAttachedEntity(HasComp<XenoComponent>),
            message,
            message,
            sound,
            popup,
            needsQueen
        );
    }

    public void AnnounceQueenMother(string message)
    {
        var sound = new BioscanComponent().XenoSound;
        AnnounceAll(default, FormatQueenMother(message), sound);
    }

    public string FormatQueenMother(string message)
    {
        return $"\n[bold][color=#7575F3][font size=24]Queen Mother Psychic Directive[/font][/color][/bold]\n\n[color=red][font size=14]{message}[/font][/color]\n\n";
    }
}
