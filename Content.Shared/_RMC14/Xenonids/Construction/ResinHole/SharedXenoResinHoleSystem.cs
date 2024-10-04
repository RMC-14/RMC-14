using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

public abstract partial class SharedXenoResinHoleSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly CMHandsSystem _rmcHands = default!;
    [Dependency] protected readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] protected readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _announce = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoResinHoleComponent, InteractUsingEvent>(OnPlaceParasiteInXenoResinHole);
        SubscribeLocalEvent<XenoResinHoleComponent, ActivateInWorldEvent>(OnActivateInWorldResinHole);

        SubscribeLocalEvent<XenoResinHoleComponent, XenoResinHoleActivationEvent>(OnResinHoleActivation);
    }

    protected bool CanPlaceInHole(EntityUid uid, Entity<XenoResinHoleComponent> resinHole, EntityUid user)
    {
        if (!HasComp<XenoParasiteComponent>(uid) ||
            _mobState.IsDead(uid))
        {
            return false;
        }

        if (resinHole.Comp.TrapPrototype is not null)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-construction-resin-hole-full"), resinHole, user);
            return false;
        }

        if (!_rmcHands.IsPickupByAllowed(uid, user))
            return false;

        if (!HasComp<ParasiteAIComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-awake-child", ("parasite", uid)), user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }
    private void OnPlaceParasiteInXenoResinHole(Entity<XenoResinHoleComponent> resinHole, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (!CanPlaceInHole(args.Used, resinHole, args.User))
            return;

        var ev = new XenoPlaceParasiteInHoleDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, resinHole.Comp.AddParasiteDelay, ev, resinHole, resinHole, args.Used)
        {
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-filling-parasite"), resinHole, args.User);

    }

    private void OnActivateInWorldResinHole(Entity<XenoResinHoleComponent> resinHole, ref ActivateInWorldEvent args)
    {
        if (!HasComp<XenoParasiteComponent>(args.User))
            return;

        args.Handled = true;

        if (_mobState.IsDead(args.User))
            return;

        if (_net.IsClient)
            return;

        if (resinHole.Comp.TrapPrototype != null)
            return;

        resinHole.Comp.TrapPrototype = XenoResinHoleComponent.ParasitePrototype;
        Dirty(resinHole);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-enter-parasite", ("parasite", args.User)), resinHole);
        // Yes, parasites claim any resin traps as their own
        _hive.SetSameHive(args.User, resinHole.Owner);
        QueueDel(args.User);

        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Parasite);
    }

    private void OnResinHoleActivation(Entity<XenoResinHoleComponent> ent, ref XenoResinHoleActivationEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        var locationName = "Unknown";

        if (_areas.TryGetArea(_transform.GetMoverCoordinates(ent), out var area))
            locationName = Name(area);

        var msg = Loc.GetString(args.message, ("location", locationName), ("type", GetTrapTypeName(ent)));
        _announce.AnnounceToHive(ent.Owner, hive, msg, color: ent.Comp.MessageColor);
    }

    public string GetTrapTypeName(Entity<XenoResinHoleComponent> resinHole)
    {
        switch(resinHole.Comp.TrapPrototype)
        {
            case XenoResinHoleComponent.ParasitePrototype:
                return Loc.GetString("rmc-xeno-construction-resin-hole-parasite-name");
            case XenoResinHoleComponent.AcidGasPrototype:
            case XenoResinHoleComponent.NeuroGasPrototype:
                return Loc.GetString("rmc-xeno-construction-resin-hole-gas-name");
            case XenoResinHoleComponent.WeakAcidPrototype:
            case XenoResinHoleComponent.AcidPrototype:
            case XenoResinHoleComponent.StrongAcidPrototype:
                return Loc.GetString("rmc-xeno-construction-resin-hole-acid-name");
            default:
                return Loc.GetString("rmc-xeno-construction-resin-hole-empty-name");
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class XenoResinHoleActivationEvent : EntityEventArgs
{
    public LocId message;

    public XenoResinHoleActivationEvent(LocId msg)
    {
        message = msg;
    }
}

[Serializable, NetSerializable]
public sealed partial class XenoPlaceParasiteInHoleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class XenoPlaceFluidInHoleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId ResinHolePrototype;
    public int PlasmaCost;

    public XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent(EntProtoId resinHolePrototype, int plasmaCost)
    {
        ResinHolePrototype = resinHolePrototype;
        PlasmaCost = plasmaCost;
    }
}
