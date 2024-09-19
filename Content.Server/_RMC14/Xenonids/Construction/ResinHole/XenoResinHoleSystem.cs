using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Bombard;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
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
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Server._RMC14.Xenonids.Construction.ResinHole;

public sealed partial class XenoResinHoleSystem : SharedXenoResinHoleSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
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
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoPlaceResinHoleActionEvent>(OnPlaceXenoResinHole);
        SubscribeLocalEvent<XenoComponent, XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent>(OnCompleteRemoveWeedSource);

        SubscribeLocalEvent<XenoResinHoleComponent, XenoPlaceParasiteInHoleDoAfterEvent>(OnPlaceParasiteInXenoResinHoleDoAfter);

        SubscribeLocalEvent<XenoResinHoleComponent, InteractHandEvent>(OnEmptyHandInteract);
        SubscribeLocalEvent<XenoResinHoleComponent, XenoPlaceFluidInHoleDoAfterEvent>(OnPlaceFluidInResinHole);

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

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, args.PlasmaCost, false))
        {
            return;
        }

        if (_xenoWeeds.GetWeedsOnFloor((gridId, grid), location, true) is EntityUid weeds)
        {
            var ev = new XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent(args.Prototype, args.PlasmaCost);
            var doAfterArgs = new DoAfterArgs(_entities, xeno.Owner, args.DestroyWeedSourceDelay, ev, xeno.Owner, weeds)
            {
                BlockDuplicate = true,
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameTarget
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            return;
        }

        _xenoPlasma.TryRemovePlasma(xeno.Owner, args.PlasmaCost);
        PlaceResinHole(xeno.Owner, location, args.Prototype);
    }

    private void OnCompleteRemoveWeedSource(Entity<XenoComponent> xeno, ref XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        if (args.Target is null)
        {
            return;
        }

        var location = _transform.GetMoverCoordinates(xeno).SnapToGrid(_entities);

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, args.PlasmaCost, false))
        {
            return;
        }
        _xenoPlasma.TryRemovePlasma(xeno.Owner, args.PlasmaCost);
        PlaceResinHole(xeno.Owner, location, args.ResinHolePrototype);
        QueueDel(args.Target);
    }

    protected override void OnPlaceParasiteInXenoResinHole(Entity<XenoResinHoleComponent> resinHole, ref InteractUsingEvent args)
    {
        base.OnPlaceParasiteInXenoResinHole(resinHole, ref args);
        args.Handled = false;
        var (ent, comp) = resinHole;

        if (args.Handled)
        {
            return;
        }

        if (!HasComp<XenoParasiteComponent>(args.Used) ||
            _mobState.IsDead(args.Used))
        {
            return;
        }

        if (comp.TrapPrototype is not null)
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
        args.Handled = true;
    }

    private void OnPlaceParasiteInXenoResinHoleDoAfter(Entity<XenoResinHoleComponent> resinHole, ref XenoPlaceParasiteInHoleDoAfterEvent args)
    {
        var (ent, comp) = resinHole;

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

        if (comp.TrapPrototype is not null)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-construction-resin-hole-full"), resinHole.Owner);
            return;
        }

        comp.TrapPrototype = XenoResinHoleComponent.ParasitePrototype;
        QueueDel(args.Used);

        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Parasite);
        args.Handled = true;
    }

    private void OnEmptyHandInteract(Entity<XenoResinHoleComponent> resinHole, ref InteractHandEvent args)
    {
        var (ent, comp) = resinHole;

        if (args.Handled)
        {
            return;
        }

        if (comp.TrapPrototype is not EntProtoId containedProto)
        {
            if (TryComp(args.User, out XenoBombardComponent? bombardComp))
            {
                if (!_xenoPlasma.HasPlasmaPopup(args.User, bombardComp.PlasmaCost, false))
                {
                    return;
                }
            }

            var ev = new XenoPlaceFluidInHoleDoAfterEvent();
            var doAfterArgs = new DoAfterArgs(_entities, args.User, comp.AddFluidDelay, ev, ent)
            {
                BreakOnMove = true
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            return;
        }

        if (containedProto != XenoResinHoleComponent.ParasitePrototype)
        {
            return;
        }

        _hands.TryPickupAnyHand(args.User, Spawn(containedProto));

        comp.TrapPrototype = null;
        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);
        args.Handled = true;
    }

    private void OnPlaceFluidInResinHole(Entity<XenoResinHoleComponent> resinHole, ref XenoPlaceFluidInHoleDoAfterEvent args)
    {
        var (ent, comp) = resinHole;

        if (args.Handled || args.Cancelled)
        {
            return;
        }

        // TODO: Praetorian logic of adding acid spray
        if (TryComp(args.User, out XenoBombardComponent? bombardComp))
        {

            if (!_xenoPlasma.HasPlasmaPopup(args.User, bombardComp.PlasmaCost, false))
            {
                return;
            }
            _xenoPlasma.TryRemovePlasma(args.User, bombardComp.PlasmaCost);

            comp.TrapPrototype = bombardComp.Projectile;
            switch (bombardComp.Projectile.Id)
            {
                case XenoResinHoleComponent.AcidGasPrototype:
                    _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.AcidGas);
                    break;

                case XenoResinHoleComponent.NeuroGasPrototype:
                    _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.NeuroticGas);
                    break;
            }
        }
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
                _popup.PopupEntity(msg, uid.Value, user, PopupType.SmallCaution);
                return false;
            }

            if (HasComp<XenoConstructComponent>(uid) ||
                _tags.HasAnyTag(uid.Value, StructureTag, AirlockTag) ||
                HasComp<StrapComponent>(uid))
            {
                var msg = Loc.GetString("cm-xeno-construction-resin-hole-blocked");
                _popup.PopupEntity(msg, uid.Value, user, PopupType.SmallCaution);
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
                _popup.PopupEntity(msg, user, user, PopupType.SmallCaution);
                return false;
            }
        }

        if (_turf.IsTileBlocked(gridId, tile, Impassable | MidImpassable | HighImpassable, grid))
        {
            var msg = Loc.GetString("cm-xeno-construction-resin-hole-blocked");
            _popup.PopupEntity(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        if (!hasWeeds)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-construction-resin-hole-failed-must-weeds"), user, user);
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
        var resinHole = Spawn(resinHolePrototype, coords);
        _adminLogs.Add(LogType.RMCXenoConstruct, $"Xeno {ToPrettyString(xeno):xeno} placed a resin hole at {coords}");
    }

    private bool ActivateTrap(Entity<XenoResinHoleComponent> resinHole)
    {
        var (ent, comp) = resinHole;

        if (comp.TrapPrototype is not EntProtoId trapEntityProto)
        {
            return false;
        }

        var trapEntity = Spawn(trapEntityProto);
        _transform.DropNextTo(trapEntity, resinHole.Owner);

        if (TryComp(trapEntity, out XenoProjectileComponent? projectileComp))
        {
            _gun.ShootProjectile(trapEntity, new System.Numerics.Vector2(), new System.Numerics.Vector2(), resinHole);
        }

        comp.TrapPrototype = null;
        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);
        return true;
    }
}
