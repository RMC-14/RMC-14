using System.Collections.Immutable;
using System.Linq;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Construction;

public sealed class SharedXenoConstructionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly XenoNestSystem _xenoNest = default!;

    private static readonly ImmutableArray<Direction> Directions = Enum.GetValues<Direction>()
        .Where(d => d != Direction.Invalid)
        .ToImmutableArray();

    private EntityQuery<HiveConstructionNodeComponent> _hiveConstructionNodeQuery;
    private EntityQuery<XenoConstructionSupportComponent> _constructionSupportQuery;
    private EntityQuery<XenoConstructionRequiresSupportComponent> _constructionRequiresSupportQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<XenoConstructComponent> _xenoConstructQuery;

    public override void Initialize()
    {
        base.Initialize();

        _hiveConstructionNodeQuery = GetEntityQuery<HiveConstructionNodeComponent>();
        _constructionSupportQuery = GetEntityQuery<XenoConstructionSupportComponent>();
        _constructionRequiresSupportQuery = GetEntityQuery<XenoConstructionRequiresSupportComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _xenoConstructQuery = GetEntityQuery<XenoConstructComponent>();

        SubscribeLocalEvent<XenoConstructionComponent, XenoPlantWeedsActionEvent>(OnXenoPlantWeedsAction);

        SubscribeLocalEvent<XenoConstructionComponent, XenoChooseStructureActionEvent>(OnXenoChooseStructureAction);

        SubscribeLocalEvent<XenoConstructionComponent, XenoSecreteStructureActionEvent>(OnXenoSecreteStructureAction);
        SubscribeLocalEvent<XenoConstructionComponent, XenoSecreteStructureDoAfterEvent>(OnXenoSecreteStructureDoAfter);

        SubscribeLocalEvent<XenoConstructionComponent, XenoOrderConstructionActionEvent>(OnXenoOrderConstructionAction);
        SubscribeLocalEvent<XenoConstructionComponent, XenoOrderConstructionDoAfterEvent>(OnXenoOrderConstructionDoAfter);
        SubscribeLocalEvent<XenoConstructionComponent, XenoConstructionAddPlasmaDoAfterEvent>(OnHiveConstructionNodeAddPlasmaDoAfter);

        SubscribeLocalEvent<XenoChooseConstructionActionComponent, XenoConstructionChosenEvent>(OnActionConstructionChosen);
        SubscribeLocalEvent<XenoConstructionActionComponent, ValidateActionEntityWorldTargetEvent>(
            OnSecreteActionValidateTarget);

        SubscribeLocalEvent<HiveConstructionNodeComponent, ExaminedEvent>(OnHiveConstructionNodeExamined);
        SubscribeLocalEvent<HiveConstructionNodeComponent, ActivateInWorldEvent>(OnHiveConstructionNodeActivated);

        SubscribeLocalEvent<HiveCoreComponent, MapInitEvent>(OnHiveCoreMapInit);

        SubscribeLocalEvent<XenoConstructionSupportComponent, ComponentRemove>(OnCheckAdjacentCollapse);
        SubscribeLocalEvent<XenoConstructionSupportComponent, EntityTerminatingEvent>(OnCheckAdjacentCollapse);

        Subs.BuiEvents<XenoConstructionComponent>(XenoChooseStructureUI.Key, subs =>
        {
            subs.Event<XenoChooseStructureBuiMsg>(OnXenoChooseStructureBui);
        });

        Subs.BuiEvents<XenoConstructionComponent>(XenoOrderConstructionUI.Key, subs =>
        {
            subs.Event<XenoOrderConstructionBuiMsg>(OnXenoOrderConstructionBui);
        });

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnXenoPlantWeedsAction(Entity<XenoConstructionComponent> xeno, ref XenoPlantWeedsActionEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(xeno).SnapToGrid(EntityManager, _map);
        if (_transform.GetGrid(coordinates) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return;
        }

        if (_xenoWeeds.IsOnWeeds((gridUid, grid), coordinates, true))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-weeds-source-already-here"), xeno.Owner, xeno.Owner);
            return;
        }

        var tile = _mapSystem.CoordinatesToTile(gridUid, grid, coordinates);
        if (!_xenoWeeds.CanPlaceWeeds((gridUid, grid), tile))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-weeds"), xeno.Owner, xeno.Owner);
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        args.Handled = true;
        if (_net.IsServer)
            Spawn(args.Prototype, coordinates);
    }

    private void OnXenoChooseStructureAction(Entity<XenoConstructionComponent> xeno, ref XenoChooseStructureActionEvent args)
    {
        args.Handled = true;
        _ui.TryOpenUi(xeno.Owner, XenoChooseStructureUI.Key, xeno);
    }

    private void OnXenoChooseStructureBui(Entity<XenoConstructionComponent> xeno, ref XenoChooseStructureBuiMsg args)
    {
        if (!xeno.Comp.CanBuild.Contains(args.StructureId))
            return;

        xeno.Comp.BuildChoice = args.StructureId;
        Dirty(xeno);

        _ui.CloseUi(xeno.Owner, XenoChooseStructureUI.Key, xeno);

        var ev = new XenoConstructionChosenEvent(args.StructureId);
        foreach (var (id, _) in _actions.GetActions(xeno))
        {
            RaiseLocalEvent(id, ref ev);
        }
    }

    private void OnXenoSecreteStructureAction(Entity<XenoConstructionComponent> xeno, ref XenoSecreteStructureActionEvent args)
    {
        if (xeno.Comp.BuildChoice is not { } choice)
            return;

        var canReplace = args.Entity != null && CanReplaceStructure(xeno, choice, args.Entity.Value, true);

        var canCreate = args.Coords != null &&
                        CanSecreteOnTilePopup(xeno, choice, args.Coords.Value, true, true, canReplace);

        if (!canReplace && !canCreate)
            return;

        var attempt = new XenoSecreteStructureAttemptEvent(args.Entity);
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;
        var ev = new XenoSecreteStructureDoAfterEvent(GetNetEntity(args.Entity), GetNetCoordinates(args.Coords), choice);
        var doAfter = new DoAfterArgs(EntityManager, xeno, canReplace ? new TimeSpan(0) : xeno.Comp.BuildDelay, ev, xeno)
        {
            BreakOnMove = true
        };

        // TODO RMC14 building animation
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoSecreteStructureDoAfter(Entity<XenoConstructionComponent> xeno, ref XenoSecreteStructureDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!xeno.Comp.CanBuild.Contains(args.StructureId))
            return;

        // TODO RMC14 stop collision for mobs until they move off

        // Replace existing construction
        var entity = GetEntity(args.EntityToReplace);
        if (entity != null && entity.Value.IsValid() && CanReplaceStructure(xeno, args.StructureId, entity.Value, true))
        {
            if (GetStructurePlasmaCost(args.StructureId) is { } cost &&
                !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, cost))
                return;

            args.Handled = true;

            if (!_net.IsServer)
                return;

            var coords = _transform.GetMapCoordinates(entity.Value);
            var spawned = Spawn(args.StructureId, coords);

            if (TryComp(entity, out XenoNestSurfaceComponent? nestSurface) &&
                TryComp(spawned, out XenoNestSurfaceComponent? spawnedNestSurface))
            {
                _xenoNest.TransferNested((entity.Value, nestSurface), (spawned, spawnedNestSurface));
            }

            Del(entity);
            return;
        }

        // Build new construction
        var coordinates = GetCoordinates(args.Coordinates);
        if (coordinates != null &&
            coordinates.Value.IsValid(EntityManager) &&
            CanSecreteOnTilePopup(xeno, args.StructureId, coordinates.Value, true, true))
        {
            if (GetStructurePlasmaCost(args.StructureId) is { } cost &&
                !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, cost))
                return;

            args.Handled = true;

            if (!_net.IsServer)
                return;

            Spawn(args.StructureId, coordinates.Value);
            return;
        }
    }

    private void OnXenoOrderConstructionAction(Entity<XenoConstructionComponent> xeno, ref XenoOrderConstructionActionEvent args)
    {
        if (!CanOrderConstructionPopup(xeno, args.Target, null))
            return;

        xeno.Comp.OrderingConstructionAt = args.Target;
        Dirty(xeno);

        args.Handled = true;
        _ui.TryOpenUi(xeno.Owner, XenoOrderConstructionUI.Key, xeno);
    }

    private void OnXenoOrderConstructionBui(Entity<XenoConstructionComponent> xeno, ref XenoOrderConstructionBuiMsg args)
    {
        _ui.CloseUi(xeno.Owner, XenoOrderConstructionUI.Key, xeno);
        if (xeno.Comp.OrderingConstructionAt is not { } target ||
            !xeno.Comp.CanOrderConstruction.Contains(args.StructureId) ||
            !CanOrderConstructionPopup(xeno, target, args.StructureId))
        {
            return;
        }

        if (!_prototype.TryIndex(args.StructureId, out var prototype))
            return;

        if (prototype.TryGetComponent(out HiveConstructionNodeComponent? node, _compFactory) &&
            !_xenoPlasma.HasPlasmaPopup(xeno.Owner, node.InitialPlasmaCost))
        {
            return;
        }

        var ev = new XenoOrderConstructionDoAfterEvent(args.StructureId, GetNetCoordinates(target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.OrderConstructionDelay, ev, xeno)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoOrderConstructionDoAfter(Entity<XenoConstructionComponent> xeno, ref XenoOrderConstructionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var target = GetCoordinates(args.Coordinates);
        if (!xeno.Comp.CanOrderConstruction.Contains(args.StructureId) ||
            !CanOrderConstructionPopup(xeno, target, args.StructureId) ||
            !TryComp(xeno, out XenoPlasmaComponent? plasma))
        {
            return;
        }

        if (!_prototype.TryIndex(args.StructureId, out var prototype))
            return;

        if (prototype.TryGetComponent(out HiveConstructionNodeComponent? node, _compFactory) &&
            !_xenoPlasma.TryRemovePlasmaPopup((xeno, plasma), node.InitialPlasmaCost))
        {
            return;
        }

        args.Handled = true;

        if (_net.IsClient)
            return;

        var structure = Spawn(args.StructureId, target.SnapToGrid(EntityManager, _map));
        if (TryComp(xeno, out XenoComponent? xenoComp))
        {
            var member = EnsureComp<HiveMemberComponent>(structure);
            _hive.SetHive((structure, member), xenoComp.Hive);
        }
    }

    private void OnHiveConstructionNodeAddPlasmaDoAfter(Entity<XenoConstructionComponent> xeno, ref XenoConstructionAddPlasmaDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp(target, out HiveConstructionNodeComponent? node) ||
            !TryComp(target, out TransformComponent? transform) ||
            !TryComp(xeno, out XenoPlasmaComponent? plasma))
        {
            return;
        }

        if (!InRangePopup(args.User, transform.Coordinates, xeno.Comp.OrderConstructionRange.Float()))
            return;

        var plasmaLeft = node.PlasmaCost - node.PlasmaStored;
        var subtract = FixedPoint2.Min(plasma.Plasma, plasmaLeft);
        if (plasmaLeft < FixedPoint2.Zero ||
            plasma.Plasma < 1 ||
            !_xenoPlasma.TryRemovePlasmaPopup((args.User, plasma), subtract))
        {
            return;
        }

        args.Handled = true;

        node.PlasmaStored += subtract;
        plasmaLeft = node.PlasmaCost - node.PlasmaStored;

        if (node.PlasmaStored < node.PlasmaCost)
        {
            _popup.PopupClient(
                Loc.GetString("cm-xeno-requires-more-plasma", ("construction", target), ("plasma", plasmaLeft)),
                target,
                args.User);
            return;
        }

        if (_net.IsClient)
            return;

        var hive = CompOrNull<HiveMemberComponent>(target)?.Hive;
        var spawn = Spawn(node.Spawn, transform.Coordinates);
        var member = EnsureComp<HiveMemberComponent>(spawn);
        _hive.SetHive((spawn, member), hive);

        QueueDel(target);

        if (TryComp(spawn, out HiveConstructionUniqueComponent? unique))
        {
            var uniques = EntityQueryEnumerator<HiveConstructionUniqueComponent>();
            while (uniques.MoveNext(out var uid, out var otherUnique))
            {
                if (uid == spawn)
                    continue;

                if (otherUnique.Id == unique.Id &&
                    !TerminatingOrDeleted(uid) &&
                    !EntityManager.IsQueuedForDeletion(uid))
                {
                    QueueDel(uid);
                }
            }
        }
    }

    private void OnActionConstructionChosen(Entity<XenoChooseConstructionActionComponent> xeno, ref XenoConstructionChosenEvent args)
    {
        if (_actions.TryGetActionData(xeno, out var action) &&
            _prototype.HasIndex(args.Choice))
        {
            action.Icon = new SpriteSpecifier.EntityPrototype(args.Choice);
            Dirty(xeno, action);
        }
    }

    private void OnSecreteActionValidateTarget(Entity<XenoConstructionActionComponent> ent,
        ref ValidateActionEntityWorldTargetEvent args)
    {
        if (!TryComp(args.User, out XenoConstructionComponent? construction))
            return;

        var canReplace = args.Target != null &&
                         CanReplaceStructure((args.User, construction),
                             construction.BuildChoice,
                             args.Target.Value,
                             ent.Comp.CheckStructureSelected);

        var canCreate = args.Coords != null &&
                        CanSecreteOnTilePopup((args.User, construction),
                            construction.BuildChoice,
                            args.Coords.Value,
                            ent.Comp.CheckStructureSelected,
                            ent.Comp.CheckWeeds,
                            canReplace);

        if (!canReplace && !canCreate)
            args.Cancelled = true;
    }

    private void OnHiveConstructionNodeExamined(Entity<HiveConstructionNodeComponent> node, ref ExaminedEvent args)
    {
        var plasmaLeft = node.Comp.PlasmaCost - node.Comp.PlasmaStored;
        args.PushMarkup(Loc.GetString("cm-xeno-construction-plasma-left", ("construction", node.Owner), ("plasma", plasmaLeft)));
    }

    private void OnHiveConstructionNodeActivated(Entity<HiveConstructionNodeComponent> node, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        var plasmaLeft = node.Comp.PlasmaCost - node.Comp.PlasmaStored;
        if (!TryComp(user, out XenoConstructionComponent? xeno) ||
            plasmaLeft < FixedPoint2.Zero ||
            !TryComp(node, out TransformComponent? nodeTransform) ||
            !TryComp(user, out XenoPlasmaComponent? plasma))
        {
            return;
        }

        if (!InRangePopup(user, nodeTransform.Coordinates, xeno.OrderConstructionRange.Float()))
            return;

        var subtract = FixedPoint2.Min(plasma.Plasma, plasmaLeft);
        if (plasma.Plasma < 1 ||
            !_xenoPlasma.HasPlasmaPopup((user, plasma), subtract))
        {
            return;
        }

        var ev = new XenoConstructionAddPlasmaDoAfterEvent();
        var delay = xeno.OrderConstructionAddPlasmaDelay;
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, user, node)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnHiveCoreMapInit(Entity<HiveCoreComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        var coordinates = _transform.GetMoverCoordinates(ent).SnapToGrid(EntityManager, _map);
        Spawn(ent.Comp.Spawns, coordinates);
    }

    private void OnCheckAdjacentCollapse<T>(Entity<XenoConstructionSupportComponent> ent, ref T args)
    {
        if (!_transformQuery.TryComp(ent, out var xform) ||
            _transform.GetGrid((ent, xform)) is not { Valid: true } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return;
        }

        var coordinates = _transform.GetMapCoordinates((ent, xform));
        var indices = _mapSystem.TileIndicesFor(gridId, grid, coordinates);
        for (var i = 0; i < 4; i++)
        {
            var dir = (AtmosDirection) (1 << i);
            var pos = indices.Offset(dir);
            var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, pos);
            while (anchored.MoveNext(out var uid))
            {
                if (TerminatingOrDeleted(uid.Value) || EntityManager.IsQueuedForDeletion(uid.Value))
                    continue;

                if (!_constructionRequiresSupportQuery.HasComp(uid))
                    continue;

                if (!IsSupported((gridId, grid), pos))
                    QueueDel(uid);
            }
        }
    }

    public FixedPoint2? GetStructurePlasmaCost(EntProtoId prototype)
    {
        if (_prototype.TryIndex(prototype, out var buildChoice) &&
            buildChoice.TryGetComponent(out XenoConstructionPlasmaCostComponent? cost, _compFactory))
        {
            return cost.Plasma;
        }

        return null;
    }

    private FixedPoint2? GetStructurePlasmaCost(EntProtoId? building)
    {
        if (building is { } choice &&
            GetStructurePlasmaCost(choice) is { } cost)
        {
            return cost;
        }

        return null;
    }

    private bool TileSolidAndNotBlocked(EntityCoordinates target)
    {
        return target.GetTileRef(EntityManager, _map) is { } tile &&
               !tile.IsSpace() &&
               tile.GetContentTileDefinition().Sturdy &&
               !_turf.IsTileBlocked(tile, CollisionGroup.Impassable);
    }

    private bool InRangePopup(EntityUid xeno, EntityCoordinates target, float range)
    {
        var origin = _transform.GetMoverCoordinates(xeno);
        target = target.SnapToGrid(EntityManager, _map);
        if (!_transform.InRange(origin, target, range))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), target, xeno);
            return false;
        }

        if (_transform.InRange(origin, target, 0.75f))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-build-in-self"), target, xeno);
            return false;
        }

        return true;
    }

    private bool CanSecreteResin(Entity<XenoConstructionComponent> xeno, EntProtoId? buildChoice, EntityCoordinates
            target, bool checkStructureSelected, FixedPoint2? plasmaCost = null)
    {
        if (checkStructureSelected)
        {
            if (buildChoice == null)
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-select-structure"), target, xeno);
                return false;
            }

            var cost = plasmaCost ?? GetStructurePlasmaCost(buildChoice);
            if (cost != null && !_xenoPlasma.HasPlasmaPopup(xeno.Owner, cost.Value))
            {
                return false;
            }
        }

        if (!InRangePopup(xeno, target, xeno.Comp.BuildRange.Float()))
            return false;

        return true;
    }

    private bool CanReplaceStructure(Entity<XenoConstructionComponent> xeno, EntProtoId? choice, EntityUid target,
        bool checkStructureSelected)
    {
        if (!TryComp(target, out XenoConstructionUpgradeComponent? comp))
            return false;

        var cost = GetStructurePlasmaCost(choice);
        if (TryComp(target, out XenoConstructionPlasmaCostComponent? targetPlasmaCost))
            cost -= targetPlasmaCost.Plasma;

        if (!CanSecreteResin(xeno, choice, target.ToCoordinates(), checkStructureSelected, cost))
            return false;

        if (choice != comp.Proto)
            return false;

        return true;
    }

    private bool CanSecreteOnTilePopup(Entity<XenoConstructionComponent> xeno,
        EntProtoId? buildChoice,
        EntityCoordinates target,
        bool checkStructureSelected,
        bool checkWeeds,
        bool silent = false)
    {
        if (!CanSecreteResin(xeno, buildChoice, target, checkStructureSelected))
            return false;

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            if (!silent)
                _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-cant-build"), target, xeno);
            return false;
        }

        target = target.SnapToGrid(EntityManager, _map);
        if (checkWeeds && !_xenoWeeds.IsOnWeeds((gridId, grid), target))
        {
            if (!silent)
                _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-need-weeds"), target, xeno);
            return false;
        }

        if (!TileSolidAndNotBlocked(target))
        {
            if (!silent)
                _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-cant-build"), target, xeno);
            return false;
        }

        var tile = _mapSystem.CoordinatesToTile(gridId, grid, target);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_xenoConstructQuery.HasComp(uid))
            {
                if (!silent)
                    _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-cant-build"), target, xeno);
                return false;
            }
        }

        if (checkStructureSelected &&
            buildChoice is { } choice &&
            _prototype.TryIndex(choice, out var choiceProto) &&
            choiceProto.HasComponent<XenoConstructionRequiresSupportComponent>(_compFactory))
        {
            if (!IsSupported((gridId, grid), target))
            {
                if (!silent)
                    _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-requires-support", ("choice", choiceProto.Name)), target, xeno);
                return false;
            }
        }

        return true;
    }

    private bool CanOrderConstructionPopup(Entity<XenoConstructionComponent> xeno, EntityCoordinates target, EntProtoId? choice)
    {
        if (!CanSecreteOnTilePopup(xeno, xeno.Comp.BuildChoice, target, false, false))
            return false;

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return false;
        }

        var tile = _mapSystem.TileIndicesFor(gridId, grid, target);
        foreach (var direction in Directions)
        {
            var pos = SharedMapSystem.GetDirection(tile, direction);
            var directionEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, pos);

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (_hiveConstructionNodeQuery.TryGetComponent(ent, out var node) &&
                    node.BlockOtherNodes)
                {
                    _popup.PopupClient(Loc.GetString("cm-xeno-too-close-to-other-node", ("target", ent.Value)), xeno, xeno);
                    return false;
                }
            }
        }

        if (choice != null &&
            _prototype.TryIndex(choice, out var choiceProto) &&
            choiceProto.TryGetComponent(out HiveConstructionUniqueComponent? unique, _compFactory) &&
            OtherUniqueExists(unique.Id))
        {
            // server-only as the core may not be in the client's PVS bubble
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cm-xeno-unique-exists", ("choice", choiceProto.Name)), xeno, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    private bool OtherUniqueExists(EntProtoId id)
    {
        var uniques = EntityQueryEnumerator<HiveConstructionUniqueComponent>();
        while (uniques.MoveNext(out var otherUnique))
        {
            if (otherUnique.Id == id)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSupported(Entity<MapGridComponent> grid, EntityCoordinates coordinates)
    {
        var indices = _mapSystem.TileIndicesFor(grid, grid, coordinates);
        return IsSupported(grid, indices);
    }

    private bool IsSupported(Entity<MapGridComponent> grid, Vector2i tile)
    {
        var supported = false;
        for (var i = 0; i < 4; i++)
        {
            var dir = (AtmosDirection) (1 << i);
            var pos = tile.Offset(dir);
            var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, pos);
            while (anchored.MoveNext(out var uid))
            {
                if (TerminatingOrDeleted(uid.Value) || EntityManager.IsQueuedForDeletion(uid.Value))
                    continue;

                if (_constructionSupportQuery.HasComp(uid))
                {
                    supported = true;
                    break;
                }
            }

            if (supported)
                break;
        }

        return supported;
    }
}
