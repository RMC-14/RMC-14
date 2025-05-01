using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._RMC14.Lobby;

public sealed class RMCLobbyUIController : UIController
{
    private JoinXenoWindow? _joinXenoWindow;

    public override void Initialize()
    {
        SubscribeLocalEvent<BurrowedLarvaChangedEvent>(OnBurrowedLarvaChanged);
    }

    private void OnBurrowedLarvaChanged(ref BurrowedLarvaChangedEvent ev)
    {
        if (_joinXenoWindow is not { IsOpen: true })
            return;

        RefreshWindow(ev.Larva);
    }

    public void OpenJoinXenoWindow()
    {
        var system = EntityManager.System<JoinXenoSystem>();
        RefreshWindow(system.ClientBurrowedLarva);
    }

    private void RefreshWindow(int larva)
    {
        var system = EntityManager.System<JoinXenoSystem>();
        if (_joinXenoWindow == null || _joinXenoWindow.Disposed)
        {
            _joinXenoWindow = new JoinXenoWindow();
            _joinXenoWindow.OnClose += () => _joinXenoWindow = null;
            _joinXenoWindow.LarvaButton.OnPressed += _ =>
            {
                system.ClientJoinLarva();
                _joinXenoWindow.Close();
            };
        }

        if (larva == 0)
        {
            _joinXenoWindow.Label.Text = Loc.GetString("rmc-lobby-no-burrowed-larva");
            _joinXenoWindow.Buttons.Visible = false;
        }
        else
        {
            _joinXenoWindow.Label.Text = Loc.GetString("rmc-lobby-burrowed-larva-available");
            _joinXenoWindow.Buttons.Visible = true;
        }

        _joinXenoWindow.OpenCentered();
        system.RequestBurrowedLarvaStatus();
    }
}
