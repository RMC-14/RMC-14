using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Fruit.Events;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._RMC14.Xenonids.Fruit;

public sealed class SharedXenoFruitSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CMHandsSystem _rmcHands = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruct = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;

	private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
	private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    private EntityQuery<XenoConstructComponent> _xenoConstructQuery;
    private EntityQuery<XenoEggComponent> _xenoEggQuery;
    private EntityQuery<XenoWeedsComponent> _xenoWeedsQuery;

    public override void Initialize()
    {
        _xenoConstructQuery = GetEntityQuery<XenoConstructComponent>();
        _xenoEggQuery = GetEntityQuery<XenoEggComponent>();
        _xenoWeedsQuery = GetEntityQuery<XenoWeedsComponent>();

        SubscribeLocalEvent<XenoFruitPlanterComponent, XenoChooseFruitActionEvent>(OnXenoChooseFruitAction);
        SubscribeLocalEvent<XenoChooseFruitActionComponent, XenoFruitChosenEvent>(OnActionFruitChosen);

        SubscribeLocalEvent<XenoFruitPlanterComponent, XenoPlantFruitActionEvent>(OnXenoPlantFruitAction);
        SubscribeLocalEvent<XenoFruitPlanterComponent, XenoPlantFruitDoAfterEvent>(OnXenoPlantFruitDoAfter);
/*
        SubscribeLocalEvent<XenoFruitComponent, AfterAutoHandleStateEvent>(OnXenoFruitAfterState);
        SubscribeLocalEvent<XenoFruitComponent, GettingPickedUpAttemptEvent>(OnXenoFruitPickedUpAttempt);
        SubscribeLocalEvent<XenoFruitComponent, InteractUsingEvent>(OnXenoFruitInteractUsing);
        SubscribeLocalEvent<XenoFruitComponent, AfterInteractEvent>(OnXenoFruitAfterInteract);
        SubscribeLocalEvent<XenoFruitComponent, ActivateInWorldEvent>(OnXenoFruitActivateInWorld);
*/
        Subs.BuiEvents<XenoFruitPlanterComponent>(XenoChooseFruitUI.Key, subs =>
        {
            subs.Event<XenoChooseFruitBuiMsg>(OnXenoChooseFruitBui);
        });
    }

    private void OnXenoChooseFruitAction(Entity<XenoFruitPlanterComponent> xeno, ref XenoChooseFruitActionEvent args)
    {
        args.Handled = true;
        _ui.TryOpenUi(xeno.Owner, XenoChooseFruitUI.Key, xeno);
    }

    private void OnXenoChooseFruitBui(Entity<XenoFruitPlanterComponent> xeno, ref XenoChooseFruitBuiMsg args)
    {
        if (!xeno.Comp.CanPlant.Contains(args.FruitId))
            return;

        xeno.Comp.FruitChoice = args.FruitId;
        Dirty(xeno);

        var ev = new XenoFruitChosenEvent(args.FruitId);
        foreach (var (id, _) in _actions.GetActions(xeno))
        {
            RaiseLocalEvent(id, ref ev);
        }
    }

    private void OnActionFruitChosen(Entity<XenoChooseFruitActionComponent> xeno, ref XenoFruitChosenEvent args)
    {
        if (_actions.TryGetActionData(xeno, out var action) &&
            _prototype.HasIndex(args.Choice))
        {
            action.Icon = new SpriteSpecifier.EntityPrototype(args.Choice);
            Dirty(xeno, action);
        }
    }

    private void OnXenoPlantFruitAction(Entity<XenoFruitPlanterComponent> xeno, ref XenoPlantFruitActionEvent args)
    {
        if (xeno.Comp.FruitChoice is not { } choice ||
            !CanPlantOnTilePopup(xeno, choice, args.Target, true, true))
        {
            return;
        }

        // TODO: catch this event in XenoRestSystem.cs
        var attempt = new XenoPlantFruitAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        var coordinates = GetNetCoordinates(args.Target);
        var ev = new XenoPlantFruitDoAfterEvent(coordinates, choice);
        args.Handled = true;
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.PlantDelay, ev, xeno)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoPlantFruitDoAfter(Entity<XenoFruitPlanterComponent> xeno, ref XenoPlantFruitDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!coordinates.IsValid(EntityManager) ||
            !xeno.Comp.CanPlant.Contains(args.FruitId) ||
            !CanPlantOnTilePopup(xeno, args.FruitId, GetCoordinates(args.Coordinates), true, true))
        {
            return;
        }

        if (GetFruitPlasmaCost(args.FruitId) is { } cost &&
            !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, cost))
        {
            return;
        }

        args.Handled = true;

        if (_net.IsServer)
        {
            var fruit = Spawn(args.FruitId, coordinates);
            // TODO: add the fruit to the gardener's list
            _adminLogs.Add(LogType.RMCXenoPlantFruit, $"Xeno {ToPrettyString(xeno):xeno} planted {ToPrettyString(fruit):fruit} at {coordinates}");
        }

        // TODO: check if gardener's limit exceeded; if yes, remove first planted fruit
    }
