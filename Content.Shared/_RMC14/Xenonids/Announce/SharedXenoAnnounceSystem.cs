using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Coordinates;
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
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAnnounceDeathComponent, MobStateChangedEvent>(OnAnnounceDeathMobStateChanged);

        SubscribeLocalEvent<XenoResinHoleComponent, DestructionEventArgs>(OnResinHoleDestruction);
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
            AnnounceSameHive(ent.Owner, Loc.GetString(ent.Comp.Message, ("xeno", ent.Owner), ("location", locationName)), color: ent.Comp.Color);
    }

    private void OnResinHoleDestruction(Entity<XenoResinHoleComponent> ent, ref DestructionEventArgs args)
    {
        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        var locationName = "Unknown";

        if (_areas.TryGetArea(_transform.GetMoverCoordinates(ent), out var areaProto, out _))
            locationName = areaProto.Name;

        if (TryComp(ent.Owner, out DamageableComponent? damageComp))
        {
            var totalDamage = damageComp.TotalDamage;

            if (!damageComp.DamagePerGroup.TryGetValue("Burn", out var burnDamage))
            {
                return;
            }

            var id = burnDamage / totalDamage > 0.5
                ? "cm-xeno-construction-resin-hole-burned-down"
                : "cm-xeno-construction-resin-hole-destroyed";
            var msg = Loc.GetString(id, ("location", locationName));
            AnnounceToHive(ent.Owner, hive, msg);
        }
    }

    private void OnResinHoleActivation(Entity<XenoResinHoleComponent> ent, ref XenoResinHoleActivationEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        var locationName = "Unknown";

        if (_areas.TryGetArea(_transform.GetMoverCoordinates(ent), out var areaProto, out _))
            locationName = areaProto.Name;

        var msg = Loc.GetString(args.LocMsg, ("location", locationName));
        AnnounceToHive(ent.Owner, hive, msg);
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
        var filter = Filter.Empty().AddWhereAttachedEntity(e => _hive.IsMember(e, hive));
        Announce(source, filter, message, WrapHive(message, color), sound, popup);
    }

    public void AnnounceSameHive(Entity<HiveMemberComponent?> xeno,
        string message,
        SoundSpecifier? sound = null,
        PopupType? popup = null,
        Color? color = null)
    {
        if (_hive.GetHive(xeno) is not {} hive)
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
