using Content.Client._RMC14.UserInterface;
using Content.Client.Message;
using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Construction.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Construction;

[UsedImplicitly]
public sealed class RMCConstructionBui : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private RMCConstructionWindow? _window;

    public RMCConstructionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCConstructionWindow>();
        _window.Title = $"Construction using the {EntMan.GetComponent<MetaDataComponent>(Owner).EntityName}";

        if (!EntMan.TryGetComponent(Owner, out RMCConstructionItemComponent? constructionItem))
            return;

        if (constructionItem.Buildable is not { } entries)
            return;

        Refresh(entries);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (State is RMCConstructionBuiState s)
            RefreshStackAmount();
    }

    private void AddEntry(ProtoId<RMCConstructionPrototype> prototypeId)
    {
        if (!_prototype.TryIndex(prototypeId, out var build))
            return;

        if (build.IsDivider)
        {
            var divider = new BlueHorizontalSeparator();
            divider.Margin = new Thickness(1);

            _window?.ConstructionContainer.AddChild(divider);
            return;
        }

        if (build.Listed != null)
        {
            AddListButton(build);
            return;
        }

        var control = new RMCBuildChoiceControl();
        control.Set(build.Name);

        control.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCConstructionBuiMsg(build, 1));
        };

        _window?.ConstructionContainer.AddChild(control);
    }

    private void AddListButton(RMCConstructionPrototype build)
    {
        if (build.Listed is not { } listed)
            return;

        var control = new RMCBuildChoiceControl();
        control.Set(build.Name);

        control.Button.OnPressed += _ =>
        {
            _window?.ConstructionContainer.Children.Clear();
            Refresh(listed);
        };

        _window?.ConstructionContainer.AddChild(control);
    }

    public void Refresh(ProtoId<RMCConstructionPrototype>[] entries)
    {
        if (_window == null)
            return;

        RefreshStackAmount();

        foreach (var entry in entries)
        {
            AddEntry(entry);
        }
    }

    public void RefreshStackAmount()
    {
        if (_window == null)
            return;

        if (EntMan.TryGetComponent(Owner, out StackComponent? stack))
            _window.MaterialLabel.Text = $"Amount Left: {stack.Count}";
    }
}
