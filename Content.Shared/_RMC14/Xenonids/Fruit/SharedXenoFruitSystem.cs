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
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;

    [Dependency] private readonly CMHandsSystem _rmcHands = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
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
        if (args.Handled)
            return;

        args.Handled = true;

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

        // Only start cooldown if planting successful
        foreach (var (actionId, _) in _actions.GetActions(xeno))
        {
            if (TryComp(actionId, out XenoPlantFruitActionComponent? action))
            {
                _actions.SetCooldown(actionId, action.PlantCooldown);
            }
        }

        _audio.PlayPredicted(xeno.Comp.PlantSound, coordinates, xeno);

        args.Handled = true;

        if (_net.IsServer)
        {
            var entity = Spawn(args.FruitId, coordinates);
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

            // Deduct health
            _damageable.TryChangeDamage(xeno.Owner, fruit.CostHealth,
                ignoreResistances: true, interruptsDoAfters: false);

            // TODO: update action icon to display number of planted fruit?

            var popupOthers = Loc.GetString("rmc-xeno-fruit-plant-success-others", ("xeno", xeno.Owner));
            _popup.PopupPredicted(Loc.GetString("rmc-xeno-fruit-plant-success-self"), popupOthers, xeno.Owner, xeno.Owner);
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

    private DamageSpecifier? GetFruitHealthCost(EntProtoId? fruit)
    {
        if (fruit is { } choice)
        {
            if (_prototype.TryIndex(fruit, out var fruitChoice) &&
                fruitChoice.TryGetComponent(out XenoFruitComponent? fruitComp, _compFactory))
            {
                return fruitComp.CostHealth;
            }
        }

        return null;
    }

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

    private bool InRangePopup(Entity<XenoFruitPlanterComponent> xeno, EntityCoordinates target, float range)
    {
        var origin = _transform.GetMoverCoordinates(xeno.Owner);
        target = target.SnapToGrid(EntityManager, _map);
        if (!_transform.InRange(origin, target, range))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), target, xeno.Owner);
            return false;
        }

        return true;
    }

    private bool CanPlantOnTilePopup(Entity<XenoFruitPlanterComponent> xeno,
        EntProtoId? fruitChoice, EntityCoordinates target,
        bool checkFruitSelected, bool checkWeeds)
    {
        // Fruit not selected
        if (checkFruitSelected && fruitChoice == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-failed-select-fruit"), target, xeno.Owner);
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

        // Target out of range
        if (!InRangePopup(xeno, target, xeno.Comp.PlantRange.Float()))
            return false;

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

        // TODO: check if fruit planted nearby already
        // TODO: check for weed nodes as well
        // TODO: and resin holes (when implemented)
        // TODO: and whether the weeds even belong to our hive

        if (checkFruitSelected &&
            GetFruitPlasmaCost(fruitChoice) is { } cost &&
            !_xenoPlasma.HasPlasmaPopup(xeno.Owner, cost))
        {
            return false;
        }

        return true;
    }
}
