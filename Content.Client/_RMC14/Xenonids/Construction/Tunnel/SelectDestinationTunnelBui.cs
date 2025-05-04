using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Xenonids.Construction.Tunnel;
[UsedImplicitly]
public sealed partial class SelectDestinationTunnelBui(EntityUid ent, Enum key) : BoundUserInterface(ent, key)
{
    private SelectDestinationTunnelWindow? _window;
    private NetEntity? _selectedTunnel;

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
            var tunnel = EntMan.GetEntity(netTunnel);
            if (tunnel == Owner)
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

        _window = this.CreateWindow<SelectDestinationTunnelWindow>();
        _window.SelectButton.Disabled = true;

        _window.SelectableTunnels.OnItemSelected += (args) =>
        {
            _window.SelectButton.Disabled = false;

            var newSelectedItem = args.ItemList[args.ItemIndex];
            _selectedTunnel = (NetEntity) newSelectedItem.Metadata!;
        };

        _window.SelectButton.OnButtonDown += (args) =>
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
}
