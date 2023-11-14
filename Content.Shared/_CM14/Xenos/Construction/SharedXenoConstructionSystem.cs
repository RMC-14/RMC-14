using Content.Shared._CM14.Xenos.Construction.Events;
using Content.Shared.Actions;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
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
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, Events.XenoPlantWeedsActionEvent>(OnXenoPlantWeeds);
        SubscribeLocalEvent<XenoComponent, Events.XenoChooseStructureActionEvent>(OnXenoChooseStructure);
        SubscribeLocalEvent<XenoComponent, XenoChooseStructureBuiMessage>(OnXenoChooseStructureBui);
        SubscribeLocalEvent<XenoComponent, Events.XenoSecreteStructureEvent>(OnXenoSecreteStructure);
        SubscribeLocalEvent<XenoComponent, Events.XenoSecreteStructureDoAfterEvent>(OnXenoSecreteStructureDoAfter);
        SubscribeLocalEvent<XenoWeedsComponent, AnchorStateChangedEvent>(OnWeedsAnchorChanged);
        SubscribeLocalEvent<XenoChooseConstructionActionComponent, XenoConstructionChosenEvent>(OnActionConstructionChosen);
    }

    private void OnXenoPlantWeeds(Entity<XenoComponent> ent, ref Events.XenoPlantWeedsActionEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(ent).SnapToGrid(EntityManager, _map);

        if (coordinates.GetGridUid(EntityManager) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return;
        }

        var position = _mapSystem.LocalToTile(gridUid, grid, coordinates);
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, position);

        while (enumerator.MoveNext(out var anchored))
        {
            if (HasComp<XenoWeedsComponent>(anchored))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-weeds-already-here"), ent.Owner, ent.Owner);
                return;
            }
        }

        if (!_xeno.TryRemovePlasmaPopup(ent, args.PlasmaCost))
            return;

        if (_net.IsServer)
            Spawn(args.Prototype, coordinates);
    }

    private void OnXenoChooseStructure(Entity<XenoComponent> xeno, ref Events.XenoChooseStructureActionEvent args)
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

    private void OnXenoSecreteStructure(Entity<XenoComponent> xeno, ref Events.XenoSecreteStructureEvent args)
    {
        if (xeno.Comp.BuildChoice == null || !CanBuildOnTilePopup(xeno, args.Target))
            return;

        var ev = new Events.XenoSecreteStructureDoAfterEvent(GetNetCoordinates(args.Target), xeno.Comp.BuildChoice.Value);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.BuildDelay, ev, xeno)
        {
            BreakOnUserMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoSecreteStructureDoAfter(Entity<XenoComponent> xeno, ref Events.XenoSecreteStructureDoAfterEvent args)
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

        // TODO CM14 stop collision for mobs until they move off
        if (_net.IsServer)
            Spawn(args.StructureId, coordinates);
    }

    private void OnWeedsAnchorChanged(Entity<XenoWeedsComponent> weeds, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
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

    private bool CanBuildOnTilePopup(Entity<XenoComponent> xeno, EntityCoordinates target)
    {
        var origin = _transform.GetMoverCoordinates(xeno);
        if (!origin.InRange(EntityManager, _transform, target, xeno.Comp.BuildRange) ||
            target.GetTileRef(EntityManager, _map) is not { } tile ||
            tile.IsSpace() ||
            !tile.GetContentTileDefinition().Sturdy ||
            _turf.IsTileBlocked(tile, CollisionGroup.Impassable))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), xeno, xeno);
            return false;
        }

        return true;
    }
}
