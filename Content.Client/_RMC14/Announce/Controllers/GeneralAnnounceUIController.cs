using Content.Client.Gameplay;
using Content.Shared._RMC14.Announce;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client._RMC14.Announce;

public sealed class GeneralAnnounceUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [UISystemDependency] private readonly AudioSystem _audio = default!;

    private const int MaxQueuedAnnouncements = 32;
    private readonly List<QueuedAnnouncement> _queuedAnnouncements = new();
    private long _nextOrder;

    public void OnStateEntered(GameplayState state)
    {
        TryShowNextQueuedAnnouncement();
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
            EnqueueAnnouncement(announcement);
            return;
        }

        var widget = screen.GetOrAddWidget<AnnouncementWidget>();

        widget.OnAnnouncementFinished -= OnAnnouncementFinished;
        widget.OnAnnouncementFinished += OnAnnouncementFinished;

        if (widget.ActiveAnnouncement != null &&
            !CanInterrupt(widget.ActiveAnnouncement.Data, announcement))
        {
            EnqueueAnnouncement(announcement);
            return;
        }

        PlayAnnouncementSound(announcement);
        widget.ShowAnnouncement(announcement);
    }

    private void OnAnnouncementFinished()
    {
        TryShowNextQueuedAnnouncement();
    }

    private void TryShowNextQueuedAnnouncement()
    {
        if (!TryDequeueNext(out var next))
            return;
        ShowAnnouncement(next);
    }

    private void EnqueueAnnouncement(AnnouncementNetData announcement)
    {
        var queued = new QueuedAnnouncement(announcement, _nextOrder++);

        if (_queuedAnnouncements.Count >= MaxQueuedAnnouncements)
        {
            var lowestIndex = FindLowestPriorityIndex();
            if (lowestIndex < 0 || !HasHigherQueuePriority(queued, _queuedAnnouncements[lowestIndex]))
                return;

            _queuedAnnouncements.RemoveAt(lowestIndex);
        }

        _queuedAnnouncements.Add(queued);
    }

    private bool TryDequeueNext(out AnnouncementNetData announcement)
    {
        announcement = default!;
        if (_queuedAnnouncements.Count == 0)
            return false;

        var nextIndex = FindHighestPriorityIndex();
        if (nextIndex < 0)
            return false;

        announcement = _queuedAnnouncements[nextIndex].Data;
        _queuedAnnouncements.RemoveAt(nextIndex);
        return true;
    }

    private int FindHighestPriorityIndex()
    {
        if (_queuedAnnouncements.Count == 0)
            return -1;

        var bestIndex = 0;
        var best = _queuedAnnouncements[0];
        for (var i = 1; i < _queuedAnnouncements.Count; i++)
        {
            var current = _queuedAnnouncements[i];
            if (HasHigherQueuePriority(current, best))
            {
                best = current;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private int FindLowestPriorityIndex()
    {
        if (_queuedAnnouncements.Count == 0)
            return -1;

        var worstIndex = 0;
        var worst = _queuedAnnouncements[0];
        for (var i = 1; i < _queuedAnnouncements.Count; i++)
        {
            var current = _queuedAnnouncements[i];
            if (HasHigherQueuePriority(worst, current))
            {
                worst = current;
                worstIndex = i;
            }
        }

        return worstIndex;
    }

    private static bool HasHigherQueuePriority(QueuedAnnouncement incoming, QueuedAnnouncement current)
    {
        if (incoming.Data.Priority > current.Data.Priority)
            return true;

        if (incoming.Data.Priority < current.Data.Priority)
            return false;

        return incoming.Order < current.Order;
    }

    private static bool CanInterrupt(AnnouncementNetData current, AnnouncementNetData incoming)
    {
        if (!incoming.CanInterrupt)
            return false;

        if (current.CanBeInterrupted)
            return true;

        return incoming.Priority > current.Priority;
    }

    private void PlayAnnouncementSound(AnnouncementNetData announcement)
    {
        if (announcement.Sound == null)
            return;

        _audio.PlayGlobal(
            announcement.Sound,
            Filter.Local(),
            false,
            AudioParams.Default.WithVolume(announcement.SoundVolume));
    }

    private readonly record struct QueuedAnnouncement(AnnouncementNetData Data, long Order);
}
