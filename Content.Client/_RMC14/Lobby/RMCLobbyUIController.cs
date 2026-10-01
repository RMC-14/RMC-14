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

        RefreshXenoWindow(ev.Larva);
    }

    public void OpenJoinXenoWindow()
    {
        var system = EntityManager.System<JoinXenoSystem>();
        RefreshXenoWindow(system.ClientBurrowedLarva);
    }

    public void OpenJoinFaxResponderWindow()
    {
        new JoinFaxResponderWindow().OpenCentered();
    }

    private void RefreshXenoWindow(int larva)
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

            _joinXenoWindow.OpenCentered();
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

        system.RequestBurrowedLarvaStatus();
    }
}
