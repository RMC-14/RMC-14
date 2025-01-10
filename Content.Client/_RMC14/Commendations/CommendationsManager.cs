using System.Linq;
using Content.Shared._RMC14.Commendations;
using Robust.Shared.Network;

namespace Content.Client._RMC14.Commendations;

public sealed class CommendationsManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;

    private CommendationsWindow? _window;
    private readonly List<Commendation> _commendations = new();

    public void PostInject()
    {
        _net.RegisterNetMessage<CommendationsMsg>(OnCommendations);
    }

    private void OnCommendations(CommendationsMsg message)
    {
        _commendations.Clear();
        _commendations.AddRange(message.Commendations.OrderByDescending(c => c.Round));
    }

    public void OpenWindow()
    {
        if (_window != null)
        {
            _window.MoveToFront();
            return;
        }

        _window = new CommendationsWindow();
        _window.OnClose += () => _window = null;
        _window.OpenCentered();

        foreach (var commendation in _commendations)
        {
            var container = new CommendationContainer();
            container.Title.Text = $"[bold]Round {commendation.Round} - {commendation.Name}[/bold]";
            container.Description.Text = $"Issued to [bold]{commendation.Receiver}[/bold] by [bold]{commendation.Giver}[/bold] for:\n{commendation.Text}";
            _window.Commendations.AddChild(container);
        }
    }
}
