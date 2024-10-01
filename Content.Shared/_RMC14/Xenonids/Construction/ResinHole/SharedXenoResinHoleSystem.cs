using Content.Shared.Mobs.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Hands;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

public abstract partial class SharedXenoResinHoleSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly CMHandsSystem _rmcHands = default!;
    [Dependency] protected readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoResinHoleComponent, InteractUsingEvent>(OnPlaceParasiteInXenoResinHole);
        SubscribeLocalEvent<XenoResinHoleComponent, ActivateInWorldEvent>(OnActivateInWorldResinHole);
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

        if (HasComp<ParasiteAIComponent>(uid))
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

        if (!TryComp<XenoComponent>(args.User, out var xeno))
            return;

        resinHole.Comp.TrapPrototype = XenoResinHoleComponent.ParasitePrototype;
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-enter-parasite", ("parasite", args.User)), resinHole);
        resinHole.Comp.Hive = xeno.Hive; // Yes, parasites claim any resin traps as their own
        QueueDel(args.User);

        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Parasite);
    }
}

[Serializable, NetSerializable]
public sealed partial class XenoResinHoleActivationEvent : EntityEventArgs
{
    public LocId LocMsg;
    public XenoResinHoleActivationEvent(LocId locMsg)
    {
        LocMsg = locMsg;
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
