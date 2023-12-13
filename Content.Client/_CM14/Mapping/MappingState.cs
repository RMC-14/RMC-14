using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._CM14.Input;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using static System.StringComparison;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client._CM14.Mapping;

public sealed class MappingState : GameplayStateBase
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly MappingManager _mapping = default!;
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly ISawmill _sawmill;
    private readonly GameplayStateLoadController _loadController;

    private bool _setup;
    private readonly List<MappingPrototype> _allPrototypes = new();
    private readonly List<MappingPrototype> _prototypes = new();
    private (TimeSpan At, MappingSpawnButton Button)? _lastClicked;
    private CursorState _state;

    private MappingScreen Screen => (MappingScreen) UserInterfaceManager.ActiveScreen!;
    private MainViewport Viewport => UserInterfaceManager.ActiveScreen!.GetWidget<MainViewport>()!;

    public MappingState()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("mapping");
        _loadController = UserInterfaceManager.GetUIController<GameplayStateLoadController>();
    }

    protected override void Startup()
    {
        EnsureSetup();
        base.Startup();

        UserInterfaceManager.LoadScreen<MappingScreen>();
        _loadController.LoadScreen();
        _input.Contexts.GetContext("common").AddFunction(CMKeyFunctions.SaveMap);

        Screen.Prototypes.SearchBar.OnTextChanged += OnSearch;
        Screen.Prototypes.ClearSearchButton.OnPressed += OnClearSearch;
        Screen.Prototypes.GetPrototypeData += OnGetData;
        Screen.Prototypes.SelectionChanged += OnSelected;
        Screen.Prototypes.CollapseToggled += OnCollapseToggled;
        Screen.Pick.OnPressed += OnPickPressed;
        _placement.PlacementChanged += OnPlacementChanged;

        CommandBinds.Builder
            .Bind(CMKeyFunctions.SaveMap, new PointerInputCmdHandler(HandleSaveMap, outsidePrediction: true))
            .Register<MappingState>();

        Screen.Prototypes.UpdateVisible(_prototypes);
    }

    protected override void Shutdown()
    {
        CommandBinds.Unregister<MappingState>();

        Screen.Prototypes.SearchBar.OnTextChanged -= OnSearch;
        Screen.Prototypes.GetPrototypeData -= OnGetData;
        Screen.Prototypes.SelectionChanged -= OnSelected;
        Screen.Prototypes.CollapseToggled -= OnCollapseToggled;
        Screen.Pick.OnPressed -= OnPickPressed;
        _placement.PlacementChanged -= OnPlacementChanged;

        UserInterfaceManager.ClearWindows();
        _loadController.UnloadScreen();
        UserInterfaceManager.UnloadScreen();
        _input.Contexts.GetContext("common").RemoveFunction(CMKeyFunctions.SaveMap);

        base.Shutdown();
    }

    private void EnsureSetup()
    {
        if (_setup)
            return;

        _setup = true;

        var entities = new MappingPrototype(null, "Entities") { Children = new List<MappingPrototype>() };
        _prototypes.Add(entities);

        var prototypes = new Dictionary<string, MappingPrototype>();
        foreach (var entity in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HideSpawnMenu)
                continue;

            if (!prototypes.TryGetValue(entity.ID, out var prototype))
            {
                var name = string.IsNullOrWhiteSpace(entity.Name) ? entity.ID : entity.Name;
                if (!string.IsNullOrWhiteSpace(entity.EditorSuffix))
                    name = $"{name} [{entity.EditorSuffix}]";

                prototype = new MappingPrototype(entity, name);
                prototypes.Add(entity.ID, prototype);
                _allPrototypes.Add(prototype);
            }

            if (entity.Parents == null)
            {
                entities.Children.Add(prototype);
                continue;
            }

            foreach (var parent in entity.Parents)
            {
                if (!prototypes.TryGetValue(parent, out var parentPrototype))
                {
                    string id;
                    string name;

                    if (_prototypeManager.TryIndex(parent, out EntityPrototype? parentEntity))
                    {
                        id = parentEntity.ID;
                        name = parentEntity.Name;
                    }
                    else
                    {
                        if (!_prototypeManager.TryGetMapping(typeof(EntityPrototype), parent, out var parentNode))
                        {
                            _sawmill.Error($"No {nameof(EntityPrototype)} found with id {parent}");
                            continue;
                        }

                        id = parentNode.Get<ValueDataNode>("id").Value;
                        name = parentNode.TryGet("name", out ValueDataNode? nameNode)
                            ? nameNode.Value
                            : id;
                    }

                    if (!string.IsNullOrWhiteSpace(entity.EditorSuffix))
                        name = $"{name} [{entity.EditorSuffix}]";

                    parentPrototype = new MappingPrototype(parentEntity, name);
                    prototypes.Add(id, parentPrototype);
                    _allPrototypes.Add(prototype);
                    entities.Children.Add(prototype);
                }

                parentPrototype.Children ??= new List<MappingPrototype>();
                parentPrototype.Children.Add(prototype);
            }
        }

        foreach (var prototype in prototypes.Values)
        {
            prototype.Children?.Sort(static (a, b) =>
            {
                var entA = (EntityPrototype?) a.Prototype;
                var entB = (EntityPrototype?) b.Prototype;
                return string.Compare(entA?.Name, entB?.Name, OrdinalIgnoreCase);
            });
        }

        entities.Children.Sort(static (a, b) => string.Compare(a.Name, b.Name, OrdinalIgnoreCase));

        prototypes.Clear();
        var tiles = new MappingPrototype(null, "Tiles") { Children = new List<MappingPrototype>() };
        _prototypes.Add(tiles);

        foreach (var tile in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
        {
            if (!prototypes.TryGetValue(tile.ID, out var prototype))
            {
                var name = string.IsNullOrWhiteSpace(tile.Name) ? tile.ID : Loc.GetString(tile.Name);
                prototype = new MappingPrototype(tile, name);
                prototypes.Add(tile.ID, prototype);
                _allPrototypes.Add(prototype);
            }

            if (tile.Parents == null)
            {
                tiles.Children.Add(prototype);
                continue;
            }

            foreach (var parent in tile.Parents)
            {
                if (!prototypes.TryGetValue(parent, out var parentPrototype))
                {
                    var parentEntity = _prototypeManager.Index<ContentTileDefinition>(parent);
                    parentPrototype = new MappingPrototype(parentEntity, parentEntity.Name);
                    prototypes.Add(parentEntity.ID, parentPrototype);
                    _allPrototypes.Add(prototype);
                }

                parentPrototype.Children ??= new List<MappingPrototype>();
                parentPrototype.Children.Add(prototype);
            }
        }

        foreach (var prototype in prototypes.Values)
        {
            prototype.Children?.Sort(static (a, b) =>
            {
                var tileA = (ContentTileDefinition?) a.Prototype;
                var tileB = (ContentTileDefinition?) b.Prototype;
                return string.Compare(tileA?.Name, tileB?.Name, OrdinalIgnoreCase);
            });
        }
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        Deselect();
    }

    protected override void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
    {
        if (args.Viewport == null)
            base.OnKeyBindStateChanged(new ViewportBoundKeyEventArgs(args.KeyEventArgs, Viewport.Viewport));
        else
            base.OnKeyBindStateChanged(args);
    }

    private void OnSearch(LineEditEventArgs args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            Screen.Prototypes.PrototypeList.Visible = true;
            Screen.Prototypes.SearchList.Visible = false;
            return;
        }

        var matches = new List<MappingPrototype>();
        foreach (var prototype in _allPrototypes)
        {
            if (prototype.Name.Contains(args.Text, OrdinalIgnoreCase))
                matches.Add(prototype);
        }

        matches.Sort(static (a, b) => string.Compare(a.Name, b.Name, OrdinalIgnoreCase));

        Screen.Prototypes.PrototypeList.Visible = false;
        Screen.Prototypes.SearchList.Visible = true;
        Screen.Prototypes.Search(matches);
    }

    private void OnClearSearch(ButtonEventArgs obj)
    {
        Screen.Prototypes.SearchBar.Text = string.Empty;
        OnSearch(new LineEditEventArgs(Screen.Prototypes.SearchBar, string.Empty));
    }

    private void OnGetData(IPrototype prototype, List<Texture> textures)
    {
        switch (prototype)
        {
            case EntityPrototype entity:
                textures.AddRange(SpriteComponent.GetPrototypeTextures(entity, _resources).Select(t => t.Default));

                break;
            case ContentTileDefinition tile:
                if (tile.Sprite?.ToString() is { } sprite)
                    textures.Add(_resources.GetResource<TextureResource>(sprite).Texture);

                break;
        }
    }

    private void OnSelected(MappingSpawnButton button, IPrototype? prototype)
    {
        var time = _timing.CurTime;
        if (_lastClicked is { } lastClicked &&
            lastClicked.Button == button &&
            lastClicked.At > time - TimeSpan.FromSeconds(0.333) &&
            string.IsNullOrEmpty(Screen.Prototypes.SearchBar.Text))
        {
            button.CollapseButton.Pressed = !button.CollapseButton.Pressed;
            ToggleCollapse(button);
            button.Button.Pressed = true;
            Screen.Prototypes.Selected = button;
            _lastClicked = (time, button);
            return;
        }

        _lastClicked = (time, button);

        if (button.Prototype == null)
            return;

        if (Screen.Prototypes.Selected is { } oldButton &&
            oldButton != button)
        {
            Deselect();
        }

        if (prototype == null)
            return;

        var placement = new PlacementInformation();

        if (prototype is ContentTileDefinition tile)
        {
            placement.PlacementOption = "AlignTileAny";
            placement.TileType = tile.TileId;
            placement.IsTile = true;
        }
        else
        {
            placement.PlacementOption = ((EntityPrototype) prototype).PlacementMode;
            placement.EntityType = prototype.ID;
            placement.IsTile = false;
        }

        _placement.BeginPlacing(placement);

        Screen.Prototypes.Selected = button;
        button.Button.Pressed = true;
    }

    private void Deselect()
    {
        if (Screen.Prototypes.Selected is { } selected)
        {
            selected.Button.Pressed = false;
            Screen.Prototypes.Selected = null;
        }
    }

    private void OnCollapseToggled(MappingSpawnButton button, ButtonToggledEventArgs args)
    {
        ToggleCollapse(button);
    }

    private void OnPickPressed(ButtonEventArgs args)
    {
        _state = CursorState.Pick;
    }

    private bool HandleSaveMap(in PointerInputCmdArgs args)
    {
#if FULL_RELEASE
        return false;
#endif
        if (!_admin.IsAdmin(true) || !_admin.HasFlag(AdminFlags.Host))
            return false;

        SaveMap();
        return true;
    }

    private async void SaveMap()
    {
        await _mapping.SaveMap();
    }

    private void ToggleCollapse(MappingSpawnButton button)
    {
        if (button.CollapseButton.Pressed)
        {
            if (button.Prototype?.Children != null)
            {
                foreach (var child in button.Prototype.Children)
                {
                    Screen.Prototypes.Insert(button.ChildrenPrototypes, child, true);
                }
            }

            button.CollapseButton.Label.Text = "▼";
        }
        else
        {
            button.ChildrenPrototypes.DisposeAllChildren();
            button.CollapseButton.Label.Text = "▶";
        }
    }

    private enum CursorState
    {
        Pick
    }
}