/*
    private void OnXenoFruitAfterState(Entity<XenoFruitComponent> fruit, ref AfterAutoHandleStateEvent args)
    {
        var ev = new XenoFruitStateChangedEvent();
        RaiseLocalEvent(fruit, ref ev);
    }

    private void OnXenoFruitPickedUpAttempt(Entity<XenoFruitComponent> fruit, ref GettingPickedUpAttemptEvent args)
    {
        if (fruit.Comp.State == XenoFruitState.Growing)
            args.Cancel();
    }

    private void OnXenoFruitAfterInteract(Entity<XenoFruitComponent> fruit, ref AfterInteractEvent args)
    {

    }

    private void OnXenoFruitActivateInWorld(Entity<XenoFruitComponent> fruit, ref ActivateInWorldEvent args)
    {

    }

    private void OnXenoFruitInteractUsing(Entity<XenoFruitComponent> fruit, ref InteractUsingEvent args)
    {

    }
*/
    public FixedPoint2? GetFruitPlasmaCost(EntProtoId prototype)
    {
        if (_prototype.TryIndex(prototype, out var fruitChoice) &&
            fruitChoice.TryGetComponent(out XenoFruitComponent? fruit, _compFactory))
        {
            return fruit.CostPlasma;
        }

        return null;
    }

    private FixedPoint2? GetFruitPlasmaCost(EntProtoId? fruit)
    {
        if (fruit is { } choice &&
            GetFruitPlasmaCost(choice) is { } cost)
        {
            return cost;
        }

        return null;
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

        return true;
    }

    private bool CanPlantOnTilePopup(Entity<XenoFruitPlanterComponent> xeno, EntProtoId? fruitChoice, EntityCoordinates target, bool checkFruitSelected, bool checkWeeds)
    {
        if (checkFruitSelected && fruitChoice == null)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fruit-failed-select-fruit"), target, xeno);
            return false;
        }

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fruit-failed-cant-plant"), target, xeno);
            return false;
        }

        target = target.SnapToGrid(EntityManager, _map);
        if (checkWeeds && !_xenoWeeds.IsOnWeeds((gridId, grid), target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fruit-failed-need-weeds"), target, xeno);
            return false;
        }

        if (!InRangePopup(xeno, target, xeno.Comp.PlantRange.Float()))
            return false;

        if (!_xenoConstruct.TileSolidAndNotBlocked(target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fruit-failed-cant-plant"), target, xeno);
            return false;
        }

        var tile = _mapSystem.CoordinatesToTile(gridId, grid, target);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_xenoConstructQuery.HasComp(uid) || _xenoEggQuery.HasComp(uid))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-fruit-failed-cant-plant"), target, xeno);
                return false;
            }
        }

        if (checkFruitSelected &&
            GetFruitPlasmaCost(fruitChoice) is { } cost &&
            !_xenoPlasma.HasPlasmaPopup(xeno.Owner, cost))
        {
            return false;
        }

        return true;
    }
}
