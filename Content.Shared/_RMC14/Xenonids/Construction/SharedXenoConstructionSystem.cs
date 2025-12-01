using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._RMC14.Xenonids.Construction;

public sealed class SharedXenoConstructionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _announce = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly QueenEyeSystem _queenEye = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoNestSystem _xenoNest = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;

    private static readonly ImmutableArray<Direction> Directions = Enum.GetValues<Direction>()
        .Where(d => d != Direction.Invalid)
        .ToImmutableArray();

    private EntityQuery<BlockXenoConstructionComponent> _blockXenoConstructionQuery;
    private EntityQuery<XenoConstructionSupportComponent> _constructionSupportQuery;
    private EntityQuery<XenoConstructionRequiresSupportComponent> _constructionRequiresSupportQuery;
    private EntityQuery<HiveConstructionNodeComponent> _hiveConstructionNodeQuery;
    private EntityQuery<SentryComponent> _sentryQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<XenoConstructComponent> _xenoConstructQuery;
    private EntityQuery<XenoEggComponent> _xenoEggQuery;
    private EntityQuery<XenoTunnelComponent> _xenoTunnelQuery;
    private EntityQuery<QueenBuildingBoostComponent> _queenBoostQuery;

    private const string XenoStructuresAnimation = "RMCEffect";
    private const string XenoHiveCoreNodeId = "HiveCoreXenoConstructionNode";

    private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<TagPrototype> PlatformTag = "Platform";

    private float _densityThreshold;

    public override void Initialize()
    {
        _blockXenoConstructionQuery = GetEntityQuery<BlockXenoConstructionComponent>();
        _constructionSupportQuery = GetEntityQuery<XenoConstructionSupportComponent>();
        _constructionRequiresSupportQuery = GetEntityQuery<XenoConstructionRequiresSupportComponent>();
        _hiveConstructionNodeQuery = GetEntityQuery<HiveConstructionNodeComponent>();
        _sentryQuery = GetEntityQuery<SentryComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _xenoConstructQuery = GetEntityQuery<XenoConstructComponent>();
        _xenoEggQuery = GetEntityQuery<XenoEggComponent>();
        _xenoTunnelQuery = GetEntityQuery<XenoTunnelComponent>();
        _queenBoostQuery = GetEntityQuery<QueenBuildingBoostComponent>();

        SubscribeLocalEvent<XenoConstructionComponent, XenoPlantWeedsActionEvent>(OnXenoPlantWeedsAction);
        SubscribeLocalEvent<XenoConstructionComponent, XenoExpandWeedsActionEvent>(OnXenoExpandWeedsAction);

        SubscribeLocalEvent<XenoConstructionComponent, XenoChooseStructureActionEvent>(OnXenoChooseStructureAction);

        SubscribeLocalEvent<XenoConstructionComponent, XenoSecreteStructureActionEvent>(OnXenoSecreteStructureAction);
        SubscribeLocalEvent<XenoConstructionComponent, XenoSecreteStructureDoAfterEvent>(OnXenoSecreteStructureDoAfter);

        SubscribeLocalEvent<XenoConstructionComponent, XenoOrderConstructionActionEvent>(OnXenoOrderConstructionAction);
        SubscribeLocalEvent<XenoConstructionComponent, XenoOrderConstructionDoAfterEvent>(OnXenoOrderConstructionDoAfter);
        SubscribeLocalEvent<XenoCanAddPlasmaToConstructComponent, XenoConstructionAddPlasmaDoAfterEvent>(OnHiveConstructionNodeAddPlasmaDoAfter);

        SubscribeLocalEvent<XenoChooseConstructionActionComponent, XenoConstructionChosenEvent>(OnActionConstructionChosen);
        SubscribeLocalEvent<XenoConstructionActionComponent, ActionValidateEvent>(OnSecreteActionValidateTarget);

        SubscribeLocalEvent<HiveConstructionNodeComponent, ExaminedEvent>(OnHiveConstructionNodeExamined);
        SubscribeLocalEvent<HiveConstructionNodeComponent, ActivateInWorldEvent>(OnHiveConstructionNodeActivated);

        SubscribeLocalEvent<RepairableXenoStructureComponent, ActivateInWorldEvent>(OnHiveConstructionRepair);
        SubscribeLocalEvent<RepairableXenoStructureComponent, XenoRepairStructureDoAfterEvent>(OnHiveConstructionRepairDoAfter);

        SubscribeLocalEvent<XenoWeedsComponent, XenoStructureRepairedEvent>(OnWeedStructureRepair);

        SubscribeLocalEvent<XenoConstructionSupportComponent, ComponentRemove>(OnCheckAdjacentCollapse);
        SubscribeLocalEvent<XenoConstructionSupportComponent, EntityTerminatingEvent>(OnCheckAdjacentCollapse);

        SubscribeLocalEvent<XenoAnnounceStructureDestructionComponent, DestructionEventArgs>(OnXenoStructureDestruction);

        SubscribeLocalEvent<DeleteXenoResinOnHitComponent, ProjectileHitEvent>(OnDeleteXenoResinHit);

        SubscribeNetworkEvent<XenoOrderConstructionClickEvent>(OnXenoOrderConstructionClick);
        SubscribeNetworkEvent<XenoOrderConstructionCancelEvent>(OnXenoOrderConstructionCancel);

        SubscribeLocalEvent<XenoConstructComponent, MapInitEvent>(OnXenoConstructMapInit);
        SubscribeLocalEvent<XenoConstructComponent, EntityTerminatingEvent>(OnXenoConstructRemoved);

        Subs.BuiEvents<XenoConstructionComponent>(XenoChooseStructureUI.Key,
            subs =>
            {
                subs.Event<XenoChooseStructureBuiMsg>(OnXenoChooseStructureBui);
            });

        Subs.BuiEvents<XenoConstructionComponent>(XenoOrderConstructionUI.Key,
            subs =>
            {
                subs.Event<XenoOrderConstructionBuiMsg>(OnXenoOrderConstructionBui);
            });

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));

        Subs.CVar(_config, RMCCVars.RMCResinConstructionDensityCostIncreaseThreshold, v => _densityThreshold = v, true);
    }

    private void OnXenoOrderConstructionClick(XenoOrderConstructionClickEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user ||
            !TryComp(user, out XenoConstructionComponent? construction))
        {
            return;
        }

        if (!construction.OrderConstructionTargeting || construction.OrderConstructionChoice != ev.StructureId)
            return;

        var target = GetCoordinates(ev.Target);

        if (!CanOrderConstructionPopup((user, construction), target, ev.StructureId))
            return;

        var doAfterEvent = new XenoOrderConstructionDoAfterEvent(ev.StructureId, ev.Target);
        var doAfter = new DoAfterArgs(EntityManager, user, construction.OrderConstructionDelay, doAfterEvent, user)
        {
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            construction.OrderConstructionTargeting = false;
            construction.OrderConstructionChoice = null;
            if (construction.ConfirmOrderConstructionAction != null)
            {
                _actions.SetToggled(construction.ConfirmOrderConstructionAction, false);
            }
            Dirty(user, construction);
        }
    }

    private void OnXenoOrderConstructionCancel(XenoOrderConstructionCancelEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user ||
            !TryComp(user, out XenoConstructionComponent? construction))
        {
            return;
        }

        CancelOrderConstructionTargeting((user, construction));
    }

    private void OnXenoStructureDestruction(Entity<XenoAnnounceStructureDestructionComponent> ent, ref DestructionEventArgs args)
    {
        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        var locationName = "Unknown";
        var structureName = "Unknown";

        if (_area.TryGetArea(ent.Owner, out _, out var areaProto))
            locationName = areaProto.Name;

        if (ent.Comp.StructureName is null)
        {
            if (Prototype(ent.Owner) is { } entProto)
                structureName = entProto.Name;
        }
        else
        {
            structureName = ent.Comp.StructureName;
        }

        var msg = Loc.GetString(ent.Comp.MessageID, ("location", locationName), ("structureName", structureName), ("destructionVerb", ent.Comp.DestructionVerb));
        _announce.AnnounceToHive(ent.Owner, hive, msg, color: ent.Comp.MessageColor);
    }

    private void OnXenoPlantWeedsAction(Entity<XenoConstructionComponent> xeno, ref XenoPlantWeedsActionEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(xeno).SnapToGrid(EntityManager, _map);
        if (_transform.GetGrid(coordinates) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? gridComp))
        {
            return;
        }

        var grid = new Entity<MapGridComponent>(gridUid, gridComp);
        if (_xenoWeeds.IsOnWeeds(grid, coordinates, true))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-weeds-source-already-here"), xeno.Owner, xeno.Owner);
            return;
        }

        var tile = _mapSystem.CoordinatesToTile(gridUid, gridComp, coordinates);
        if (!_xenoWeeds.CanSpreadWeedsPopup(grid, tile, xeno, args.UseOnSemiWeedable, true))
            return;

        if (!_xenoWeeds.CanPlaceWeedsPopup(xeno, grid, coordinates, args.LimitDistance))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        args.Handled = true;
        if (_net.IsServer)
        {
            var weeds = Spawn(args.Prototype, coordinates);
            _adminLogs.Add(LogType.RMCXenoPlantWeeds, $"Xeno {ToPrettyString(xeno):xeno} planted weeds {ToPrettyString(weeds):weeds} at {coordinates}");
            _hive.SetSameHive(xeno.Owner, weeds);
        }

        _audio.PlayPredicted(xeno.Comp.BuildSound, coordinates, xeno);
    }

    private void OnXenoExpandWeedsAction(Entity<XenoConstructionComponent> xeno, ref XenoExpandWeedsActionEvent args)
    {
        var coordinates = args.Target;
        if (_transform.GetGrid(coordinates) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? gridComp))
        {
            return;
        }

        if (_queenEye.IsInQueenEye(xeno.Owner) &&
            !_queenEye.CanSeeTarget(xeno.Owner, coordinates))
        {
            return;
        }

        var grid = new Entity<MapGridComponent>(gridUid, gridComp);
        var existing = _xenoWeeds.GetWeedsOnFloor(grid, coordinates);
        if (existing is { Comp.IsSource: true })
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-weeds-source-already-here"), xeno.Owner, xeno.Owner);
            return;
        }

        if (existing == null)
        {
            var hasAdjacent = false;
            foreach (var direction in _rmcMap.CardinalDirections)
            {
                if (!_rmcMap.HasAnchoredEntityEnumerator<XenoWeedsComponent>(coordinates))
                    continue;

                hasAdjacent = true;
                break;
            }

            if (!hasAdjacent)
            {
                // TODO RMC14
            }
        }

        var toSpawn = existing == null ? args.Expand : args.Source;
        var tile = _mapSystem.CoordinatesToTile(gridUid, gridComp, coordinates);
        if (!_xenoWeeds.CanSpreadWeedsPopup(grid, tile, xeno, false, true))
            return;

        if (!_xenoWeeds.CanPlaceWeedsPopup(xeno, grid, coordinates, false))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        args.Handled = true;
        if (_net.IsServer)
        {
            var weeds = Spawn(toSpawn, coordinates);
            _adminLogs.Add(LogType.RMCXenoPlantWeeds, $"Xeno {ToPrettyString(xeno):xeno} planted weeds {ToPrettyString(weeds):weeds} at {coordinates}");
            _hive.SetSameHive(xeno.Owner, weeds);
        }

        _audio.PlayPredicted(xeno.Comp.BuildSound, coordinates, xeno);
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

        if (xeno.Comp.OrderConstructionTargeting)
        {
            xeno.Comp.OrderConstructionTargeting = false;
            if (xeno.Comp.ConfirmOrderConstructionAction != null)
            {
                _actions.SetToggled(xeno.Comp.ConfirmOrderConstructionAction, false);
            }
        }

        Dirty(xeno);

        var ev = new XenoConstructionChosenEvent(args.StructureId, xeno.Owner);
        foreach (var (id, _) in _actions.GetActions(xeno))
        {
            RaiseLocalEvent(id, ref ev);
        }
    }

    private void OnXenoSecreteStructureAction(Entity<XenoConstructionComponent> xeno, ref XenoSecreteStructureActionEvent args)
    {
        if (xeno.Comp.OrderConstructionTargeting)
            return;

        HandleSecreteResinPlacement(xeno, ref args);
    }

    private EntProtoId GetQueenVariant(EntProtoId originalId)
    {
        return originalId.Id switch
        {
            "WallXenoResin" => "WallXenoResinQueen",
            "WallXenoMembrane" => "WallXenoMembraneQueen",
            "DoorXenoResin" => "DoorXenoResinQueen",
            _ => originalId
        };
    }

    private EntProtoId GetQueenAnimationVariant(EntProtoId originalId)
    {
        return originalId.Id switch
        {
            "WallXenoResin" => "WallXenoResinThick",
            "WallXenoMembrane" => "WallXenoMembraneThick",
            "DoorXenoResin" => "DoorXenoResinThick",
            _ => originalId
        };
    }

    private void HandleSecreteResinPlacement(Entity<XenoConstructionComponent> xeno, ref XenoSecreteStructureActionEvent args)
    {
        var snapped = args.Target.SnapToGrid(EntityManager, _map);
        var hasBoost = _queenBoostQuery.HasComp(xeno.Owner);

        if ((xeno.Comp.CanUpgrade || hasBoost) &&
            _rmcMap.HasAnchoredEntityEnumerator<XenoStructureUpgradeableComponent>(snapped, out var upgradeable) &&
            upgradeable.Comp.To is { } to &&
            _prototype.HasIndex(to))
        {
            var inRange = _queenEye.IsInQueenEye(xeno.Owner) ||
                        (hasBoost && _queenBoostQuery.TryComp(xeno.Owner, out var boost)
                            ? _transform.InRange(_transform.GetMoverCoordinates(xeno.Owner), args.Target, boost.RemoteUpgradeRange)
                            : _transform.InRange(_transform.GetMoverCoordinates(xeno.Owner), args.Target, xeno.Comp.BuildRange.Float()));

            if (!inRange)
                return;

            var cost = upgradeable.Comp.Cost;
            if (_area.TryGetArea(snapped, out var area, out _))
                cost = GetDensityCost(area.Value, xeno, cost);


            if (!hasBoost && !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, cost))
                return;

            var msg = hasBoost
                ? "We regurgitate some resin and thicken the " + Name(upgradeable) + " effortlessly."
                : $"We regurgitate some resin and thicken the {Name(upgradeable)}, using {cost} plasma.";
            _popup.PopupClient(msg, upgradeable, xeno);

            if (_net.IsClient)
                return;

            QueueDel(upgradeable);
            var spawn = Spawn(to, snapped);
            _hive.SetSameHive(xeno.Owner, spawn);
            args.Handled = true;
            return;
        }

        if (xeno.Comp.BuildChoice is not { } choice ||
            !CanSecreteOnTilePopup(xeno, choice, args.Target, true, true))
        {
            return;
        }

        var attempt = new XenoSecreteStructureAttemptEvent(args.Target);
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        var animationChoice = hasBoost ? GetQueenAnimationVariant(choice) : choice;
        var effectId = XenoStructuresAnimation + animationChoice;
        var coordinates = GetNetCoordinates(args.Target);
        var entityCoords = GetCoordinates(coordinates);
        EntityUid? effect = null;

        var buildMult = GetBuildSpeed(choice) ?? 1;

        if (hasBoost && _queenBoostQuery.TryComp(xeno.Owner, out var boostComp))
            buildMult *= boostComp.BuildSpeedMultiplier;

        var finalBuildTime = xeno.Comp.BuildDelay * buildMult;

        if (_net.IsServer && _prototype.HasIndex(effectId))
        {
            effect = Spawn(effectId, entityCoords);
            RaiseNetworkEvent(new XenoConstructionAnimationStartEvent(GetNetEntity(effect.Value), GetNetEntity(xeno), finalBuildTime), Filter.PvsExcept(effect.Value));
        }

        var ev = new XenoSecreteStructureDoAfterEvent(coordinates, choice, GetNetEntity(effect));
        args.Handled = true;
        var doAfter = new DoAfterArgs(EntityManager, xeno, finalBuildTime, ev, xeno)
        {
            BreakOnMove = true,
            RootEntity = true,
            CancelDuplicate = false
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            if (effect != null && _net.IsServer)
                QueueDel(effect);
        }
    }

    public void CancelOrderConstructionTargeting(Entity<XenoConstructionComponent> xeno)
    {
        if (xeno.Comp.OrderConstructionTargeting)
        {
            xeno.Comp.OrderConstructionTargeting = false;
            xeno.Comp.OrderConstructionChoice = null;
            if (xeno.Comp.ConfirmOrderConstructionAction != null)
            {
                _actions.SetToggled(xeno.Comp.ConfirmOrderConstructionAction, false);
            }
            Dirty(xeno);
        }
    }

    private void OnXenoSecreteStructureDoAfter(Entity<XenoConstructionComponent> xeno, ref XenoSecreteStructureDoAfterEvent args)
    {
        if (_net.IsServer && args.Effect != null)
            QueueDel(GetEntity(args.Effect));

        if (args.Handled || args.Cancelled)
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!coordinates.IsValid(EntityManager) ||
            !xeno.Comp.CanBuild.Contains(args.StructureId) ||
            !CanSecreteOnTilePopup(xeno, args.StructureId, GetCoordinates(args.Coordinates), true, true))
        {
            return;
        }

        var hasBoost = _queenBoostQuery.HasComp(xeno.Owner);
        if (_area.TryGetArea(GetCoordinates(args.Coordinates), out var area, out _) &&
            GetStructurePlasmaCost(args.StructureId) is { } baseCost)
        {
            var cost = baseCost;
            if (area.Value.Comp.ResinConstructCount != 0 &&
                !area.Value.Comp.Unweedable &&
                _prototype.TryIndex(args.StructureId, out var structure) &&
                structure.TryGetComponent(out XenoConstructionPlasmaCostComponent? plasmaCost, _compFactory) &&
                plasmaCost.ScalingCost)
            {
                cost = GetDensityCost(area.Value, xeno, cost);
            }

            if (!hasBoost && !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, cost))
                return;
        }

        args.Handled = true;

        // TODO RMC14 stop collision for mobs until they move off
        if (_net.IsServer)
        {
            var structureToSpawn = args.StructureId;

            if (hasBoost)
            {
                var queenVariant = GetQueenVariant(args.StructureId);
                if (_prototype.HasIndex(queenVariant))
                {
                    structureToSpawn = queenVariant;
                }
            }

            var structure = Spawn(structureToSpawn, coordinates);
            _hive.SetSameHive(xeno.Owner, structure);
            _adminLogs.Add(LogType.RMCXenoConstruct, $"Xeno {ToPrettyString(xeno):xeno} constructed {ToPrettyString(structure):structure} at {coordinates}");
        }

        _audio.PlayPredicted(xeno.Comp.BuildSound, coordinates, xeno);
    }

    private void OnXenoOrderConstructionAction(Entity<XenoConstructionComponent> xeno, ref XenoOrderConstructionActionEvent args)
    {
        args.Handled = true;
        _ui.TryOpenUi(xeno.Owner, XenoOrderConstructionUI.Key, xeno);
    }

    private void OnXenoOrderConstructionBui(Entity<XenoConstructionComponent> xeno, ref XenoOrderConstructionBuiMsg args)
    {
        if (!xeno.Comp.CanOrderConstruction.Contains(args.StructureId))
            return;

        xeno.Comp.OrderConstructionChoice = args.StructureId;
        xeno.Comp.OrderConstructionTargeting = true;
        if (xeno.Comp.ConfirmOrderConstructionAction != null)
        {
            _actions.SetToggled(xeno.Comp.ConfirmOrderConstructionAction, true);
        }
        Dirty(xeno);

        var ev = new XenoConstructionChosenEvent(args.StructureId, xeno.Owner);
        foreach (var (id, _) in _actions.GetActions(xeno))
        {
            RaiseLocalEvent(id, ref ev);
        }

        _ui.CloseUi(xeno.Owner, XenoOrderConstructionUI.Key);
    }

    private void OnXenoOrderConstructionDoAfter(Entity<XenoConstructionComponent> xeno, ref XenoOrderConstructionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var target = GetCoordinates(args.Coordinates);

        if (!xeno.Comp.CanOrderConstruction.Contains(args.StructureId))
            return;

        if (!CanOrderConstructionPopup(xeno, target, args.StructureId))
            return;

        if (!TryComp(xeno, out XenoPlasmaComponent? plasma))
            return;

        if (!_prototype.TryIndex(args.StructureId, out var prototype))
            return;

        var hasBoost = _queenBoostQuery.HasComp(xeno.Owner);

        if (prototype.TryGetComponent(out HiveConstructionNodeComponent? node, _compFactory) &&
            !hasBoost &&
            !_xenoPlasma.TryRemovePlasmaPopup((xeno, plasma), node.InitialPlasmaCost))
        {
            return;
        }

        if (_net.IsClient)
            return;

        var coordinates = target.SnapToGrid(EntityManager, _map);
        var structure = Spawn(args.StructureId, coordinates);

        _hive.SetSameHive(xeno.Owner, structure);

        _adminLogs.Add(LogType.RMCXenoOrderConstruction, $"Xeno {ToPrettyString(xeno):xeno} ordered construction of {ToPrettyString(structure):structure} at {coordinates}");

        if (!_prototype.TryIndex(args.StructureId, out var structureProto))
            return;

        string msg;
        if (TryComp(structure, out HiveConstructionLimitedComponent? hiveLimitedComp) &&
            CanPlaceLimitedHiveStructure(xeno.Owner, hiveLimitedComp, out var limit, out var curCount))
        {
            var remainCount = limit - curCount;
            msg = Loc.GetString("rmc-xeno-order-construction-limited-structure-designated",
                ("construct", structureProto.Name),
                ("remainCount", remainCount),
                ("maxCount", limit)
            );
            _popup.PopupEntity(msg, xeno.Owner, xeno.Owner);
        }

        var areaName = "Unknown";
        if (_area.TryGetArea(target, out _, out var areaProto))
            areaName = areaProto.Name;

        msg = Loc.GetString("rmc-xeno-order-construction-structure-designated", ("construct", structureProto.Name), ("area", areaName));
        _announce.AnnounceSameHive(xeno.Owner, msg, needsQueen: true);
    }

    private void OnHiveConstructionNodeAddPlasmaDoAfter(Entity<XenoCanAddPlasmaToConstructComponent> xeno, ref XenoConstructionAddPlasmaDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp(target, out HiveConstructionNodeComponent? node) ||
            !TryComp(target, out TransformComponent? transform) ||
            !TryComp(xeno, out XenoPlasmaComponent? plasma))
        {
            return;
        }

        if (!InRangePopup(args.User, transform.Coordinates, xeno.Comp.Range.Float()))
            return;

        var hasBoost = _queenBoostQuery.HasComp(xeno.Owner);
        var plasmaLeft = node.PlasmaCost - node.PlasmaStored;

        if (hasBoost)
        {
            node.PlasmaStored = node.PlasmaCost;
            plasmaLeft = 0;
        }
        else
        {
            var subtract = FixedPoint2.Min(plasma.Plasma, plasmaLeft);
            if (plasmaLeft < FixedPoint2.Zero ||
                plasma.Plasma < 1 ||
                !_xenoPlasma.TryRemovePlasmaPopup((args.User, plasma), subtract))
            {
                return;
            }

            node.PlasmaStored += subtract;
            plasmaLeft = node.PlasmaCost - node.PlasmaStored;
        }

        args.Handled = true;

        _adminLogs.Add(LogType.RMCXenoOrderConstructionPlasma, $"Xeno {ToPrettyString(xeno):xeno} added plasma to {ToPrettyString(target):target} at {transform.Coordinates}");

        if (node.PlasmaStored < node.PlasmaCost)
        {
            _popup.PopupClient(
                Loc.GetString("cm-xeno-requires-more-plasma", ("construction", target), ("plasma", plasmaLeft)),
                target,
                args.User);
            return;
        }

        if (!_transformQuery.TryComp(xeno.Owner, out var xform) ||
            _transform.GetGrid((xeno.Owner, xform)) is not { Valid: true } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return;
        }

        if (HasComp<HiveConstructionRequiresHiveWeedsComponent>(target) && !_xenoWeeds.IsOnHiveWeeds((gridId, grid), target.ToCoordinates()))
        {
            _popup.PopupClient(
                Loc.GetString("rmc-xeno-construction-requires-hive-weeds", ("choice", target)),
                target,
                args.User);
            return;
        }

        if (HasComp<HiveConstructionRequiresSpaceComponent>(target))
        {
            var mapCoords = _transform.GetMapCoordinates(target);
            if (!CanPlaceSpaceRequiringStructurePopup(mapCoords, (gridId, grid), xeno.Owner, MetaData(target).EntityName))
            {
                return;
            }
        }

        if (_net.IsClient)
            return;

        EntityUid? floorWeeds = null;
        if (_prototype.TryIndex(node.Spawn, out var spawnProto) &&
            spawnProto.HasComponent<XenoWeedsComponent>())
        {
            floorWeeds = _xenoWeeds.GetWeedsOnFloor(transform.Coordinates);
        }

        var spawn = Spawn(node.Spawn, transform.Coordinates);

        var hive = _hive.GetHive(target);
        _hive.SetHive(spawn, hive);

        QueueDel(target);
        QueueDel(floorWeeds);

        _adminLogs.Add(LogType.RMCXenoOrderConstructionComplete, $"Xeno {ToPrettyString(xeno):xeno} completed construction of {ToPrettyString(target):xeno} which turned into {ToPrettyString(spawn):spawn} at {transform.Coordinates}");
    }

    private void OnActionConstructionChosen(Entity<XenoChooseConstructionActionComponent> xeno, ref XenoConstructionChosenEvent args)
    {
        if (!TryComp(args.User, out XenoConstructionComponent? construction))
            return;

        if (construction.OrderConstructionTargeting)
            return;

        if (_actions.GetAction(xeno.Owner) is { } action &&
            _prototype.HasIndex(args.Choice))
        {
            var hasBoost = _queenBoostQuery.HasComp(args.User);
            var displayChoice = hasBoost ? GetQueenVariant(args.Choice) : args.Choice;
            _actions.SetIcon(action.AsNullable(), new SpriteSpecifier.EntityPrototype(displayChoice));
        }
    }

    private void OnSecreteActionValidateTarget(Entity<XenoConstructionActionComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid)
            return;

        if (!TryComp(args.User, out XenoConstructionComponent? construction))
            return;

        if (GetCoordinates(args.Input.EntityCoordinatesTarget) is not { } target)
            return;

        var snapped = target.SnapToGrid(EntityManager, _map);

        var adjustEv = new XenoSecreteStructureAdjustFields(snapped);
        RaiseLocalEvent(args.User, ref adjustEv);

        var hasBoost = _queenBoostQuery.HasComp(args.User);

        if (ent.Comp.CanUpgrade &&
            (construction.CanUpgrade || hasBoost) &&
            _rmcMap.HasAnchoredEntityEnumerator<XenoStructureUpgradeableComponent>(snapped, out var upgradeable) &&
            upgradeable.Comp.To != null)
        {
            if (_queenEye.IsInQueenEye(args.User))
                return;

            if (hasBoost && _queenBoostQuery.TryComp(args.User, out var boost))
            {
                var inRange = _transform.InRange(_transform.GetMoverCoordinates(args.User), target, boost.RemoteUpgradeRange);
                if (inRange)
                    return;
            }
            if (_transform.InRange(_transform.GetMoverCoordinates(args.User), upgradeable.Owner.ToCoordinates(), construction.BuildRange.Float()))
                return;
        }

        if (construction.OrderConstructionTargeting && construction.OrderConstructionChoice != null)
        {
            if (_queenEye.IsInQueenEye(args.User) &&
                !_queenEye.CanSeeTarget(args.User, target))
            {
                args.Invalid = true;
                return;
            }

            if (!CanOrderConstructionPopup((args.User, construction), target, construction.OrderConstructionChoice))
            {
                args.Invalid = true;
            }
            return;
        }

        if (!CanSecreteOnTilePopup((args.User, construction), construction.BuildChoice, target, ent.Comp.CheckStructureSelected, ent.Comp.CheckWeeds))
        {
            args.Invalid = true;
        }
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
        if (!TryComp(user, out XenoCanAddPlasmaToConstructComponent? xeno) ||
            plasmaLeft < FixedPoint2.Zero ||
            !TryComp(node, out TransformComponent? nodeTransform) ||
            !TryComp(user, out XenoPlasmaComponent? plasma))
        {
            return;
        }

        if (!InRangePopup(user, nodeTransform.Coordinates, xeno.Range.Float()))
            return;

        var subtract = FixedPoint2.Min(plasma.Plasma, plasmaLeft);
        if (plasma.Plasma < 1 ||
            !_xenoPlasma.HasPlasmaPopup((user, plasma), subtract))
        {
            return;
        }

        var ev = new XenoConstructionAddPlasmaDoAfterEvent();
        var delay = xeno.AddPlasmaDelay;
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, user, node)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnHiveConstructionRepair(Entity<RepairableXenoStructureComponent> xenoStructure, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        var plasmaLeft = xenoStructure.Comp.PlasmaCost - xenoStructure.Comp.StoredPlasma;
        if (!TryComp(user, out XenoConstructionComponent? xeno) ||
            plasmaLeft < FixedPoint2.Zero ||
            !TryComp(xenoStructure, out TransformComponent? xenoStructureTransform) ||
            !TryComp(user, out XenoPlasmaComponent? plasma) ||
            !TryComp(xenoStructure, out DamageableComponent? xenoStructureDamage))
        {
            return;
        }

        if (xenoStructureDamage.TotalDamage <= 0)
        {
            var undamagedStructureMessage = Loc.GetString("rmc-xeno-construction-repair-structure-no-damage-failure", ("struct", xenoStructure.Owner));
            _popup.PopupClient(undamagedStructureMessage, xenoStructure.Owner.ToCoordinates(), user);
            return;
        }

        if (!InRangePopup(user, xenoStructureTransform.Coordinates, xeno.OrderConstructionRange.Float()))
            return;

        if (plasma.Plasma < 1)
            return;

        var ev = new XenoRepairStructureDoAfterEvent();
        var delay = xenoStructure.Comp.RepairLength;
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, xenoStructure, xenoStructure)
        {
            BreakOnMove = true,
            RootEntity = true
        };

        _doAfter.TryStartDoAfter(doAfter);
        _popup.PopupClient(Loc.GetString("rmc-xeno-construction-repair-structure-start-attempt",
                ("struct", xenoStructure.Owner)),
            xenoStructureTransform.Coordinates,
            user
        );
    }

    private void OnHiveConstructionRepairDoAfter(Entity<RepairableXenoStructureComponent> xenoStructure, ref XenoRepairStructureDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var user = args.User;
        var plasmaLeft = xenoStructure.Comp.PlasmaCost - xenoStructure.Comp.StoredPlasma;
        if (!TryComp(user, out XenoConstructionComponent? xeno) ||
            plasmaLeft < FixedPoint2.Zero ||
            !TryComp(xenoStructure, out TransformComponent? xenoStructureTransform) ||
            !TryComp(user, out XenoPlasmaComponent? plasma) ||
            !TryComp(xenoStructure, out DamageableComponent? xenoStructureDamage) ||
            xenoStructureDamage.TotalDamage <= 0)
        {
            return;
        }

        args.Handled = true;
        if (!InRangePopup(user, xenoStructureTransform.Coordinates, xeno.OrderConstructionRange.Float()))
            return;

        var subtract = FixedPoint2.Min(plasma.Plasma, plasmaLeft);
        if (plasma.Plasma < 1 ||
            !_xenoPlasma.TryRemovePlasma((user, plasma), subtract))
        {
            return;
        }

        xenoStructure.Comp.StoredPlasma += subtract;
        if (xenoStructure.Comp.StoredPlasma >= xenoStructure.Comp.PlasmaCost)
        {
            xenoStructure.Comp.StoredPlasma = 0;
        }
        else
        {
            var notEnoughPlasmaMessage = Loc.GetString(
                "rmc-xeno-construction-repair-structure-insufficient-plasma-warn",
                ("struct", xenoStructure.Owner),
                ("remainingPlasma", xenoStructure.Comp.PlasmaCost - xenoStructure.Comp.StoredPlasma)
            );
            _popup.PopupClient(notEnoughPlasmaMessage, xenoStructure.Owner.ToCoordinates(), user);
            return;
        }

        _damageable.SetAllDamage(xenoStructure.Owner, xenoStructureDamage, 0);
        var ev = new XenoStructureRepairedEvent();
        RaiseLocalEvent(xenoStructure, ev);

        _popup.PopupClient(
            Loc.GetString("rmc-xeno-construction-repair-structure-success", ("struct", xenoStructure.Owner)),
            xenoStructureTransform.Coordinates,
            user
        );
    }

    private void OnWeedStructureRepair(Entity<XenoWeedsComponent> weedsStructure, ref XenoStructureRepairedEvent args)
    {
        var (ent, comp) = weedsStructure;

        var spreaderComp = EnsureComp<XenoWeedsSpreadingComponent>(ent);
        var spreadTime = _timing.CurTime + spreaderComp.RepairedSpreadDelay;

        spreaderComp.SpreadAt = spreadTime;
        Dirty(ent, spreaderComp);

        foreach (var weed in comp.Spread)
        {
            spreaderComp = EnsureComp<XenoWeedsSpreadingComponent>(weed);
            spreaderComp.SpreadAt = spreadTime;
            Dirty(weed, spreaderComp);
        }
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
            var dir = (AtmosDirection)(1 << i);
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

    private void OnDeleteXenoResinHit(Entity<DeleteXenoResinOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (_net.IsServer && _xenoConstructQuery.HasComp(args.Target))
            QueueDel(args.Target);
    }

    private void OnXenoConstructMapInit(Entity<XenoConstructComponent> ent, ref MapInitEvent args)
    {
        if (!_area.TryGetArea(ent, out var area , out _))
            return;

        area.Value.Comp.ResinConstructCount++;
        Dirty(area.Value);
    }

    private void OnXenoConstructRemoved(Entity<XenoConstructComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!_area.TryGetArea(ent, out var area , out _))
            return;

        area.Value.Comp.ResinConstructCount--;
        Dirty(area.Value);
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

    public FixedPoint2 GetStructureMinRange(EntProtoId prototype)
    {
        XenoConstructionMinRangeComponent? minRangeComp = null;
        if (_prototype.TryIndex(prototype, out var buildChoice))
            buildChoice.TryGetComponent(out minRangeComp, _compFactory);
        if (minRangeComp != null)
            return minRangeComp.MinRange.Float();
        return 0;
    }

    private float? GetBuildSpeed(EntProtoId prototype)
    {
        if (_prototype.TryIndex(prototype, out var buildChoice) &&
            buildChoice.TryGetComponent(out XenoConstructionBuildSpeedComponent? speed, _compFactory))
        {
            return speed.BuildTimeMult;
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

    public FixedPoint2 GetStructureMinRange(EntProtoId? building)
    {
        if (building is { } choice &&
            GetStructureMinRange(choice) is { } minRange)
        {
            return minRange;
        }

        return 0;
    }

    private bool TileSolidAndNotBlocked(EntityCoordinates target)
    {
        return _turf.GetTileRef(target) is { } tile &&
               !_turf.IsSpace(tile) &&
               _turf.GetContentTileDefinition(tile).Sturdy &&
               !_turf.IsTileBlocked(tile, Impassable) &&
               !_xenoNest.HasAdjacentNestFacing(target);
    }

    private bool InRangePopup(EntityUid xeno, EntityCoordinates target, float range, float minRange = 0)
    {
        var origin = _transform.GetMoverCoordinates(xeno);
        target = target.SnapToGrid(EntityManager, _map);
        if (!_transform.InRange(origin, target, range))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), target, xeno);
            return false;
        }

        if (minRange != 0 && _transform.InRange(origin, target, minRange))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-build-in-self"), target, xeno);
            return false;
        }

        return true;
    }

    private bool CanSecreteOnTilePopup(Entity<XenoConstructionComponent> xeno, EntProtoId? buildChoice, EntityCoordinates target, bool checkStructureSelected, bool checkWeeds)
    {
        if (checkStructureSelected && buildChoice == null)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-select-structure"), target, xeno);
            return false;
        }

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-cant-build"), target, xeno);
            return false;
        }

        target = target.SnapToGrid(EntityManager, _map);
        var hasBoost = _queenBoostQuery.HasComp(xeno.Owner);

        if (checkWeeds && !_xenoWeeds.IsOnWeeds((gridId, grid), target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-need-weeds"), target, xeno);
            return false;
        }

        var ev = new XenoConstructionRangeEvent(xeno.Comp.BuildRange);
        RaiseLocalEvent(xeno, ref ev);

        if (ev.Range > 0 && !_queenEye.IsInQueenEye(xeno.Owner))
        {
            if (!InRangePopup(xeno, target, ev.Range.Float(), GetStructureMinRange(buildChoice).Float()))
                return false;
        }

        if (!TileSolidAndNotBlocked(target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-cant-build"), target, xeno);
            return false;
        }

        var tile = _mapSystem.CoordinatesToTile(gridId, grid, target);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_xenoConstructQuery.HasComp(uid) ||
                _xenoEggQuery.HasComp(uid) ||
                _xenoTunnelQuery.HasComp(uid) ||
                _sentryQuery.HasComp(uid) ||
                _blockXenoConstructionQuery.HasComp(uid))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-cant-build"), target, xeno);
                return false;
            }

            if (!HasComp<BarricadeComponent>(uid))
            {
                if ((_tags.HasAnyTag(uid.Value, StructureTag) || HasComp<StrapComponent>(uid) || HasComp<ClimbableComponent>(uid))  &&
                    !_tags.HasTag(uid.Value, PlatformTag) &&
                    !HasComp<DoorComponent>(uid) ||
                    TryComp(uid, out DoorComponent? door) &&
                    door.State != DoorState.Open)
                {
                    _popup.PopupClient(Loc.GetString("rmc-xeno-construction-blocked-structure"), xeno, xeno, PopupType.SmallCaution);
                    return false;
                }
            }
        }

        if (checkStructureSelected &&
            GetStructurePlasmaCost(buildChoice) is { } cost &&
            !hasBoost &&
            !_xenoPlasma.HasPlasmaPopup(xeno.Owner, cost))
        {
            return false;
        }

        if (checkStructureSelected &&
            buildChoice is { } choice &&
            _prototype.TryIndex(choice, out var choiceProto) &&
            choiceProto.HasComponent<XenoConstructionRequiresSupportComponent>(_compFactory))
        {
            if (!IsSupported((gridId, grid), target))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-requires-support", ("choice", choiceProto.Name)), target, xeno);
                return false;
            }
        }

        if (!_area.CanResinPopup((gridId, grid, null), tile, xeno))
            return false;

        return true;
    }

    private bool CanOrderConstructionPopup(Entity<XenoConstructionComponent> xeno, EntityCoordinates target, EntProtoId? choice)
    {
        if (_queenEye.IsInQueenEye(xeno.Owner) &&
            !_queenEye.CanSeeTarget(xeno.Owner, target))
        {
            return false;
        }

        if (!CanSecreteOnTilePopup(xeno, choice, target, false, false))
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
            _prototype.TryIndex(choice, out var choiceProto))
        {
            if (choiceProto.HasComponent<HiveConstructionRequiresWeedableSurfaceComponent>(_compFactory))
            {
                if (!_mapSystem.TryGetTileRef(gridId, grid, tile, out var tileRef) ||
                    !_tile.TryGetDefinition(tileRef.Tile.TypeId, out var tileDef) ||
                    tileDef.ID == ContentTileDefinition.SpaceID ||
                    tileDef is ContentTileDefinition { WeedsSpreadable: false })
                {
                    _popup.PopupClient(Loc.GetString("cm-xeno-construction-failed-cant-build"), xeno, xeno);
                    return false;
                }
            }

            if (choiceProto.HasComponent<HiveConstructionRequiresHiveCoreComponent>(_compFactory))
            {
                if (_hive.GetHive(xeno.Owner) is { } hiveEnt)
                {
                    if (!_hive.HasHiveCore(hiveEnt))
                    {
                        if (_net.IsServer)
                            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-requires-hive-core", ("choice", choiceProto.Name)), xeno, xeno, PopupType.MediumCaution);
                        return false;
                    }
                }
                else
                {
                    if (_net.IsServer)
                        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-requires-hive-core", ("choice", choiceProto.Name)), xeno, xeno, PopupType.MediumCaution);
                    return false;
                }
            }

            if (choiceProto.HasComponent<HiveConstructionRequiresHiveWeedsComponent>(_compFactory) && !_xenoWeeds.IsOnHiveWeeds((gridId, grid), target))
            {
                if (_net.IsServer)
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-requires-hive-weeds", ("choice", choiceProto.Name)), xeno, xeno, PopupType.MediumCaution);
                return false;
            }

            if (choiceProto.HasComponent<HiveConstructionRequiresSpaceComponent>(_compFactory))
            {
                if (!CanPlaceSpaceRequiringStructurePopup(_transform.ToMapCoordinates(target), (gridId, grid), xeno.Owner, choiceProto.Name))
                {
                    return false;
                }
            }

            if (choiceProto.TryGetComponent(out HiveConstructionLimitedComponent? limited, _compFactory) &&
                !CanPlaceLimitedHiveStructure(xeno.Owner, limited, out var limit, out _))
            {
                // server-only as the structure may not be in the client's PVS bubble
                if (_net.IsServer)
                {
                    var msg = limit == 1
                        ? Loc.GetString("rmc-xeno-construction-unique-exists", ("choice", choiceProto.Name))
                        : Loc.GetString("rmc-xeno-construction-hive-limit-met", ("choice", choiceProto.Name));
                    _popup.PopupEntity(msg, xeno, xeno, PopupType.MediumCaution);
                }

                return false;
            }

            if (choiceProto.ID == XenoHiveCoreNodeId && _hive.GetHive(xeno.Owner) is { } hive && hive.Comp.NewCoreAt > _timing.CurTime)
            {
                if (_net.IsServer)
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-cant-build-new-yet", ("choice", choiceProto.Name)), xeno, xeno, PopupType.MediumCaution);

                return false;
            }
        }

        return true;
    }

    private bool CanPlaceLimitedHiveStructure(EntityUid hiveMember, HiveConstructionLimitedComponent comp, [NotNullWhen(true)] out int? limit, [NotNullWhen(true)] out int? curCount)
    {
        limit = null;
        curCount = null;
        var id = comp.Id;
        if (_hive.GetHive(hiveMember) is not { } hive ||
            !_hive.TryGetStructureLimit(hive, id, out var trueLimit))
        {
            return false;
        }

        limit = trueLimit;

        curCount = 0;
        var limitedConstructs = EntityQueryEnumerator<HiveConstructionLimitedComponent, HiveMemberComponent>();
        while (limitedConstructs.MoveNext(out var otherUnique, out _))
        {
            if (otherUnique.Id == id)
            {
                curCount++;
            }
        }

        return (limit > curCount);
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
            var dir = (AtmosDirection)(1 << i);
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

    private bool CanPlaceSpaceRequiringStructurePopup(MapCoordinates mapCoords, Entity<MapGridComponent> map, EntityUid user, string structName)
    {
        var mapId = mapCoords.MapId;
        var aabbRange = new Box2(mapCoords.X - 1.5F, mapCoords.Y + 1.5F, mapCoords.X + 1.5F, mapCoords.Y - 1.5F);
        var nearHiveLimitedStructure = _lookup.AnyComponentsIntersecting(typeof(HiveConstructionLimitedComponent), mapId, aabbRange);
        var centerTile = _mapSystem.GetTileRef(map, mapCoords);
        var userCoords = _transform.ToCoordinates(user, mapCoords);

        if (nearHiveLimitedStructure)
        {
            _popup.PopupClient(
                Loc.GetString("rmc-xeno-construction-requires-space", ("choice", structName)),
                userCoords,
                user
            );

            return false;
        }

        for (var adjacentX = centerTile.X - 1; adjacentX <= centerTile.X + 1; adjacentX++)
        {
            for (var adjacentY = centerTile.Y - 1; adjacentY <= centerTile.Y + 1; adjacentY++)
            {
                if (adjacentX == adjacentY && adjacentX == 0)
                {
                    continue;
                }

                var adjacentTile = new Vector2i(adjacentX, adjacentY);
                if (_turf.IsTileBlocked(map, adjacentTile, MobMask, map.Comp))
                {
                    _popup.PopupClient(
                    Loc.GetString("rmc-xeno-construction-requires-space", ("choice", structName)),
                    userCoords,
                    user);
                    return false;
                }
            }
        }
        return true;
    }

    public bool CanPlaceXenoStructure(EntityUid user, EntityCoordinates coords, [NotNullWhen(false)] out string? popupType, bool needsWeeds = true)
    {
        popupType = null;
        if (_transform.GetGrid(coords) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            popupType = "rmc-xeno-construction-no-map";
            return false;
        }

        var tile = _mapSystem.TileIndicesFor(gridId, grid, coords);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        var hasWeeds = false;
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<XenoEggComponent>(uid))
            {
                popupType = "rmc-xeno-construction-blocked";
                return false;
            }

            if (HasComp<XenoConstructComponent>(uid) ||
                _tags.HasAnyTag(uid.Value, StructureTag, AirlockTag) ||
                HasComp<StrapComponent>(uid) ||
                _xenoTunnelQuery.HasComp(uid) ||
                _sentryQuery.HasComp(uid) ||
                _blockXenoConstructionQuery.HasComp(uid))
            {
                popupType = "rmc-xeno-construction-blocked";
                return false;
            }

            if (HasComp<XenoWeedsComponent>(uid))
                hasWeeds = true;
        }

        if (_turf.IsTileBlocked(gridId, tile, Impassable | MidImpassable | HighImpassable, grid))
        {
            popupType = "rmc-xeno-construction-blocked";
            return false;
        }

        if (!hasWeeds && needsWeeds)
        {
            popupType = "rmc-xeno-construction-must-have-weeds";
            return false;
        }

        return true;
    }

    public void GiveQueenBoost(EntityUid queen, float speedMultiplier, float remoteRange)
    {
        var boost = EnsureComp<QueenBuildingBoostComponent>(queen);
        boost.BuildSpeedMultiplier = speedMultiplier;
        boost.RemoteUpgradeRange = remoteRange;
        Dirty(queen, boost);
    }

    public void RemoveQueenBoost(EntityUid queen)
    {
        RemCompDeferred<QueenBuildingBoostComponent>(queen);
    }

    private FixedPoint2 GetDensityCost(Entity<AreaComponent> area, Entity<XenoConstructionComponent> xeno, FixedPoint2 cost)
    {
        var density = (float) area.Comp.ResinConstructCount / area.Comp.BuildableTiles;
        if (density >= _densityThreshold)
            cost = Math.Ceiling(cost.Float() * (density + xeno.Comp.DensityConstructionCostModifier) * xeno.Comp.DensityConstructionCostMultiplier);

        // Don't make the cost higher than the max plasma.
        if (TryComp(xeno, out XenoPlasmaComponent? plasma) && cost > plasma.MaxPlasma)
            cost = plasma.MaxPlasma;

        return cost;
    }
}
