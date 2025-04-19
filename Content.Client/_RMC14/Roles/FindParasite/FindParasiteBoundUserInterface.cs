using Content.Client._RMC14.Xenonids.Projectiles.Parasite;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Roles.FindParasite;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Roles.FindParasite;

[UsedImplicitly]
public sealed partial class FindParasiteBoundUserInterface : BoundUserInterface
{
    private IEntityManager _entManager;
    private EntityUid _owner;

    private ItemList.Item? _selectedItem;

    // Deselecting directly via code still activates events,
    // prevent activation of function "OnItemDeselect" if "_impledDeselect" is true
    private bool _impledDeselect = false;

    private ItemList? _spawnerList;

    [ViewVariables]
    private FindParasiteWindow? _window;

    public FindParasiteBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
        _entManager = IoCManager.Resolve<IEntityManager>();
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not FindParasiteUIState ||
            _spawnerList is null)
        {
            return;
        }
        var uiState = (FindParasiteUIState)state;

        var activeParasiteSpawners = uiState.ActiveParasiteSpawners;
        _spawnerList.Clear();

        foreach (var spawnerData in activeParasiteSpawners)
        {
            var item = new ItemList.Item(_spawnerList);
            item.Text = spawnerData.Name;
            item.Metadata = spawnerData.Spawner;
            _spawnerList.Add(item);
        }
    }
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

        spawnButton.OnButtonDown += (BaseButton.ButtonEventArgs args) =>
        {
            if (_selectedItem is null)
            {
                args.Button.Disabled = true;
                return;
            }
            NetEntity selected = (NetEntity)_selectedItem.Metadata!;

            TakeParasiteRole(selected);
            Close();
        };

    }
    private void OnItemSelect(ItemList.ItemListSelectedEventArgs args)
    {
        _window!.SpawnButton.Disabled = false;

        ItemList.Item newSelectedItem = args.ItemList[args.ItemIndex];
        NetEntity newSelected = (NetEntity)newSelectedItem.Metadata!;


        if (_selectedItem is null)
        {
            FollowParasiteSpawner(newSelected);
            _selectedItem = newSelectedItem;
            return;
        }

        NetEntity originalSelected = (NetEntity)_selectedItem.Metadata!;

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
        NetEntity deselected = (NetEntity)args.ItemList[args.ItemIndex].Metadata!;

        if (_selectedItem is null)
        {
            return;
        }

        if (_impledDeselect)
        {
            _impledDeselect = false;
            return;
        }

        NetEntity originalSelected = (NetEntity)_selectedItem.Metadata!;

        if (deselected == originalSelected)
        {
            TakeParasiteRole(originalSelected);
            Close();
            return;
        }

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
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
