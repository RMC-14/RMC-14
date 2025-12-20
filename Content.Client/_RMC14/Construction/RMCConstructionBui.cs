using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Message;
using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Construction.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Construction;

[UsedImplicitly]
public sealed class RMCConstructionBui : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [ViewVariables]
    private RMCConstructionWindow? _window;
    private RMCConstructionGhostSystem? _ghostSystem;
    private List<ProtoId<RMCConstructionPrototype>> _currentEntries = new();
    private string _searchText = string.Empty;

    public RMCConstructionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _ghostSystem = _entityManager.System<RMCConstructionGhostSystem>();
        _window = this.CreateWindow<RMCConstructionWindow>();
        _window.Title = $"Construction using the {EntMan.GetComponent<MetaDataComponent>(Owner).EntityName}";
        _window.ClearGhostsPressed += OnClearGhostsPressed;
        _window.SearchBar.OnTextChanged += SearchBarOnTextChanged;

        if (!EntMan.TryGetComponent(Owner, out RMCConstructionItemComponent? constructionItem))
            return;

        if (constructionItem.Buildable is not { } entries)
            return;

        Refresh(entries);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_window != null)
        {
            _window.ClearGhostsPressed -= OnClearGhostsPressed;
            _window.SearchBar.OnTextChanged -= SearchBarOnTextChanged;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (State is RMCConstructionBuiState s)
            RefreshStackAmount();
    }

    private void OnClearGhostsPressed()
    {
        _ghostSystem?.ClearAllGhosts();
    }

    private void AddEntry(ProtoId<RMCConstructionPrototype> prototypeId)
    {
        if (!_prototype.TryIndex(prototypeId, out var build))
            return;

        if (build.IsDivider)
        {
            if (!string.IsNullOrWhiteSpace(_searchText))
                return;

            var divider = new BlueHorizontalSeparator();
            divider.Margin = new Thickness(5);

            _window?.ConstructionContainer.AddChild(divider);
            return;
        }

        if (build.Listed != null)
        {
            AddListButton(build);
            return;
        }

        var nameString = Loc.GetString("rmc-construction-list", ("name", build.Name));

        if (build.MaterialCost != null)
            nameString = Loc.GetString("rmc-construction-entry", ("name", build.Name), ("amount", build.MaterialCost), ("material", Owner));

        var control = new RMCBuildChoiceControl();
        control.Set(nameString);
        control.SetPrototype(build.Prototype);

        if (build.StackAmounts is { } stackAmounts)
        {
            foreach (var stack in stackAmounts)
            {
                var button = new Button()
                {
                    Text = "x" + stack,
                    StyleClasses = { "OpenBoth" },
                    EnableAllKeybinds = true,
                    SetWidth = 45,
                    Margin = new Thickness(0, 0, 0, 3),
                    HorizontalAlignment = Control.HAlignment.Right
                };

                control.StackAmountContainer.AddChild(button);

                button.OnPressed += args =>
                {
                    if (args.Event.Function != EngineKeyFunctions.UIClick &&
                        args.Event.Function != EngineKeyFunctions.UIRightClick)
                    {
                        return;
                    }

                    var directBuild = args.Event.Function == EngineKeyFunctions.UIRightClick;
                    HandleConstruction(build, stack, directBuild);
                };

                control.Button.SetWidth = 250;
                control.Button.HorizontalAlignment = Control.HAlignment.Left;
            }
        }

        control.Button.OnPressed += args =>
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick &&
                args.Event.Function != EngineKeyFunctions.UIRightClick)
            {
                return;
            }

            var directBuild = args.Event.Function == EngineKeyFunctions.UIRightClick;
            HandleConstruction(build, build.Amount, directBuild);
        };

        _window?.ConstructionContainer.AddChild(control);
    }

    private void HandleConstruction(RMCConstructionPrototype prototype, int amount, bool directBuild)
    {
        if (prototype.Type == RMCConstructionType.Item)
        {
            _ghostSystem?.StopPlacement();
            SendMessage(new RMCConstructionBuiMsg(prototype.ID, amount));
        }
        else if (directBuild)
        {
            StartDirectBuild(prototype, amount);
        }
        else
        {
            StartGhostPlacement(prototype);
        }
    }

    private void StartGhostPlacement(RMCConstructionPrototype prototype)
    {
        if (_ghostSystem == null)
        {
            return;
        }

        _ghostSystem.StartPlacement(prototype, Owner);
    }

    private void StartDirectBuild(RMCConstructionPrototype prototype, int amount)
    {
        if (_ghostSystem == null)
            return;

        _ghostSystem.StopPlacement();
        _ghostSystem.TryBuildAtPlayer(prototype, Owner, amount);
    }

    private void AddListButton(RMCConstructionPrototype build)
    {
        if (build.Listed is not { } listed)
            return;

        var control = new RMCBuildChoiceControl();
        control.Set(build.Name);
        control.SetPrototype(build.Prototype);

        control.Button.OnPressed += args =>
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick &&
                args.Event.Function != EngineKeyFunctions.UIRightClick)
            {
                return;
            }

            if (args.Event.Function == EngineKeyFunctions.UIRightClick)
                return;

            _window?.ConstructionContainer.Children.Clear();
            Refresh(listed);
        };

        _window?.ConstructionContainer.AddChild(control);
    }

    public void Refresh(HashSet<ProtoId<RMCConstructionPrototype>> entries)
    {
        if (_window == null)
            return;

        _currentEntries = entries.ToList();
        RebuildEntries();
    }

    public void Refresh(ProtoId<RMCConstructionPrototype>[] entries)
    {
        if (_window == null)
            return;

        _currentEntries = entries.ToList();
        RebuildEntries();
    }

    public void RefreshStackAmount()
    {
        if (_window == null)
            return;

        if (EntMan.TryGetComponent(Owner, out StackComponent? stack))
            _window.MaterialLabel.Text = $"Amount Left: {stack.Count}";
    }

    private void OnSearchTextChanged(string text)
    {
        _searchText = text.Trim().ToLowerInvariant();
        RebuildEntries();
    }

    private void SearchBarOnTextChanged(LineEdit.LineEditEventArgs args)
    {
        OnSearchTextChanged(args.Text);
    }

    private void RebuildEntries()
    {
        if (_window == null)
            return;

        _window.ConstructionContainer.Children.Clear();
        RefreshStackAmount();

        foreach (var entry in _currentEntries)
        {
            if (!MatchesSearch(entry))
                continue;

            AddEntry(entry);
        }
    }

    private bool MatchesSearch(ProtoId<RMCConstructionPrototype> prototypeId)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        if (!_prototype.TryIndex(prototypeId, out var build))
            return false;

        return build.Name.ToLowerInvariant().Contains(_searchText);
    }
}
