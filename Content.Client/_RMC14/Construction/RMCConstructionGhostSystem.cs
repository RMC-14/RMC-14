using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Construction.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Input;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Map.Components;
using Content.Client.Clickable;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Construction;

[UsedImplicitly]
public sealed class RMCConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCConstructionSystem _constructionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private SharedMapSystem _mapSystem = default!;

    private readonly Dictionary<RMCConstructionGhostKey, EntityUid> _ghosts = new();
    private readonly Dictionary<RMCConstructionGhostKey, BuildGhostState> _buildingGhosts = new();

    private EntityUid? _currentGhost;
    private RMCConstructionPrototype? _currentPrototype;
    private EntityUid? _currentConstructionItem;
    private EntityCoordinates _lastPosition = EntityCoordinates.Invalid;
    private bool _isPlacementActive = false;
    private DateTime _lastRotationTime = DateTime.MinValue;
    private Direction _currentDirection = Direction.North;
    private bool _isFlashing = false;
    private TimeSpan _flashEndTime;
    private MapId _lastPlayerMapId = MapId.Nullspace;
    private readonly Dictionary<RMCConstructionGhostKey, TimeSpan> _ghostFlashes = new();

    private static readonly Color StaticGhostColor = new(136, 199, 250, 160);
    private static readonly Color CursorGhostValidColor = new(74, 163, 232, 140);
    private static readonly Color CursorGhostInvalidColor = new(206, 210, 43, 140);
    private static readonly Color CursorGhostOverlapColor = new(255, 170, 90, 200);
    private static readonly Color FlashColor = new(164, 38, 37, 160);
    private static readonly TimeSpan FlashDuration = TimeSpan.FromMilliseconds(300);
    private static readonly Vector2 CursorGhostOverlapScale = new(1.07f, 1.07f);
    private const float BuildFadeMinDuration = 0.05f;
    private const float BuildCancelDistance = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        _mapSystem = EntityManager.System<SharedMapSystem>();
        UpdatesOutsidePrediction = true;

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(HandleUse, outsidePrediction: true))
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(HandleRightClick, outsidePrediction: true))
            .Bind(ContentKeyFunctions.EditorFlipObject, InputCmdHandler.FromDelegate(HandleRotate, outsidePrediction: true))
            .Register<RMCConstructionGhostSystem>();

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RMCAckStructureConstructionMessage>(HandleAckStructure);
        SubscribeNetworkEvent<RMCConstructionGhostBuildFailedMessage>(HandleBuildFailed);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var currentTime = _timing.CurTime;

        if (_isFlashing && currentTime >= _flashEndTime)
            _isFlashing = false;

        var userEntity = _playerManager.LocalEntity;
        foreach (var (ghostKey, state) in _buildingGhosts.ToList())
        {
            if (!_ghosts.TryGetValue(ghostKey, out var ghost) || !EntityManager.EntityExists(ghost))
            {
                _ghosts.Remove(ghostKey);
                _buildingGhosts.Remove(ghostKey);
                continue;
            }

            if (userEntity is { } userUid)
            {
                if (!userUid.IsValid() || userUid != state.User)
                {
                    HandleLocalBuildCancelled(ghostKey, state);
                    continue;
                }

                var currentPos = _transformSystem.GetWorldPosition(userUid);
                if ((currentPos - state.UserStartWorldPosition).LengthSquared() > BuildCancelDistance * BuildCancelDistance)
                {
                    HandleLocalBuildCancelled(ghostKey, state);
                    continue;
                }
            }

            if (!EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
                continue;

            var durationSeconds = Math.Max((float) state.Duration.TotalSeconds, BuildFadeMinDuration);
            var elapsedSeconds = (float) (currentTime - state.StartTime).TotalSeconds;
            var progress = Math.Clamp(elapsedSeconds / durationSeconds, 0f, 1f);
            sprite.Color = StaticGhostColor.WithAlpha(StaticGhostColor.A * progress);

            ApplyGhostFlash(ghostKey, sprite);

            if (progress >= 1f)
                ClearGhost(ghostKey);
        }

        UpdateGhostFlashes(currentTime);

        var playerForMap = _playerManager.LocalEntity;
        if (playerForMap is { } playerUid)
        {
            var mapId = EntityManager.GetComponent<TransformComponent>(playerUid).MapID;
            if (_lastPlayerMapId != MapId.Nullspace && mapId != _lastPlayerMapId)
            {
                StopPlacement();
                ClearAllGhosts();
            }

            _lastPlayerMapId = mapId;
        }
        else
        {
            _lastPlayerMapId = MapId.Nullspace;
        }

        var player = _playerManager.LocalEntity;
        if (player == null)
        {
            ClearCurrentGhost();
            return;
        }

        if (_isPlacementActive && _currentPrototype != null && _currentConstructionItem != null)
        {
            if (_currentPrototype.Type == RMCConstructionType.Structure)
            {
                if (_currentGhost == null)
                    CreateCursorFollowingGhost();
                UpdateGhostPosition();
            }
            else
            {
                ClearCurrentGhost();
            }
        }
        else
        {
            ClearCurrentGhost();
        }
    }

    private void HandleRotate(ICommonSession? session)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastRotationTime).TotalMilliseconds < 200)
            return;

        _lastRotationTime = now;

        if (_currentGhost != null && EntityManager.EntityExists(_currentGhost.Value))
        {
            RotateCurrentGhost();
            return;
        }

        var mousePos = SnapToGrid(_inputManager.MouseScreenPosition);
        if (!mousePos.IsValid(EntityManager))
            return;

        foreach (var (_, ghostEntity) in _ghosts)
        {
            var ghostTransform = EntityManager.GetComponent<TransformComponent>(ghostEntity);
            if (ghostTransform.Coordinates.Equals(mousePos))
            {
                RotateGhost(ghostEntity);
                return;
            }
        }
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
    {
        StopPlacement();
        ClearAllGhosts();
        _lastPlayerMapId = MapId.Nullspace;
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (_playerManager.LocalEntity is not { } player)
        {
            _lastPlayerMapId = MapId.Nullspace;
            return;
        }

        var xform = EntityManager.GetComponent<TransformComponent>(player);
        _lastPlayerMapId = xform.MapID;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        StopPlacement();
        ClearAllGhosts();
        _lastPlayerMapId = MapId.Nullspace;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCConstructionGhostSystem>();
    }

    private void CreateCursorFollowingGhost()
    {
        var player = _playerManager.LocalEntity;
        if (player == null || _currentPrototype == null || _currentConstructionItem == null)
            return;

        var playerCoords = EntityManager.GetComponent<TransformComponent>(player.Value).Coordinates;
        var ghost = EntityManager.SpawnEntity("rmcconstructionghost", playerCoords);

        if (EntityManager.TryGetComponent<ClickableComponent>(ghost, out _))
            EntityManager.RemoveComponent<ClickableComponent>(ghost);

        ConfigureGhostSprite(ghost, _currentPrototype, true);
        var direction = NormalizeDirection(_currentPrototype, _currentDirection);
        SetGhostDirection(ghost, direction);

        _currentGhost = ghost;
        _lastPosition = EntityCoordinates.Invalid;
    }

    private void UpdateGhostPosition()
    {
        var player = _playerManager.LocalEntity;
        if (player == null || _currentGhost == null || !EntityManager.EntityExists(_currentGhost.Value))
            return;

        var coords = SnapToGrid(_inputManager.MouseScreenPosition);
        if (!coords.IsValid(EntityManager))
            return;

        if (!coords.Equals(_lastPosition))
        {
            var ghostTransform = EntityManager.GetComponent<TransformComponent>(_currentGhost.Value);
            _transformSystem.SetCoordinates(_currentGhost.Value, ghostTransform, coords);
            _lastPosition = coords;
        }

        if (EntityManager.TryGetComponent<SpriteComponent>(_currentGhost.Value, out var sprite))
        {
            var overlapsGhost = IsOverlappingPlacedGhost(coords);
            sprite.Scale = overlapsGhost ? CursorGhostOverlapScale : Vector2.One;

            Color color;
            if (_isFlashing)
            {
                color = FlashColor;
            }
            else if (overlapsGhost)
            {
                color = CursorGhostOverlapColor;
            }
            else
            {
                var isValid = IsValidConstructionLocation(player.Value, coords);
                color = isValid ? CursorGhostValidColor : CursorGhostInvalidColor;
            }
            sprite.Color = color;
        }
    }

    private EntityCoordinates SnapToGrid(ScreenCoordinates screenCoords)
    {
        var mapCoords = _eyeManager.PixelToMap(screenCoords.Position);

        if (mapCoords.MapId == MapId.Nullspace)
        {
            var player = _playerManager.LocalEntity;
            return player != null ? _transformSystem.GetMoverCoordinates(player.Value) : EntityCoordinates.Invalid;
        }

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
        {
            var player = _playerManager.LocalEntity;
            if (player != null)
            {
                var playerTransform = EntityManager.GetComponent<TransformComponent>(player.Value);
                var playerGrid = _transformSystem.GetGrid(playerTransform.Coordinates);
                if (playerGrid != null && EntityManager.TryGetComponent<MapGridComponent>(playerGrid.Value, out grid))
                    gridUid = playerGrid.Value;
                else
                    return EntityCoordinates.Invalid;
            }
            else
            {
                return EntityCoordinates.Invalid;
            }
        }

        var gridLocalCoords = _transformSystem.ToCoordinates(gridUid, mapCoords);
        var tileCoords = _mapSystem.CoordinatesToTile(gridUid, grid, gridLocalCoords);
        return _mapSystem.GridTileToLocal(gridUid, grid, tileCoords);
    }

    private bool IsValidConstructionLocation(EntityUid player, EntityCoordinates coords)
    {
        if (_currentPrototype == null || _currentConstructionItem == null)
            return false;

        try
        {
            var direction = NormalizeDirection(_currentPrototype, _currentDirection);
            return TryValidateConstruction(_currentPrototype, _currentPrototype.Amount, coords, direction, player, _currentConstructionItem, requireSameTile: false, allowMissingMaterials: true, out _);
        }
        catch
        {
            return false;
        }
    }

    private bool IsOverlappingPlacedGhost(EntityCoordinates coords)
    {
        var targetTile = coords.ToVector2i(EntityManager, _mapManager, _transformSystem);
        foreach (var ghost in _ghosts.Values)
        {
            if (!EntityManager.EntityExists(ghost))
                continue;

            var ghostCoords = EntityManager.GetComponent<TransformComponent>(ghost).Coordinates;
            var ghostTile = ghostCoords.ToVector2i(EntityManager, _mapManager, _transformSystem);
            if (ghostTile == targetTile)
                return true;
        }

        return false;
    }

    private void ClearCurrentGhost()
    {
        if (_currentGhost != null)
        {
            if (EntityManager.EntityExists(_currentGhost.Value))
                EntityManager.QueueDeleteEntity(_currentGhost.Value);
        }

        _currentGhost = null;
        _lastPosition = EntityCoordinates.Invalid;
    }

    private void RotateCurrentGhost()
    {
        if (_currentPrototype?.NoRotate == true || _currentGhost == null || !EntityManager.EntityExists(_currentGhost.Value))
            return;

        _currentDirection = _currentDirection.GetClockwise90Degrees();
        SetGhostDirection(_currentGhost.Value, _currentDirection);
    }

    private void SetGhostDirection(EntityUid ghost, Direction direction)
    {
        EntityManager.GetComponent<TransformComponent>(ghost).LocalRotation = direction.ToAngle();
    }

    private Direction GetGhostDirection(EntityUid ghost)
    {
        return EntityManager.GetComponent<TransformComponent>(ghost).LocalRotation.GetCardinalDir();
    }

    private void StartFlash()
    {
        _isFlashing = true;
        _flashEndTime = _timing.CurTime + FlashDuration;
    }

    private void StartGhostFlash(RMCConstructionGhostKey ghostKey)
    {
        _ghostFlashes[ghostKey] = _timing.CurTime + FlashDuration;

        if (TryGetGhostEntity(ghostKey, out var ghost) &&
            EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
        {
            sprite.Color = FlashColor.WithAlpha(sprite.Color.A);
        }
    }

    private void HandleAckStructure(RMCAckStructureConstructionMessage msg)
    {
        ClearGhost(msg.GhostKey);
    }

    private void HandleBuildFailed(RMCConstructionGhostBuildFailedMessage msg)
    {
        if (msg.Reason == RMCConstructionFailureReason.Cancelled)
        {
            HandleServerBuildCancelled(msg.GhostKey);
        }
        else
        {
            EnsureGhostForFailure(msg.GhostKey);
            StopBuildingGhost(msg.GhostKey);
            StartGhostFlash(msg.GhostKey);
        }

        ShowFailurePopup(msg.Reason, msg.GhostKey, includeMissingMaterials: false);

        if (_currentGhost != null)
            StartFlash();
    }

    private void RotateGhost(EntityUid ghost)
    {
        if (!TryComp<RMCConstructionGhostComponent>(ghost, out var ghostComp) || ghostComp.Prototype?.NoRotate == true)
            return;

        var currentDirection = GetGhostDirection(ghost);
        var newDirection = currentDirection.GetClockwise90Degrees();
        SetGhostDirection(ghost, newDirection);

        var coords = EntityManager.GetComponent<TransformComponent>(ghost).Coordinates;
        TryUpdateGhostKey(ghost, ghostComp, coords, newDirection);
    }

    private bool HandleRightClick(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down)
            return false;

        if (args.EntityUid.IsValid() && IsClientSide(args.EntityUid) && HasComp<RMCConstructionGhostComponent>(args.EntityUid))
        {
            DeleteGhost(args.EntityUid);
            return true;
        }

        if (_isPlacementActive)
        {
            StopPlacement();
            return true;
        }

        return false;
    }

    private bool HandleUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down)
            return false;

        if (_isPlacementActive && _currentPrototype != null && _currentConstructionItem != null)
        {
            if (_currentPrototype.Type == RMCConstructionType.Item)
            {
                StartItemConstruction();
                return true;
            }
            else if (_currentGhost != null)
            {
                var mousePos = SnapToGrid(_inputManager.MouseScreenPosition);
                if (mousePos.IsValid(EntityManager))
                {
                    PlaceStructureAtLocation(mousePos);
                    return true;
                }
            }
        }

        if (args.EntityUid.IsValid() && IsClientSide(args.EntityUid) && HasComp<RMCConstructionGhostComponent>(args.EntityUid))
        {
            TryStartConstruction(args.EntityUid);
            return true;
        }

        return false;
    }

    private void StartItemConstruction()
    {
        var player = _playerManager.LocalEntity;
        if (player == null || _currentPrototype == null || _currentConstructionItem == null)
            return;

        var playerCoords = EntityManager.GetComponent<TransformComponent>(player.Value).Coordinates;
        var direction = NormalizeDirection(_currentPrototype, _currentDirection);
        var ghostKey = MakeGhostKey(_currentPrototype, playerCoords, direction);

        if (!TryValidateConstruction(_currentPrototype, _currentPrototype.Amount, playerCoords, direction, player.Value, _currentConstructionItem, out var reason))
        {
            StartFlash();
            ShowFailurePopup(reason, ghostKey, includeMissingMaterials: true);
            return;
        }

        var msg = new RMCConstructionGhostBuildMessage(_currentPrototype.Amount, ghostKey);

        RaiseNetworkEvent(msg);
        StopPlacement();
    }

    private void PlaceStructureAtLocation(EntityCoordinates coords)
    {
        var player = _playerManager.LocalEntity;
        if (player == null || _currentPrototype == null || _currentConstructionItem == null)
            return;

        var direction = NormalizeDirection(_currentPrototype, _currentDirection);
        var ghostKey = MakeGhostKey(_currentPrototype, coords, direction);
        if (!TryValidateConstruction(_currentPrototype, _currentPrototype.Amount, coords, direction, player.Value, _currentConstructionItem, requireSameTile: false, allowMissingMaterials: true, out var reason))
        {
            StartFlash();
            ShowFailurePopup(reason, ghostKey, includeMissingMaterials: true);
            return;
        }

        if (!TrySpawnGhost(_currentPrototype, coords, direction, _currentConstructionItem.Value, out _))
            StartFlash();

        ClearCurrentGhost();
    }

    public void StartPlacement(RMCConstructionPrototype prototype, EntityUid constructionItem)
    {
        StopPlacement();
        _currentPrototype = prototype;
        _currentConstructionItem = constructionItem;
        _currentDirection = Direction.North;
        _isPlacementActive = true;
    }

    public bool TryBuildAtPlayer(RMCConstructionPrototype prototype, EntityUid constructionItem, int amount)
    {
        if (prototype.Type != RMCConstructionType.Structure)
            return false;

        var player = _playerManager.LocalEntity;
        if (player == null)
            return false;

        var playerTransform = EntityManager.GetComponent<TransformComponent>(player.Value);
        var coords = playerTransform.Coordinates;
        var gridUid = _transformSystem.GetGrid(coords);
        if (gridUid != null && EntityManager.TryGetComponent<MapGridComponent>(gridUid.Value, out var grid))
        {
            var tileCoords = _mapSystem.CoordinatesToTile(gridUid.Value, grid, coords);
            coords = _mapSystem.GridTileToLocal(gridUid.Value, grid, tileCoords);
        }
        var direction = NormalizeDirection(prototype, playerTransform.LocalRotation.GetCardinalDir());

        if (!TryValidateConstruction(prototype, amount, coords, direction, player.Value, constructionItem, out var reason))
        {
            StartFlash();
            ShowFailurePopup(reason, MakeGhostKey(prototype, coords, direction), includeMissingMaterials: true);
            return false;
        }

        if (!TrySpawnGhost(prototype, coords, direction, constructionItem, out var ghost))
        {
            StartFlash();
            return false;
        }

        if (!TryComp<RMCConstructionGhostComponent>(ghost, out var ghostComp))
        {
            return false;
        }

        var ghostKey = ghostComp.GhostKey ?? MakeGhostKey(prototype, coords, direction);
        ghostComp.GhostKey = ghostKey;
        _ghosts[ghostKey] = ghost.Value;

        var msg = new RMCConstructionGhostBuildMessage(amount, ghostKey);

        RaiseNetworkEvent(msg);
        StartBuildingGhost(ghostKey, player.Value, _transformSystem.GetWorldPosition(player.Value), GetBuildDuration(prototype), clearOnCancel: true);
        return true;
    }

    public void StopPlacement()
    {
        _isPlacementActive = false;
        _currentConstructionItem = null;
        ClearCurrentGhost();
    }

    public void SpawnGhost(RMCConstructionPrototype prototype, EntityCoordinates loc, Direction dir)
        => TrySpawnGhost(prototype, loc, dir, out _);

    public bool TrySpawnGhost(RMCConstructionPrototype prototype, EntityCoordinates loc, Direction dir, [NotNullWhen(true)] out EntityUid? ghost)
    {
        ghost = null;
        if (_playerManager.LocalEntity is not { } user || !user.IsValid())
            return false;

        var normalizedDirection = NormalizeDirection(prototype, dir);
        if (prototype.NoRotate)
            ClearGhostAtLocation(loc);
        else
            ClearGhostAtLocation(loc, normalizedDirection);

        ghost = EntityManager.SpawnEntity("rmcconstructionghost", loc);
        var comp = EntityManager.GetComponent<RMCConstructionGhostComponent>(ghost.Value);
        comp.Prototype = prototype;
        comp.GhostKey = MakeGhostKey(prototype, loc, normalizedDirection);

        SetGhostDirection(ghost.Value, normalizedDirection);
        _ghosts[comp.GhostKey.Value] = ghost.Value;
        ConfigureGhostSprite(ghost.Value, prototype, false);

        return true;
    }

    public bool TrySpawnGhost(RMCConstructionPrototype prototype, EntityCoordinates loc, Direction dir, EntityUid constructionItem, [NotNullWhen(true)] out EntityUid? ghost)
    {
        return TrySpawnGhost(prototype, loc, dir, out ghost);
    }

    private void ConfigureGhostSprite(EntityUid ghost, RMCConstructionPrototype prototype, bool isCursor)
    {
        if (!EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
            return;

        var color = isCursor ? CursorGhostValidColor : StaticGhostColor;
        var depth = isCursor ? 10 : 9;

        sprite.Color = color;
        sprite.DrawDepth = depth;
        sprite.Visible = true;

        if (!_prototypeManager.TryIndex<EntityPrototype>(prototype.Prototype, out var entityProto))
            return;

        if (entityProto.TryGetComponent<SpriteComponent>(out var prototypeSprite, EntityManager.ComponentFactory))
        {
            sprite.CopyFrom(prototypeSprite);
            sprite.Color = color;
            sprite.DrawDepth = depth;

            foreach (var (index, layer) in sprite.AllLayers.Select((layer, i) => (i, layer)))
            {
                sprite.LayerSetShader(index, "unshaded");
                // Preserve layer visibility from the prototype to avoid showing folded/alt layers.
                if (!layer.Visible)
                    continue;
            }
        }
    }

    private bool TryValidateConstruction(RMCConstructionPrototype prototype, int amount, EntityCoordinates loc, Direction dir, EntityUid user, EntityUid? constructionItem, bool requireSameTile, bool allowMissingMaterials, out RMCConstructionFailureReason reason)
    {
        reason = RMCConstructionFailureReason.Unknown;

        if (constructionItem is not { } item || !item.IsValid())
        {
            reason = RMCConstructionFailureReason.MissingMaterials;
            return allowMissingMaterials;
        }

        if (!_constructionSystem.IsValidConstructionItemForPrototype(item, prototype, ignoreStack: true))
        {
            reason = RMCConstructionFailureReason.InvalidConstructionItem;
            return false;
        }

        if (_constructionSystem.TryValidateGhostBuild(user, item, prototype, amount, loc, dir, requireSameTile, out reason))
            return true;

        return allowMissingMaterials && reason == RMCConstructionFailureReason.MissingMaterials;
    }

    private bool TryValidateConstruction(RMCConstructionPrototype prototype, int amount, EntityCoordinates loc, Direction dir, EntityUid user, EntityUid? constructionItem, out RMCConstructionFailureReason reason)
    {
        return TryValidateConstruction(prototype, amount, loc, dir, user, constructionItem, requireSameTile: true, allowMissingMaterials: false, out reason);
    }

    private RMCConstructionGhostKey MakeGhostKey(RMCConstructionPrototype prototype, EntityCoordinates coordinates, Direction direction)
    {
        var normalizedDirection = NormalizeDirection(prototype, direction);
        return new RMCConstructionGhostKey(prototype.ID, GetNetCoordinates(coordinates), normalizedDirection);
    }

    private static Direction NormalizeDirection(RMCConstructionPrototype prototype, Direction direction)
    {
        return prototype.NoRotate ? Direction.North : direction;
    }

    private bool TryUpdateGhostKey(EntityUid ghost, RMCConstructionGhostComponent ghostComp, EntityCoordinates coordinates, Direction direction)
    {
        if (ghostComp.Prototype == null)
            return false;

        var newKey = MakeGhostKey(ghostComp.Prototype, coordinates, direction);
        if (ghostComp.GhostKey.HasValue && ghostComp.GhostKey.Value.Equals(newKey))
            return true;

        if (ghostComp.GhostKey.HasValue)
        {
            var oldKey = ghostComp.GhostKey.Value;
            if (_ghosts.Remove(oldKey))
                _ghosts[newKey] = ghost;

            if (_buildingGhosts.TryGetValue(oldKey, out var state))
            {
                _buildingGhosts.Remove(oldKey);
                _buildingGhosts[newKey] = state;
            }

            if (_ghostFlashes.TryGetValue(oldKey, out var flashEnd))
            {
                _ghostFlashes.Remove(oldKey);
                _ghostFlashes[newKey] = flashEnd;
            }
        }
        else
        {
            _ghosts[newKey] = ghost;
        }

        ghostComp.GhostKey = newKey;
        return true;
    }

    private void ClearGhostAtLocation(EntityCoordinates loc, Direction? direction = null)
    {
        var ghostsToRemove = _ghosts.Where(kvp =>
        {
            var ghostTransform = EntityManager.GetComponent<TransformComponent>(kvp.Value);
            if (!ghostTransform.Coordinates.Equals(loc))
                return false;

            if (direction.HasValue)
            {
                var ghostDirection = GetGhostDirection(kvp.Value);
                return ghostDirection == direction.Value;
            }

            return true;
        })
        .Select(kvp => kvp.Key)
        .ToList();

        foreach (var ghostKey in ghostsToRemove)
            ClearGhost(ghostKey);
    }

    public void TryStartConstruction(EntityUid ghostId, RMCConstructionGhostComponent? ghostComp = null)
    {
        if (!Resolve(ghostId, ref ghostComp) || ghostComp.Prototype == null)
        {
            return;
        }

        var user = _playerManager.LocalEntity;
        if (user == null)
        {
            return;
        }

        var transform = EntityManager.GetComponent<TransformComponent>(ghostId);
        var direction = NormalizeDirection(ghostComp.Prototype, GetGhostDirection(ghostId));
        if (ghostComp.Prototype.NoRotate)
            SetGhostDirection(ghostId, direction);

        var constructionItem = _constructionSystem.FindValidConstructionItem(user.Value, ghostComp.Prototype.ID);
        if (constructionItem == null)
        {
            var missingKey = ghostComp.GhostKey ?? MakeGhostKey(ghostComp.Prototype, transform.Coordinates, direction);
            StartGhostFlash(missingKey);
            ShowFailurePopup(RMCConstructionFailureReason.MissingMaterials, missingKey, includeMissingMaterials: true);
            return;
        }

        if (!TryValidateConstruction(ghostComp.Prototype, ghostComp.Prototype.Amount, transform.Coordinates, direction, user.Value, constructionItem.Value, out var reason))
        {
            var failKey = ghostComp.GhostKey ?? MakeGhostKey(ghostComp.Prototype, transform.Coordinates, direction);
            StartGhostFlash(failKey);
            ShowFailurePopup(reason, failKey, includeMissingMaterials: true);
            return;
        }

        var ghostKey = ghostComp.GhostKey ?? MakeGhostKey(ghostComp.Prototype, transform.Coordinates, direction);
        ghostComp.GhostKey = ghostKey;
        _ghosts[ghostKey] = ghostId;

        var msg = new RMCConstructionGhostBuildMessage(ghostComp.Prototype.Amount, ghostKey);

        RaiseNetworkEvent(msg);
        StartBuildingGhost(ghostKey, user.Value, _transformSystem.GetWorldPosition(user.Value), GetBuildDuration(ghostComp.Prototype), clearOnCancel: false);
    }

    public void ClearGhost(RMCConstructionGhostKey ghostKey)
    {
        if (TryGetGhostEntity(ghostKey, out var ghost))
            EntityManager.QueueDeleteEntity(ghost);

        _ghosts.Remove(ghostKey);
        _buildingGhosts.Remove(ghostKey);
        _ghostFlashes.Remove(ghostKey);
    }

    private void StopBuildingGhost(RMCConstructionGhostKey ghostKey)
    {
        _buildingGhosts.Remove(ghostKey);
        _ghostFlashes.Remove(ghostKey);

        if (TryGetGhostEntity(ghostKey, out var ghost) &&
            EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
        {
            sprite.Color = StaticGhostColor;
        }
    }

    private void UpdateGhostFlashes(TimeSpan currentTime)
    {
        if (_ghostFlashes.Count == 0)
            return;

        foreach (var (ghostKey, endTime) in _ghostFlashes.ToList())
        {
            if (currentTime < endTime)
            {
                if (TryGetGhostEntity(ghostKey, out var ghost) &&
                    EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
                {
                    sprite.Color = FlashColor.WithAlpha(sprite.Color.A);
                }

                continue;
            }

            _ghostFlashes.Remove(ghostKey);

            if (TryGetGhostEntity(ghostKey, out var restoreGhost) &&
                EntityManager.TryGetComponent<SpriteComponent>(restoreGhost, out var restoreSprite))
            {
                restoreSprite.Color = StaticGhostColor.WithAlpha(GetGhostAlpha(ghostKey, currentTime));
            }
        }
    }

    private void ApplyGhostFlash(RMCConstructionGhostKey ghostKey, SpriteComponent sprite)
    {
        if (_ghostFlashes.TryGetValue(ghostKey, out var endTime) && _timing.CurTime < endTime)
            sprite.Color = FlashColor.WithAlpha(sprite.Color.A);
    }

    private float GetGhostAlpha(RMCConstructionGhostKey ghostKey, TimeSpan currentTime)
    {
        if (_buildingGhosts.TryGetValue(ghostKey, out var state))
        {
            var durationSeconds = Math.Max((float) state.Duration.TotalSeconds, BuildFadeMinDuration);
            var elapsedSeconds = (float) (currentTime - state.StartTime).TotalSeconds;
            var progress = Math.Clamp(elapsedSeconds / durationSeconds, 0f, 1f);
            return StaticGhostColor.A * progress;
        }

        return StaticGhostColor.A;
    }

    private bool TryGetGhostEntity(RMCConstructionGhostKey ghostKey, out EntityUid ghost)
    {
        if (_ghosts.TryGetValue(ghostKey, out ghost) && EntityManager.EntityExists(ghost))
            return true;

        var query = EntityQueryEnumerator<RMCConstructionGhostComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Prototype == null)
                continue;

            var key = comp.GhostKey ?? MakeGhostKey(comp.Prototype, xform.Coordinates, GetGhostDirection(uid));
            if (!key.Equals(ghostKey))
                continue;

            comp.GhostKey = key;
            ghost = uid;
            _ghosts[ghostKey] = uid;
            return true;
        }

        _ghosts.Remove(ghostKey);
        ghost = default;
        return false;
    }

    private void EnsureGhostForFailure(RMCConstructionGhostKey ghostKey)
    {
        if (TryGetGhostEntity(ghostKey, out _))
            return;

        if (!_prototypeManager.TryIndex<RMCConstructionPrototype>(ghostKey.Prototype, out var prototype))
            return;

        var coords = GetCoordinates(ghostKey.Coordinates);
        if (!TrySpawnGhost(prototype, coords, ghostKey.Direction, out var ghost))
            return;

        if (!TryComp<RMCConstructionGhostComponent>(ghost.Value, out var comp))
            return;

        if (comp.GhostKey.HasValue && !comp.GhostKey.Value.Equals(ghostKey))
            _ghosts.Remove(comp.GhostKey.Value);

        comp.GhostKey = ghostKey;
        _ghosts[ghostKey] = ghost.Value;
    }

    private void ShowFailurePopup(RMCConstructionFailureReason reason, RMCConstructionGhostKey? ghostKey, bool includeMissingMaterials)
    {
        if (reason == RMCConstructionFailureReason.Unknown ||
            reason == RMCConstructionFailureReason.Cancelled ||
            (!includeMissingMaterials && reason == RMCConstructionFailureReason.MissingMaterials))
        {
            return;
        }

        if (_playerManager.LocalEntity is not { } user)
            return;

        var message = GetFailurePopupMessage(reason, ghostKey);
        if (string.IsNullOrEmpty(message))
            return;

        _popup.PopupClient(message, user, user, PopupType.SmallCaution);
    }

    private string? GetFailurePopupMessage(RMCConstructionFailureReason reason, RMCConstructionGhostKey? ghostKey)
    {
        var prototypeName = TryGetGhostPrototypeName(ghostKey);

        return reason switch
        {
            RMCConstructionFailureReason.MissingMaterials => "You don't have the required materials.",
            RMCConstructionFailureReason.InvalidConstructionItem => prototypeName != null
                ? $"You don't have the correct item to build {prototypeName}."
                : "You don't have the correct item to build that.",
            RMCConstructionFailureReason.SkillMissing => prototypeName != null
                ? $"You lack the skill to build {prototypeName}."
                : "You lack the skill to build that.",
            RMCConstructionFailureReason.NotOnSameTile => prototypeName != null
                ? $"You need to stand on the same tile to build {prototypeName}."
                : "You need to stand on the same tile to build that.",
            RMCConstructionFailureReason.InvalidLocation => prototypeName != null
                ? $"You can't build {prototypeName} here."
                : "You can't build there.",
            RMCConstructionFailureReason.ConstructionDisabled => "You cannot construct right now.",
            _ => null
        };
    }

    private string? TryGetGhostPrototypeName(RMCConstructionGhostKey? ghostKey)
    {
        if (ghostKey.HasValue &&
            _prototypeManager.TryIndex<RMCConstructionPrototype>(ghostKey.Value.Prototype, out var proto))
            return proto.Name;

        if (_currentPrototype != null)
            return _currentPrototype.Name;

        return null;
    }

    public void DeleteGhost(EntityUid ghost)
    {
        if (!TryComp<RMCConstructionGhostComponent>(ghost, out var ghostComp))
            return;

        if (ghostComp.GhostKey is { } ghostKey)
        {
            ClearGhost(ghostKey);
            return;
        }

        var coords = EntityManager.GetComponent<TransformComponent>(ghost).Coordinates;
        var direction = GetGhostDirection(ghost);
        if (ghostComp.Prototype == null)
            return;

        ClearGhost(MakeGhostKey(ghostComp.Prototype, coords, direction));
    }

    public void ClearAllGhosts()
    {
        foreach (var ghost in _ghosts.Values)
            EntityManager.QueueDeleteEntity(ghost);

        var query = EntityQueryEnumerator<RMCConstructionGhostComponent>();
        while (query.MoveNext(out var uid, out _))
            EntityManager.QueueDeleteEntity(uid);

        _ghosts.Clear();
        _buildingGhosts.Clear();
        _ghostFlashes.Clear();
        ClearCurrentGhost();
    }

    private void StartBuildingGhost(RMCConstructionGhostKey ghostKey, EntityUid user, Vector2 userStartWorldPosition, TimeSpan duration, bool clearOnCancel)
    {
        _buildingGhosts[ghostKey] = new BuildGhostState(_timing.CurTime, duration, user, userStartWorldPosition, clearOnCancel);

        if (_ghosts.TryGetValue(ghostKey, out var ghost) && EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
            sprite.Color = StaticGhostColor.WithAlpha(0f);
    }

    private static TimeSpan GetBuildDuration(RMCConstructionPrototype prototype)
    {
        var duration = prototype.DoAfterTime;
        if (prototype.DoAfterTimeMin > duration)
            duration = prototype.DoAfterTimeMin;
        return duration;
    }

    private readonly struct BuildGhostState
    {
        public readonly TimeSpan StartTime;
        public readonly TimeSpan Duration;
        public readonly EntityUid User;
        public readonly Vector2 UserStartWorldPosition;
        public readonly bool ClearOnCancel;

        public BuildGhostState(TimeSpan startTime, TimeSpan duration, EntityUid user, Vector2 userStartWorldPosition, bool clearOnCancel)
        {
            StartTime = startTime;
            Duration = duration;
            User = user;
            UserStartWorldPosition = userStartWorldPosition;
            ClearOnCancel = clearOnCancel;
        }
    }

    private void HandleLocalBuildCancelled(RMCConstructionGhostKey ghostKey, BuildGhostState state)
    {
        if (state.ClearOnCancel)
        {
            ClearGhost(ghostKey);
            return;
        }

        StopBuildingGhost(ghostKey);
        StartGhostFlash(ghostKey);
    }

    private void HandleServerBuildCancelled(RMCConstructionGhostKey ghostKey)
    {
        if (_buildingGhosts.TryGetValue(ghostKey, out var state) && state.ClearOnCancel)
        {
            ClearGhost(ghostKey);
            return;
        }

        StopBuildingGhost(ghostKey);
        StartGhostFlash(ghostKey);
    }
}
