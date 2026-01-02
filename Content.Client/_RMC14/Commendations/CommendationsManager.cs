using System.Linq;
using Content.Shared._RMC14.Commendations;
using Robust.Shared.Network;

namespace Content.Client._RMC14.Commendations;

public sealed class CommendationsManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;

    private CommendationsWindow? _receivedWindow;
    private CommendationsWindow? _givenWindow;

    private readonly List<Commendation> _commendationsReceived = new();
    private readonly List<Commendation> _commendationsGiven = new();

    public void PostInject()
    {
        _net.RegisterNetMessage<CommendationsMsg>(OnCommendations);
    }

    private void OnCommendations(CommendationsMsg message)
    {
        _commendationsReceived.Clear();
        _commendationsReceived.AddRange(message.CommendationsReceived.OrderByDescending(c => c.Round));

        _commendationsGiven.Clear();
        _commendationsGiven.AddRange(message.CommendationsGiven.OrderByDescending(c => c.Round));
    }

    private void OpenWindow(ref CommendationsWindow? window, Action onClose, List<Commendation> commendations)
    {
        if (window != null)
        {
            window.MoveToFront();
            return;
        }

        window = new CommendationsWindow();
        window.OnClose += onClose;
        window.OpenCentered();

        foreach (var commendation in commendations)
        {
            var container = new CommendationContainer();
            container.Title.Text = $"[bold]Round {commendation.Round} - {commendation.Name}[/bold]";
            container.Description.Text = $"Issued to [bold]{commendation.Receiver}[/bold] by [bold]{commendation.Giver}[/bold] for:\n{commendation.Text}";
            window.Commendations.AddChild(container);
        }
    }

    public void OpenReceivedWindow()
    {
        OpenWindow(ref _receivedWindow, () => _receivedWindow = null, _commendationsReceived);
    }

    public void OpenGivenWindow()
    {
        OpenWindow(ref _givenWindow, () => _givenWindow = null, _commendationsGiven);
    }
}
