using System.Collections.Immutable;
using System.Linq;
using Content.Shared._CM14.Xenos.Construction.Events;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Xenos.Construction;

public abstract class SharedXenoConstructionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private static readonly ImmutableArray<Direction> Directions = Enum.GetValues<Direction>()
        .Where(d => d != Direction.Invalid)
        .ToImmutableArray();

    private EntityQuery<XenoWeedsComponent> _weedsQuery;
    private EntityQuery<HiveConstructionNodeComponent> _hiveConstructionNodeQuery;

    public override void Initialize()
    {
        base.Initialize();

        _weedsQuery = GetEntityQuery<XenoWeedsComponent>();
        _hiveConstructionNodeQuery = GetEntityQuery<HiveConstructionNodeComponent>();

        SubscribeLocalEvent<XenoComponent, XenoPlantWeedsActionEvent>(OnXenoPlantWeedsAction);

        SubscribeLocalEvent<XenoComponent, XenoChooseStructureActionEvent>(OnXenoChooseStructureAction);
        SubscribeLocalEvent<XenoComponent, XenoChooseStructureBuiMessage>(OnXenoChooseStructureBui);

        SubscribeLocalEvent<XenoComponent, XenoSecreteStructureActionEvent>(OnXenoSecreteStructureAction);
        SubscribeLocalEvent<XenoComponent, XenoSecreteStructureDoAfterEvent>(OnXenoSecreteStructureDoAfter);

        SubscribeLocalEvent<XenoComponent, XenoOrderConstructionActionEvent>(OnXenoOrderConstructionAction);
        SubscribeLocalEvent<XenoComponent, XenoOrderConstructionBuiMessage>(OnXenoOrderConstructionBui);
        SubscribeLocalEvent<XenoComponent, XenoOrderConstructionDoAfterEvent>(OnXenoOrderConstructionDoAfter);
        SubscribeLocalEvent<XenoComponent, XenoConstructionAddPlasmaDoAfterEvent>(OnHiveConstructionNodeAddPlasmaDoAfter);

        SubscribeLocalEvent<XenoWeedsComponent, AnchorStateChangedEvent>(OnWeedsAnchorChanged);

        SubscribeLocalEvent<XenoChooseConstructionActionComponent, XenoConstructionChosenEvent>(OnActionConstructionChosen);

        SubscribeLocalEvent<HiveConstructionNodeComponent, ExaminedEvent>(OnHiveConstructionNodeExamined);
        SubscribeLocalEvent<HiveConstructionNodeComponent, InteractedNoHandEvent>(OnHiveConstructionNodeInteractedNoHand);
    }

    private void OnXenoPlantWeedsAction(Entity<XenoComponent> xeno, ref XenoPlantWeedsActionEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(xeno).SnapToGrid(EntityManager, _map);

        if (coordinates.GetGridUid(EntityManager) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return;
        }

        if (IsOnWeeds((gridUid, grid), coordinates))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-weeds-already-here"), xeno.Owner, xeno.Owner);
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup((xeno, xeno), args.PlasmaCost))
            return;

        if (_net.IsServer)
            Spawn(args.Prototype, coordinates);
    }

    private void OnXenoChooseStructureAction(Entity<XenoComponent> xeno, ref XenoChooseStructureActionEvent args)
    {
        if (_net.IsClient || !TryComp(xeno, out ActorComponent? actor))
            return;

        _ui.TryOpen(xeno, XenoChooseStructureUI.Key, actor.PlayerSession);
    }

    private void OnXenoChooseStructureBui(Entity<XenoComponent> xeno, ref XenoChooseStructureBuiMessage args)
    {
        if (!xeno.Comp.CanBuild.Contains(args.StructureId))
            return;

        xeno.Comp.BuildChoice = args.StructureId;

        Dirty(xeno);

        if (TryComp(xeno, out ActorComponent? actor))
            _ui.TryClose(xeno, XenoChooseStructureUI.Key, actor.PlayerSession);

        var ev = new XenoConstructionChosenEvent(args.StructureId);
        foreach (var (id, _) in _actions.GetActions(xeno))
        {
            RaiseLocalEvent(id, ref ev);
        }
    }

    private void OnXenoSecreteStructureAction(Entity<XenoComponent> xeno, ref XenoSecreteStructureActionEvent args)
    {
        if (xeno.Comp.BuildChoice == null || !CanBuildOnTilePopup(xeno, args.Target))
            return;

        var ev = new XenoSecreteStructureDoAfterEvent(GetNetCoordinates(args.Target), xeno.Comp.BuildChoice.Value);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.BuildDelay, ev, xeno)
        {
            BreakOnUserMove = true
        };

        // TODO CM14 building animation
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoSecreteStructureDoAfter(Entity<XenoComponent> xeno, ref XenoSecreteStructureDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!coordinates.IsValid(EntityManager) ||
            !xeno.Comp.CanBuild.Contains(args.StructureId) ||
            !CanBuildOnTilePopup(xeno, GetCoordinates(args.Coordinates)))
        {
            return;
        }

        // TODO CM14 construction plasma cost
        // TODO CM14 stop collision for mobs until they move off
        if (_net.IsServer)
            Spawn(args.StructureId, coordinates);
    }

    private void OnXenoOrderConstructionAction(Entity<XenoComponent> xeno, ref XenoOrderConstructionActionEvent args)
    {
        if (!CanOrderConstructionPopup(xeno, args.Target))
            return;

        if (_net.IsClient || !TryComp(xeno, out ActorComponent? actor))
            return;

        xeno.Comp.OrderingConstructionAt = args.Target;
        _ui.TryOpen(xeno, XenoOrderConstructionUI.Key, actor.PlayerSession);
    }

    private void OnXenoOrderConstructionBui(Entity<XenoComponent> xeno, ref XenoOrderConstructionBuiMessage args)
    {
        if (xeno.Comp.OrderingConstructionAt is not { } target ||
            !xeno.Comp.CanOrderConstruction.Contains(args.StructureId) ||
            !CanOrderConstructionPopup(xeno, target))
        {
            return;
        }

        if (!_prototype.TryIndex(args.StructureId, out var prototype))
            return;

        if (prototype.TryGetComponent(out HiveConstructionNodeComponent? node) &&
            !_xenoPlasma.HasPlasmaPopup(xeno, node.InitialPlasmaCost, false))
        {
            return;
        }

        var ev = new XenoOrderConstructionDoAfterEvent(args.StructureId, GetNetCoordinates(target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.OrderConstructionDelay, ev, xeno)
        {
            BreakOnUserMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);

        if (_net.IsClient || !TryComp(xeno, out ActorComponent? actor))
            return;

        _ui.TryOpen(xeno, XenoOrderConstructionUI.Key, actor.PlayerSession);
    }

    private void OnXenoOrderConstructionDoAfter(Entity<XenoComponent> xeno, ref XenoOrderConstructionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var target = GetCoordinates(args.Coordinates);
        if (!xeno.Comp.CanOrderConstruction.Contains(args.StructureId) ||
            !CanOrderConstructionPopup(xeno, target))
        {
            return;
        }

        if (!_prototype.TryIndex(args.StructureId, out var prototype))
            return;

        if (prototype.TryGetComponent(out HiveConstructionNodeComponent? node) &&
            !_xenoPlasma.TryRemovePlasmaPopup((xeno, xeno), node.InitialPlasmaCost))
        {
            return;
        }

        if (_net.IsServer)
            Spawn(args.StructureId, target.SnapToGrid(EntityManager, _map));
    }

    private void OnWeedsAnchorChanged(Entity<XenoWeedsComponent> weeds, ref AnchorStateChangedEvent args)
    {
        if (_net.IsServer && !args.Anchored)
            QueueDel(weeds);
    }

    private void OnActionConstructionChosen(Entity<XenoChooseConstructionActionComponent> ent, ref XenoConstructionChosenEvent args)
    {
        if (_actions.TryGetActionData(ent, out var action) &&
            _prototype.HasIndex(args.Choice))
        {
            action.Icon = new SpriteSpecifier.EntityPrototype(args.Choice);
            Dirty(ent, action);
        }
    }

    private void OnHiveConstructionNodeExamined(Entity<HiveConstructionNodeComponent> node, ref ExaminedEvent args)
    {
        var plasmaLeft = node.Comp.PlasmaCost - node.Comp.PlasmaStored;
        args.PushMarkup(Loc.GetString("cm-xeno-construction-plasma-left", ("construction", node.Owner), ("plasma", plasmaLeft)));
    }

    private void OnHiveConstructionNodeInteractedNoHand(Entity<HiveConstructionNodeComponent> node, ref InteractedNoHandEvent args)
    {
        var plasmaLeft = node.Comp.PlasmaCost - node.Comp.PlasmaStored;
        if (!TryComp(args.User, out XenoComponent? xeno) ||
            plasmaLeft < FixedPoint2.Zero ||
            !TryComp(node, out TransformComponent? nodeTransform))
        {
            return;
        }

        if (!InRangePopup(args.User, nodeTransform.Coordinates, xeno.OrderConstructionRange.Float()))
            return;

        var subtract = FixedPoint2.Min(xeno.Plasma, plasmaLeft);
        if (xeno.Plasma < 1 ||
            !_xenoPlasma.HasPlasmaPopup((args.User, xeno), subtract))
        {
            return;
        }

        var ev = new XenoConstructionAddPlasmaDoAfterEvent();
        var delay = xeno.OrderConstructionAddPlasmaDelay;
        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, args.User, node)
        {
            BreakOnUserMove = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnHiveConstructionNodeAddPlasmaDoAfter(Entity<XenoComponent> xeno, ref XenoConstructionAddPlasmaDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp(target, out HiveConstructionNodeComponent? node) ||
            !TryComp(target, out TransformComponent? transform))
        {
            return;
        }

        if (!InRangePopup(args.User, transform.Coordinates, xeno.Comp.OrderConstructionRange.Float()))
            return;

        var plasmaLeft = node.PlasmaCost - node.PlasmaStored;
        var subtract = FixedPoint2.Min(xeno.Comp.Plasma, plasmaLeft);
        if (plasmaLeft < FixedPoint2.Zero ||
            xeno.Comp.Plasma < 1 ||
            !_xenoPlasma.TryRemovePlasmaPopup((args.User, xeno), subtract))
        {
            return;
        }

        node.PlasmaStored += subtract;
        plasmaLeft = node.PlasmaCost - node.PlasmaStored;

        args.Handled = true;

        if (node.PlasmaStored < node.PlasmaCost)
        {
            _popup.PopupClient(
                Loc.GetString("cm-xeno-requires-more-plasma", ("construction", target), ("plasma", plasmaLeft)), target,
                args.User);
            return;
        }

        if (_net.IsClient)
            return;

        Spawn(node.Spawn, transform.Coordinates);
        QueueDel(target);
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
        if (!origin.InRange(EntityManager, _transform, target, range))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), xeno, xeno);
            return false;
        }

        return true;
    }

    private bool CanBuildOnTilePopup(Entity<XenoComponent> xeno, EntityCoordinates target)
    {
        // TODO CM14 calculate range limit from grid-snapped coordinates
        if (!InRangePopup(xeno, target, xeno.Comp.BuildRange.Float()) ||
            !TileSolidAndNotBlocked(target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), xeno, xeno);
            return false;
        }

        return true;
    }

    private bool CanOrderConstructionPopup(Entity<XenoComponent> xeno, EntityCoordinates target)
    {
        // TODO CM14 calculate range limit from grid-snapped coordinates
        if (!InRangePopup(xeno, target, xeno.Comp.OrderConstructionRange.Float()) ||
            !TileSolidAndNotBlocked(target) ||
            target.GetGridUid(EntityManager) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), xeno, xeno);
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

        return true;
    }

    public bool IsOnWeeds(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var coordinates = _transform.GetMoverCoordinates(entity, entity.Comp).SnapToGrid(EntityManager, _map);

        if (coordinates.GetGridUid(EntityManager) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return false;
        }

        return IsOnWeeds((gridUid, grid), coordinates);
    }

    private bool IsOnWeeds(Entity<MapGridComponent> grid, EntityCoordinates coordinates)
    {
        // TODO CM14 use collision enter and exit to calculate xenos being on weeds
        var position = _mapSystem.LocalToTile(grid, grid, coordinates);
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, position);

        while (enumerator.MoveNext(out var anchored))
        {
            if (_weedsQuery.HasComponent(anchored))
            {
                return true;
            }
        }

        return false;
    }
}
