using System.Linq;
using System.Numerics;
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
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Sequence;
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
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly MappingManager _mapping = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly ISawmill _sawmill;
    private readonly GameplayStateLoadController _loadController;
    private bool _setup;
    private readonly MappingPrototype _entities = new(null, "Entities");
    private readonly List<MappingPrototype> _allPrototypes = new();
    private readonly Dictionary<IPrototype, MappingPrototype> _allPrototypesDict = new();
    private readonly Dictionary<string, MappingPrototype> _entityIdDict = new();
    private readonly List<MappingPrototype> _prototypes = new();
    private (TimeSpan At, MappingSpawnButton Button)? _lastClicked;
    private Control? _scrollTo;

    private MappingScreen Screen => (MappingScreen) UserInterfaceManager.ActiveScreen!;
    private MainViewport Viewport => UserInterfaceManager.ActiveScreen!.GetWidget<MainViewport>()!;

    public CursorState State { get; set; }

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
        _input.Contexts.GetContext("common").AddFunction(CMKeyFunctions.MappingEnablePick);
        _input.Contexts.GetContext("common").AddFunction(CMKeyFunctions.MappingPick);

        Screen.Prototypes.SearchBar.OnTextChanged += OnSearch;
        Screen.Prototypes.ClearSearchButton.OnPressed += OnClearSearch;
        Screen.Prototypes.GetPrototypeData += OnGetData;
        Screen.Prototypes.SelectionChanged += OnSelected;
        Screen.Prototypes.CollapseToggled += OnCollapseToggled;
        Screen.Pick.OnPressed += OnPickPressed;
        _placement.PlacementChanged += OnPlacementChanged;

        CommandBinds.Builder
            .Bind(CMKeyFunctions.SaveMap, new PointerInputCmdHandler(HandleSaveMap, outsidePrediction: true))
            .Bind(CMKeyFunctions.MappingEnablePick, new PointerStateInputCmdHandler(HandleEnablePick, HandleDisablePick, outsidePrediction: true))
            .Bind(CMKeyFunctions.MappingPick, new PointerInputCmdHandler(HandlePick, outsidePrediction: true))
            .Register<MappingState>();

        _overlays.AddOverlay(new MappingOverlay(this));

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

        _overlays.RemoveOverlay<MappingOverlay>();

        base.Shutdown();
    }

    private void EnsureSetup()
    {
        if (_setup)
            return;

        _setup = true;

        _entities.Children ??= new List<MappingPrototype>();
        _prototypes.Add(_entities);

        var mappings = new Dictionary<string, MappingPrototype>();
        foreach (var entity in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            RegisterEntity(entity, entity.ID);
        }

        static int Compare(MappingPrototype a, MappingPrototype b)
        {
            return string.Compare(a.Name, b.Name, OrdinalIgnoreCase);
        }

        foreach (var prototype in mappings.Values)
        {
            if (prototype.Parents == null && prototype != _entities)
            {
                prototype.Parents = new List<MappingPrototype> { _entities };
                _entities.Children.Add(prototype);
            }

            prototype.Parents?.Sort(Compare);
            prototype.Children?.Sort(Compare);
        }

        _entities.Children.Sort(Compare);

        mappings.Clear();
        var tiles = new MappingPrototype(null, "Tiles") { Children = new List<MappingPrototype>() };
        _prototypes.Add(tiles);

        foreach (var tile in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
        {
            if (!mappings.TryGetValue(tile.ID, out var prototype))
            {
                var name = string.IsNullOrWhiteSpace(tile.Name) ? tile.ID : Loc.GetString(tile.Name);
                prototype = new MappingPrototype(tile, name);
                mappings.Add(tile.ID, prototype);
                _allPrototypes.Add(prototype);
                _allPrototypesDict.Add(tile, prototype);
            }

            if (tile.Parents == null)
            {
                tiles.Children.Add(prototype);
                prototype.Parents ??= new List<MappingPrototype>();
                prototype.Parents.Add(tiles);
                continue;
            }

            foreach (var parent in tile.Parents)
            {
                if (!mappings.TryGetValue(parent, out var parentPrototype))
                {
                    var parentTile = _prototypeManager.Index<ContentTileDefinition>(parent);
                    parentPrototype = new MappingPrototype(parentTile, parentTile.Name);
                    mappings.Add(parentTile.ID, parentPrototype);
                    _allPrototypes.Add(parentPrototype);
                    _allPrototypesDict.Add(parentTile, parentPrototype);
                }

                parentPrototype.Children ??= new List<MappingPrototype>();
                parentPrototype.Children.Add(prototype);
                prototype.Parents ??= new List<MappingPrototype>();
                prototype.Parents.Add(parentPrototype);
            }
        }

        foreach (var prototype in mappings.Values)
        {
            if (prototype.Parents == null && prototype != tiles)
            {
                prototype.Parents = new List<MappingPrototype> { tiles };
                tiles.Children.Add(prototype);
            }

            prototype.Children?.Sort(Compare);
        }
    }

    private MappingPrototype? RegisterEntity(EntityPrototype? prototype, string id)
    {
        if (prototype == null && _prototypeManager.TryIndex(id, out prototype))
        {
            if (prototype.HideSpawnMenu || prototype.Abstract)
                prototype = null;
        }

        if (prototype == null)
        {
            if (!_prototypeManager.TryGetMapping(typeof(EntityPrototype), id, out var node))
            {
                _sawmill.Error($"No {nameof(EntityPrototype)} found with id {id}");
                return null;
            }

            if (_entityIdDict.TryGetValue(id, out var mapping))
            {
                return mapping;
            }
            else
            {
                var name = node.TryGet("name", out ValueDataNode? nameNode)
                    ? nameNode.Value
                    : id;

                if (node.TryGet("suffix", out ValueDataNode? suffix))
                    name = $"{name} [{suffix.Value}]";

                mapping = new MappingPrototype(prototype, name);
                _allPrototypes.Add(mapping);
                _entityIdDict.Add(id, mapping);

                if (node.TryGet("parent", out ValueDataNode? parentValue))
                {
                    var parent = RegisterEntity(null, parentValue.Value);

                    if (parent != null)
                    {
                        mapping.Parents ??= new List<MappingPrototype>();
                        mapping.Parents.Add(parent);
                        parent.Children ??= new List<MappingPrototype>();
                        parent.Children.Add(mapping);
                    }
                }
                else if (node.TryGet("parent", out SequenceDataNode? parentSequence))
                {
                    foreach (var parentNode in parentSequence.Cast<ValueDataNode>())
                    {
                        var parent = RegisterEntity(null, parentNode.Value);

                        if (parent != null)
                        {
                            mapping.Parents ??= new List<MappingPrototype>();
                            mapping.Parents.Add(parent);
                            parent.Children ??= new List<MappingPrototype>();
                            parent.Children.Add(mapping);
                        }
                    }
                }
                else
                {
                    _entities.Children ??= new List<MappingPrototype>();
                    _entities.Children.Add(mapping);
                    mapping.Parents ??= new List<MappingPrototype>();
                    mapping.Parents.Add(_entities);
                }

                return mapping;
            }
        }
        else
        {
            if (_entityIdDict.TryGetValue(id, out var mapping))
            {
                return mapping;
            }
            else
            {
                var name = prototype.Name;

                if (!string.IsNullOrWhiteSpace(prototype.EditorSuffix))
                    name = $"{name} [{prototype.EditorSuffix}]";

                mapping = new MappingPrototype(prototype, name);
                _allPrototypes.Add(mapping);
                _allPrototypesDict.Add(prototype, mapping);
                _entityIdDict.Add(prototype.ID, mapping);
            }

            if (prototype.Parents == null)
            {
                _entities.Children ??= new List<MappingPrototype>();
                _entities.Children.Add(mapping);
                mapping.Parents ??= new List<MappingPrototype>();
                mapping.Parents.Add(_entities);
                return mapping;
            }

            foreach (var parentId in prototype.Parents)
            {
                var parent = RegisterEntity(null, parentId);

                if (parent != null)
                {
                    mapping.Parents ??= new List<MappingPrototype>();
                    mapping.Parents.Add(parent);
                    parent.Children ??= new List<MappingPrototype>();
                    parent.Children.Add(mapping);
                }
            }

            return mapping;
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

    private void OnSelected(MappingPrototype mapping)
    {
        if (mapping.Prototype == null)
            return;

        var chain = new Stack<MappingPrototype>();
        chain.Push(mapping);

        var parent = mapping.Parents?.FirstOrDefault();
        while (parent != null)
        {
            chain.Push(parent);
            parent = parent.Parents?.FirstOrDefault();
        }

        _lastClicked = null;

        Control? last = null;
        var children = Screen.Prototypes.PrototypeList.Children;
        foreach (var prototype in chain)
        {
            foreach (var child in children)
            {
                if (child is MappingSpawnButton button &&
                    button.Prototype == prototype)
                {
                    UnCollapse(button);
                    OnSelected(button, prototype.Prototype);
                    children = button.ChildrenPrototypes.Children;
                    last = child;
                    break;
                }
            }
        }

        if (last != null && Screen.Prototypes.PrototypeList.Visible)
            _scrollTo = last;
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
        EnablePick();
    }

    private void EnablePick()
    {
        Screen.UnPressActionsExcept(Screen.Pick);
        State = CursorState.Pick;
    }

    private void DisablePick()
    {
        Screen.Pick.Pressed = false;
        State = CursorState.None;
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

    private bool HandleEnablePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        EnablePick();
        return true;
    }

    private bool HandleDisablePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        DisablePick();
        return true;
    }

    private bool HandlePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (State != CursorState.Pick)
            return false;

        if (uid == EntityUid.Invalid ||
            _entityManager.GetComponentOrNull<MetaDataComponent>(uid) is not { EntityPrototype: { } prototype } ||
            !_allPrototypesDict.TryGetValue(prototype, out var button))
        {
            // we always block other input handlers if pick mode is enabled
            // this makes you not accidentally place something in space because you
            // miss-clicked while holding down the pick hotkey
            return true;
        }

        OnSelected(button);
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

    private void UnCollapse(MappingSpawnButton button)
    {
        if (button.CollapseButton.Pressed)
            return;

        button.CollapseButton.Pressed = true;
        ToggleCollapse(button);
    }

    public override void FrameUpdate(FrameEventArgs e)
    {
        if (_scrollTo is not { } scrollTo)
            return;

        // this is not ideal but we wait until the control's height is computed to use
        // its position to scroll to
        if (scrollTo.Height > 0 && Screen.Prototypes.PrototypeList.Visible)
        {
            var y = scrollTo.GlobalPosition.Y - Screen.Prototypes.ScrollContainer.Height / 2 + scrollTo.Height;
            var scroll = Screen.Prototypes.ScrollContainer;
            scroll.SetScrollValue(scroll.GetScrollValue() + new Vector2(0, y));
            _scrollTo = null;
        }
    }

    public enum CursorState
    {
        None,
        Pick
    }
}
