using Content.Shared._RMC14.Roles.FindParasite;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Roles.FindParasite;

[UsedImplicitly]
public sealed partial class FindParasiteBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private ItemList.Item? _selectedItem;

    // Deselecting directly via code still activates events,
    // prevent activation of function "OnItemDeselect" if "_impledDeselect" is true
    private bool _impledDeselect;

    private ItemList? _spawnerList;

    [ViewVariables]
    private FindParasiteWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<FindParasiteWindow>();

        _spawnerList = _window.ParasiteSpawners;
        var spawnButton = _window.SpawnButton;

        _spawnerList.OnItemSelected += OnItemSelect;

        _spawnerList.OnItemDeselected += OnItemDeselect;

        spawnButton.Text = Loc.GetString("xeno-ui-find-parasite-spawn-button");
        spawnButton.Disabled = true;

        spawnButton.OnButtonDown += args =>
        {
            if (_selectedItem is null)
            {
                args.Button.Disabled = true;
                return;
            }

            var selected = (NetEntity)_selectedItem.Metadata!;
            TakeParasiteRole(selected);
            Close();
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not FindParasiteUIState uiState ||
            _spawnerList is null)
        {
            return;
        }

        var activeParasiteSpawners = uiState.ActiveParasiteSpawners;
        _spawnerList.Clear();

        foreach (var spawnerData in activeParasiteSpawners)
        {
            var item = new ItemList.Item(_spawnerList)
            {
                Text = spawnerData.Name,
                Metadata = spawnerData.Spawner,
            };
            _spawnerList.Add(item);
        }
    }

    private void OnItemSelect(ItemList.ItemListSelectedEventArgs args)
    {
        _window!.SpawnButton.Disabled = false;

        var newSelectedItem = args.ItemList[args.ItemIndex];
        var newSelected = (NetEntity)newSelectedItem.Metadata!;


        if (_selectedItem is null)
        {
            FollowParasiteSpawner(newSelected);
            _selectedItem = newSelectedItem;
            return;
        }

        var originalSelected = (NetEntity)_selectedItem.Metadata!;

        if (newSelected == originalSelected)
        {
            TakeParasiteRole(originalSelected);
            Close();
            return;
        }
        else
        {
            _impledDeselect = true;
            _selectedItem.Selected = false;
        }
        _selectedItem = newSelectedItem;
        FollowParasiteSpawner(newSelected);
    }

    private void OnItemDeselect(ItemList.ItemListDeselectedEventArgs args)
    {
        var deselected = (NetEntity)args.ItemList[args.ItemIndex].Metadata!;

        if (_selectedItem is null)
        {
            return;
        }

        if (_impledDeselect)
        {
            _impledDeselect = false;
            return;
        }

        var originalSelected = (NetEntity)_selectedItem.Metadata!;

        if (deselected == originalSelected)
        {
            TakeParasiteRole(originalSelected);
            Close();
        }
    }

    public void FollowParasiteSpawner(NetEntity spawner)
    {
        SendMessage(new FollowParasiteSpawnerMessage(spawner));
    }

    public void TakeParasiteRole(NetEntity spawner)
    {
        SendMessage(new TakeParasiteRoleMessage(spawner));
    }
}
