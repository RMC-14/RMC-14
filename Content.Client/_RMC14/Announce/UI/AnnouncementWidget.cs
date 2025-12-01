using System.Numerics;
using Content.Shared._RMC14.Announce;
using Content.Client._RMC14.Announce.Styling;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget : UIWidget
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public event Action? OnAnnouncementFinished;

    public ActiveAnnouncement? ActiveAnnouncement { get; private set; }

    private RichTextLabel[] _richTextLabels = Array.Empty<RichTextLabel>();
    private Control? _spriteContainer;
    private readonly List<Control> _textContainers = new();
    private bool _hasTitle;
    private int _titleOffset;

    public AnnouncementWidget()
    {
        Orientation = LayoutOrientation.Horizontal;
        HorizontalAlignment = HAlignment.Center;
        VerticalAlignment = VAlignment.Center;
        Visible = false;
    }

    public void ShowAnnouncement(AnnouncementNetData announcement)
    {
        if (ActiveAnnouncement != null)
            CleanupCurrentAnnouncement();

        ActiveAnnouncement = new ActiveAnnouncement
        {
            Data = announcement,
            Priority = announcement.Priority,
            CanInterrupt = announcement.CanInterrupt,
            CanBeInterrupted = announcement.CanBeInterrupted,
            StartTime = _timing.CurTime,
            CurrentLine = 0,
            CurrentChar = 0,
            State = AnnouncementState.Animating,
            CleanText = PreprocessText(announcement.Text),
            SlideStartPosition = GetSlideStartPosition(announcement.Style),
            ZoomCurrentScale = announcement.Style.AnimationEnhancements?.EnableZoom == true
                ? announcement.Style.AnimationEnhancements.ZoomStartScale
                : 1.0f,
            FadeAlpha = announcement.Style.Animation == AnnouncementAnimation.Fade ? 0.0f : 1.0f,
            PulseScale = 1.0f,
            PulseAlpha = 1.0f
        };

        SetupUI();
        Visible = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (ActiveAnnouncement == null)
            return;

        var deltaTime = (float) args.DeltaSeconds;
        var currentTime = _timing.CurTime;

        UpdateAnnouncement(deltaTime, currentTime);
        UpdatePosition();
    }

    private void FinishAnnouncement()
    {
        CleanupCurrentAnnouncement();
        Visible = false;
        OnAnnouncementFinished?.Invoke();
    }

    private void CleanupCurrentAnnouncement()
    {
        ActiveAnnouncement = null;
        RemoveAllChildren();
        _textContainers.Clear();
        _richTextLabels = Array.Empty<RichTextLabel>();
        _spriteContainer = null;
        Modulate = Color.White;
        _hasTitle = false;
        _titleOffset = 0;
    }

    private string[] PreprocessText(string[] originalText)
    {
        var cleanText = new string[originalText.Length];
        for (var i = 0; i < originalText.Length; i++)
        {
            cleanText[i] = StripMarkup(originalText[i]);
        }

        return cleanText;
    }

    private static string StripMarkup(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return System.Text.RegularExpressions.Regex.Replace(text, @"\[/?[^\]]*\]", "");
    }
}
