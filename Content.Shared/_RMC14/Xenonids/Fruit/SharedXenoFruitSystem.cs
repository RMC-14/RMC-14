using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.Fruit.Effects;
using Content.Shared._RMC14.Xenonids.Fruit.Events;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;      // TODO: Add examine texts for when fruit are ready to be harvested
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
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Prototypes;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
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

// TODO: clean up all the dependencies once everything's working

namespace Content.Shared._RMC14.Xenonids.Fruit;

public sealed class SharedXenoFruitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly XenoShieldSystem _xenoShield = default!;

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

    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<XenoConstructComponent> _xenoConstructQuery;
    private EntityQuery<XenoEggComponent> _xenoEggQuery;
    private EntityQuery<XenoWeedsComponent> _xenoWeedsQuery;

    public override void Initialize()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _xenoConstructQuery = GetEntityQuery<XenoConstructComponent>();
        _xenoEggQuery = GetEntityQuery<XenoEggComponent>();
        _xenoWeedsQuery = GetEntityQuery<XenoWeedsComponent>();

        // Fruit choosing
        SubscribeLocalEvent<XenoFruitPlanterComponent, XenoFruitChooseActionEvent>(OnXenoFruitChooseAction);
        SubscribeLocalEvent<XenoFruitChooseActionComponent, XenoFruitChosenEvent>(OnActionFruitChosen);
        // Fruit interactions
        SubscribeLocalEvent<XenoFruitComponent, ActivateInWorldEvent>(OnXenoFruitActivateInWorld);
        SubscribeLocalEvent<XenoFruitComponent, GetVerbsEvent<AlternativeVerb>>(OnXenoFruitGetVerbs);
        //SubscribeLocalEvent<XenoFruitComponent, InteractHandEvent>(OnXenoFruitInteractHand);
        // Fruit planting
        SubscribeLocalEvent<XenoFruitPlanterComponent, XenoFruitPlantActionEvent>(OnXenoFruitPlantAction);
        // Fruit harvesting
        SubscribeLocalEvent<XenoFruitComponent, XenoFruitHarvestDoAfterEvent>(OnXenoFruitHarvestDoAfter);
        // Fruit consuming
        SubscribeLocalEvent<XenoFruitComponent, XenoFruitConsumeDoAfterEvent>(OnXenoFruitConsumeDoAfter);
        // Fruit effects
        SubscribeLocalEvent<GardenerShieldComponent, RemovedShieldEvent>(OnShieldRemove);
        SubscribeLocalEvent<XenoFruitEffectRegenComponent, XenoFruitEffectRegenEvent>(OnXenoFruitEffectRegen);
        SubscribeLocalEvent<XenoFruitEffectPlasmaComponent, XenoFruitEffectPlasmaEvent>(OnXenoFruitEffectPlasma);
        SubscribeLocalEvent<XenoFruitEffectSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnXenoFruitSpeedRefresh);
        SubscribeLocalEvent<XenoFruitEffectSpeedComponent, ComponentShutdown>(OnXenoFruitEffectSpeedShutdown);
        SubscribeLocalEvent<XenoFruitEffectHasteComponent, MeleeHitEvent>(OnXenoFruitEffectHasteHit);
        SubscribeLocalEvent<XenoFruitEffectHasteComponent, ComponentShutdown>(OnXenoFruitEffectHasteShutdown);

        //SubscribeLocalEvent<XenoFruitComponent, GettingPickedUpAttemptEvent>(OnXenoFruitPickedUpAttempt);
        //SubscribeLocalEvent<XenoFruitComponent, AfterInteractEvent>(OnXenoFruitAfterInteract);
        //SubscribeLocalEvent<XenoFruitComponent, AfterInteractUsingEvent>(OnXenoFruitAfterInteractUsing);
        // Fruit state updates
        SubscribeLocalEvent<XenoFruitComponent, AfterAutoHandleStateEvent>(OnXenoFruitAfterState);
        SubscribeLocalEvent<XenoFruitComponent, ComponentShutdown>(OnXenoFruitShutdown);
        SubscribeLocalEvent<XenoFruitComponent, EntityTerminatingEvent>(OnXenoFruitTerminating);

        Subs.BuiEvents<XenoFruitPlanterComponent>(XenoFruitChooseUI.Key, subs =>
        {
            subs.Event<XenoFruitChooseBuiMsg>(OnXenoFruitChooseBui);
        });
    }

    // Fruit selection
    #region Choosing

    private void OnXenoFruitChooseAction(Entity<XenoFruitPlanterComponent> xeno, ref XenoFruitChooseActionEvent args)
    {
        args.Handled = true;
        _ui.TryOpenUi(xeno.Owner, XenoFruitChooseUI.Key, xeno);
    }

    private void OnXenoFruitChooseBui(Entity<XenoFruitPlanterComponent> xeno, ref XenoFruitChooseBuiMsg args)
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

    private void OnActionFruitChosen(Entity<XenoFruitChooseActionComponent> xeno, ref XenoFruitChosenEvent args)
    {
        if (_actions.TryGetActionData(xeno, out var action) &&
            _prototype.HasIndex(args.Choice))
        {
            action.Icon = new SpriteSpecifier.EntityPrototype(args.Choice);
            Dirty(xeno, action);
        }
    }

    #endregion

    // Fruit interactions
    #region Interactions

    private void OnXenoFruitActivateInWorld(Entity<XenoFruitComponent> fruit, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.User;

        // TODO: give xenos a way of eating the fruit without harvesting

        if (fruit.Comp.State != XenoFruitState.Item)
        {
            TryHarvest(fruit, user);
            return;
        }

        TryConsume(fruit, user);
    }

    private void OnXenoFruitGetVerbs(EntityUid fruit, XenoFruitComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (comp.State != XenoFruitState.Item)
        {
            // Harvest verb
            AlternativeVerb harvestVerb = new()
            {
                Act = () => TryHarvest((fruit, comp), args.User),
                Text = Loc.GetString("rmc-xeno-fruit-verb-harvest"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png")),
                Priority = 1
            };
            args.Verbs.Add(harvestVerb);
        }

        // Consume verb
        AlternativeVerb consumeVerb = new()
        {
            Act = () => TryConsume((fruit, comp), args.User),
            Text = Loc.GetString("rmc-xeno-fruit-verb-consume"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png"))
        };
        args.Verbs.Add(consumeVerb);
    }

    #endregion

    // Fruit planting
    #region Planting

    private bool InRangePopup(Entity<XenoFruitPlanterComponent> xeno, EntityCoordinates target, float range)
    {
        var origin = _transform.GetMoverCoordinates(xeno.Owner);
        target = target.SnapToGrid(EntityManager, _map);
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
        EntityCoordinates target, bool checkWeeds, out string popup)
    {
        popup = Loc.GetString("rmc-xeno-fruit-plant-failed");

        // Target out of range
        if (!InRangePopup(xeno, target, xeno.Comp.PlantRange.Float()))
        {
            popup = Loc.GetString("cm-xeno-cant-reach-there");
            return false;
        }

        // Target not on grid
        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            popup = Loc.GetString("rmc-xeno-fruit-plant-failed");
            return false;
        }

        // Target not on weeds
        target = target.SnapToGrid(EntityManager, _map);
        if (checkWeeds && !_xenoWeeds.IsOnWeeds((gridId, grid), target))
        {
            popup = Loc.GetString("rmc-xeno-fruit-plant-failed-weeds");
            return false;
        }

        // TODO RMC14: check if weeds belong to our hive

        // TODO RMC14: check for resin holes

        // Target directly on weed node
        if (_xenoWeeds.IsOnWeeds((gridId, grid), target, true))
        {
            popup = Loc.GetString("rmc-xeno-fruit-plant-failed-node");
            return false;
        }

        // Target on another fruit
        if (TileHasFruit((gridId, grid), target))
        {
            popup = Loc.GetString("rmc-xeno-fruit-plant-failed-fruit");
            return false;
        }

        // Target blocked
        if (!_xenoConstruct.TileSolidAndNotBlocked(target))
        {
            popup = Loc.GetString("rmc-xeno-fruit-plant-failed");
            return false;
        }

        // Target has egg, xeno construct or other obstruction on it
        var tile = _mapSystem.CoordinatesToTile(gridId, grid, target);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<StrapComponent>(uid) ||
                HasComp<XenoEggComponent>(uid) ||
                HasComp<XenoConstructComponent>(uid) ||
                _tags.HasAnyTag(uid.Value, StructureTag, AirlockTag))
            {
                popup = Loc.GetString("rmc-xeno-fruit-plant-failed");
                return false;
            }
        }

        // One more check, just for good measure
        if (_turf.IsTileBlocked(gridId, tile, Impassable | MidImpassable | HighImpassable, grid))
        {
            popup = Loc.GetString("rmc-xeno-fruit-plant-failed");
            return false;
        }

        return true;
    }

    private void OnXenoFruitPlantAction(Entity<XenoFruitPlanterComponent> xeno, ref XenoFruitPlantActionEvent args)
    {
        // Check for selected fruit
        if (xeno.Comp.FruitChoice is not { } fruitChoice)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-plant-failed-select"), args.Target, xeno.Owner);
            return;
        }

        foreach (var (actionId, _) in _actions.GetActions(xeno))
        {
            // Find the planting action
            if (!TryComp(actionId, out XenoFruitPlantActionComponent? action))
                continue;

            // Check if target location valid
            if (!CanPlantOnTilePopup(xeno, args.Target, action.CheckWeeds, out var popup))
            {
                _popup.PopupClient(popup, args.Target, xeno.Owner);
                return;
            }

            // TODO: catch this event in XenoRestSystem.cs
            var attempt = new XenoFruitPlantAttemptEvent();
            RaiseLocalEvent(xeno, ref attempt);

            if (attempt.Cancelled)
                return;

            // Remove plasma if possible
            if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, action.PlasmaCost))
            {
                return;
            }

            // Deduct health and apply cooldown
            _damageable.TryChangeDamage(xeno.Owner, action.HealthCost, interruptsDoAfters: false);
            _actions.SetCooldown(actionId, action.PlantCooldown);

            break;
        }

        var coordinates = args.Target;
        if (!coordinates.IsValid(EntityManager))
            return;

        _audio.PlayPredicted(xeno.Comp.PlantSound, coordinates, xeno);

        // Was another fruit sacrificed to grow this one?
        var fruitOverflow = false;

        if (_net.IsServer)
        {
            var entity = Spawn(xeno.Comp.FruitChoice, coordinates);
            var fruit = EnsureComp<XenoFruitComponent>(entity);
            var xform = Transform(entity);

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
                fruitOverflow = true;
                var removedFruit = xeno.Comp.PlantedFruit[0];
                xeno.Comp.PlantedFruit.Remove(removedFruit);
                QueueDel(removedFruit);
            }

            // TODO: update action icon to display number of planted fruit?

            _adminLogs.Add(LogType.RMCXenoFruitPlant, $"Xeno {ToPrettyString(xeno.Owner):xeno} planted {ToPrettyString(entity):entity} at {coordinates}");
        }

        var popupSelf = fruitOverflow ? Loc.GetString("rmc-xeno-fruit-plant-limit-exceeded")
            : Loc.GetString("rmc-xeno-fruit-plant-success-self");
        var popupOthers = Loc.GetString("rmc-xeno-fruit-plant-success-others", ("xeno", xeno));
        _popup.PopupPredicted(popupSelf, popupOthers, xeno.Owner, xeno.Owner);
    }

    #endregion

    // Fruit harvesting
    #region Harvesting
    private bool TryHarvest(Entity<XenoFruitComponent> fruit, EntityUid user)
    {
        // Check if fruit has already been harvested
        if (fruit.Comp.State == XenoFruitState.Item)
        {
            return false;
        }

        // Can't harvest without hands
        if (!HasComp<HandsComponent>(user))
        {
            return false;
        }

        // TODO RMC14: check for hive as well
        // Non-planter xenos can't harvest growing fruit
        if (HasComp<XenoComponent>(user) &&
            !HasComp<XenoFruitPlanterComponent>(user) &&
            fruit.Comp.State == XenoFruitState.Growing)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-pick-failed-not-mature"), user, user);
            return false;
        }

        var ev = new XenoFruitHarvestDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, fruit.Comp.HarvestDelay, ev, fruit, fruit.Owner)
        {
            NeedHand = true,
            BreakOnMove = true,
            RequireCanInteract = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            if (HasComp<XenoComponent>(user))
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-failed-xeno"), user, user);
            else
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-failed-marine"), user, user);

            return false;
        }

        if (HasComp<XenoComponent>(user))
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-start-xeno", ("fruit", fruit)), user, user);
        else
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-start-marine", ("fruit", fruit)), user, user);

        return true;
    }

    private void OnXenoFruitHarvestDoAfter(Entity<XenoFruitComponent> fruit, ref XenoFruitHarvestDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        args.Handled = true;

        _audio.PlayPredicted(fruit.Comp.HarvestSound, fruit, args.User);

        // Fruit not mature
        if (fruit.Comp.State == XenoFruitState.Growing)
        {
            if (HasComp<XenoComponent>(args.User))
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-failed-not-mature-xeno", ("fruit", fruit)), args.User, args.User);
            else
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-failed-not-mature-marine", ("fruit", fruit)), args.User, args.User);

            if (_net.IsClient)
                return;

            if (!TerminatingOrDeleted(fruit) && !EntityManager.IsQueuedForDeletion(fruit))
                QueueDel(fruit);

            return;
        }

        // Mark as harvested to display correct pop-up later
        fruit.Comp.IsHarvested = true;

        if (HasComp<XenoComponent>(args.User))
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-success-xeno", ("fruit", fruit)), args.User, args.User);
        else
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-harvest-success-marine", ("fruit", fruit)), args.User, args.User);

        var xform = Transform(fruit);
        _transform.Unanchor(fruit, xform);
        SetFruitState(fruit, XenoFruitState.Item);
        _hands.TryPickup(args.User, fruit);
    }

    #endregion

    // Eating/feeding the fruit; applying effects
    #region Consuming
    private bool TryConsume(Entity<XenoFruitComponent> fruit, EntityUid user)
    {
        if (!HasComp<XenoComponent>(user))
        {
            // TODO RMC14: allow non-xenos to eat the forbidden froot
            return false;
        }

        // Check if fruit is ripe
        if (fruit.Comp.State == XenoFruitState.Growing)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-pick-failed-not-mature", ("fruit", fruit)), user, user);
            return false;
        }

        // Check if fruit is already being consumed by anyone
        if (fruit.Comp.IsPicked)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-pick-failed-already", ("fruit", fruit)), user, user);
            return false;
        }

        // TODO RMC14: check for hive

        // Check if user is already under the effects of consumed fruit
        if (HasComp<XenoFruitSpeedComponent>(fruit) && HasComp<XenoFruitEffectSpeedComponent>(user))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-effect-already"), user, user);
            return false;
        }

        // Check if fruit can be consumed at current (full) health
        if (!TryComp(user, out DamageableComponent? damage))
            return false;

        if (!fruit.Comp.CanConsumeAtFull && damage.TotalDamage == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-pick-failed-health-full"), user, user);
            return false;
        }

        var ev = new XenoFruitConsumeDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, fruit.Comp.ConsumeDelay, ev, fruit, user)
        {
            NeedHand = true,
            BreakOnMove = true,
            RequireCanInteract = true
        };

        var popupSelf = Loc.GetString("rmc-xeno-fruit-eat-fail-self", ("fruit", fruit));
        var popupOthers = Loc.GetString("rmc-xeno-fruit-eat-fail-others", ("fruit", fruit), ("xeno", user));

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupPredicted(popupSelf, popupOthers, user, user);
            return false;
        }

        fruit.Comp.IsPicked = true;

        popupSelf = Loc.GetString("rmc-xeno-fruit-eat-start-self", ("fruit", fruit));
        popupOthers = Loc.GetString("rmc-xeno-fruit-eat-start-others", ("fruit", fruit), ("xeno", user));
        _popup.PopupPredicted(popupSelf, popupOthers, user, user);
        return true;
    }

    private bool TryFeed(Entity<XenoFruitComponent> fruit, EntityUid user, EntityUid target)
    {
        // Non-xenos can't feed the froot
        if (!HasComp<XenoComponent>(user))
            return false;

        // Can't feed non-xenos
        if (!HasComp<XenoComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-feed-refuse", ("target",target), ("fruit", fruit)), user, user);
            return false;
        }

        // Check if target is alive
        if (_mobState.IsDead(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-feed-dead", ("target",target), ("fruit", fruit)), user, user);
            return false;
        }

        // TODO RMC14: check for hive

        // TODO RMC14: Check if xeno is already under the effects of a fruit

        // Check if fruit can be consumed at current (full) health
        if (!TryComp(target, out DamageableComponent? damage))
            return false;

        if (!fruit.Comp.CanConsumeAtFull && damage.TotalDamage == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-pick-failed-health-full-target"), user, user);
            return false;
        }

        var ev = new XenoFruitConsumeDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, fruit.Comp.ConsumeDelay, ev, fruit, target)
        {
            NeedHand = true,
            BreakOnMove = true,
            RequireCanInteract = true
        };

        var popupSelf = Loc.GetString("rmc-xeno-fruit-feed-fail-self", ("target", target), ("fruit", fruit));
        var popupTarget = Loc.GetString("rmc-xeno-fruit-feed-fail-target", ("user", user), ("fruit", fruit));
        var popupOthers = Loc.GetString("rmc-xeno-fruit-feed-fail-others", ("user", user), ("target", target), ("fruit", fruit));

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient(popupTarget, target, target);
            _popup.PopupPredicted(popupSelf, popupOthers, user, user);
            return false;
        }

        fruit.Comp.IsPicked = true;

        popupSelf = Loc.GetString("rmc-xeno-fruit-feed-start-self", ("target", target), ("fruit", fruit));
        popupTarget = Loc.GetString("rmc-xeno-fruit-feed-start-target", ("user", user), ("fruit", fruit));
        popupOthers = Loc.GetString("rmc-xeno-fruit-feed-start-others", ("user", user), ("target", target), ("fruit", fruit));
        _popup.PopupClient(popupTarget, target, target);
        _popup.PopupPredicted(popupSelf, popupOthers, user, user);
        return true;
    }

    // Handles both eating and feeding
    private void OnXenoFruitConsumeDoAfter(Entity<XenoFruitComponent> fruit, ref XenoFruitConsumeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            fruit.Comp.IsPicked = false;
            return;
        }

        args.Handled = true;

        if (args.Target is not { } target)
            return;

        var user = args.User;

        // Fruit was consumed/destroyed during the doafter
        if (TerminatingOrDeleted(fruit) || EntityManager.IsQueuedForDeletion(fruit))
        {
            if (user == target)
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-pick-failed-no-longer", ("fruit", fruit)), user, user);

            return;
        }

        // Display the correct popups
        var popupSelf = Loc.GetString("rmc-xeno-fruit-feed-success-self", ("target", target), ("fruit", fruit));
        var popupTarget = Loc.GetString("rmc-xeno-fruit-feed-success-target", ("user", user), ("fruit", fruit));
        var popupOthers = Loc.GetString("rmc-xeno-fruit-feed-success-others", ("user", user), ("target", target), ("fruit", fruit));

        // TODO: make either this or the effect pop-up bigger to stop them from overlapping
        if (user != target)
        {
            _popup.PopupClient(popupTarget, target, target);
        }
        else
        {
            popupSelf = Loc.GetString("rmc-xeno-fruit-eat-success-self", ("fruit", fruit));
            popupOthers = Loc.GetString("rmc-xeno-fruit-eat-success-others", ("xeno", target), ("fruit", fruit));
        }

        // Display general eating/feeding pop-ups
        _popup.PopupPredicted(popupSelf, popupOthers, user, user);
        ApplyFruitEffects(fruit, target);

        // Send pop-up to target describing effect
        _popup.PopupClient(Loc.GetString(fruit.Comp.Popup), target, target, PopupType.Medium);

        // If neither the user nor the target were the planter, inform the planter as well
        if (fruit.Comp.Planter is { } planter)
            if (target != planter && user != planter)
                _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-consumed"), planter, planter);

        if (_net.IsServer)
            QueueDel(fruit);
    }



    #endregion

    // Fruit effect management
    #region Effects

    private void ApplyFruitEffects(Entity<XenoFruitComponent> fruit, EntityUid target)
    {
        // There has to be a better way to do this, but it'll take someone smarter than me to develop that

        if (TryComp(fruit, out XenoFruitHealComponent? healComp))
        {
            _xeno.HealDamage((target, null), healComp.HealAmount);
        }

        if (TryComp(fruit, out XenoFruitRegenComponent? regenComp))
        {
            ApplyFruitRegen((fruit.Owner, regenComp), target);
        }

        if (TryComp(fruit, out XenoFruitPlasmaComponent? plasmaComp))
        {
            ApplyFruitPlasma((fruit.Owner, plasmaComp), target);
        }

        if (TryComp(fruit, out XenoFruitShieldComponent? shieldComp))
        {
            ApplyFruitShield((fruit.Owner, shieldComp), target);
        }

        if (TryComp(fruit, out XenoFruitSpeedComponent? speedComp))
        {
            ApplyFruitSpeed((fruit.Owner, speedComp), target);
        }

        if (TryComp(fruit, out XenoFruitHasteComponent? hasteComp))
        {
            ApplyFruitHaste((fruit.Owner, hasteComp), target);
        }
    }

    // Health regen (greater and unstable)
    private void ApplyFruitRegen(Entity<XenoFruitRegenComponent> fruit, EntityUid target)
    {
        var comp = EnsureComp<XenoFruitEffectRegenComponent>(target);

        comp.TickPeriod = fruit.Comp.TickPeriod;
        comp.TicksLeft = fruit.Comp.TickCount;
        comp.RegenPerTick = fruit.Comp.RegenPerTick;
    }

    private void OnXenoFruitEffectRegen(Entity<XenoFruitEffectRegenComponent> xeno, ref XenoFruitEffectRegenEvent ev)
    {
        _xeno.HealDamage((xeno.Owner, null), xeno.Comp.RegenPerTick);
    }

    // Plasma regen
    private void ApplyFruitPlasma(Entity<XenoFruitPlasmaComponent> fruit, EntityUid target)
    {
        var comp = EnsureComp<XenoFruitEffectPlasmaComponent>(target);

        comp.TickPeriod = fruit.Comp.TickPeriod;
        comp.TicksLeft = fruit.Comp.TickCount;
        comp.RegenPerTick = fruit.Comp.RegenPerTick;
    }

    private void OnXenoFruitEffectPlasma(Entity<XenoFruitEffectPlasmaComponent> xeno, ref XenoFruitEffectPlasmaEvent ev)
    {
        _xenoPlasma.RegenPlasma((xeno.Owner, null), xeno.Comp.RegenPerTick);
    }

    // Movement speed
    private void ApplyFruitSpeed(Entity<XenoFruitSpeedComponent> fruit, EntityUid target)
    {
        // Add the speed effect component from the fruit to the xeno
        var comp = EnsureComp<XenoFruitEffectSpeedComponent>(target);

        comp.Duration = fruit.Comp.Duration;
        comp.SpeedModifier = fruit.Comp.SpeedModifier;

        _movementSpeed.RefreshMovementSpeedModifiers(target);
    }

    private void OnXenoFruitEffectSpeedShutdown(Entity<XenoFruitEffectSpeedComponent> xeno, ref ComponentShutdown ev)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-effect-end"), xeno.Owner, xeno.Owner);
        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnXenoFruitSpeedRefresh(Entity<XenoFruitEffectSpeedComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        var comp = xeno.Comp;
        var speed = 1 + comp.SpeedModifier.Float();
        args.ModifySpeed(speed, speed);
    }

    // Shield (unstable fruit)
    private void ApplyFruitShield(Entity<XenoFruitShieldComponent> fruit, EntityUid target)
    {
        var ent = target;
        var comp = fruit.Comp;
        var maxShield = _mobThreshold.GetThresholdForState(ent, MobState.Dead) * comp.ShieldRatio;
        var shieldAmount = maxShield < comp.ShieldAmount ? maxShield : comp.ShieldAmount;

        _xenoShield.ApplyShield(ent, XenoShieldSystem.ShieldType.Gardener, shieldAmount,
            comp.Duration, comp.ShieldDecay.Double(), true, shieldAmount.Double());

        EnsureComp<GardenerShieldComponent>(target);
    }

    public void OnShieldRemove(Entity<GardenerShieldComponent> ent, ref RemovedShieldEvent args)
    {
        if (args.Type == XenoShieldSystem.ShieldType.Gardener)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-defensive-shield-end"), ent, ent, PopupType.MediumCaution);

        RemCompDeferred<GardenerShieldComponent>(ent);
    }

    // Cooldown reduction (spore fruit)
    public void ApplyFruitHaste(Entity<XenoFruitHasteComponent> fruit, EntityUid target)
    {
        var comp = EnsureComp<XenoFruitEffectHasteComponent>(target);

        comp.Duration = fruit.Comp.Duration;
        comp.ReductionMax = fruit.Comp.ReductionMax;
        comp.ReductionPerSlash = fruit.Comp.ReductionPerSlash;
        // Only reset current reduction if user was not already under the effect
        comp.ReductionCurrent = comp.ReductionCurrent != null ? comp.ReductionCurrent : 0;
        // Mark as null for Update() to refresh end time in case user was already under the effect
        comp.EndAt = null;
    }

    private void RefreshUseDelays(EntityUid user, FixedPoint2 amount)
    {
        // Reduces/resets the use-delays and cooldowns of all actions

        foreach (var (actionId, _) in _actions.GetActions(user))
        {
            if (!HasComp<ActionReducedUseDelayComponent>(actionId))
                continue;

            var ev = new ActionReducedUseDelayEvent(amount);
            RaiseLocalEvent(actionId, ev);
        }
    }

    private void OnXenoFruitEffectHasteHit(Entity<XenoFruitEffectHasteComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, entity))
                continue;

            if (!_mobStateQuery.TryComp(entity, out var mobState) ||
                mobState.CurrentState == MobState.Dead)
                continue;

            break;
        }

        if (xeno.Comp.ReductionCurrent >= xeno.Comp.ReductionMax)
            return;

        xeno.Comp.ReductionCurrent += xeno.Comp.ReductionPerSlash;

        // Reduce cooldowns and usedelays
        RefreshUseDelays(xeno.Owner, xeno.Comp.ReductionCurrent);
    }

    private void OnXenoFruitEffectHasteShutdown(Entity<XenoFruitEffectHasteComponent> xeno, ref ComponentShutdown ev)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-effect-end"), xeno.Owner, xeno.Owner);

        // Reset cooldowns and usedelays to default
        RefreshUseDelays(xeno.Owner, 0);
    }

    #endregion

    // Fruit state updates
    #region State updates

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

        // TODO RMC14: rework desctruction popup to use DestructionEventArgs

        // TODO RMC14: raise event on consumption to display popup instead of *this*

        if (fruit.Comp.IsPicked)
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-consumed"), planter, planter);
        else
            _popup.PopupClient(Loc.GetString("rmc-xeno-fruit-destroyed"), planter, planter);

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

    private void OnXenoFruitAfterState(Entity<XenoFruitComponent> fruit, ref AfterAutoHandleStateEvent args)
    {
        var ev = new XenoFruitStateChangedEvent();
        RaiseLocalEvent(fruit, ref ev);
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

        // Handle fruit growing
        var fruitQuery = EntityQueryEnumerator<XenoFruitComponent>();
        while (fruitQuery.MoveNext(out var uid, out var fruit))
        {
            if (fruit.State != XenoFruitState.Growing)
                continue;

            fruit.GrowAt ??= time + fruit.GrowTime;

            if (time < fruit.GrowAt)
                continue;

            SetFruitState((uid, fruit), XenoFruitState.Grown);
        }

        // Handle fruit effects
        // Speed
        var speedQuery = EntityQueryEnumerator<XenoFruitEffectSpeedComponent>();
        while (speedQuery.MoveNext(out var uid, out var effect))
        {
            effect.EndAt ??= time + effect.Duration;

            if (time < effect.EndAt)
                continue;

            RemCompDeferred<XenoFruitEffectSpeedComponent>(uid);
        }

        // Haste (cooldown reduction)
        var hasteQuery = EntityQueryEnumerator<XenoFruitEffectHasteComponent>();
        while (hasteQuery.MoveNext(out var uid, out var effect))
        {
            effect.EndAt ??= time + effect.Duration;

            if (time < effect.EndAt)
                continue;

            RemCompDeferred<XenoFruitEffectHasteComponent>(uid);
        }

        // Health regen
        var regenQuery = EntityQueryEnumerator<XenoFruitEffectRegenComponent>();
        while (regenQuery.MoveNext(out var uid, out var effect))
        {
            effect.NextTickAt ??= time + effect.TickPeriod;

            if (time < effect.NextTickAt)
                continue;

            if (effect.TicksLeft <= 0)
            {
                RemCompDeferred<XenoFruitEffectRegenComponent>(uid);
                continue;
            }

            var ev = new XenoFruitEffectRegenEvent();
            RaiseLocalEvent(uid, ev);

            effect.TicksLeft--;
            effect.NextTickAt = time + effect.TickPeriod;
        }

        // Plasma regen
        var plasmaQuery = EntityQueryEnumerator<XenoFruitEffectPlasmaComponent>();
        while (plasmaQuery.MoveNext(out var uid, out var effect))
        {
            effect.NextTickAt ??= time + effect.TickPeriod;

            if (time < effect.NextTickAt)
                continue;

            if (effect.TicksLeft <= 0)
            {
                RemCompDeferred<XenoFruitEffectPlasmaComponent>(uid);
                continue;
            }

            var ev = new XenoFruitEffectPlasmaEvent();
            RaiseLocalEvent(uid, ev);

            effect.TicksLeft--;
            effect.NextTickAt = time + effect.TickPeriod;
        }
    }
    #endregion
}
