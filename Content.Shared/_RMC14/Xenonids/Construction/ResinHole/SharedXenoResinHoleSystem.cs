using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.ShakeStun;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

public abstract partial class SharedXenoResinHoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoPlaceResinHoleActionEvent>(OnPlaceXenoResinHole);
        SubscribeLocalEvent<XenoComponent, XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent>(OnCompleteRemoveWeedSource);

        SubscribeLocalEvent<XenoResinHoleComponent, InteractUsingEvent>(OnPlaceParasiteInXenoResinHole);
        SubscribeLocalEvent<XenoResinHoleComponent, XenoPlaceParasiteInHoleDoAfterEvent>(OnPlaceParasiteInXenoResinHoleDoAfter);

        SubscribeLocalEvent<XenoResinHoleComponent, InteractHandEvent>(OnEmptyHandInteract);

        SubscribeLocalEvent<XenoResinHoleComponent, DamageChangedEvent>(OnXenoResinHoleTakeDamage);
        SubscribeLocalEvent<XenoResinHoleComponent, StepTriggerAttemptEvent>(OnXenoResinHoleStepTriggerAttempt);
        SubscribeLocalEvent<XenoResinHoleComponent, StepTriggeredOffEvent>(OnXenoResinHoleStepTriggered);
        // TODO: For Non-parasite traps, XenoResinHole should activate if walked by in a radius of 1 tile away

    }

    private void OnPlaceXenoResinHole(Entity<XenoComponent> xeno, ref XenoPlaceResinHoleActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var location = _transform.GetMoverCoordinates(xeno).SnapToGrid(_entities);
        if (!CanPlaceResinHole(args.Performer, location))
        {
            return;
        }

        if (_transform.GetGrid(location) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
        {
            return;
        }

        if (_xenoWeeds.GetWeedsOnFloor((gridId, grid), location, true) is EntityUid weeds)
        {
            var ev = new XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent(args.Prototype);
            var doAfterArgs = new DoAfterArgs(_entities, xeno.Owner, args.DestroyWeedSourceDelay, ev, xeno.Owner, weeds)
            {
                BlockDuplicate = true,
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameTarget
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            return;
        }
        PlaceResinHole(xeno.Owner, location, args.Prototype);
    }

    private void OnCompleteRemoveWeedSource(Entity<XenoComponent> xeno, ref XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        if (!_net.IsServer)
        {
            return;
        }

        if (args.Target is null)
        {
            return;
        }

        var location = _transform.GetMoverCoordinates(xeno).SnapToGrid(_entities);
        PlaceResinHole(xeno.Owner, location, args.Prototype);
        QueueDel(args.Target);
    }

    private void OnPlaceParasiteInXenoResinHole(Entity<XenoResinHoleComponent> resinHole, ref InteractUsingEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        if (!HasComp<XenoParasiteComponent>(args.Used) ||
            _mobState.IsDead(args.Used))
        {
            return;
        }

        var resinHoleSlot = _container.EnsureContainer<ContainerSlot>(resinHole.Owner, XenoResinHoleComponent.HoleSlotId);
        if (resinHoleSlot.ContainedEntity is not null)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-construction-resin-hole-full"), resinHole.Owner);
            return;
        }

        var ev = new XenoPlaceParasiteInHoleDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(_entities, args.User, resinHole.Comp.AddParasiteDelay, ev, resinHole.Owner, resinHole.Owner, args.Used)
        {
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnPlaceParasiteInXenoResinHoleDoAfter(Entity<XenoResinHoleComponent> resinHole, ref XenoPlaceParasiteInHoleDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        if (args.Used is null)
        {
            return;
        }

        if (!HasComp<XenoParasiteComponent>(args.Used) ||
            _mobState.IsDead(args.Used.Value))
        {
            return;
        }

        var resinHoleSlot = _container.EnsureContainer<ContainerSlot>(resinHole.Owner, XenoResinHoleComponent.HoleSlotId);
        if (resinHoleSlot.ContainedEntity is not null)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-construction-resin-hole-full"), resinHole.Owner);
            return;
        }

        _container.Insert(args.Used.Value, resinHoleSlot);
        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Parasite);
        args.Handled = true;
    }

    private void OnEmptyHandInteract(Entity<XenoResinHoleComponent> resinHole, ref InteractHandEvent args)
    {
        if (args.Handled)
        {
            return;
        }
        var resinHoleSlot = _container.EnsureContainer<ContainerSlot>(resinHole.Owner, XenoResinHoleComponent.HoleSlotId);

        if (resinHoleSlot.ContainedEntity is not EntityUid possibleParasite)
        {
            // TODO: Add boiler and praetorian logic of adding acid/gas
            return;
        }

        if (!HasComp<XenoParasiteComponent>(possibleParasite))
        {
            return;
        }

        _hands.TryPickupAnyHand(args.User, possibleParasite);
        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);
        args.Handled = true;
    }

    private void OnXenoResinHoleTakeDamage(Entity<XenoResinHoleComponent> resinHole, ref DamageChangedEvent args)
    {
        ActivateTrap(resinHole);
    }

    private void OnXenoResinHoleStepTriggerAttempt(Entity<XenoResinHoleComponent> resinHole, ref StepTriggerAttemptEvent args)
    {
        if (CanTrigger(args.Tripper))
            args.Continue = true;
    }

    private void OnXenoResinHoleStepTriggered(Entity<XenoResinHoleComponent> resinHole, ref StepTriggeredOffEvent args)
    {
        if (ActivateTrap(resinHole))
        {
            _stun.TryParalyze(args.Tripper, resinHole.Comp.StepStunDuration, true);
        }
    }

    private bool CanPlaceResinHole(EntityUid user, EntityCoordinates coords)
    {
        if (_transform.GetGrid(coords) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return false;
        }

        var tile = _map.TileIndicesFor(gridId, grid, coords);
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        var hasWeeds = false;
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<XenoEggComponent>(uid))
            {
                var msg = Loc.GetString("cm-xeno-construction-resin-hole-blocked");
                _popup.PopupClient(msg, uid.Value, user, PopupType.SmallCaution);
                return false;
            }

            if (HasComp<XenoConstructComponent>(uid) ||
                _tags.HasAnyTag(uid.Value, StructureTag, AirlockTag) ||
                HasComp<StrapComponent>(uid))
            {
                var msg = Loc.GetString("cm-xeno-construction-resin-hole-blocked");
                _popup.PopupClient(msg, uid.Value, user, PopupType.SmallCaution);
                return false;
            }

            if (HasComp<XenoWeedsComponent>(uid))
                hasWeeds = true;
        }

        var neighborAnchoredEntities = _map.GetCellsInSquareArea(gridId, grid, coords, 1);

        foreach (var entity in neighborAnchoredEntities)
        {
            if (HasComp<XenoResinHoleComponent>(entity))
            {
                var msg = Loc.GetString("cm-xeno-construction-resin-hole-too-close");
                _popup.PopupClient(msg, coords, user, PopupType.SmallCaution);
                return false;
            }
        }

        if (_turf.IsTileBlocked(gridId, tile, Impassable | MidImpassable | HighImpassable, grid))
        {
            var msg = Loc.GetString("cm-xeno-construction-resin-hole-blocked");
            _popup.PopupClient(msg, coords, user, PopupType.SmallCaution);
            return false;
        }

        if (!hasWeeds)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-construction-resin-hole-failed-must-weeds"), user, user);
            return false;
        }
        return true;
    }

    private bool CanTrigger(EntityUid user)
    {
        return !_mobState.IsDead(user) && HasComp<MarineComponent>(user);
    }

    private void PlaceResinHole(EntityUid xeno, EntityCoordinates coords, EntProtoId resinHolePrototype)
    {
        if (_net.IsClient)
        {
            return;
        }
        var resinHole = Spawn(resinHolePrototype, coords);
        //_transform.AnchorEntity(resinHole, Transform(resinHole));
        _adminLogs.Add(LogType.RMCXenoConstruct, $"Xeno {ToPrettyString(xeno):xeno} placed a resin hole at {coords}");
    }

    private bool ActivateTrap(Entity<XenoResinHoleComponent> resinHole)
    {
        var resinHoleSlot = _container.EnsureContainer<ContainerSlot>(resinHole.Owner, XenoResinHoleComponent.HoleSlotId);

        if (resinHoleSlot.ContainedEntity is not EntityUid trapEntity)
        {
            return false;
        }

        if (HasComp<XenoParasiteComponent>(trapEntity))
        {
            _transform.DropNextTo(trapEntity, resinHole.Owner);
            _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);
            return true;
        }
        // TODO: Add Activating acid/gas trap
        return false;
    }
}

[Serializable, NetSerializable]
public sealed partial class HideResinHoleOutlineMessage : EntityEventArgs
{
    public NetEntity ResinHole;

    public HideResinHoleOutlineMessage(NetEntity resinHole)
    {
        ResinHole = resinHole;
    }
}


[Serializable, NetSerializable]
public sealed partial class XenoPlaceParasiteInHoleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId Prototype;

    public XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent(EntProtoId prototype)
    {
        Prototype = prototype;
    }
}
