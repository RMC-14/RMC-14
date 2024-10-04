using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Bombard;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Hands.Components;
using Content.Shared.Maps;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using static Content.Shared.Physics.CollisionGroup;
using Content.Shared.Examine;
using Content.Shared.Standing;


namespace Content.Server._RMC14.Xenonids.Construction.ResinHole;

public sealed partial class XenoResinHoleSystem : SharedXenoResinHoleSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

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
        // TODO needs a specific event when set on fire/onfire/fire is used on it etc
        // the burned message is used specifically for when a parasite trap gets burned up
        SubscribeLocalEvent<XenoResinHoleComponent, StepTriggerAttemptEvent>(OnXenoResinHoleStepTriggerAttempt);
        SubscribeLocalEvent<XenoResinHoleComponent, StepTriggeredOffEvent>(OnXenoResinHoleStepTriggered);
        SubscribeLocalEvent<XenoResinHoleComponent, ExaminedEvent>(OnExamine);

    }

    private void OnPlaceXenoResinHole(Entity<XenoComponent> xeno, ref XenoPlaceResinHoleActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var location = _transform.GetMoverCoordinates(xeno).SnapToGrid(EntityManager);
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
            var doAfterArgs = new DoAfterArgs(EntityManager, xeno.Owner, args.DestroyWeedSourceDelay, ev, xeno.Owner, weeds)
            {
                BlockDuplicate = true,
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameTarget
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-uproot"), args.Performer, args.Performer);
            return;
        }

        _xenoPlasma.TryRemovePlasma(xeno.Owner, args.PlasmaCost);
        PlaceResinHole(xeno, location, args.Prototype);
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

        var location = _transform.GetMoverCoordinates(xeno).SnapToGrid(EntityManager);

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, args.PlasmaCost, false))
        {
            return;
        }
        _xenoPlasma.TryRemovePlasma(xeno.Owner, args.PlasmaCost);
        PlaceResinHole(xeno, location, args.ResinHolePrototype);
        QueueDel(args.Target);
    }

    private void OnPlaceParasiteInXenoResinHoleDoAfter(Entity<XenoResinHoleComponent> resinHole, ref XenoPlaceParasiteInHoleDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Used == null)
            return;

        if (!CanPlaceInHole(args.Used.Value, resinHole, args.User))
            return;

        resinHole.Comp.TrapPrototype = XenoResinHoleComponent.ParasitePrototype;
        Dirty(resinHole);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-finished-parasite"), resinHole, args.User);
        QueueDel(args.Used);

        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Parasite);
        args.Handled = true;
    }

    private void OnEmptyHandInteract(Entity<XenoResinHoleComponent> resinHole, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        if (resinHole.Comp.TrapPrototype == null && TryComp(args.User, out XenoBombardComponent? bombardComp))
        {
            if (!_xenoPlasma.HasPlasmaPopup(args.User, bombardComp.PlasmaCost, false))
            {
                return;
            }

            var ev = new XenoPlaceFluidInHoleDoAfterEvent();
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, resinHole.Comp.AddFluidDelay, ev, resinHole)
            {
                BreakOnMove = true,
                CancelDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameEvent
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-filling-gas"), resinHole, args.User, PopupType.Small);
            return;
        }
        else if (TryComp<AcidTrapComponent>(args.User, out var ourAcid) && (resinHole.Comp.TrapPrototype == null
            || (IsAcidPrototype(resinHole.Comp.TrapPrototype, out var level) && ourAcid.TrapLevel > level)))
        {
            if (!_xenoPlasma.HasPlasmaPopup(args.User, ourAcid.Cost, false))
            {
                return;
            }

            var ev = new XenoPlaceFluidInHoleDoAfterEvent();
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, resinHole.Comp.AddFluidDelay, ev, resinHole)
            {
                BreakOnMove = true,
                CancelDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameEvent
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-filling-acid"), resinHole, args.User, PopupType.Small);
            return;
        }
        else if (resinHole.Comp.TrapPrototype == null || resinHole.Comp.TrapPrototype != XenoResinHoleComponent.ParasitePrototype)
        {
            if (HasComp<AcidTrapComponent>(args.User))
                _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-good-acid"), resinHole, args.User, PopupType.SmallCaution);
            else
                _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-no-acid"), resinHole, args.User, PopupType.SmallCaution);
            return;
        }

        var para = Spawn(resinHole.Comp.TrapPrototype);
        _hive.SetSameHive(para, resinHole.Owner);

        if (!_rmcHands.IsPickupByAllowed(para, args.User) || !_hands.TryPickupAnyHand(args.User, para))
        {
            QueueDel(para);
            return;
        }

        resinHole.Comp.TrapPrototype = null;
        Dirty(resinHole);
        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);
        args.Handled = true;
    }

    private void OnPlaceFluidInResinHole(Entity<XenoResinHoleComponent> resinHole, ref XenoPlaceFluidInHoleDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        if (TryComp(args.User, out XenoBombardComponent? bombardComp) && resinHole.Comp.TrapPrototype == null)
        {

            if (!_xenoPlasma.HasPlasmaPopup(args.User, bombardComp.PlasmaCost, false))
                return;
            _xenoPlasma.TryRemovePlasma(args.User, bombardComp.PlasmaCost);

            switch (bombardComp.Projectile.Id)
            {
                case XenoResinHoleComponent.BoilerAcid:
                    resinHole.Comp.TrapPrototype = XenoResinHoleComponent.AcidGasPrototype;
                    _appearanceSystem.SetData(resinHole, XenoResinHoleVisuals.Contained, ContainedTrap.AcidGas);
                    break;

                case XenoResinHoleComponent.BoilerNeuro:
                    resinHole.Comp.TrapPrototype = XenoResinHoleComponent.NeuroGasPrototype;
                    _appearanceSystem.SetData(resinHole, XenoResinHoleVisuals.Contained, ContainedTrap.NeuroticGas);
                    break;
            }
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-finished-gas-self"), args.User, args.User);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-finished-gas", ("xeno", args.User)), args.User, Filter.PvsExcept(args.User), true);
        }
        else
        {
            if (!TryComp<AcidTrapComponent>(args.User, out var acid))
                return;

            if (resinHole.Comp.TrapPrototype != null && (!IsAcidPrototype(resinHole.Comp.TrapPrototype, out var level) || level >= acid.TrapLevel))
                return;

            if (!_xenoPlasma.HasPlasmaPopup(args.User, acid.Cost, false))
                return;

            resinHole.Comp.TrapPrototype = acid.Spray;
            switch (acid.Spray.Id)
            {
                case XenoResinHoleComponent.WeakAcidPrototype:
                    _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Acid1);
                    break;

                case XenoResinHoleComponent.AcidPrototype:
                    _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Acid2);
                    break;

                case XenoResinHoleComponent.StrongAcidPrototype:
                    _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Acid3);
                    break;
            }
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-finished-acid-self"), args.User, args.User);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-finished-acid", ("xeno", args.User)), args.User, Filter.PvsExcept(args.User), true);
        }
        _audio.PlayPvs(resinHole.Comp.FluidFillSound, resinHole);
        args.Handled = true;
    }

    private void OnXenoResinHoleTakeDamage(Entity<XenoResinHoleComponent> resinHole, ref DamageChangedEvent args)
    {
        if (args.Origin is {} origin && _hive.FromSameHive(origin, resinHole.Owner))
        {
            if (resinHole.Comp.TrapPrototype != null && args.DamageDelta != null)
                args.DamageDelta.ClampMax(0);
            return;
        }
        //TODO Flames should make the trigger message never happen but destroyed will
        if (args.DamageDelta == null)
            return;
        var destroyed = args.Damageable.TotalDamage + args.DamageDelta.GetTotal() > resinHole.Comp.TotalHealth;
        ActivateTrap(resinHole, destroyed);
    }

    private void OnExamine(Entity<XenoResinHoleComponent> resinHole, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(XenoResinHoleComponent)))
        {
            if (resinHole.Comp.TrapPrototype == null)
                args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-empty"));
            else
            {
                switch (resinHole.Comp.TrapPrototype)
                {
                    case XenoResinHoleComponent.AcidGasPrototype:
                    case XenoResinHoleComponent.NeuroGasPrototype:
                        args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-gas"));
                        break;
                    case XenoResinHoleComponent.WeakAcidPrototype:
                    case XenoResinHoleComponent.AcidPrototype:
                    case XenoResinHoleComponent.StrongAcidPrototype:
                        args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-acid"));
                        break;
                    case XenoResinHoleComponent.ParasitePrototype:
                        args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-parasite"));
                        break;
                }
            }
        }
    }

    private void OnXenoResinHoleStepTriggerAttempt(Entity<XenoResinHoleComponent> resinHole, ref StepTriggerAttemptEvent args)
    {
        var (ent, comp) = resinHole;

        if (comp.TrapPrototype is null)
            return;

        if (_hive.FromSameHive(args.Tripper, resinHole.Owner))
        {
            args.Continue = false;
            return;
        }

        if (comp.TrapPrototype == XenoResinHoleComponent.ParasitePrototype)
        {
            if (!_interaction.InRangeUnobstructed(args.Source, args.Tripper, comp.ParasiteActivationRange))
            {
                args.Continue = false;
                return;
            }

            args.Continue = HasComp<InfectableComponent>(args.Tripper) &&
                !HasComp<VictimInfectedComponent>(args.Tripper);
            return;
        }
        else if (_mobState.IsDead(args.Tripper) || _standing.IsDown(args.Tripper))
        {
            args.Continue = false;
            return;
        }

        args.Continue = true;
    }

    private void OnXenoResinHoleStepTriggered(Entity<XenoResinHoleComponent> resinHole, ref StepTriggeredOffEvent args)
    {
        if (resinHole.Comp.TrapPrototype == XenoResinHoleComponent.ParasitePrototype)
        {
            _stun.TryParalyze(args.Tripper, resinHole.Comp.StepStunDuration, true);
        }
        ActivateTrap(resinHole);
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

    private void PlaceResinHole(Entity<XenoComponent> xeno, EntityCoordinates coords, EntProtoId resinHolePrototype)
    {
        var resinHole = Spawn(resinHolePrototype, coords);

        _adminLogs.Add(LogType.RMCXenoConstruct, $"Xeno {ToPrettyString(xeno.Owner):xeno} placed a resin hole at {coords}");

        _hive.SetSameHive(xeno.Owner, resinHole);

        var resinHoleComp = EnsureComp<XenoResinHoleComponent>(resinHole);
        _audio.PlayPvs(resinHoleComp.BuildSound, resinHole);
    }

    private bool ActivateTrap(Entity<XenoResinHoleComponent> resinHole, bool destroyed = false)
    {
        var (ent, comp) = resinHole;

        if (comp.TrapPrototype is not EntProtoId trapEntityProto)
            return false;

        if (IsAcidPrototype(trapEntityProto, out _))
        {
            var chain = _onCollide.SpawnChain();
            //Try to make a 3x3 grid - simple is best
            for (float i = -1f; i < 2; i += 1f)
            {
                for (float j = -1f; j < 2; j += 1f)
                {
                    var coords = _transform.GetMoverCoordinates(resinHole).Offset(new Vector2(i, j));

                    var tuff = TurfHelpers.GetTileRef(coords);
                    if (tuff != null && !_turf.IsTileBlocked(tuff.Value, FullTileMask))
                    {
                        var acid = SpawnAtPosition(trapEntityProto, coords);
                        _hive.SetSameHive(resinHole.Owner, acid);
                        if (TryComp<DamageOnCollideComponent>(acid, out var collide))
                            _onCollide.SetChain((acid, collide), chain);
                    }
                }
            }
        }
        else
        {
            var trapEntity = SpawnAtPosition(trapEntityProto, _transform.GetMoverCoordinates(resinHole));
            _hive.SetSameHive(resinHole.Owner, trapEntity);
        }

		string msg = destroyed ? "cm-xeno-construction-resin-hole-destroyed" : "rmc-xeno-construction-resin-hole-activate";

		var ev = new XenoResinHoleActivationEvent(msg);
		RaiseLocalEvent(ent, ev);

		comp.TrapPrototype = null;
		Dirty(resinHole);
		_appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);

		return true;
	}

    private bool IsAcidPrototype(string proto, out int level)
    {
        level = 0;
        switch (proto)
        {
            case XenoResinHoleComponent.StrongAcidPrototype:
                level = 3;
                return true;
            case XenoResinHoleComponent.AcidPrototype:
                level = 2;
                return true;
            case XenoResinHoleComponent.WeakAcidPrototype:
                level = 1;
                return true;
            default:
                return false;
        }
    }
}
