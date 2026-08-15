using Content.Client.Gameplay;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementOverlayUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public const int MinVisibleAnnouncements = 1;
    public const int MaxVisibleAnnouncements = 4;

    private const int MaxQueuedAnnouncements = 32;

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly List<ActiveAnnouncement> _activeAnnouncements = new();
    private readonly List<QueuedAnnouncement> _queuedAnnouncements = new();
    private long _nextOrder;
    private int _maxVisibleAnnouncements = 2;

    public event Action<uint>? AnnouncementDone;

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementMaxVisible, OnMaxVisibleAnnouncementsChanged, true);
    }

    public void OnStateEntered(GameplayState state)
    {
        FillAvailableSlots();
    }

    public void OnStateExited(GameplayState state)
    {
        foreach (var queued in _queuedAnnouncements)
        {
            NotifyAnnouncementDone(queued.Data);
        }

        _queuedAnnouncements.Clear();

        for (var i = _activeAnnouncements.Count - 1; i >= 0; i--)
        {
            RemoveActiveAt(i, notifyDone: true, cancelPlayback: true);
        }

        GetOverlay(create: false)?.ClearAnnouncements();
    }

    public void ShowAnnouncement(AnnouncementDisplayData announcement)
    {
        var queued = new QueuedAnnouncement(announcement, _nextOrder++);
        if (GetOverlay(create: true) == null)
        {
            EnqueueAnnouncement(queued);
            return;
        }

        if (_activeAnnouncements.Count < _maxVisibleAnnouncements)
        {
            ShowActive(queued.Data);
            return;
        }

        var replacementIndex = FindOldestLowerPriorityActive(announcement.Priority);
        if (replacementIndex >= 0)
        {
            RemoveActiveAt(replacementIndex, notifyDone: true, cancelPlayback: true);
            ShowActive(queued.Data);
            return;
        }

        EnqueueAnnouncement(queued);
    }

    private void OnMaxVisibleAnnouncementsChanged(int value)
    {
        var clamped = Math.Clamp(value, MinVisibleAnnouncements, MaxVisibleAnnouncements);
        if (value != clamped)
        {
            _cfg.SetCVar(RMCCVars.RMCAnnouncementMaxVisible, clamped);
            return;
        }

        var previous = _maxVisibleAnnouncements;
        _maxVisibleAnnouncements = clamped;

        if (_activeAnnouncements.Count > _maxVisibleAnnouncements)
            TrimActiveAnnouncements();
        else if (_maxVisibleAnnouncements > previous)
            FillAvailableSlots();

        GetOverlay(create: false)?.Reflow();
    }

    private void TrimActiveAnnouncements()
    {
        while (_activeAnnouncements.Count > _maxVisibleAnnouncements)
        {
            var removalIndex = FindLowestPriorityNewestActive();
            if (removalIndex < 0)
                break;

            RemoveActiveAt(removalIndex, notifyDone: true, cancelPlayback: true);
        }
    }

    private void ShowActive(AnnouncementDisplayData announcement)
    {
        var overlay = GetOverlay(create: true);
        if (overlay == null)
        {
            EnqueueAnnouncement(new QueuedAnnouncement(announcement, _nextOrder++));
            return;
        }

        var widget = new AnnouncementWidget();
        widget.OnAnnouncementFinished += OnAnnouncementFinished;
        _activeAnnouncements.Add(new ActiveAnnouncement(announcement, widget));
        overlay.AddAnnouncement(widget);
        widget.ShowAnnouncement(announcement);
        overlay.Reflow();
    }

    private void OnAnnouncementFinished(AnnouncementWidget widget, uint overrideId)
    {
        UIManager.DeferAction(() => CompleteActiveAnnouncement(widget, overrideId));
    }

    private void CompleteActiveAnnouncement(AnnouncementWidget widget, uint overrideId)
    {
        var index = _activeAnnouncements.FindIndex(active => active.Widget == widget);
        if (index < 0)
            return;

        RemoveActiveAt(index, notifyDone: false, cancelPlayback: false);
        AnnouncementDone?.Invoke(overrideId);
        FillAvailableSlots();
    }

    private void RemoveActiveAt(int index, bool notifyDone, bool cancelPlayback)
    {
        var active = _activeAnnouncements[index];
        _activeAnnouncements.RemoveAt(index);

        active.Widget.OnAnnouncementFinished -= OnAnnouncementFinished;
        if (cancelPlayback)
            active.Widget.CancelAnnouncement();

        GetOverlay(create: false)?.RemoveAnnouncement(active.Widget);

        if (notifyDone)
            NotifyAnnouncementDone(active.Data);
    }

    private void FillAvailableSlots()
    {
        if (GetOverlay(create: true) == null)
            return;

        while (_activeAnnouncements.Count < _maxVisibleAnnouncements && TryDequeueNext(out var next))
        {
            ShowActive(next.Data);
        }
    }

    private void EnqueueAnnouncement(QueuedAnnouncement announcement)
    {
        if (_queuedAnnouncements.Count >= MaxQueuedAnnouncements)
        {
            var lowestIndex = FindLowestPriorityQueuedIndex();
            if (lowestIndex < 0 || !HasHigherQueuePriority(announcement, _queuedAnnouncements[lowestIndex]))
            {
                NotifyAnnouncementDone(announcement.Data);
                return;
            }

            var removed = _queuedAnnouncements[lowestIndex];
            _queuedAnnouncements.RemoveAt(lowestIndex);
            NotifyAnnouncementDone(removed.Data);
        }

        _queuedAnnouncements.Add(announcement);
    }

    private bool TryDequeueNext(out QueuedAnnouncement announcement)
    {
        announcement = default;
        if (_queuedAnnouncements.Count == 0)
            return false;

        var nextIndex = FindHighestPriorityQueuedIndex();
        if (nextIndex < 0)
            return false;

        announcement = _queuedAnnouncements[nextIndex];
        _queuedAnnouncements.RemoveAt(nextIndex);
        return true;
    }

    private int FindOldestLowerPriorityActive(float incomingPriority)
    {
        var priorities = new float[_activeAnnouncements.Count];
        for (var i = 0; i < _activeAnnouncements.Count; i++)
        {
            priorities[i] = _activeAnnouncements[i].Data.Priority;
        }

        return FindOldestLowerPriorityIndex(priorities, incomingPriority);
    }

    private int FindLowestPriorityNewestActive()
    {
        var priorities = new float[_activeAnnouncements.Count];
        for (var i = 0; i < _activeAnnouncements.Count; i++)
        {
            priorities[i] = _activeAnnouncements[i].Data.Priority;
        }

        return FindLowestPriorityNewestIndex(priorities);
    }

    internal static int FindOldestLowerPriorityIndex(IReadOnlyList<float> activePriorities, float incomingPriority)
    {
        for (var i = 0; i < activePriorities.Count; i++)
        {
            if (activePriorities[i] < incomingPriority)
                return i;
        }

        return -1;
    }

    internal static int FindLowestPriorityNewestIndex(IReadOnlyList<float> activePriorities)
    {
        if (activePriorities.Count == 0)
            return -1;

        var candidateIndex = 0;
        for (var i = 1; i < activePriorities.Count; i++)
        {
            var current = activePriorities[i];
            var candidate = activePriorities[candidateIndex];
            if (current < candidate || MathHelper.CloseTo(current, candidate))
                candidateIndex = i;
        }

        return candidateIndex;
    }

    private int FindHighestPriorityQueuedIndex()
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

    private int FindLowestPriorityQueuedIndex()
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

    private AnnouncementOverlayWidget? GetOverlay(bool create)
    {
        var screen = UIManager.ActiveScreen;
        if (screen == null)
            return null;

        return create
            ? screen.GetOrAddWidget<AnnouncementOverlayWidget>()
            : screen.GetWidget<AnnouncementOverlayWidget>();
    }

    private void NotifyAnnouncementDone(AnnouncementDisplayData announcement)
    {
        AnnouncementDone?.Invoke(announcement.OverrideId);
    }

    private sealed record ActiveAnnouncement(AnnouncementDisplayData Data, AnnouncementWidget Widget);
    private readonly record struct QueuedAnnouncement(AnnouncementDisplayData Data, long Order);
}
