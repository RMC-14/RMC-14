using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Ping;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Linq;

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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ColoredSimpleRadialMenu? _menu;
    private EntityCoordinates? _targetCoordinates;
    private EntityUid? _targetEntity;

    private static readonly ResPath XenoMarkersRsi = new("/Textures/_RMC14/Markers/xeno_markers.rsi");
    private static readonly SpriteSpecifier DefaultPingSprite = new SpriteSpecifier.Rsi(XenoMarkersRsi, "no_direction");
    private static readonly Color MenuBackgroundColor = Color.FromHex("#8000FF").WithAlpha(0.1f);
    private static readonly Color MenuHoverBackgroundColor = Color.FromHex("#8000FF").WithAlpha(0.5f);
    private static readonly string[] CategoryOrder = ["tactical", "construction"];
    private static readonly Dictionary<string, (string Name, SpriteSpecifier Icon)> CategoryInfo = new()
    {
        ["tactical"] = ("Tactical Commands", new SpriteSpecifier.Rsi(XenoMarkersRsi, "fortify")),
        ["construction"] = ("Construction", new SpriteSpecifier.Rsi(XenoMarkersRsi, "weed")),
    };

    private readonly record struct PingButtonData(
        string PingType,
        string Name,
        string Description,
        Color Color,
        int Priority,
        string? UiCategory,
        SpriteSpecifier Sprite);

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

    private bool TogglePingMenu()
    {
        if (_playerManager.LocalEntity is not { } player)
        {
            return false;
        }

        var hasXenoComp = _entityManager.HasComponent<XenoComponent>(player);
        var hasPingComp = _entityManager.HasComponent<XenoPingComponent>(player);

        if (!hasXenoComp || !hasPingComp)
        {
            return false;
        }

        if (_menu != null)
        {
            CloseMenu();
            return true;
        }

        _targetEntity = GetEntityUnderCursor();
        _targetCoordinates = GetTargetCoordinates(player);

        var models = ConvertToPingButtons();

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
        return true;
    }

    private EntityUid? GetEntityUnderCursor()
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
            _entityManager.HasComponent<XenoComponent>(entityToClick.Value) &&
            _playerManager.LocalEntity is { } player &&
            AreSameHive(player, entityToClick.Value))
        {
            return entityToClick;
        }

        return null;
    }

    private bool AreSameHive(EntityUid a, EntityUid b)
    {
        if (!_entityManager.TryGetComponent(a, out HiveMemberComponent? aHiveComp) ||
            !_entityManager.TryGetComponent(b, out HiveMemberComponent? bHiveComp))
        {
            return false;
        }

        return aHiveComp.Hive != null &&
               bHiveComp.Hive != null &&
               aHiveComp.Hive == bHiveComp.Hive;
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

    private IEnumerable<RadialMenuOption> ConvertToPingButtons()
    {
        var clientPingSystem = _entityManager.System<XenoPingSystem>();
        var availablePings = clientPingSystem.GetAvailablePingTypesWithColors();
        var pingButtons = BuildPingButtons(availablePings);
        var options = new List<RadialMenuOption>(pingButtons.Count);

        foreach (var ping in GetPrimaryPings(pingButtons))
        {
            options.Add(CreatePingOption(ping));
        }

        foreach (var category in CategoryOrder)
        {
            var categoryActions = pingButtons
                .Where(ping => ping.UiCategory == category)
                .Select(CreatePingOption)
                .Cast<RadialMenuOption>()
                .ToList();

            if (categoryActions.Count == 0)
                continue;

            options.Add(CreateNestedCategoryOption(category, categoryActions));
        }

        return options;
    }

    private List<PingButtonData> BuildPingButtons(
        Dictionary<string, (string Name, Color Color, string Description)> availablePings)
    {
        var buttons = new List<PingButtonData>(availablePings.Count);

        foreach (var (pingType, pingInfo) in availablePings)
        {
            if (!TryGetPingDataAndSprite(pingType, out var pingData, out var sprite))
                continue;

            var firstCategory = pingData.Categories.FirstOrDefault();
            var uiCategory = GetUiCategory(pingType, firstCategory);
            buttons.Add(new PingButtonData(
                PingType: pingType,
                Name: pingInfo.Name,
                Description: pingInfo.Description,
                Color: pingInfo.Color,
                Priority: pingData.Priority,
                UiCategory: uiCategory,
                Sprite: sprite
            ));
        }

        return buttons;
    }

    private bool IsPrimaryTactical(string pingType)
    {
        return pingType is "XenoPingMove" or "XenoPingDefend" or "XenoPingAttack" or "XenoPingRegroup";
    }

    private string? GetUiCategory(string pingType, string? prototypeCategory)
    {
        return prototypeCategory switch
        {
            "tactical" when IsPrimaryTactical(pingType) => "primary",
            "tactical" => "tactical",
            "construction" => "construction",
            "support" => "tactical",
            "hunting" => "tactical",
            "warning" => "primary",
            _ => null
        };
    }

    private IEnumerable<PingButtonData> GetPrimaryPings(List<PingButtonData> pingButtons)
    {
        return pingButtons
            .Where(ping => ping.Priority >= 85 || IsPrimaryTactical(ping.PingType))
            .OrderByDescending(ping => ping.Priority)
            .Take(6);
    }

    private RadialMenuNestedLayerOption CreateNestedCategoryOption(string category, List<RadialMenuOption> actions)
    {
        var (categoryName, categoryIcon) = GetCategoryInfo(category);

        return new RadialMenuNestedLayerOption(actions, 100)
        {
            ToolTip = categoryName,
            BackgroundColor = MenuBackgroundColor,
            HoverBackgroundColor = MenuHoverBackgroundColor,
            Sprite = categoryIcon
        };
    }

    private (string Name, SpriteSpecifier Icon) GetCategoryInfo(string category)
    {
        return CategoryInfo.TryGetValue(category, out var info)
            ? info
            : ("Commands", DefaultPingSprite);
    }

    private ColoredRadialMenuActionOption<string> CreatePingOption(PingButtonData ping)
    {
        var tooltip = BuildTooltip(ping);

        return new ColoredRadialMenuActionOption<string>(HandlePingSelection, ping.PingType)
        {
            ToolTip = tooltip,
            BackgroundColor = MenuBackgroundColor,
            HoverBackgroundColor = MenuHoverBackgroundColor,
            Sprite = ping.Sprite,
            SpriteColor = ping.Color
        };
    }

    private bool TryGetPingDataAndSprite(
        string entityId,
        out XenoPingDataComponent pingData,
        out SpriteSpecifier sprite)
    {
        sprite = DefaultPingSprite;
        pingData = default!;

        if (!_prototypeManager.TryIndex<EntityPrototype>(entityId, out var prototype))
            return false;

        if (!prototype.TryGetComponent<XenoPingDataComponent>(out var pingDataComponent, _entityManager.ComponentFactory) ||
            pingDataComponent == null)
            return false;

        pingData = pingDataComponent;

        if (prototype.TryGetComponent<SpriteComponent>(out var spriteComponent, _entityManager.ComponentFactory) &&
            TryGetSpriteSpecifier(spriteComponent, out var spriteSpecifier))
        {
            sprite = spriteSpecifier;
        }

        return true;
    }

    private static bool TryGetSpriteSpecifier(SpriteComponent spriteComponent, out SpriteSpecifier sprite)
    {
        sprite = DefaultPingSprite;

        if (spriteComponent.BaseRSI == null)
            return false;

        foreach (var layer in spriteComponent.AllLayers)
        {
            if (!layer.RsiState.IsValid)
                continue;

            var stateName = layer.RsiState.Name;
            if (string.IsNullOrWhiteSpace(stateName))
                continue;

            var rsi = layer.Rsi ?? spriteComponent.BaseRSI;
            sprite = new SpriteSpecifier.Rsi(new ResPath(rsi.Path.ToString()), stateName);
            return true;
        }

        return false;
    }

    private string BuildTooltip(PingButtonData ping)
    {
        var tooltip = $"{ping.Name}\n{ping.Description}";

        if (ping.Priority > 90)
        {
            tooltip += "\n[High Priority]";
        }

        if (_targetEntity != null)
        {
            var targetName = _entityManager.TryGetComponent(_targetEntity.Value, out MetaDataComponent? metaData)
                ? metaData.EntityName
                : "Unknown";
            tooltip += $"\nTarget: {targetName}";
        }
        else
        {
            tooltip += "\nLocation-based ping";
        }

        return tooltip;
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
        _targetCoordinates = null;
        _targetEntity = null;

        if (_menu == null)
            return;

        _menu.OnClose -= OnWindowClosed;
        _menu.Close();
        _menu.Dispose();
        _menu = null;
    }
}
