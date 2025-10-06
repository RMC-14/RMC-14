using System.Numerics;
using Content.Server.Destructible;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Bombard;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Administration.Logs;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Server._RMC14.Xenonids.Construction.ResinHole;

public sealed class XenoResinHoleSystem : SharedXenoResinHoleSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruct = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

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

        SubscribeLocalEvent<InResinHoleRangeComponent, StoodEvent>(OnInRangeStand);
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

        if (_xenoWeeds.GetWeedsOnFloor((gridId, grid), location, true) is { } weeds)
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

        SetTrapType(resinHole, XenoResinHoleComponent.ParasitePrototype);
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
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-filling-gas"), resinHole, args.User);
            return;
        }

        if (TryComp<AcidTrapComponent>(args.User, out var ourAcid) &&
            (resinHole.Comp.TrapPrototype == null || (IsAcidPrototype(resinHole.Comp.TrapPrototype, out var level) && ourAcid.TrapLevel > level)))
        {
            if (!_xenoPlasma.HasPlasmaPopup(args.User, ourAcid.Cost, false))
                return;

            var ev = new XenoPlaceFluidInHoleDoAfterEvent();
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, resinHole.Comp.AddFluidDelay, ev, resinHole)
            {
                BreakOnMove = true,
                CancelDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameEvent
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-filling-acid"), resinHole, args.User);
            return;
        }

        if (resinHole.Comp.TrapPrototype == null || resinHole.Comp.TrapPrototype != XenoResinHoleComponent.ParasitePrototype)
        {
            var msg = HasComp<AcidTrapComponent>(args.User)
                ? Loc.GetString("rmc-xeno-construction-resin-hole-good-acid")
                : Loc.GetString("rmc-xeno-construction-resin-hole-no-acid");
            _popup.PopupEntity(msg, resinHole, args.User, PopupType.SmallCaution);
            return;
        }

        var para = Spawn(resinHole.Comp.TrapPrototype);
        _hive.SetSameHive(resinHole.Owner, para);

        if (!_rmcHands.IsPickupByAllowed(para, args.User) ||
            !_hands.TryPickupAnyHand(args.User, para))
        {
            QueueDel(para);
            return;
        }

        SetTrapType(resinHole, null);
        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);
        args.Handled = true;
    }

    private void OnPlaceFluidInResinHole(Entity<XenoResinHoleComponent> resinHole, ref XenoPlaceFluidInHoleDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (TryComp(args.User, out XenoBombardComponent? bombardComp) && resinHole.Comp.TrapPrototype == null)
        {

            if (!_xenoPlasma.TryRemovePlasmaPopup(args.User, bombardComp.PlasmaCost))
                return;

            switch (bombardComp.Projectile.Id)
            {
                case XenoResinHoleComponent.BoilerAcid:
                    SetTrapType(resinHole, XenoResinHoleComponent.AcidGasPrototype);
                    _appearanceSystem.SetData(resinHole, XenoResinHoleVisuals.Contained, ContainedTrap.AcidGas);
                    break;

                case XenoResinHoleComponent.BoilerNeuro:
                    SetTrapType(resinHole, XenoResinHoleComponent.NeuroGasPrototype);
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

            if (!_xenoPlasma.TryRemovePlasmaPopup(args.User, acid.Cost))
                return;

            SetTrapType(resinHole, acid.Spray);
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
        if (args.Origin is { } origin && _hive.FromSameHive(origin, resinHole.Owner))
            return;

        //TODO Flames should make the trigger message never happen but destroyed will
        if (args.DamageDelta == null)
            return;

        var destroyed = false;
        if (_destructible.TryGetDestroyedAt(resinHole.Owner, out var totalHealth))
        {
            destroyed = args.Damageable.TotalDamage + args.DamageDelta.GetTotal() > totalHealth;
        }
        ActivateTrap(resinHole, destroyed);
    }

    private void OnExamine(Entity<XenoResinHoleComponent> resinHole, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(XenoResinHoleComponent)))
        {
            args.PushMarkup(GetTrapExamineMessage(resinHole));
        }
    }

    private void OnXenoResinHoleStepTriggerAttempt(Entity<XenoResinHoleComponent> resinHole, ref StepTriggerAttemptEvent args)
    {
        if (resinHole.Comp.TrapPrototype is null)
            return;

        if (_hive.FromSameHive(args.Tripper, resinHole.Owner))
        {
            args.Continue = false;
            return;
        }

        if (resinHole.Comp.TrapPrototype == XenoResinHoleComponent.ParasitePrototype)
        {
            if (!_interaction.InRangeUnobstructed(args.Source, args.Tripper, resinHole.Comp.ParasiteActivationRange))
            {
                args.Continue = false;
                return;
            }

            args.Continue = TryComp<InfectableComponent>(args.Tripper, out var infected)
                && !infected.BeingInfected
                && !HasComp<VictimInfectedComponent>(args.Tripper);
            return;
        }

        if (_mobState.IsDead(args.Tripper) ||
            _standing.IsDown(args.Tripper) ||
            !_interaction.InRangeUnobstructed(args.Source, args.Tripper, 1.5f))
        {
            var inRange = EnsureComp<InResinHoleRangeComponent>(args.Tripper);
            if (!inRange.HoleList.Contains(resinHole))
                inRange.HoleList.Add(resinHole);
            args.Continue = false;
            return;
        }

        args.Continue = true;
    }

    private void OnXenoResinHoleStepTriggered(Entity<XenoResinHoleComponent> resinHole, ref StepTriggeredOffEvent args)
    {
        if (_mobState.IsDead(args.Tripper))
            return;

        if (resinHole.Comp.TrapPrototype == XenoResinHoleComponent.ParasitePrototype)
        {
            _stun.TryParalyze(args.Tripper, resinHole.Comp.StepStunDuration, true);
        }
        ActivateTrap(resinHole);
    }

    private void OnInRangeStand(Entity<InResinHoleRangeComponent> tripper, ref StoodEvent args)
    {
        UpdateInRange(tripper, true);
    }

    private bool CanPlaceResinHole(EntityUid user, EntityCoordinates coords)
    {
        var canPlaceStructure = _xenoConstruct.CanPlaceXenoStructure(user, coords, out var popupType);

        if (!canPlaceStructure)
        {
            popupType += "-resin-hole";
            _popup.PopupEntity(Loc.GetString(popupType), user, user, PopupType.SmallCaution);
            return false;
        }

        if (_transform.GetGrid(coords) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            var msg = Loc.GetString("rmc-xeno-construction-no-map-resin-hole");
            _popup.PopupEntity(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        var neighborAnchoredEntities = _mapSystem.GetCellsInSquareArea(gridId, grid, coords, 1);

        foreach (var entity in neighborAnchoredEntities)
        {
            if (!HasComp<XenoResinHoleComponent>(entity))
                continue;

            var msg = Loc.GetString("rmc-xeno-construction-similar-too-close-resin-hole");
            _popup.PopupEntity(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        foreach (var body in _lookup.GetEntitiesInRange<MobStateComponent>(coords, 0.5f))
        {
            if (!_mobState.IsDead(body))
                continue;

            var msg = Loc.GetString("rmc-xeno-construction-dead-body");
            _popup.PopupEntity(msg, user, user, PopupType.SmallCaution);
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

        if (comp.TrapPrototype is not { } trapEntityProto)
            return false;

        if (IsAcidPrototype(trapEntityProto, out _))
        {
            var chain = _onCollide.SpawnChain();
            //Try to make a 3x3 grid - simple is best
            for (var i = -1f; i < 2; i += 1f)
            {
                for (var j = -1f; j < 2; j += 1f)
                {
                    var coords = _transform.GetMoverCoordinates(resinHole).Offset(new Vector2(i, j));

                    var tuff = _turf.GetTileRef(coords);
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

            if (trapEntityProto == XenoResinHoleComponent.ParasitePrototype)
                EnsureComp<TrapParasiteComponent>(trapEntity);
        }

        var msg = "rmc-xeno-construction-resin-hole-activate";

        var ev = new XenoResinHoleActivationEvent(msg);

        // If the resin hole is destroyed, it's the XenoAnnounceStructureDestructionComponent job to announce
        // the entity's destruction
        if (!destroyed)
        {
            RaiseLocalEvent(ent, ev);

            SetTrapType(resinHole, null);
            _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Empty);
        }

        return true;
    }

    private string GetTrapExamineMessage(Entity<XenoResinHoleComponent> resinHole)
    {
        var msgId = "";
        if (resinHole.Comp.TrapPrototype == null)
        {
            msgId = "rmc-xeno-construction-resin-hole-empty";
        }
        else
        {
            switch (resinHole.Comp.TrapPrototype)
            {
                case XenoResinHoleComponent.AcidGasPrototype:
                case XenoResinHoleComponent.NeuroGasPrototype:
                    msgId = "rmc-xeno-construction-resin-hole-gas";
                    break;
                case XenoResinHoleComponent.WeakAcidPrototype:
                case XenoResinHoleComponent.AcidPrototype:
                case XenoResinHoleComponent.StrongAcidPrototype:
                    msgId = "rmc-xeno-construction-resin-hole-acid";
                    break;
                case XenoResinHoleComponent.ParasitePrototype:
                    msgId = "rmc-xeno-construction-resin-hole-parasite";
                    break;
            }
        }

        return Loc.GetString(msgId);
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

    private void UpdateInRange(Entity<InResinHoleRangeComponent> tripper, bool stoodUp)
    {
        if (_standing.IsDown(tripper))
            return;

        for (var i = tripper.Comp.HoleList.Count - 1; i >= 0; i--)
        {
            var resinHole = tripper.Comp.HoleList[i];

            if (!TryComp<XenoResinHoleComponent>(resinHole, out var holeComponent))
            {
                tripper.Comp.HoleList.Remove(resinHole);
                continue;
            }

            //Continue if trap has emptied or been replaced with a parasite
            if (holeComponent.TrapPrototype is null || holeComponent.TrapPrototype == XenoResinHoleComponent.ParasitePrototype)
                continue;

            //Check if each Resin Hole is still colliding with tripper
            if (!_physicsQuery.TryGetComponent(resinHole, out var physics))
            {
                tripper.Comp.HoleList.Remove(resinHole);
                continue;
            }

            foreach (var ent in _physics.GetContactingEntities(resinHole, physics))
            {
                if (ent != tripper.Owner)
                    continue;

                if (!IsVisibleToTrap(resinHole, ent))
                    continue;

                ActivateTrap((resinHole, holeComponent));
                tripper.Comp.HoleList.Remove(resinHole);

                if (tripper.Comp.HoleList.Count == 0)
                {
                    RemCompDeferred<InResinHoleRangeComponent>(tripper);
                }
                //Only trigger one trap maximum per stand-up
                return;
            }
        }

        if (stoodUp || tripper.Comp.HoleList.Count == 0)
            RemCompDeferred<InResinHoleRangeComponent>(tripper);
    }

    public bool IsVisibleToTrap(EntityUid resinHole, EntityUid ent)
    {
        //Basic case, can see each other clearly, open LoS
        if (_interaction.InRangeUnobstructed(resinHole, ent, 1.5f))
            return true;

        //Advanced case, checks if any point 1 tile away in any cardinal direction is capable of seeing the target.
        //Allows slipping around open passages, but NOT through encasing walls, as offset check fails if the origin coordinate is inside a wall.
        var holeCoordinates = _transform.GetMapCoordinates(resinHole);
        var offsetCoordinates = holeCoordinates;

        for (var i = 0; i < 4; i++)
        {
            offsetCoordinates = i switch
            {
                0 => holeCoordinates.Offset(1, 0),
                1 => holeCoordinates.Offset(-1, 0),
                2 => holeCoordinates.Offset(0, 1),
                3 => holeCoordinates.Offset(0, -1),
                _ => offsetCoordinates
            };

            //Trigger Range is preserved by the ContactingEntities check in the parent function, this just checks vision
            if (_interaction.InRangeUnobstructed(offsetCoordinates, ent, 1.5f))
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        var inRangeQuery = EntityQueryEnumerator<InResinHoleRangeComponent>();
        while (inRangeQuery.MoveNext(out var uid, out var comp))
        {
            UpdateInRange((uid, comp), false);
        }
    }
}
