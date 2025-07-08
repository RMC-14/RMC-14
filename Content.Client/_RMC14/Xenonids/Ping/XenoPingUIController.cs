using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Ping;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._RMC14.Xenonids.Ping;

public sealed class XenoPingUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private ColoredSimpleRadialMenu? _menu;
    private EntityCoordinates? _targetCoordinates;
    private EntityUid? _targetEntity;

    private static readonly Color MenuBackgroundColor = Color.FromHex("#8000FF").WithAlpha(0.1f);
    private static readonly Color MenuHoverBackgroundColor = Color.FromHex("#8000FF").WithAlpha(0.5f);

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(CMKeyFunctions.CMXenoPing,
                InputCmdHandler.FromDelegate(_ => TogglePingMenu()))
            .Register<XenoPingUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<XenoPingUIController>();
        CloseMenu();
    }

    private void TogglePingMenu()
    {
        if (_playerManager.LocalEntity is not { } player)
        {
            return;
        }

        var hasXenoComp = _entityManager.HasComponent<XenoComponent>(player);
        var hasPingComp = _entityManager.HasComponent<XenoPingComponent>(player);

        if (!hasXenoComp || !hasPingComp)
        {
            return;
        }

        if (_menu != null)
        {
            CloseMenu();
            return;
        }

        _targetEntity = GetEntityUnderCursor(player);
        _targetCoordinates = GetTargetCoordinates(player);

        var models = ConvertToPingButtons(player);

        _menu = new ColoredSimpleRadialMenu();
        _menu.SetButtons(models, new SimpleRadialMenuSettings
        {
            DefaultContainerRadius = 80,
            UseSectors = true,
            DisplayBorders = true,
            NoBackground = false
        });
        _menu.OnClose += OnWindowClosed;

        _menu.Open();
        _menu.OpenOverMouseScreenPosition();
    }

    private EntityUid? GetEntityUnderCursor(EntityUid player)
    {
        var currentState = _stateManager.CurrentState;
        if (currentState is not GameplayStateBase screen)
            return null;

        EntityUid? entityToClick = null;

        if (_uiManager.CurrentlyHovered is IViewportControl vp && _inputManager.MouseScreenPosition.IsValid)
        {
            var mousePosWorld = vp.PixelToMap(_inputManager.MouseScreenPosition.Position);
            entityToClick = screen.GetClickedEntity(mousePosWorld);
        }

        if (entityToClick != null &&
            _entityManager.HasComponent<XenoComponent>(entityToClick.Value))
        {
            return entityToClick;
        }

        return null;
    }

    private EntityCoordinates GetTargetCoordinates(EntityUid player)
    {
        var transformSystem = _entityManager.System<SharedTransformSystem>();

        if (_targetEntity != null)
        {
            return transformSystem.GetMoverCoordinates(_targetEntity.Value);
        }
        else
        {
            var mouseCoords = _inputManager.MouseScreenPosition;
            var mapCoords = _eyeManager.PixelToMap(mouseCoords.Position);

            if (mapCoords.MapId == MapId.Nullspace)
            {
                return transformSystem.GetMoverCoordinates(player);
            }
            else if (_mapManager.TryFindGridAt(mapCoords, out var gridUid, out _))
            {
                return new EntityCoordinates(gridUid, transformSystem.ToCoordinates(gridUid, mapCoords).Position);
            }
            else if (_mapManager.MapExists(mapCoords.MapId))
            {
                return new EntityCoordinates(_mapManager.GetMapEntityId(mapCoords.MapId), mapCoords.Position);
            }
            else
            {
                return transformSystem.GetMoverCoordinates(player);
            }
        }
    }

    private IEnumerable<RadialMenuOption> ConvertToPingButtons(EntityUid player)
    {
        var clientPingSystem = _entityManager.System<XenoPingSystem>();
        var availablePings = clientPingSystem.GetAvailablePingTypesWithColors();

        var primaryActions = new List<RadialMenuOption>
        {
            CreatePingOption("XenoPingMove", availablePings["XenoPingMove"]),
            CreatePingOption("XenoPingDefend", availablePings["XenoPingDefend"]),
            CreatePingOption("XenoPingAttack", availablePings["XenoPingAttack"]),
            CreatePingOption("XenoPingRegroup", availablePings["XenoPingRegroup"])
        };

        var tacticalActions = new List<RadialMenuOption>
        {
            CreatePingOption("XenoPingDanger", availablePings["XenoPingDanger"]),
            CreatePingOption("XenoPingHold", availablePings["XenoPingHold"]),
            CreatePingOption("XenoPingAmbush", availablePings["XenoPingAmbush"]),
            CreatePingOption("XenoPingFortify", availablePings["XenoPingFortify"])
        };

        var supportActions = new List<RadialMenuOption>
        {
            CreatePingOption("XenoPingWeed", availablePings["XenoPingWeed"]),
            CreatePingOption("XenoPingNest", availablePings["XenoPingNest"]),
            CreatePingOption("XenoPingHosts", availablePings["XenoPingHosts"]),
            CreatePingOption("XenoPingAide", availablePings["XenoPingAide"])
        };

        var options = new List<RadialMenuOption>();
        options.AddRange(primaryActions);

        var tacticalNested = new RadialMenuNestedLayerOption(tacticalActions, 100)
        {
            ToolTip = "Tactical Commands",
            BackgroundColor = MenuBackgroundColor,
            HoverBackgroundColor = MenuHoverBackgroundColor,
            Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Markers/xeno_markers.rsi"), "fortify")
        };

        var supportNested = new RadialMenuNestedLayerOption(supportActions, 100)
        {
            ToolTip = "Support Commands",
            BackgroundColor = MenuBackgroundColor,
            HoverBackgroundColor = MenuHoverBackgroundColor,
            Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Markers/xeno_markers.rsi"), "weed")
        };

        options.Add(tacticalNested);
        options.Add(supportNested);

        return options;
    }

    private ColoredRadialMenuActionOption<string> CreatePingOption(string pingType, (string Name, Color Color, string Description) pingInfo)
    {
        var markerState = GetMarkerStateForPingType(pingType);
        var sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Markers/xeno_markers.rsi"), markerState);

        var tooltip = $"{pingInfo.Name}\n{pingInfo.Description}";
        if (_targetEntity != null)
        {
            var targetName = _entityManager.GetComponent<MetaDataComponent>(_targetEntity.Value).EntityName ?? "Unknown";
            tooltip += $"\nTarget: {targetName}";
        }
        else
        {
            tooltip += "\nLocation-based ping";
        }

        return new ColoredRadialMenuActionOption<string>(HandlePingSelection, pingType)
        {
            ToolTip = tooltip,
            BackgroundColor = MenuBackgroundColor,
            HoverBackgroundColor = MenuHoverBackgroundColor,
            Sprite = sprite,
            SpriteColor = pingInfo.Color
        };
    }

    private string GetMarkerStateForPingType(string pingType)
    {
        return pingType switch
        {
            "XenoPingMove" => "rally",
            "XenoPingDefend" => "defend",
            "XenoPingAttack" => "attack",
            "XenoPingRegroup" => "rally",
            "XenoPingDanger" => "danger",
            "XenoPingHold" => "hold",
            "XenoPingAmbush" => "ambush",
            "XenoPingFortify" => "fortify",
            "XenoPingWeed" => "weed",
            "XenoPingNest" => "nest",
            "XenoPingHosts" => "hosts",
            "XenoPingAide" => "aide",
            "XenoPingGeneral" => "no_direction",
            _ => "no_direction"
        };
    }

    private void HandlePingSelection(string pingType)
    {
        if (_targetCoordinates == null)
        {
            return;
        }

        var netCoords = _entityManager.GetNetCoordinates(_targetCoordinates.Value);
        var netTargetEntity = _targetEntity.HasValue ? _entityManager.GetNetEntity(_targetEntity.Value) : (NetEntity?)null;
        var message = new XenoPingRequestEvent(pingType, netCoords, netTargetEntity);
        _entityManager.RaisePredictiveEvent(message);

        CloseMenu();
    }

    private void OnWindowClosed()
    {
        CloseMenu();
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu = null;
        _targetCoordinates = null;
        _targetEntity = null;
    }
}
