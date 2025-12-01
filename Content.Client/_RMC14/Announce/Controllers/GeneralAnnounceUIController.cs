using Content.Client.Gameplay;
using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._RMC14.Announce;

public sealed class GeneralAnnounceUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private readonly Queue<AnnouncementNetData> _queuedAnnouncements = new();

    public void OnStateEntered(GameplayState state)
    {
    }

    public void OnStateExited(GameplayState state)
    {
        _queuedAnnouncements.Clear();

        var screen = UIManager.ActiveScreen;
        if (screen != null)
        {
            var existingWidget = screen.GetWidget<AnnouncementWidget>();
            if (existingWidget != null)
            {
                existingWidget.OnAnnouncementFinished -= OnAnnouncementFinished;
                existingWidget.Visible = false;
            }
        }
    }

    public void ShowAnnouncement(AnnouncementNetData announcement)
    {
        var screen = UIManager.ActiveScreen;
        if (screen == null)
        {
            _queuedAnnouncements.Enqueue(announcement);
            return;
        }

        var widget = screen.GetOrAddWidget<AnnouncementWidget>();

        widget.OnAnnouncementFinished -= OnAnnouncementFinished;
        widget.OnAnnouncementFinished += OnAnnouncementFinished;

        if (widget.ActiveAnnouncement != null &&
            widget.ActiveAnnouncement.Data.CanBeInterrupted == false &&
            announcement.Priority <= widget.ActiveAnnouncement.Data.Priority)
        {
            _queuedAnnouncements.Enqueue(announcement);
            return;
        }

        widget.ShowAnnouncement(announcement);
    }

    private void OnAnnouncementFinished()
    {
        if (_queuedAnnouncements.Count > 0)
        {
            var next = _queuedAnnouncements.Dequeue();
            ShowAnnouncement(next);
        }
    }
}
