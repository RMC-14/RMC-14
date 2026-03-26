using System.Linq;
using Content.Client.IconSmoothing;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Construction;

[UsedImplicitly]
public sealed class XenoConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;

    private EntityUid? _currentGhost;
    private string? _currentGhostStructure;
    private EntityCoordinates _lastPosition = EntityCoordinates.Invalid;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(HandleUse, outsidePrediction: true))
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(HandleRightClick, outsidePrediction: true))
            .Register<XenoConstructionGhostSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<XenoConstructionGhostSystem>();
    }

    private bool HandleUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down)
            return false;

        var player = _playerManager.LocalEntity;
        if (player == null || !TryComp(player.Value, out XenoConstructionComponent? construction))
            return false;

        if (construction.OrderConstructionTargeting && construction.OrderConstructionChoice != null)
        {
            var mouseScreenPos = _inputManager.MouseScreenPosition;
            var coords = SnapToGrid(mouseScreenPos);

            if (!coords.IsValid(EntityManager))
                return false;

            if (!IsValidConstructionLocation(player.Value, coords))
                return false;

            var netCoords = GetNetCoordinates(coords);
            var clickEvent = new XenoOrderConstructionClickEvent(netCoords, construction.OrderConstructionChoice.Value);
            RaiseNetworkEvent(clickEvent);

            return true;
        }

        return false;
    }

    private bool HandleRightClick(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down)
            return false;

        var player = _playerManager.LocalEntity;
        if (player == null || !TryComp(player.Value, out XenoConstructionComponent? construction))
            return false;

        if (!construction.OrderConstructionTargeting)
            return false;

        ClearCurrentGhost();
        var cancelEvent = new XenoOrderConstructionCancelEvent();
        RaiseNetworkEvent(cancelEvent);
        return true;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _playerManager.LocalEntity;
        if (player == null)
        {
            ClearCurrentGhost();
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
                ClearCurrentGhost();
                CreateGhost(player.Value, buildChoice!);
            }
            UpdateGhostPosition();
        }
        else
        {
            ClearCurrentGhost();
        }
    }

    private (string? buildChoice, bool isActive) GetConstructionState(EntityUid player)
    {
        if (TryComp(player, out XenoConstructionComponent? construction))
        {
            if (construction.OrderConstructionTargeting && construction.OrderConstructionChoice != null)
            {
                return (construction.OrderConstructionChoice.Value.Id, true);
            }
        }

        var actionController = _uiManager.GetUIController<ActionUIController>();
        if (actionController.SelectingTargetFor is not { } selectedActionId)
            return (null, false);

        if (HasComp<XenoConstructionActionComponent>(selectedActionId) && construction != null)
        {
            var buildChoice = construction.BuildChoice?.Id;
            return (buildChoice, true);
        }

        return (null, false);
    }

    private bool IsBuilding(EntityUid player)
    {
        if (!TryComp(player, out DoAfterComponent? doAfter))
            return false;

        return doAfter.DoAfters.Values.Any(activeDoAfter =>
            activeDoAfter.Args.Event is XenoSecreteStructureDoAfterEvent or XenoOrderConstructionDoAfterEvent);
    }

    private void CreateGhost(EntityUid player, string structurePrototype)
    {
        if (!TryComp(player, out TransformComponent? xform))
            return;

        var playerCoords = xform.Coordinates;
        var ghost = Spawn("XenoConstructionGhost", playerCoords);
        var actualPrototype = GetActualBuildPrototype(player, structurePrototype);

        ConfigureGhostSprite(ghost, actualPrototype);

        _currentGhost = ghost;
        _currentGhostStructure = structurePrototype; // Keep original for comparison
        _lastPosition = EntityCoordinates.Invalid;
    }

    private string GetActualBuildPrototype(EntityUid player, string originalPrototype)
    {
        if (HasComp<QueenBuildingBoostComponent>(player))
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
        // TODO RMC14 move this to a component
        return originalId switch
        {
            "WallXenoResin" => "WallXenoResinQueen",
            "WallXenoMembrane" => "WallXenoMembraneQueen",
            "DoorXenoResin" => "DoorXenoResinQueen",
            _ => originalId,
        };
    }

    private void ConfigureGhostSprite(EntityUid ghost, string structurePrototype)
    {
        if (!TryComp(ghost, out SpriteComponent? sprite))
            return;

        sprite.Color = new Color(48, 255, 48, 128);
        sprite.DrawDepth = 9;
        sprite.Visible = true;

        if (!_prototypeManager.TryIndex<EntityPrototype>(structurePrototype, out var prototype))
            return;

        if (TryConfigureIconSmoothSprite(sprite, prototype))
            return;

        if (prototype.TryGetComponent<SpriteComponent>(out var prototypeSprite, _compFactory))
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
        if (!prototype.TryGetComponent(out IconSmoothComponent? iconSmooth, _compFactory) ||
            !prototype.TryGetComponent(out SpriteComponent? prototypeSprite, _compFactory) ||
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
        if (player == null || _currentGhost == null || !Exists(_currentGhost.Value))
            return;

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var coords = SnapToGrid(mouseScreenPos);

        if (!coords.IsValid(EntityManager))
            return;

        if (!coords.Equals(_lastPosition))
        {
            if (TryComp(_currentGhost, out TransformComponent? xform))
                _transform.SetCoordinates(_currentGhost.Value, xform, coords);

            _lastPosition = coords;
        }

        if (TryComp(_currentGhost.Value, out SpriteComponent? sprite))
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
        if (!TryComp(player, out XenoConstructionComponent? construction))
            return false;

        try
        {
            if (construction.OrderConstructionTargeting && construction.OrderConstructionChoice != null)
            {
                return _xenoConstruction.CanOrderConstructionPopup((player, construction), coords, construction.OrderConstructionChoice, false);
            }

            if (construction.BuildChoice != null)
            {
                return _xenoConstruction.CanSecreteOnTilePopup((player, construction), construction.BuildChoice, coords, true, true, false);
            }

            return false;
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
            if (Exists(_currentGhost.Value))
                QueueDel(_currentGhost.Value);
        }

        _currentGhost = null;
        _currentGhostStructure = null;
        _lastPosition = EntityCoordinates.Invalid;
    }
}
