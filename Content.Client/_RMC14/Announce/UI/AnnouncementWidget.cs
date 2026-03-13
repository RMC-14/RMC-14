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
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget : UIWidget
{
    private static readonly Vector2 FallbackScreenSize = new(1920f, 1080f);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public event Action? OnAnnouncementFinished;

    public ActiveAnnouncement? ActiveAnnouncement { get; private set; }

    private RichTextLabel[] _richTextLabels = Array.Empty<RichTextLabel>();
    private Control? _spriteContainer;
    private readonly List<Control> _textContainers = new();
    private readonly TextLayoutBuilder _textLayoutBuilder;
    private readonly DecalBuilder _decalBuilder;
    private readonly SpriteBuilder _spriteBuilder;
    private bool _hasTitle;
    private int _titleOffset;
    private float _activeTextMaxWidth;

    public AnnouncementWidget()
    {
        _decalBuilder = new DecalBuilder(this);
        _spriteBuilder = new SpriteBuilder(this, _decalBuilder);
        _textLayoutBuilder = new TextLayoutBuilder(this);
        Orientation = LayoutOrientation.Horizontal;
        HorizontalAlignment = HAlignment.Left;
        VerticalAlignment = VAlignment.Top;
        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.TopLeft);
        LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.Constrain);
        LayoutContainer.SetGrowVertical(this, LayoutContainer.GrowDirection.Constrain);
        Visible = false;
    }

    public void ShowAnnouncement(AnnouncementNetData announcement)
    {
        if (ActiveAnnouncement != null)
            CleanupCurrentAnnouncement();

        ResetLayoutState();

        ActiveAnnouncement = new ActiveAnnouncement
        {
            Data = announcement,
            StartTime = _timing.CurTime,
            CurrentLine = 0,
            CurrentChar = 0,
            State = AnnouncementState.Animating,
            CleanText = PreprocessText(announcement.Text),
            SlideStartPosition = GetSlideStartPosition(announcement.Style),
            ZoomCurrentScale = announcement.Style.AnimationConfig.Animation == AnnouncementAnimation.Zoom
                ? announcement.Style.AnimationConfig.AnimationEnhancements?.ZoomStartScale ?? 0.1f
                : 1.0f,
            FadeAlpha = announcement.Style.AnimationConfig.Animation == AnnouncementAnimation.Fade ? 0.0f : 1.0f,
            PulseScale = 1.0f,
            PulseAlpha = 1.0f
        };

        SetupUI();
        ConfigureAnimationAndEffects();
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
        _playback.Clear();
        _animationContext = null;
        ActiveAnnouncement = null;
        RemoveAllChildren();
        _textContainers.Clear();
        _richTextLabels = Array.Empty<RichTextLabel>();
        _spriteContainer = null;
        Modulate = Color.White;
        _hasTitle = false;
        _titleOffset = 0;
        _activeTextMaxWidth = 0f;
        ResetLayoutState();
    }

    private void ResetLayoutState()
    {
        SetWidth = float.NaN;
        SetHeight = float.NaN;
        MinWidth = 0f;
        MinHeight = 0f;
        MaxWidth = float.PositiveInfinity;
        MaxHeight = float.PositiveInfinity;
        LayoutContainer.SetMarginLeft(this, 0f);
        LayoutContainer.SetMarginTop(this, 0f);
        LayoutContainer.SetMarginRight(this, 0f);
        LayoutContainer.SetMarginBottom(this, 0f);
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

        var tagStart = text.IndexOf('[');
        if (tagStart == -1)
            return text;

        var sb = new System.Text.StringBuilder(text.Length);
        var insideTag = false;

        foreach (var c in text)
        {
            if (c == '[')
            {
                insideTag = true;
                continue;
            }

            if (insideTag)
            {
                if (c == ']')
                    insideTag = false;

                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private Vector2 ResolveScreenSize()
    {
        return Parent is UIScreen screen ? screen.Size : FallbackScreenSize;
    }

}

