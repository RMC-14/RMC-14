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
    private NetEntity? _selected;

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

        if (!_entManager.TryGetComponent(_owner, out FindParasiteComponent? parasiteFindercomp) ||
            _spawnerList is null)
        {
            return;
        }
        var activeParasiteSpawners = parasiteFindercomp.ActiveParasiteSpawners;
        foreach (var spawner in activeParasiteSpawners)
        {
            var item = new ItemList.Item(_spawnerList);
            item.Text = spawner.Key;
            item.Metadata = spawner.Value;
            _spawnerList.Add(item);
        }
    }
    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<FindParasiteWindow>();

        _spawnerList = _window.ParasiteSpawners;
        var spawnButton = _window.SpawnButton;

        RefreshActiveParasiteSpawners();

        _spawnerList.OnItemSelected += (ItemList.ItemListSelectedEventArgs args) =>
        {
            spawnButton.Disabled = false;
            NetEntity newSelected = (NetEntity)args.ItemList[args.ItemIndex].Metadata!;
            if (newSelected == _selected)
            {
                TakeParasiteRole(_selected.Value);
                Close();
            }
            _selected = newSelected;
            FollowParasiteSpawner(_selected.Value);
        };

        spawnButton.Text = Loc.GetString("xeno-ui-find-parasite-spawn-button");
        spawnButton.Disabled = true;

        spawnButton.OnButtonDown += (BaseButton.ButtonEventArgs args) =>
        {
            if (_selected is null)
            {
                args.Button.Disabled = true;
                return;
            }
            TakeParasiteRole(_selected.Value);
        };

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }

    public void RefreshActiveParasiteSpawners()
    {
        var msg = new GetAllActiveParasiteSpawnersMessage();
        SendMessage(msg);
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
