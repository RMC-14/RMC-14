using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using JetBrains.Annotations;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.UserInterface.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Xenonids.Construction.Tunnel;
[UsedImplicitly]
public sealed partial class SelectDestinationTunnelBui : BoundUserInterface
{
    private SelectDestinationTunnelWindow? _window;
    private NetEntity? _selectedTunnel;
    private IEntityManager _entManager;
    public SelectDestinationTunnelBui(EntityUid ent, Enum key) : base(ent, key)
    {
        _entManager = IoCManager.Resolve<IEntityManager>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SelectDestinationTunnelInterfaceState)
        {
            return;
        }

        var newState = (SelectDestinationTunnelInterfaceState)state;

        _window!.SelectableTunnels.Clear();

        foreach (var (name, netTunnel) in newState.HiveTunnels)
        {
            var tunnel = _entManager.GetEntity(netTunnel);
            if (tunnel == this.Owner)
            {
                continue;
            }

            ItemList.Item item = new(_window.SelectableTunnels)
            {
                Text = name,
                Metadata = netTunnel
            };
            _window.SelectableTunnels.Add(item);
        }
    }

    protected override void Open()
    {
        base.Open();

        _window = new SelectDestinationTunnelWindow();
        _window.OnClose += Close;
        _window.OpenCentered();

        _window.SelectButton.Disabled = true;

        _window.SelectableTunnels.OnItemSelected += (ItemList.ItemListSelectedEventArgs args) =>
        {
            _window.SelectButton.Disabled = false;

            ItemList.Item newSelectedItem = args.ItemList[args.ItemIndex];
            _selectedTunnel = (NetEntity)newSelectedItem.Metadata!;
        };

        _window.SelectButton.OnButtonDown += (BaseButton.ButtonEventArgs args) =>
        {

            if (_selectedTunnel is null)
            {
                args.Button.Disabled = true;
                return;
            }

            GoToTunnel(_selectedTunnel.Value);
            Close();
        };
    }

    private void GoToTunnel(NetEntity destinationTunnel)
    {
        SendMessage(new TraverseXenoTunnelMessage(destinationTunnel));
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}
