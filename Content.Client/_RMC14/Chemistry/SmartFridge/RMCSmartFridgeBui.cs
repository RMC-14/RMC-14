using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chemistry.SmartFridge;

[UsedImplicitly]
public sealed class RMCSmartFridgeBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private readonly ContainerSystem _container;

    private readonly EntityQuery<RMCSmartFridgeInsertableComponent> _insertableQuery;
    private readonly EntityQuery<MetaDataComponent> _metaDataQuery;

    private RMCSmartFridgeWindow? _window;

    private readonly SortedDictionary<string, SortedDictionary<string, int>> _contents = new();
    private readonly Dictionary<string, EntityUid> _first = new();

    public RMCSmartFridgeBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
        _insertableQuery = EntMan.GetEntityQuery<RMCSmartFridgeInsertableComponent>();
        _metaDataQuery = EntMan.GetEntityQuery<MetaDataComponent>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCSmartFridgeWindow>();
        if (EntMan.TryGetComponent(Owner, out MetaDataComponent? metaData))
            _window.Title = metaData.EntityName;

        Refresh();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        var tabs = _window.ContentsTabs;
        if (!EntMan.TryGetComponent(Owner, out RMCSmartFridgeComponent? fridge) ||
            !_container.TryGetContainer(Owner, fridge.ContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            tabs.RemoveAllChildren();
            _window.ContentsEmptyLabel.Visible = true;
            tabs.Visible = false;
            return;
        }

        _window.ContentsEmptyLabel.Visible = false;
        tabs.Visible = true;

        foreach (var list in _contents.Values)
        {
            list.Clear();
        }

        _first.Clear();
        foreach (var contained in container.ContainedEntities)
        {
            if (!_insertableQuery.TryComp(contained, out var comp))
                continue;

            if (!_metaDataQuery.TryComp(contained, out var metaData))
                continue;

            var categoryName = comp.Category;
            if (_loc.TryGetString(comp.Category, out var categoryLoc))
                categoryName = categoryLoc;

            var name = metaData.EntityName;
            var category = _contents.GetOrNew(categoryName);
            category[name] = category.GetValueOrDefault(name) + 1;
            _first.TryAdd(name, contained);
        }

        var i = 0;
        foreach (var (category, contents) in _contents)
        {
            if (contents.Count == 0)
                continue;

            RMCSmartFridgeSection section;
            if (i < tabs.ChildCount)
            {
                section = (RMCSmartFridgeSection) tabs.GetChild(i);
            }
            else
            {
                section = new RMCSmartFridgeSection();
                tabs.AddChild(section);
            }

            TabContainer.SetTabTitle(section, category);
            TabContainer.SetTabVisible(section, true);

            var j = 0;
            foreach (var (name, amount) in contents)
            {
                if (!_first.TryGetValue(name, out var first))
                {
                    j++;
                    continue;
                }

                var netFirst = EntMan.GetNetEntity(first);
                RMCSmartFridgeRow row;
                if (j < section.Container.ChildCount)
                {
                    row = (RMCSmartFridgeRow) section.Container.GetChild(j);
                }
                else
                {
                    row = new RMCSmartFridgeRow();
                    section.Container.AddChild(row);
                }

                row.SpriteView.SetEntity(first);
                row.AmountLabel.Text = $"{amount}";
                row.NameButton.Text = name;
                row.NameButton.ClearOnPressed();
                row.NameButton.OnPressed += _ => SendPredictedMessage(new RMCSmartFridgeVendMsg(netFirst));

                if (EntMan.TryGetComponent(first, out MetaDataComponent? metaData))
                {
                    row.TooltipLabel.Visible = true;
                    var msg = new FormattedMessage();
                    msg.AddText(name);
                    msg.PushNewline();

                    if (!string.IsNullOrWhiteSpace(metaData.EntityDescription))
                        msg.AddText(metaData.EntityDescription);

                    var tooltip = new Tooltip();
                    tooltip.SetMessage(msg);

                    row.TooltipLabel.ToolTip = metaData.EntityDescription;
                    row.TooltipLabel.TooltipDelay = 0;
                    row.TooltipLabel.TooltipSupplier = _ => tooltip;
                }
                else
                {
                    row.TooltipLabel.Visible = false;
                }

                j++;
            }

            section.Container.RemoveChildrenAfter(j);
            i++;
        }

        tabs.SetTabVisibleAfter(i, false);
        tabs.SetVisibleAfter(i, false);
        var tabIndex = tabs.CurrentTab;
        if (tabIndex < tabs.ChildCount &&
            !tabs.GetChild(tabIndex).Visible)
        {
            tabs.CurrentTab = 0;
        }
    }
}
