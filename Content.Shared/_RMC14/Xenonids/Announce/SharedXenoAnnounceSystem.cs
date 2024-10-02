using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Announce;

public abstract class SharedXenoAnnounceSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoResinHoleSystem _hole = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAnnounceDeathComponent, MobStateChangedEvent>(OnAnnounceDeathMobStateChanged);

        SubscribeLocalEvent<XenoResinHoleComponent, XenoResinHoleActivationEvent>(OnResinHoleActivation);

    }

    private void OnAnnounceDeathMobStateChanged(Entity<XenoAnnounceDeathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var locationName = "Unknown";
        if (_areas.TryGetArea(_transform.GetMoverCoordinates(ent), out var areaProto, out _))
            locationName = areaProto.Name;

        if (HasComp<ParasiteSpentComponent>(ent))
            AnnounceSameHive(ent.Owner, Loc.GetString("rmc-xeno-parasite-announce-infect", ("xeno", ent.Owner), ("location", locationName)), color: ent.Comp.Color);
        else
        {
            if (HasComp<XenoEvolutionGranterComponent>(ent) || _xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1))
                AnnounceSameHive(ent.Owner, Loc.GetString(ent.Comp.Message, ("xeno", ent.Owner), ("location", locationName)), color: ent.Comp.Color);
        }
    }

    private void OnResinHoleActivation(Entity<XenoResinHoleComponent> ent, ref XenoResinHoleActivationEvent args)
    {
        if (ent.Comp.Hive is null)
            return;

        var locationName = "Unknown";

        if (_areas.TryGetArea(_transform.GetMoverCoordinates(ent), out var areaProto, out _))
            locationName = areaProto.Name;

        var msg = Loc.GetString(args.message, ("location", locationName), ("type", _hole.GetTrapTypeName(ent)));
        AnnounceToHive(ent.Owner, ent.Comp.Hive.Value, msg);
    }

    public string WrapHive(string message, Color? color = null)
    {
        color ??= Color.FromHex("#921992");
        return $"[color={color.Value.ToHex()}][font size=16][bold]{message}[/bold][/font][/color]\n\n";
    }

    public virtual void Announce(EntityUid source,
        Filter filter,
        string message,
        string wrapped,
        SoundSpecifier? sound = null,
        PopupType? popup = null)
    {
    }

    public void AnnounceToHive(EntityUid source,
        EntityUid hive,
        string message,
        SoundSpecifier? sound = null,
        PopupType? popup = null,
        Color? color = null)
    {
        var filter = Filter.Empty().AddWhereAttachedEntity(e => CompOrNull<XenoComponent>(e)?.Hive == hive);
        Announce(source, filter, message, WrapHive(message, color), sound, popup);
    }

    public void AnnounceSameHive(Entity<XenoComponent?> xeno,
        string message,
        SoundSpecifier? sound = null,
        PopupType? popup = null,
        Color? color = null)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        if (xeno.Comp.Hive is not { } hive)
            return;

        AnnounceToHive(xeno, hive, message, sound, popup, color);
    }

    public void AnnounceAll(EntityUid source,
        string message,
        SoundSpecifier? sound = null,
        PopupType? popup = null)
    {
        Announce(
            source,
            Filter.Empty().AddWhereAttachedEntity(HasComp<XenoComponent>),
            message,
            message,
            sound,
            popup
        );
    }
}
