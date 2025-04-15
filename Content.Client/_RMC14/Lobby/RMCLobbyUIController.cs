using Content.Shared._RMC14.Xenonids.JoinXeno;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._RMC14.Lobby;

public sealed class RMCLobbyUIController : UIController
{
    private JoinXenoWindow? _joinXenoWindow;

    public void OpenJoinXenoWindow()
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

        var larva = system.BurrowedLarva;
        if (larva == 0)
        {
            _joinXenoWindow.Label.Text = "No xenos are available.";
            _joinXenoWindow.Buttons.Visible = false;
        }
        else
        {
            _joinXenoWindow.Label.Text = "Burrowed larva available";
            _joinXenoWindow.Buttons.Visible = true;
        }

        _joinXenoWindow.OpenCentered();
    }
}
