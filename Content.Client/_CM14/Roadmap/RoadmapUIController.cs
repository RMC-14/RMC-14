using Content.Client.Lobby;
using Content.Client.UserInterface.Systems.Info;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._CM14.Roadmap;

public sealed class RoadmapUIController : UIController, IOnStateEntered<LobbyState>
{
    [Dependency] private readonly InfoUIController _infoUIController = default!;

    private RoadmapWindow? _window;
    private bool _shown;

    public override void Initialize()
    {
        base.Initialize();
        _infoUIController.Accepted += OnAccepted;
    }

    public void OnStateEntered(LobbyState state)
    {
        if (_shown || _window != null)
            return;

        if (_infoUIController.RulesPopup != null)
            return;

        ToggleRoadmap();
    }

    private void OnAccepted()
    {
        if (!_shown)
            ToggleRoadmap();
    }

    public void ToggleRoadmap()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        _shown = true;
        _window = new RoadmapWindow();
        _window.OnClose += () => _window = null;

        _window.OpenCentered();
    }
}
