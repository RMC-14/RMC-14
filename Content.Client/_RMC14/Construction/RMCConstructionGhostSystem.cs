using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Construction.Prototypes;
using Content.Shared._RMC14.Map;
using Content.Shared.Examine;
using Content.Shared.Input;
using Content.Shared.Stacks;
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
using Content.Shared._RMC14.Dropship;
using Robust.Shared.Map.Components;
using Content.Client.Clickable;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Construction;

[UsedImplicitly]
public sealed class RMCConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private SharedMapSystem _mapSystem = default!;
    private RMCMapSystem _rmcMap = default!;

    private readonly Dictionary<int, EntityUid> _ghosts = new();
    private readonly Dictionary<int, TimeSpan> _flashingGhosts = new();

    private EntityUid? _currentGhost;
    private RMCConstructionPrototype? _currentPrototype;
    private EntityUid? _currentConstructionItem;
    private EntityCoordinates _lastPosition = EntityCoordinates.Invalid;
    private bool _isPlacementActive = false;
    private DateTime _lastRotationTime = DateTime.MinValue;
    private Direction _currentDirection = Direction.North;
    private bool _isFlashing = false;
    private TimeSpan _flashEndTime;

    private static readonly Color StaticGhostColor = new(48, 255, 48, 128);
    private static readonly Color CursorGhostValidColor = new(100, 150, 255, 128);
    private static readonly Color CursorGhostInvalidColor = new(255, 255, 100, 128);
    private static readonly Color FlashColor = new(255, 48, 48, 128);
    private static readonly TimeSpan FlashDuration = TimeSpan.FromMilliseconds(300);

    public override void Initialize()
    {
        base.Initialize();

        _mapSystem = EntityManager.System<SharedMapSystem>();
        _rmcMap = EntityManager.System<RMCMapSystem>();

        UpdatesOutsidePrediction = true;

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(HandleUse, outsidePrediction: true))
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(HandleRightClick, outsidePrediction: true))
            .Bind(ContentKeyFunctions.EditorFlipObject, InputCmdHandler.FromDelegate(HandleRotate, outsidePrediction: true))
            .Register<RMCConstructionGhostSystem>();

        SubscribeNetworkEvent<RMCAckStructureConstructionMessage>(HandleAckStructure);
        SubscribeNetworkEvent<RMCConstructionGhostBuildFailedMessage>(HandleBuildFailed);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var currentTime = _timing.CurTime;

        if (_isFlashing && currentTime >= _flashEndTime)
            _isFlashing = false;

        foreach (var (ghostId, endTime) in _flashingGhosts.ToList())
        {
            if (currentTime >= endTime)
            {
                _flashingGhosts.Remove(ghostId);
                if (_ghosts.TryGetValue(ghostId, out var ghost) && EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
                    sprite.Color = StaticGhostColor;
            }
        }

        var player = _playerManager.LocalEntity;
        if (player == null)
        {
            ClearCurrentGhost();
            return;
        }

        if (_isPlacementActive && _currentPrototype != null && _currentConstructionItem != null)
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
        SetGhostDirection(ghost, _currentDirection);

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
            Color color;
            if (_isFlashing)
            {
                color = FlashColor;
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
            return CheckConstructionConditionsForGhost(_currentPrototype, coords, _currentDirection, player, _currentConstructionItem.Value);
        }
        catch
        {
            return false;
        }
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

    private void HandleAckStructure(RMCAckStructureConstructionMessage msg)
    {
        ClearGhost(msg.GhostId);
    }

    private void HandleBuildFailed(RMCConstructionGhostBuildFailedMessage msg)
    {
        if (_ghosts.TryGetValue(msg.GhostId, out var ghost) && EntityManager.TryGetComponent<SpriteComponent>(ghost, out var sprite))
        {
            sprite.Color = FlashColor;
            _flashingGhosts[msg.GhostId] = _timing.CurTime + FlashDuration;
        }

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

        if (_isPlacementActive && _currentGhost != null && _currentPrototype != null && _currentConstructionItem != null)
        {
            var mousePos = SnapToGrid(_inputManager.MouseScreenPosition);
            if (mousePos.IsValid(EntityManager))
            {
                PlaceStructureAtLocation(mousePos);
                return true;
            }
        }

        if (args.EntityUid.IsValid() && IsClientSide(args.EntityUid) && HasComp<RMCConstructionGhostComponent>(args.EntityUid))
        {
            TryStartConstruction(args.EntityUid);
            return true;
        }

        return false;
    }

    private void PlaceStructureAtLocation(EntityCoordinates coords)
    {
        var player = _playerManager.LocalEntity;
        if (player == null || _currentPrototype == null || _currentConstructionItem == null)
            return;

        if (!CheckConstructionConditionsForGhost(_currentPrototype, coords, _currentDirection, player.Value, _currentConstructionItem.Value))
        {
            StartFlash();
            return;
        }

        if (!TrySpawnGhost(_currentPrototype, coords, _currentDirection, _currentConstructionItem.Value, out _))
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

        ClearGhostAtLocation(loc, dir);

        var ghostId = Random.Shared.Next();
        ghost = EntityManager.SpawnEntity("rmcconstructionghost", loc);
        var comp = EntityManager.GetComponent<RMCConstructionGhostComponent>(ghost.Value);
        comp.Prototype = prototype;
        comp.GhostId = ghostId;

        SetGhostDirection(ghost.Value, dir);
        _ghosts.Add(comp.GhostId, ghost.Value);
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

            foreach (var (index, _) in sprite.AllLayers.Select((layer, i) => (i, layer)))
            {
                sprite.LayerSetShader(index, "unshaded");
                sprite.LayerSetVisible(index, true);
            }
        }
    }

    private bool CheckConstructionConditionsForGhost(RMCConstructionPrototype prototype, EntityCoordinates loc, Direction dir, EntityUid user)
    {
        var attempt = new RMCConstructionAttemptEvent(loc, prototype.Name);
        RaiseLocalEvent(ref attempt);

        if (attempt.Cancelled)
            return false;

        if (HasComp<DisableConstructionComponent>(user))
            return false;

        if (_transformSystem.GetGrid(loc) is { } gridId && HasComp<DropshipComponent>(gridId))
            return false;

        if (prototype.RestrictedTags != null && _rmcMap.TileHasAnyTag(loc, prototype.RestrictedTags))
            return false;

        return true;
    }

    private bool CheckConstructionConditionsForGhost(RMCConstructionPrototype prototype, EntityCoordinates loc, Direction dir, EntityUid user, EntityUid constructionItem)
    {
        if (!CheckConstructionConditionsForGhost(prototype, loc, dir, user))
            return false;

        if (TryComp<StackComponent>(constructionItem, out var stack))
        {
            if (prototype.MaterialCost.HasValue && stack.Count < prototype.MaterialCost.Value)
                return false;
        }

        return true;
    }

    private bool CheckConstructionConditions(RMCConstructionPrototype prototype, EntityCoordinates loc, Direction dir, EntityUid user, EntityUid constructionItem)
    {
        return _examineSystem.InRangeUnOccluded(user, loc, 1.5f) &&
               CheckConstructionConditionsForGhost(prototype, loc, dir, user, constructionItem);
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

        foreach (var ghostId in ghostsToRemove)
            ClearGhost(ghostId);
    }

    public void TryStartConstruction(EntityUid ghostId, RMCConstructionGhostComponent? ghostComp = null)
    {
        if (!Resolve(ghostId, ref ghostComp) || ghostComp.Prototype == null)
            return;

        var user = _playerManager.LocalEntity;
        if (user == null)
            return;

        var transform = EntityManager.GetComponent<TransformComponent>(ghostId);
        var direction = GetGhostDirection(ghostId);

        var msg = new RMCConstructionGhostBuildMessage(
            ghostComp.Prototype.ID,
            ghostComp.Prototype.Amount,
            GetNetCoordinates(transform.Coordinates),
            direction,
            ghostComp.GhostId);

        RaiseNetworkEvent(msg);
    }

    public void ClearGhost(int ghostId)
    {
        if (!_ghosts.TryGetValue(ghostId, out var ghost))
            return;

        EntityManager.QueueDeleteEntity(ghost);
        _ghosts.Remove(ghostId);
        _flashingGhosts.Remove(ghostId);
    }

    public void DeleteGhost(EntityUid ghost)
    {
        if (!TryComp<RMCConstructionGhostComponent>(ghost, out var ghostComp))
            return;

        ClearGhost(ghostComp.GhostId);
    }

    public void ClearAllGhosts()
    {
        foreach (var ghost in _ghosts.Values)
            EntityManager.QueueDeleteEntity(ghost);

        _ghosts.Clear();
        _flashingGhosts.Clear();
        ClearCurrentGhost();
    }
}
