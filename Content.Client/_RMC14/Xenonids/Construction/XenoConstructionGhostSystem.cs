using System.Linq;
using Content.Client.IconSmoothing;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Client._RMC14.Xenonids.Construction;

[UsedImplicitly]
public sealed class XenoConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    private SharedTransformSystem _transform = default!;
    private SharedMapSystem _mapSystem = default!;
    private SharedXenoConstructionSystem _xenoConstruction = default!;
    private SharedXenoWeedsSystem _xenoWeeds = default!;
    private SharedXenoHiveSystem _hive = default!;
    private TurfSystem _turf = default!;
    private TagSystem _tags = default!;
    private XenoNestSystem _xenoNest = default!;
    private QueenEyeSystem _queenEye = default!;
    private ExamineSystemShared _examineSystem = default!;
    private SharedInteractionSystem _interaction = default!;

    private EntityQuery<BlockXenoConstructionComponent> _blockXenoConstructionQuery;
    private EntityQuery<XenoConstructionSupportComponent> _constructionSupportQuery;
    private EntityQuery<HiveConstructionNodeComponent> _hiveConstructionNodeQuery;
    private EntityQuery<SentryComponent> _sentryQuery;
    private EntityQuery<XenoConstructComponent> _xenoConstructQuery;
    private EntityQuery<XenoEggComponent> _xenoEggQuery;
    private EntityQuery<XenoTunnelComponent> _xenoTunnelQuery;

    private EntityUid? _currentGhost;
    private string? _currentGhostStructure;
    private EntityCoordinates _lastPosition = EntityCoordinates.Invalid;

    private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    public override void Initialize()
    {
        base.Initialize();
        _transform = EntityManager.System<SharedTransformSystem>();
        _mapSystem = EntityManager.System<SharedMapSystem>();
        _xenoConstruction = EntityManager.System<SharedXenoConstructionSystem>();
        _xenoWeeds = EntityManager.System<SharedXenoWeedsSystem>();
        _hive = EntityManager.System<SharedXenoHiveSystem>();
        _turf = EntityManager.System<TurfSystem>();
        _tags = EntityManager.System<TagSystem>();
        _xenoNest = EntityManager.System<XenoNestSystem>();
        _queenEye = EntityManager.System<QueenEyeSystem>();
        _examineSystem = EntityManager.System<ExamineSystemShared>();
        _interaction = EntityManager.System<SharedInteractionSystem>();

        _blockXenoConstructionQuery = GetEntityQuery<BlockXenoConstructionComponent>();
        _constructionSupportQuery = GetEntityQuery<XenoConstructionSupportComponent>();
        _hiveConstructionNodeQuery = GetEntityQuery<HiveConstructionNodeComponent>();
        _sentryQuery = GetEntityQuery<SentryComponent>();
        _xenoConstructQuery = GetEntityQuery<XenoConstructComponent>();
        _xenoEggQuery = GetEntityQuery<XenoEggComponent>();
        _xenoTunnelQuery = GetEntityQuery<XenoTunnelComponent>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _playerManager.LocalEntity;
        if (player == null)
        {
            ClearGhost();
            return;
        }

        var (buildChoice, isConstructionActive) = GetConstructionState(player.Value);
        var isBuilding = IsBuilding(player.Value);

        var shouldShowGhost = isConstructionActive && !string.IsNullOrEmpty(buildChoice) && !isBuilding;

        if (shouldShowGhost)
        {
            var actualPrototype = GetActualBuildPrototype(player.Value, buildChoice!);

            if (_currentGhost == null ||
                _currentGhostStructure != buildChoice ||
                GetActualBuildPrototype(player.Value, _currentGhostStructure ?? "") != actualPrototype)
            {
                ClearGhost();
                CreateGhost(player.Value, buildChoice!);
            }
            UpdateGhostPosition();
        }
        else
        {
            ClearGhost();
        }
    }

    private (string? buildChoice, bool isActive) GetConstructionState(EntityUid player)
    {
        var actionController = _uiManager.GetUIController<ActionUIController>();
        if (actionController.SelectingTargetFor is not { } selectedActionId)
            return (null, false);

        if (EntityManager.TryGetComponent<XenoConstructionActionComponent>(selectedActionId, out var xenoAction))
        {
            if (EntityManager.TryGetComponent<XenoConstructionComponent>(player, out var construction))
            {
                if (construction.OrderConstructionTargeting && construction.OrderConstructionChoice != null)
                {
                    var orderChoice = construction.OrderConstructionChoice;
                    return (orderChoice?.ToString(), true);
                }

                var buildChoice = construction.BuildChoice?.ToString();
                return (buildChoice, true);
            }
        }

        return (null, false);
    }

    private bool IsBuilding(EntityUid player)
    {
        if (!EntityManager.TryGetComponent<DoAfterComponent>(player, out var doAfter))
            return false;

        return doAfter.DoAfters.Values.Any(activeDoAfter =>
            activeDoAfter.Args.Event is XenoSecreteStructureDoAfterEvent or XenoOrderConstructionDoAfterEvent);
    }

    private void CreateGhost(EntityUid player, string structurePrototype)
    {
        var playerCoords = EntityManager.GetComponent<TransformComponent>(player).Coordinates;
        var ghost = EntityManager.SpawnEntity("XenoConstructionGhost", playerCoords);
        var actualPrototype = GetActualBuildPrototype(player, structurePrototype);

        ConfigureGhostSprite(ghost, actualPrototype);

        _currentGhost = ghost;
        _currentGhostStructure = structurePrototype; // Keep original for comparison
        _lastPosition = EntityCoordinates.Invalid;
    }

    private string GetActualBuildPrototype(EntityUid player, string originalPrototype)
    {
        if (EntityManager.HasComponent<QueenBuildingBoostComponent>(player))
        {
            var queenVariant = GetQueenVariant(originalPrototype);
            if (_prototypeManager.HasIndex(queenVariant))
            {
                return queenVariant;
            }
        }

        return originalPrototype;
    }

    private string GetQueenVariant(string originalId)
    {
        return originalId switch
        {
            "WallXenoResin" => "WallXenoResinQueen",
            "WallXenoMembrane" => "WallXenoMembraneQueen",
            "DoorXenoResin" => "DoorXenoResinQueen",
            _ => originalId
        };
    }

    private void ConfigureGhostSprite(EntityUid ghost, string structurePrototype)
    {
        if (!EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
            return;

        sprite.Color = new Color(48, 255, 48, 128);
        sprite.DrawDepth = 9;
        sprite.Visible = true;

        if (!_prototypeManager.TryIndex<EntityPrototype>(structurePrototype, out var prototype))
            return;

        if (TryConfigureIconSmoothSprite(sprite, prototype))
            return;

        if (prototype.TryGetComponent<SpriteComponent>(out var prototypeSprite, EntityManager.ComponentFactory))
        {
            sprite.CopyFrom(prototypeSprite);
            sprite.Color = new Color(48, 255, 48, 128);
            sprite.DrawDepth = 9;

            for (int i = 0; i < sprite.AllLayers.Count(); i++)
            {
                sprite.LayerSetShader(i, "unshaded");
                sprite.LayerSetVisible(i, true);
            }
        }
    }

    private bool TryConfigureIconSmoothSprite(SpriteComponent sprite, EntityPrototype prototype)
    {
        if (!prototype.TryGetComponent<IconSmoothComponent>(out var iconSmooth, EntityManager.ComponentFactory) ||
            !prototype.TryGetComponent<SpriteComponent>(out var prototypeSprite, EntityManager.ComponentFactory) ||
            string.IsNullOrEmpty(iconSmooth.StateBase))
        {
            return false;
        }

        try
        {
            sprite.LayerMapReserveBlank(0);
            sprite.LayerSetRSI(0, prototypeSprite.BaseRSI);

            if (prototypeSprite.BaseRSI?.TryGetState(iconSmooth.StateBase, out _) == true)
            {
                sprite.LayerSetState(0, iconSmooth.StateBase);
                sprite.LayerSetShader(0, "unshaded");
                sprite.LayerSetVisible(0, true);
                sprite.Color = new Color(48, 255, 48, 128);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private void UpdateGhostPosition()
    {
        var player = _playerManager.LocalEntity;
        if (player == null || _currentGhost == null || !EntityManager.EntityExists(_currentGhost.Value))
            return;

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var coords = SnapToGrid(mouseScreenPos);

        if (!coords.IsValid(EntityManager))
            return;

        if (!coords.Equals(_lastPosition))
        {
            var ghostTransform = EntityManager.GetComponent<TransformComponent>(_currentGhost.Value);
            _transform.SetCoordinates(_currentGhost.Value, ghostTransform, coords);
            _lastPosition = coords;
        }

        if (EntityManager.TryGetComponent<SpriteComponent>(_currentGhost.Value, out var sprite))
        {
            sprite.Color = IsValidConstructionLocation(player.Value, coords)
                ? new Color(48, 255, 48, 128)
                : new Color(255, 48, 48, 128);
        }
    }

    private EntityCoordinates SnapToGrid(ScreenCoordinates screenCoords)
    {
        var mapCoords = _eyeManager.PixelToMap(screenCoords.Position);

        if (mapCoords.MapId == MapId.Nullspace)
        {
            var player = _playerManager.LocalEntity;
            return player != null ? _transform.GetMoverCoordinates(player.Value) : EntityCoordinates.Invalid;
        }

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
            return _transform.ToCoordinates(mapCoords);

        var gridCoords = _transform.ToCoordinates(gridUid, mapCoords);
        var tileCoords = _mapSystem.CoordinatesToTile(gridUid, grid, gridCoords);
        return _mapSystem.GridTileToLocal(gridUid, grid, tileCoords);
    }

    private bool IsValidConstructionLocation(EntityUid player, EntityCoordinates coords)
    {
        if (!EntityManager.TryGetComponent<XenoConstructionComponent>(player, out var construction))
            return false;

        try
        {
            if (construction.OrderConstructionTargeting && construction.OrderConstructionChoice != null)
            {
                return CanOrderConstruction((player, construction), coords, construction.OrderConstructionChoice);
            }
            else if (construction.BuildChoice != null)
            {
                return CanSecreteOnTile((player, construction), construction.BuildChoice, coords, true, true);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool CanSecreteOnTile(Entity<XenoConstructionComponent> xeno, EntProtoId? buildChoice, EntityCoordinates target, bool checkStructureSelected, bool checkWeeds)
    {
        if (checkStructureSelected && buildChoice == null)
            return false;

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return false;

        target = target.SnapToGrid(EntityManager, _mapManager);

        if (checkWeeds && !_queenEye.IsInQueenEye(xeno.Owner) && !_xenoWeeds.IsOnWeeds((gridId, grid), target))
            return false;

        if (!_queenEye.IsInQueenEye(xeno.Owner))
        {
            var origin = _transform.GetMoverCoordinates(xeno.Owner);
            var (buildRange, isRemoteConstruction) = GetEffectiveBuildRange(xeno, target);

            if (buildRange > 0 && !_transform.InRange(origin, target, buildRange))
                return false;

            if (_transform.InRange(origin, target, 0.75f))
                return false;

            if (isRemoteConstruction && !CanDoRemoteConstruction(xeno, target))
                return false;
        }

        if (!_queenEye.IsInQueenEye(xeno.Owner) && !TileSolidAndNotBlocked(target))
            return false;

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
                return false;
            }
        }

        if (checkStructureSelected && buildChoice != null)
        {
            var hasBoost = EntityManager.HasComponent<QueenBuildingBoostComponent>(xeno.Owner);
            if (!hasBoost)
            {
                if (_xenoConstruction.GetStructurePlasmaCost(buildChoice.Value) is { } cost &&
                    (!TryComp(xeno.Owner, out XenoPlasmaComponent? plasma) || plasma.Plasma < cost))
                {
                    return false;
                }
            }
        }

        if (checkStructureSelected &&
            buildChoice is { } choice &&
            _prototypeManager.TryIndex(choice, out var choiceProto) &&
            choiceProto.TryGetComponent<XenoConstructionRequiresSupportComponent>(out _, _compFactory))
        {
            if (!IsSupported((gridId, grid), target))
                return false;
        }

        return true;
    }

    private bool CanOrderConstruction(Entity<XenoConstructionComponent> xeno, EntityCoordinates target, EntProtoId? choice)
    {
        if (!CanSecreteOnTile(xeno, xeno.Comp.BuildChoice, target, false, false))
            return false;

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return false;

        var tile = _mapSystem.TileIndicesFor(gridId, grid, target);
        var directions = new[] { Direction.North, Direction.East, Direction.South, Direction.West };
        foreach (var direction in directions)
        {
            var pos = SharedMapSystem.GetDirection(tile, direction);
            var directionEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, pos);

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (_hiveConstructionNodeQuery.TryGetComponent(ent, out var node) &&
                    node.BlockOtherNodes)
                {
                    return false;
                }
            }
        }

        if (choice != null && _prototypeManager.TryIndex(choice, out var choiceProto))
        {
            if (choiceProto.TryGetComponent<HiveConstructionRequiresWeedableSurfaceComponent>(out _, _compFactory))
            {
                if (!_mapSystem.TryGetTileRef(gridId, grid, tile, out var tileRef) ||
                    !_tile.TryGetDefinition(tileRef.Tile.TypeId, out var tileDef) ||
                    tileDef.ID == ContentTileDefinition.SpaceID ||
                    tileDef is ContentTileDefinition { WeedsSpreadable: false })
                {
                    return false;
                }
            }

            if (choiceProto.TryGetComponent<HiveConstructionRequiresHiveCoreComponent>(out _, _compFactory))
            {
                if (_hive.GetHive(xeno.Owner) is not { } hiveEnt || !_hive.HasHiveCore(hiveEnt))
                    return false;
            }

            if (choiceProto.TryGetComponent<HiveConstructionRequiresHiveWeedsComponent>(out _, _compFactory) &&
                !_xenoWeeds.IsOnHiveWeeds((gridId, grid), target))
                return false;

            if (choiceProto.TryGetComponent<HiveConstructionRequiresSpaceComponent>(out _, _compFactory))
            {
                if (!CanPlaceSpaceRequiringStructure(_transform.ToMapCoordinates(target), (gridId, grid)))
                    return false;
            }

            if (choiceProto.TryGetComponent<HiveConstructionLimitedComponent>(out var limited, _compFactory) &&
                !CanPlaceLimitedHiveStructure(xeno.Owner, limited))
            {
                return false;
            }
        }

        return true;
    }

    private bool TileSolidAndNotBlocked(EntityCoordinates target)
    {
        return _turf.GetTileRef(target) is { } tile &&
               !_turf.IsSpace(tile) &&
               _turf.GetContentTileDefinition(tile).Sturdy &&
               !_turf.IsTileBlocked(tile, Impassable) &&
               !_xenoNest.HasAdjacentNestFacing(target);
    }

    private bool IsSupported(Entity<MapGridComponent> grid, EntityCoordinates coordinates)
    {
        var indices = _mapSystem.TileIndicesFor(grid, grid, coordinates);
        return IsSupported(grid, indices);
    }

    private bool IsSupported(Entity<MapGridComponent> grid, Vector2i tile)
    {
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
                    return true;
            }
        }

        return false;
    }

    private bool CanPlaceSpaceRequiringStructure(MapCoordinates mapCoords, Entity<MapGridComponent> map)
    {
        var centerTile = _mapSystem.GetTileRef(map, mapCoords);

        for (var adjacentX = centerTile.X - 1; adjacentX <= centerTile.X + 1; adjacentX++)
        {
            for (var adjacentY = centerTile.Y - 1; adjacentY <= centerTile.Y + 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0)
                    continue;

                var adjacentTile = new Vector2i(adjacentX, adjacentY);
                if (_turf.IsTileBlocked(map, adjacentTile, MobMask, map.Comp))
                    return false;
            }
        }

        return true;
    }

    private bool CanPlaceLimitedHiveStructure(EntityUid hiveMember, HiveConstructionLimitedComponent comp)
    {
        var id = comp.Id;
        if (_hive.GetHive(hiveMember) is not { } hive ||
            !_hive.TryGetStructureLimit(hive, id, out var limit))
        {
            return false;
        }

        var curCount = 0;
        var limitedConstructs = EntityQueryEnumerator<HiveConstructionLimitedComponent, HiveMemberComponent>();
        while (limitedConstructs.MoveNext(out var otherUnique, out _))
        {
            if (otherUnique.Id == id)
                curCount++;
        }

        return limit > curCount;
    }

    private (float range, bool isRemote) GetEffectiveBuildRange(Entity<XenoConstructionComponent> xeno, EntityCoordinates target)
    {
        var buildRange = xeno.Comp.BuildRange;

        if (_queenEye.IsInQueenEye(xeno.Owner))
            return (float.MaxValue, false);

        if (!TryComp(xeno.Owner, out ResinWhispererComponent? resinWhisperer))
            return (buildRange.Float(), false);

        var normalRange = resinWhisperer.MaxConstructDistance?.Float() ?? buildRange.Float();

        if (_interaction.InRangeUnobstructed(xeno.Owner, target, normalRange))
            return (normalRange, false);

        return (resinWhisperer.MaxRemoteConstructDistance, true);
    }

    private bool CanDoRemoteConstruction(Entity<XenoConstructionComponent> xeno, EntityCoordinates target)
    {
        if (!TryComp(xeno.Owner, out ResinWhispererComponent? resinWhisperer))
            return false;

        if (_queenEye.IsInQueenEye(xeno.Owner))
            return true;

        if (!_xenoWeeds.IsOnFriendlyWeeds(xeno.Owner))
            return false;

        if (!TileIsVisible(xeno.Owner, target, resinWhisperer.MaxRemoteConstructDistance))
            return false;

        return true;
    }

    private bool TileIsVisible(EntityUid user, EntityCoordinates targetCoordinates, float maxDistance)
    {
        var pointCoordinates = _transform.ToMapCoordinates(targetCoordinates);
        for (int i = 0; i < 9; i++)
        {
            switch (i)
            {
                case 1:
                case 7:
                case 8:
                    pointCoordinates = pointCoordinates.Offset(0.499f, 0);
                    break;
                case 2:
                    pointCoordinates = pointCoordinates.Offset(0, -0.499f);
                    break;
                case 3:
                case 4:
                    pointCoordinates = pointCoordinates.Offset(-0.499f, 0);
                    break;
                case 5:
                case 6:
                    pointCoordinates = pointCoordinates.Offset(0, 0.499f);
                    break;
                default:
                    break;
            }

            if (_examineSystem.InRangeUnOccluded(user, pointCoordinates, maxDistance))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearGhost()
    {
        if (_currentGhost != null)
        {
            if (EntityManager.EntityExists(_currentGhost.Value))
            {
                EntityManager.QueueDeleteEntity(_currentGhost.Value);
            }
        }

        _currentGhost = null;
        _currentGhostStructure = null;
        _lastPosition = EntityCoordinates.Invalid;
    }
}
