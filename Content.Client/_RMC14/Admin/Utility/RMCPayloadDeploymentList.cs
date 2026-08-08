using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Admin.Utility;

public sealed class RMCPayloadDeploymentList : ListContainer
{
    private readonly IEntityManager _entities;
    private readonly List<PayloadListData> _items = [];
    private readonly HashSet<int> _selected = [];
    private ItemList.ItemListSelectMode _selectMode = ItemList.ItemListSelectMode.Single;
    private int _nextId;

    public event Action? SelectionChanged;

    public bool AllSelected => _items.Count > 0 && _selected.Count == _items.Count;

    public ItemList.ItemListSelectMode SelectMode
    {
        get => _selectMode;
        set
        {
            _selectMode = value;
            Group = value == ItemList.ItemListSelectMode.Single;
        }
    }

    public RMCPayloadDeploymentList()
    {
        IoCManager.Resolve(ref _entities);
        Group = true;
        GenerateItem = GenerateListItem;
        ItemPressed = OnItemPressed;
    }

    public void SetItems(IEnumerable<RMCPayloadDeploymentListEntry> entries)
    {
        _items.Clear();
        _selected.Clear();
        foreach (var entry in entries)
        {
            _items.Add(new PayloadListData(_nextId++, entry));
        }

        PopulateList(_items);
        SelectionChanged?.Invoke();
    }

    public IEnumerable<RMCPayloadDeploymentListEntry> GetSelected()
    {
        foreach (var item in _items)
        {
            if (_selected.Contains(item.Id))
                yield return item.Entry;
        }
    }

    public void ToggleAll()
    {
        if (AllSelected)
        {
            _selected.Clear();
        }
        else
        {
            foreach (var item in _items)
            {
                _selected.Add(item.Id);
            }
        }

        PopulateList(_items);
        SelectionChanged?.Invoke();
    }

    private void GenerateListItem(ListData listData, ListContainerButton button)
    {
        if (listData is not PayloadListData data)
            return;

        button.ToggleMode = true;
        button.HorizontalExpand = true;
        button.Pressed = _selected.Contains(data.Id);
        button.ToolTip = data.Entry.Tooltip;

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };

        if (data.Entry.Entity is { } entity)
        {
            row.AddChild(new SpriteView(entity, _entities)
            {
                OverrideDirection = Direction.South,
                SetSize = new Vector2(32, 32),
                Stretch = SpriteView.StretchMode.Fill,
                VerticalAlignment = VAlignment.Center,
            });
        }
        else if (data.Entry.Prototype is { } prototype)
        {
            row.AddChild(new EntityPrototypeView(prototype, _entities)
            {
                OverrideDirection = Direction.South,
                SetSize = new Vector2(32, 32),
                Stretch = SpriteView.StretchMode.Fill,
                VerticalAlignment = VAlignment.Center,
            });
        }

        row.AddChild(new Label
        {
            Text = data.Entry.Text,
            ClipText = true,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        });
        button.AddChild(row);
    }

    private void OnItemPressed(BaseButton.ButtonEventArgs args, ListData listData)
    {
        if (listData is not PayloadListData selected)
            return;

        if (SelectMode == ItemList.ItemListSelectMode.Single && args.Button.Pressed)
            _selected.Clear();

        if (args.Button.Pressed)
            _selected.Add(selected.Id);
        else
            _selected.Remove(selected.Id);

        SelectionChanged?.Invoke();
    }

    private sealed record PayloadListData(int Id, RMCPayloadDeploymentListEntry Entry) : ListData;
}

public sealed record RMCPayloadDeploymentListEntry(
    string Text,
    object Metadata,
    string? Tooltip = null,
    NetEntity? Entity = null,
    EntProtoId? Prototype = null);
