using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.Fruit.Events;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
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
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruct = default!;
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

        SubscribeLocalEvent<XenoFruitComponent, AfterAutoHandleStateEvent>(OnXenoFruitAfterState);
        SubscribeLocalEvent<XenoFruitComponent, GettingPickedUpAttemptEvent>(OnXenoFruitPickedUpAttempt);
        SubscribeLocalEvent<XenoFruitComponent, AfterInteractEvent>(OnXenoFruitAfterInteract);
        SubscribeLocalEvent<XenoFruitComponent, ComponentShutdown>(OnXenoFruitShutdown);
        SubscribeLocalEvent<XenoFruitComponent, EntityTerminatingEvent>(OnXenoFruitTerminating);

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
        if (xeno.Comp.FruitChoice is not { } fruitChoice)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-select-fruit"), args.Target, xeno.Owner);
            return;
        }

        var checkWeeds = true;

        // Check if given action can plant off weeds
        foreach (var (actionId, _) in _actions.GetActions(xeno))
        {
            if (TryComp(actionId, out XenoPlantFruitActionComponent? action))
            {
                checkWeeds = action.CheckWeeds;
                break;
            }
        }

        if (!CanPlantOnTilePopup(xeno, fruitChoice, args.Target, checkWeeds))
        {
            return;
        }

        // TODO: catch this event in XenoRestSystem.cs
        var attempt = new XenoPlantFruitAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        var coordinates = GetNetCoordinates(args.Target);
        var ev = new XenoPlantFruitDoAfterEvent(coordinates);
        args.Handled = true;
        var doAfter = new DoAfterArgs(EntityManager, xeno.Owner, xeno.Comp.PlantDelay, ev, xeno)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoPlantFruitDoAfter(Entity<XenoFruitPlanterComponent> xeno, ref XenoPlantFruitDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var chosenFruitId = xeno.Comp.FruitChoice;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!coordinates.IsValid(EntityManager))
        {
            return;
        }

        // Only deduct health and set cooldown if plasma removal succeeds
        foreach (var (actionId, _) in _actions.GetActions(xeno))
        {
            if (TryComp(actionId, out XenoPlantFruitActionComponent? action))
            {
                if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, action.PlasmaCost))
                {
                    _popup.PopupClient(Loc.GetString("cm-xeno-not-enough-plasma"), coordinates, xeno.Owner);
                    return;
                }

                _damageable.TryChangeDamage(xeno.Owner, action.HealthCost, interruptsDoAfters: false);
                _actions.SetCooldown(actionId, action.PlantCooldown);
                break;
            }
        }

        _audio.PlayPredicted(xeno.Comp.PlantSound, coordinates, xeno);

        args.Handled = true;

        if (_net.IsServer)
        {
            var entity = Spawn(chosenFruitId, coordinates);
            var fruit = EnsureComp<XenoFruitComponent>(entity);
            var xform = EnsureComp<TransformComponent>(entity);

            _transform.SetCoordinates(entity, coordinates);
            _transform.SetLocalRotation(entity, 0);

            SetFruitState((entity, fruit), XenoFruitState.Growing);
            _transform.AnchorEntity(entity, xform);

            // Add the fruit to the planter's list and vice-versa
            if (!xeno.Comp.PlantedFruit.Contains(entity))
                xeno.Comp.PlantedFruit.Add(entity);

            fruit.Planter = xeno.Owner;

            if (xeno.Comp.PlantedFruit.Count > xeno.Comp.MaxFruitAllowed)
            {
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-limit-exceeded"), coordinates, xeno.Owner);
                var removedFruit = xeno.Comp.PlantedFruit[0];
                xeno.Comp.PlantedFruit.Remove(removedFruit);
                QueueDel(removedFruit);
            }

            // TODO: update action icon to display number of planted fruit?

            var popupOthers = Loc.GetString("rmc-xeno-fruit-plant-success-others", ("xeno", xeno));
            var popupSelf = Loc.GetString("rmc-xeno-fruit-plant-success-self");
            _popup.PopupPredicted(popupSelf, popupOthers, xeno.Owner, xeno.Owner);
            _adminLogs.Add(LogType.RMCXenoPlantFruit, $"Xeno {ToPrettyString(xeno.Owner):xeno} planted {ToPrettyString(entity):entity} at {coordinates}");
        }
    }

    private void OnXenoFruitAfterState(Entity<XenoFruitComponent> fruit, ref AfterAutoHandleStateEvent args)
    {
        var ev = new XenoFruitStateChangedEvent();
        RaiseLocalEvent(fruit, ref ev);
    }

    private void OnXenoFruitPickedUpAttempt(Entity<XenoFruitComponent> fruit, ref GettingPickedUpAttemptEvent args)
    {
        if (!TryComp(fruit.Owner, out TransformComponent? xform) ||
            !HasComp<XenoFruitPlanterComponent>(fruit.Owner))
        {
            args.Cancel();
            return;
        }

        // Only pick up fully grown or already harvested fruit
        if (fruit.Comp.State == XenoFruitState.Growing)
        {
            args.Cancel();
            return;
        }

        if (fruit.Comp.State != XenoFruitState.Item)
        {
            _transform.Unanchor(fruit.Owner, xform);
            SetFruitState(fruit, XenoFruitState.Item);
        }
    }

    private void OnXenoFruitAfterInteract(Entity<XenoFruitComponent> fruit, ref AfterInteractEvent args)
    {
        if (fruit.Comp.State != XenoFruitState.Item)
        {
            return;
        }

        var user = args.User;
        if (!args.CanReach)
        {
            if (_timing.IsFirstTimePredicted)
                _popup.PopupCoordinates(Loc.GetString("cm-xeno-cant-reach-there"), args.ClickLocation, Filter.Local(), true);

            return;
        }

        if (HasComp<MarineComponent>(user))
        {
            // TODO: figure out what to do when marines interact with the fruit
            if (!args.Handled)
            {
                _hands.TryDrop(user, fruit, args.ClickLocation);
            }
            args.Handled = true;
            return;
        }

        if (HasComp<XenoComponent>(user))
        {
            // TODO: feed fruit to xeno
            if (!args.Handled)
            {

            }
            args.Handled = true;
            return;
        }
    }

    private void XenoFruitRemoved(Entity<XenoFruitComponent> fruit)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (fruit.Comp.Planter is not { } planter)
            return;

        if (!TryComp(planter, out XenoFruitPlanterComponent? planterComp))
            return;

        if (!planterComp.PlantedFruit.Contains(fruit.Owner))
            return;

        planterComp.PlantedFruit.Remove(fruit.Owner);
    }

    private void OnXenoFruitShutdown(Entity<XenoFruitComponent> fruit, ref ComponentShutdown args)
    {
        XenoFruitRemoved(fruit);
    }

    private void OnXenoFruitTerminating(Entity<XenoFruitComponent> fruit, ref EntityTerminatingEvent args)
    {
        XenoFruitRemoved(fruit);
    }

    private void SetFruitState(Entity<XenoFruitComponent> fruit, XenoFruitState state)
    {
        fruit.Comp.State = state;
        Dirty(fruit);

        var ev = new XenoFruitStateChangedEvent();
        RaiseLocalEvent(fruit, ref ev);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        // TODO: make fruit grow
        var fruitQuery = EntityQueryEnumerator<XenoFruitComponent, TransformComponent>();
        while (fruitQuery.MoveNext(out var uid, out var fruit, out var xform))
        {
            if (fruit.State != XenoFruitState.Growing)
                continue;

            fruit.GrowAt ??= time + fruit.GrowTime;

            if (time < fruit.GrowAt)
                continue;

            SetFruitState((uid, fruit), XenoFruitState.Grown);
        }
    }

    private bool InRangePopup(Entity<XenoFruitPlanterComponent> xeno, EntityCoordinates target, float range)
    {
        var origin = _transform.GetMoverCoordinates(xeno.Owner);
        target = target.SnapToGrid(EntityManager, _mapSystem);
        if (!_transform.InRange(origin, target, range))
        {
            return false;
        }

        return true;
    }

    public bool TileHasFruit(Entity<MapGridComponent> grid, EntityCoordinates coordinates)
    {
        var tile = _mapSystem.TileIndicesFor(grid.Owner, grid.Comp, coordinates);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid.Owner, grid.Comp, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<XenoFruitComponent>(uid))
                return true;
        }
        return false;
    }

    private bool CanPlantOnTilePopup(Entity<XenoFruitPlanterComponent> xeno,
        EntProtoId? fruitChoice, EntityCoordinates target, bool checkWeeds)
    {
        // Target out of range
        if (!InRangePopup(xeno, target, xeno.Comp.PlantRange.Float()))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), target, xeno.Owner);
            return false;
        }

        // Target not on grid
        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-cant-plant"), target, xeno.Owner);
            return false;
        }

        // Target not on weeds
        target = target.SnapToGrid(EntityManager, _map);
        if (checkWeeds && !_xenoWeeds.IsOnWeeds((gridId, grid), target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-need-weeds"), target, xeno.Owner);
            return false;
        }

        // TODO RMC14: check if weeds belong to our hive

        // TODO RMC14: check for resin holes

        // Target directly on weed node
        if (_xenoWeeds.IsOnWeeds((gridId, grid), target, true))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-weeds-source"), target, xeno.Owner);
            return false;
        }

        // Target on another fruit
        if (TileHasFruit((gridId, grid), target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-fruit-close"), target, xeno.Owner);
            return false;
        }

        // Target blocked
        if (!_xenoConstruct.TileSolidAndNotBlocked(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-cant-plant"), target, xeno.Owner);
            return false;
        }

        // Target already has xeno construction or egg on it
        var tile = _mapSystem.CoordinatesToTile(gridId, grid, target);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_xenoConstructQuery.HasComp(uid) || _xenoEggQuery.HasComp(uid))
            {
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-cant-plant"), target, xeno.Owner);
                return false;
            }
        }

        return true;
    }
}
