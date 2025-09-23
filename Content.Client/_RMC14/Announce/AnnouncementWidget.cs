using System.Numerics;
using Content.Shared._RMC14.Announce;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Maths;
using Robust.Client.Graphics;
using System.Linq;
using System.Text;
using Content.Client._RMC14.Announce.Styling;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementWidget : UIWidget
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
        {
            CleanupCurrentAnnouncement();
        }

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

    private void SetupUI()
    {
        if (ActiveAnnouncement == null) return;

        RemoveAllChildren();
        _textContainers.Clear();

        var announcement = ActiveAnnouncement.Data;
        var style = announcement.Style;

        var titleText = !string.IsNullOrEmpty(announcement.Title) ? announcement.Title : style.Title;
        _hasTitle = style.ShowTitle && !string.IsNullOrEmpty(titleText);
        _titleOffset = _hasTitle ? 1 : 0;

        var contentContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top,
            SeparationOverride = 0
        };

        CreateTextLabels(announcement.Text, titleText, style);
        CreateSpriteContainer(announcement);

        if (_spriteContainer != null)
        {
            var spritePos = style.SpritePosition;

            var spriteWrapper = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                HorizontalExpand = true,
                VerticalExpand = true
            };
            spriteWrapper.AddChild(_spriteContainer);

            if (spritePos == AnnouncementSpritePosition.Left ||
                spritePos == AnnouncementSpritePosition.Above)
            {
                if (spritePos == AnnouncementSpritePosition.Above)
                {
                    var spriteVerticalContainer = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        HorizontalAlignment = HAlignment.Center,
                        VerticalAlignment = VAlignment.Top,
                        SeparationOverride = 0,
                        HorizontalExpand = true,
                        VerticalExpand = true
                    };
                    spriteVerticalContainer.AddChild(spriteWrapper);
                    foreach (var container in _textContainers)
                    {
                        spriteVerticalContainer.AddChild(container);
                    }
                    contentContainer.AddChild(spriteVerticalContainer);
                }
                else
                {
                    contentContainer.AddChild(spriteWrapper);
                    foreach (var container in _textContainers)
                    {
                        contentContainer.AddChild(container);
                    }
                }
            }
            else if (spritePos == AnnouncementSpritePosition.Below)
            {
                var spriteVerticalContainer = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top,
                    SeparationOverride = 0,
                    HorizontalExpand = true,
                    VerticalExpand = true
                };
                foreach (var container in _textContainers)
                {
                    spriteVerticalContainer.AddChild(container);
                }
                spriteVerticalContainer.AddChild(spriteWrapper);
                contentContainer.AddChild(spriteVerticalContainer);
            }
            else
            {
                foreach (var container in _textContainers)
                {
                    contentContainer.AddChild(container);
                }
                contentContainer.AddChild(spriteWrapper);
            }
        }
        else
        {
            foreach (var container in _textContainers)
            {
                contentContainer.AddChild(container);
            }
        }

        AddChild(contentContainer);
        SetInitialVisibility();
        ApplyUIScale(style.UIScale);
    }

    private void CreateTextLabels(string[] text, string? titleText, AnnouncementStyle style)
    {
        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);
        var optimalWidth = AnnouncementStyling.CalculateOptimalTextWidth(text, style, screenSize);

        var totalLabels = text.Length + _titleOffset;
        _richTextLabels = new RichTextLabel[totalLabels];

        var outerContainer = new Control
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top
        };

        var container = new PanelContainer
        {
            HorizontalAlignment = HAlignment.Stretch,
            VerticalAlignment = VAlignment.Stretch
        };

        var textContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top
        };

        var labelIndex = 0;

        if (_hasTitle && !string.IsNullOrEmpty(titleText))
        {
            var titleLabel = new RichTextLabel
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                MaxWidth = optimalWidth
            };

            var titleMessage = new FormattedMessage();
            titleMessage.PushColor(style.TitleColor);
            titleMessage.AddText(titleText);
            titleMessage.Pop();
            titleLabel.SetMessage(titleMessage);

            textContainer.AddChild(titleLabel);
            _richTextLabels[labelIndex] = titleLabel;
            labelIndex++;
        }

        for (int i = 0; i < text.Length; i++)
        {
            var label = new RichTextLabel
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                MaxWidth = optimalWidth
            };

            textContainer.AddChild(label);
            _richTextLabels[labelIndex] = label;
            labelIndex++;
        }

        container.AddChild(textContainer);
        outerContainer.AddChild(container);

        if (style.AnimationEnhancements?.EnableCRT == true)
        {
            var crtSettings = GetCRTSettingsFromStyle(style);
            var crtOverlay = new CRTOverlay
            {
                Settings = crtSettings,
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch
            };
            outerContainer.AddChild(crtOverlay);
        }

        _textContainers.Add(outerContainer);
        ApplyTextStyling();
    }

    private void CreateSpriteContainer(AnnouncementNetData announcement)
    {
        if (!announcement.SpeakerEntity.HasValue ||
            !announcement.ShowSprite ||
            !_entityManager.TryGetEntity(announcement.SpeakerEntity.Value, out var speakerUid))
        {
            return;
        }

        var style = announcement.Style;
        var spriteScale = style.SpriteScale * announcement.SpriteScale;

        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);
        var screenScaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);

        var baseContainerWidth = 120f * screenScaleFactor;
        var baseContainerHeight = 120f * screenScaleFactor;

        var clipContainer = new Control
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top,
            RectClipContent = true
        };

        var spriteView = new SpriteView(_entityManager)
        {
            HorizontalAlignment = HAlignment.Stretch,
            VerticalAlignment = VAlignment.Stretch,
            Stretch = SpriteView.StretchMode.Fill
        };

        spriteView.SetEntity(speakerUid.Value);

        SetSpriteDisplayProperties(clipContainer, spriteView, style, spriteScale, screenScaleFactor);

        clipContainer.AddChild(spriteView);

        Control container = clipContainer;

        if (style.ShowSpriteBox)
        {
            var outerContainer = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top
            };

            var panel = new PanelContainer
            {
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalExpand = true,
                VerticalExpand = true
            };

            var styleBox = new StyleBoxFlat
            {
                BackgroundColor = style.SpriteBoxColor,
                BorderColor = style.SpriteBoxBorderColor,
                BorderThickness = new Thickness(style.SpriteBoxBorderThickness)
            };

            var padding = style.SpriteDisplayMode == SpriteDisplayMode.TopHalf
                ? Math.Max(style.SpriteBoxPadding * 0.3f, 5f * screenScaleFactor)
                : Math.Max(style.SpriteBoxPadding, 15f * screenScaleFactor);

            styleBox.ContentMarginTopOverride = padding;
            styleBox.ContentMarginBottomOverride = padding;
            styleBox.ContentMarginLeftOverride = padding;
            styleBox.ContentMarginRightOverride = padding;

            panel.PanelOverride = styleBox;
            panel.AddChild(clipContainer);
            outerContainer.AddChild(panel);

            if (style.AnimationEnhancements?.EnableCRT == true)
            {
                var crtSettings = GetCRTSettingsFromStyle(style);
                var crtOverlay = new CRTOverlay
                {
                    Settings = crtSettings,
                    HorizontalAlignment = HAlignment.Stretch,
                    VerticalAlignment = VAlignment.Stretch
                };
                outerContainer.AddChild(crtOverlay);
            }

            container = outerContainer;
        }
        else if (style.AnimationEnhancements?.EnableCRT == true)
        {
            var outerContainer = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top
            };

            outerContainer.AddChild(clipContainer);

            var crtSettings = GetCRTSettingsFromStyle(style);
            var crtOverlay = new CRTOverlay
            {
                Settings = crtSettings,
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch
            };

            outerContainer.AddChild(crtOverlay);
            container = outerContainer;
        }

        if (style.ShowSpeakerName && !string.IsNullOrEmpty(announcement.SpeakerName))
        {
            var nameContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                SeparationOverride = 0
            };

            var nameLabel = new RichTextLabel
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            };

            var nameMessage = CreateFormattedMessage(announcement.SpeakerName, new AnnouncementStyle
            {
                PrimaryColor = style.SpeakerNameColor,
                FontSize = style.SpeakerNameFontSize
            });
            nameLabel.SetMessage(nameMessage);

            if (style.SpeakerNamePosition == AnnouncementSpeakerNamePosition.Above)
            {
                nameContainer.AddChild(nameLabel);
                nameContainer.AddChild(container);
            }
            else
            {
                nameContainer.AddChild(container);
                nameContainer.AddChild(nameLabel);
            }

            container = nameContainer;
        }

        _spriteContainer = container;
    }

    private void SetSpriteDisplayProperties(Control clipContainer, SpriteView spriteView, AnnouncementStyle style, float spriteScale, float screenScaleFactor)
    {
        var baseContainerWidth = 120f * screenScaleFactor;
        var baseContainerHeight = 120f * screenScaleFactor;

        var spriteMultiplier = 2.0f;

        switch (style.SpriteDisplayMode)
        {
            case SpriteDisplayMode.TopHalf:
                var topHalfWidth = baseContainerWidth * spriteScale;
                var topHalfHeight = baseContainerHeight * spriteScale * 0.6f;

                clipContainer.SetWidth = topHalfWidth;
                clipContainer.SetHeight = topHalfHeight;
                spriteView.SetWidth = topHalfWidth * spriteMultiplier;
                spriteView.SetHeight = topHalfHeight * spriteMultiplier * 1.5f;
                spriteView.VerticalAlignment = VAlignment.Top;
                break;

            case SpriteDisplayMode.FullSprite:
                clipContainer.SetWidth = baseContainerWidth * spriteScale;
                clipContainer.SetHeight = baseContainerHeight * spriteScale * 2f;
                spriteView.SetWidth = baseContainerWidth * spriteScale * spriteMultiplier;
                spriteView.SetHeight = baseContainerHeight * spriteScale * spriteMultiplier;
                spriteView.VerticalAlignment = VAlignment.Center;
                break;

            case SpriteDisplayMode.HeadOnly:
                clipContainer.SetWidth = baseContainerWidth * spriteScale;
                clipContainer.SetHeight = baseContainerHeight * spriteScale * 0.5f;
                spriteView.SetWidth = baseContainerWidth * spriteScale * spriteMultiplier;
                spriteView.SetHeight = baseContainerHeight * spriteScale * spriteMultiplier;
                spriteView.VerticalAlignment = VAlignment.Top;
                break;

            case SpriteDisplayMode.CustomClip:
                var clipSize = style.SpriteClipSize * spriteScale * screenScaleFactor;
                clipContainer.SetWidth = Math.Max(clipSize.X, baseContainerWidth * spriteScale);
                clipContainer.SetHeight = Math.Max(clipSize.Y, baseContainerHeight * spriteScale);
                spriteView.SetWidth = clipContainer.Width * spriteMultiplier;
                spriteView.SetHeight = clipContainer.Height * spriteMultiplier;

                var offset = style.SpriteClipOffset * screenScaleFactor;
                spriteView.Margin = new Thickness(-offset.X * spriteScale, -offset.Y * spriteScale, 0, 0);
                break;

            default:
                clipContainer.SetWidth = baseContainerWidth * spriteScale;
                clipContainer.SetHeight = baseContainerHeight * spriteScale;
                spriteView.SetWidth = baseContainerWidth * spriteScale * spriteMultiplier;
                spriteView.SetHeight = baseContainerHeight * spriteScale * spriteMultiplier;
                spriteView.VerticalAlignment = VAlignment.Top;
                break;
        }

        spriteView.HorizontalAlignment = HAlignment.Center;
        spriteView.Stretch = SpriteView.StretchMode.Fill;
    }

    private void ApplyUIScale(float uiScale)
    {
        if (uiScale != 1.0f)
        {
            SetWidth = DesiredSize.X * uiScale;
            SetHeight = DesiredSize.Y * uiScale;
        }
    }

    private CRTSettings GetCRTSettingsFromStyle(AnnouncementStyle style)
    {
        if (style.AnimationEnhancements?.EnableCRT == true &&
            style.AnimationEnhancements.CRTSettings != null)
        {
            return style.AnimationEnhancements.CRTSettings;
        }

        return new CRTSettings
        {
            Enabled = true,
            ShowScanlines = true,
            ScanlineSpacing = 3f,
            ScanlineAlpha = 0.8f,
            ScanlineThickness = 2f,
            NoiseIntensity = 0.5f,
            GlowColor = Color.FromHex("#ffffff"),
            VignetteIntensity = 0.3f,
            ShowNoise = true,
            ShowVignette = true
        };
    }

    private void ApplyTextStyling()
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;

        foreach (var outerContainer in _textContainers)
        {
            if (outerContainer.Children.FirstOrDefault() is PanelContainer panel)
            {
                var styleBox = new StyleBoxFlat();

                if (style.ShowBackground)
                {
                    styleBox.BackgroundColor = style.BackgroundColor.WithAlpha(style.BackgroundAlpha);
                    var padding = 10f;
                    styleBox.ContentMarginTopOverride = padding;
                    styleBox.ContentMarginBottomOverride = padding;
                    styleBox.ContentMarginLeftOverride = padding;
                    styleBox.ContentMarginRightOverride = padding;
                }
                else
                {
                    styleBox.BackgroundColor = Color.Transparent;
                }

                panel.PanelOverride = styleBox;
            }
        }
    }

    private void SetInitialVisibility()
    {
        if (ActiveAnnouncement == null) return;

        var animation = ActiveAnnouncement.Data.Style.Animation;

        if (animation == AnnouncementAnimation.Typewriter || animation == AnnouncementAnimation.Glitch)
        {
            for (int i = _titleOffset; i < _richTextLabels.Length; i++)
            {
                _richTextLabels[i].SetMessage(FormattedMessage.FromMarkupPermissive(""));
            }
        }
        else
        {
            SetAllLabelsText();
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (ActiveAnnouncement == null)
            return;

        var deltaTime = (float)args.DeltaSeconds;
        var currentTime = _timing.CurTime;

        UpdateAnnouncement(deltaTime, currentTime);
        UpdatePosition();
    }

    private void UpdateAnnouncement(float deltaTime, TimeSpan currentTime)
    {
        if (ActiveAnnouncement == null) return;

        var elapsed = (float)(currentTime - ActiveAnnouncement.StartTime).TotalSeconds;

        UpdateTimers(deltaTime);

        switch (ActiveAnnouncement.State)
        {
            case AnnouncementState.Animating:
                UpdateAnimatingState(deltaTime);
                break;
            case AnnouncementState.Holding:
                UpdateHoldingState(elapsed);
                break;
        }

        ApplyVisualEffects(currentTime);
    }

    private void UpdateTimers(float deltaTime)
    {
        if (ActiveAnnouncement == null) return;

        ActiveAnnouncement.TypewriterTimer += deltaTime;
        ActiveAnnouncement.GlitchTimer += deltaTime;
        ActiveAnnouncement.SlideTimer += deltaTime;
        ActiveAnnouncement.ZoomTimer += deltaTime;
        ActiveAnnouncement.BounceTimer += deltaTime;
        ActiveAnnouncement.FadeTimer += deltaTime;
        ActiveAnnouncement.PulseTimer += deltaTime;
    }

    private void UpdateAnimatingState(float deltaTime)
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;

        switch (style.Animation)
        {
            case AnnouncementAnimation.Typewriter:
                UpdateTypewriterAnimation();
                break;
            case AnnouncementAnimation.Glitch:
                UpdateGlitchAnimation();
                break;
            case AnnouncementAnimation.Slide:
                UpdateSlideAnimation();
                break;
            case AnnouncementAnimation.Zoom:
                UpdateZoomAnimation();
                break;
            case AnnouncementAnimation.Bounce:
                UpdateBounceAnimation();
                break;
            case AnnouncementAnimation.Fade:
                UpdateFadeAnimation();
                break;
            case AnnouncementAnimation.Pulse:
                UpdatePulseAnimation();
                break;
            default:
                ActiveAnnouncement.State = AnnouncementState.Holding;
                break;
        }
    }

    private void UpdateTypewriterAnimation()
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;
        var printSpeed = style.PrintSpeed;

        if (ActiveAnnouncement.TypewriterTimer >= printSpeed)
        {
            ActiveAnnouncement.TypewriterTimer = 0f;
            AdvanceTypewriter();
        }
    }

    private void AdvanceTypewriter()
    {
        if (ActiveAnnouncement == null) return;

        var cleanText = ActiveAnnouncement.CleanText;
        var currentLine = ActiveAnnouncement.CurrentLine;
        var currentChar = ActiveAnnouncement.CurrentChar;

        if (currentLine >= cleanText.Length)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            return;
        }

        var lineText = cleanText[currentLine];
        if (currentChar >= lineText.Length)
        {
            ActiveAnnouncement.CurrentLine++;
            ActiveAnnouncement.CurrentChar = 0;
            return;
        }

        ActiveAnnouncement.CurrentChar++;
        UpdateTypewriterDisplay();
    }

    private void UpdateTypewriterDisplay()
    {
        if (ActiveAnnouncement == null) return;

        var cleanText = ActiveAnnouncement.CleanText;
        var originalText = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.Data.Style;

        for (int i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < ActiveAnnouncement.CurrentLine)
            {
                var message = CreateFormattedMessage(originalText[textIndex], style);
                _richTextLabels[i].SetMessage(message);
            }
            else if (textIndex == ActiveAnnouncement.CurrentLine)
            {
                var currentLineText = originalText[textIndex];
                var maxLength = Math.Min(ActiveAnnouncement.CurrentChar, currentLineText.Length);
                var partialText = currentLineText.Substring(0, maxLength);
                var message = CreateFormattedMessage(partialText, style);
                _richTextLabels[i].SetMessage(message);
            }
            else
            {
                _richTextLabels[i].SetMessage(FormattedMessage.FromMarkupPermissive(""));
            }
        }
    }

    private void UpdateGlitchAnimation()
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;
        var printSpeed = style.PrintSpeed * 0.5f;

        if (ActiveAnnouncement.GlitchTimer >= printSpeed)
        {
            ActiveAnnouncement.GlitchTimer = 0f;
            AdvanceGlitch();
        }

        if (_random.Prob(style.GlitchChance))
        {
            ApplyGlitchEffect();
        }
    }

    private void AdvanceGlitch()
    {
        if (ActiveAnnouncement == null) return;

        var cleanText = ActiveAnnouncement.CleanText;
        var currentLine = ActiveAnnouncement.CurrentLine;
        var currentChar = ActiveAnnouncement.CurrentChar;

        if (currentLine >= cleanText.Length)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            return;
        }

        var lineText = cleanText[currentLine];
        if (currentChar >= lineText.Length)
        {
            ActiveAnnouncement.CurrentLine++;
            ActiveAnnouncement.CurrentChar = 0;
            return;
        }

        ActiveAnnouncement.CurrentChar++;
        UpdateGlitchDisplay();
    }

    private void UpdateGlitchDisplay()
    {
        if (ActiveAnnouncement == null) return;

        var cleanText = ActiveAnnouncement.CleanText;
        var originalText = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.Data.Style;

        for (int i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < ActiveAnnouncement.CurrentLine)
            {
                var message = CreateFormattedMessage(originalText[textIndex], style);
                _richTextLabels[i].SetMessage(message);
            }
            else if (textIndex == ActiveAnnouncement.CurrentLine)
            {
                var currentLineText = originalText[textIndex];
                var maxLength = Math.Min(ActiveAnnouncement.CurrentChar, currentLineText.Length);
                var partialText = currentLineText.Substring(0, maxLength);
                var message = CreateFormattedMessage(partialText, style);
                _richTextLabels[i].SetMessage(message);
            }
            else
            {
                _richTextLabels[i].SetMessage(FormattedMessage.FromMarkupPermissive(""));
            }
        }
    }

    private void ApplyGlitchEffect()
    {
        if (ActiveAnnouncement == null) return;

        var glitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~".ToCharArray();
        var style = ActiveAnnouncement.Data.Style;

        for (int i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex <= ActiveAnnouncement.CurrentLine && _random.Prob(0.1f))
            {
                var originalText = ActiveAnnouncement.Data.Text[textIndex];
                var glitchedText = string.Join("", originalText.Select(c =>
                    _random.Prob(0.05f) ? glitchChars[_random.Next(glitchChars.Length)] : c));

                var message = CreateFormattedMessage(glitchedText, style);
                _richTextLabels[i].SetMessage(message);
            }
        }
    }

    private void UpdateSlideAnimation()
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;
        var enhancements = style.AnimationEnhancements;
        if (enhancements?.EnableSlide != true) return;

        var duration = enhancements.SlideDuration;
        var progress = Math.Min(ActiveAnnouncement.SlideTimer / duration, 1.0f);

        var startPos = ActiveAnnouncement.SlideStartPosition;
        var currentOffset = Vector2.Lerp(startPos, Vector2.Zero, progress);
        ActiveAnnouncement.CurrentSlideOffset = currentOffset;

        if (progress >= 1.0f)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            SetAllLabelsText();
        }
    }

    private void UpdateZoomAnimation()
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;
        var enhancements = style.AnimationEnhancements;
        if (enhancements?.EnableZoom != true) return;

        var duration = enhancements.ZoomDuration;
        var progress = Math.Min(ActiveAnnouncement.ZoomTimer / duration, 1.0f);

        var startScale = enhancements.ZoomStartScale;
        var currentScale = startScale + (1.0f - startScale) * progress;
        ActiveAnnouncement.ZoomCurrentScale = currentScale;

        if (progress >= 1.0f)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            SetAllLabelsText();
        }
    }

    private void UpdateBounceAnimation()
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;
        var enhancements = style.AnimationEnhancements;
        if (enhancements?.EnableBounce != true) return;

        var bounceCount = enhancements.BounceCount;
        var bounceHeight = enhancements.BounceHeight;
        var cycleDuration = 0.5f;
        var totalDuration = bounceCount * cycleDuration;

        if (ActiveAnnouncement.BounceTimer >= totalDuration)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            ActiveAnnouncement.CurrentBounceOffset = Vector2.Zero;
            SetAllLabelsText();
            return;
        }

        var cycleProgress = (ActiveAnnouncement.BounceTimer % cycleDuration) / cycleDuration;
        var bounceY = MathF.Sin(cycleProgress * MathF.PI) * bounceHeight;
        ActiveAnnouncement.CurrentBounceOffset = new Vector2(0, -bounceY);
    }

    private void UpdateFadeAnimation()
    {
        if (ActiveAnnouncement == null) return;

        var duration = 2.0f;
        var progress = Math.Min(ActiveAnnouncement.FadeTimer / duration, 1.0f);

        ActiveAnnouncement.FadeAlpha = progress;

        if (progress >= 1.0f)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            SetAllLabelsText();
        }
    }

    private void UpdatePulseAnimation()
    {
        if (ActiveAnnouncement == null) return;

        var pulseSpeed = 2.0f;
        var pulseIntensity = 0.3f;

        var pulseValue = MathF.Sin(ActiveAnnouncement.PulseTimer * pulseSpeed);
        ActiveAnnouncement.PulseScale = 1.0f + (pulseValue * pulseIntensity);
        ActiveAnnouncement.PulseAlpha = 0.7f + (pulseValue * 0.3f);

        SetAllLabelsText();
    }

    private void UpdateHoldingState(float elapsed)
    {
        if (ActiveAnnouncement == null) return;

        var holdDuration = ActiveAnnouncement.Data.Style.HoldDuration;
        if (elapsed >= GetAnimationDuration() + holdDuration)
        {
            FinishAnnouncement();
        }
    }

    private void SetAllLabelsText()
    {
        if (ActiveAnnouncement == null) return;

        var originalText = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.Data.Style;

        for (int i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < originalText.Length)
            {
                var message = CreateFormattedMessage(originalText[textIndex], style);
                _richTextLabels[i].SetMessage(message);
            }
        }
    }

    private FormattedMessage CreateFormattedMessage(string text, AnnouncementStyle style)
    {
        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);
        var optimalWidth = AnnouncementStyling.CalculateOptimalTextWidth(ActiveAnnouncement?.Data.Text ?? new[] { text }, style, screenSize);
        var responsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(ActiveAnnouncement?.Data.Text ?? new[] { text }, style.FontSize, optimalWidth, screenSize);

        return AnnouncementStyling.CreateFormattedMessage(text, responsiveFontSize, style.PrimaryColor);
    }

    private void ApplyVisualEffects(TimeSpan currentTime)
    {
        if (ActiveAnnouncement == null) return;

        var style = ActiveAnnouncement.Data.Style;
        var time = (float)currentTime.TotalSeconds;

        foreach (var label in _richTextLabels)
        {
            var baseColor = style.PrimaryColor;

            if (style.SpriteGlow)
            {
                baseColor = ApplyGlow(baseColor, style.SpriteGlowIntensity, currentTime);
            }

            if (style.FlickerChance > 0)
            {
                baseColor = ApplyFlicker(baseColor, style.FlickerChance, currentTime);
            }

            if (ActiveAnnouncement.Data.Style.Animation == AnnouncementAnimation.Fade)
            {
                baseColor = new Color(baseColor.R, baseColor.G, baseColor.B, baseColor.A * ActiveAnnouncement.FadeAlpha);
            }

            if (ActiveAnnouncement.Data.Style.Animation == AnnouncementAnimation.Pulse)
            {
                baseColor = new Color(baseColor.R, baseColor.G, baseColor.B, baseColor.A * ActiveAnnouncement.PulseAlpha);
            }

            label.Modulate = baseColor;
        }
    }

    private Color ApplyGlow(Color baseColor, float intensity, TimeSpan currentTime)
    {
        var time = (float)currentTime.TotalSeconds;
        var glow = MathF.Sin(time * 3f) * 0.5f + 0.5f;
        var glowFactor = 1.0f + (glow * intensity);

        return new Color(
            Math.Min(baseColor.R * glowFactor, 1.0f),
            Math.Min(baseColor.G * glowFactor, 1.0f),
            Math.Min(baseColor.B * glowFactor, 1.0f),
            baseColor.A
        );
    }

    private Color ApplyFlicker(Color baseColor, float flickerChance, TimeSpan currentTime)
    {
        var time = (float)currentTime.TotalSeconds;
        var noise = MathF.Sin(time * 100f) * 0.5f + 0.5f;
        if (noise < flickerChance)
        {
            return new Color(
                baseColor.R * 0.3f,
                baseColor.G * 0.3f,
                baseColor.B * 0.3f,
                baseColor.A
            );
        }
        return baseColor;
    }

    private void UpdatePosition()
    {
        if (Parent is not UIScreen screen || ActiveAnnouncement == null)
            return;

        var screenSize = screen.Size;
        Measure(screenSize);
        var widgetSize = DesiredSize;
        var style = ActiveAnnouncement.Data.Style;

        var position = CalculatePosition(screenSize, widgetSize, style);
        position += ActiveAnnouncement.CurrentSlideOffset + ActiveAnnouncement.CurrentBounceOffset;

        LayoutContainer.SetPosition(this, position);

        if (ActiveAnnouncement.Data.Style.AnimationEnhancements?.EnableZoom == true)
        {
            SetWidth = widgetSize.X * ActiveAnnouncement.ZoomCurrentScale;
            SetHeight = widgetSize.Y * ActiveAnnouncement.ZoomCurrentScale;
        }
    }

    private Vector2 CalculatePosition(Vector2 screenSize, Vector2 widgetSize, AnnouncementStyle style)
    {
        const float padding = 50f;
        const float topPadding = 100f;

        return style.Position switch
        {
            AnnouncementPosition.TopLeft => new Vector2(padding, topPadding),
            AnnouncementPosition.TopCenter => new Vector2((screenSize.X - widgetSize.X) / 2, padding),
            AnnouncementPosition.TopRight => new Vector2(screenSize.X - widgetSize.X - padding, topPadding),
            AnnouncementPosition.MiddleLeft => new Vector2(padding, (screenSize.Y - widgetSize.Y) / 2),
            AnnouncementPosition.MiddleCenter => new Vector2((screenSize.X - widgetSize.X) / 2, (screenSize.Y - widgetSize.Y) / 2),
            AnnouncementPosition.MiddleRight => new Vector2(screenSize.X - widgetSize.X - padding, (screenSize.Y - widgetSize.Y) / 2),
            AnnouncementPosition.BottomLeft => new Vector2(padding, screenSize.Y - widgetSize.Y - padding),
            AnnouncementPosition.BottomCenter => new Vector2((screenSize.X - widgetSize.X) / 2, screenSize.Y - widgetSize.Y - padding),
            AnnouncementPosition.BottomRight => new Vector2(screenSize.X - widgetSize.X - padding, screenSize.Y - widgetSize.Y - padding),
            _ => new Vector2((screenSize.X - widgetSize.X) / 2, (screenSize.Y - widgetSize.Y) / 2)
        };
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

    private float GetAnimationDuration()
    {
        if (ActiveAnnouncement == null) return 0f;

        var style = ActiveAnnouncement.Data.Style;
        var cleanText = ActiveAnnouncement.CleanText;

        return style.Animation switch
        {
            AnnouncementAnimation.Typewriter => cleanText.Sum(line => line.Length) * style.PrintSpeed,
            AnnouncementAnimation.Glitch => cleanText.Sum(line => line.Length) * style.PrintSpeed * 0.5f,
            AnnouncementAnimation.Slide => style.AnimationEnhancements?.SlideDuration ?? 1.0f,
            AnnouncementAnimation.Zoom => style.AnimationEnhancements?.ZoomDuration ?? 1.0f,
            AnnouncementAnimation.Bounce => (style.AnimationEnhancements?.BounceCount ?? 3) * 0.5f,
            _ => 0f
        };
    }

    private string[] PreprocessText(string[] originalText)
    {
        var cleanText = new string[originalText.Length];
        for (int i = 0; i < originalText.Length; i++)
        {
            cleanText[i] = StripMarkup(originalText[i]);
        }
        return cleanText;
    }

    private string StripMarkup(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return System.Text.RegularExpressions.Regex.Replace(text, @"\[/?[^\]]*\]", "");
    }

    private Vector2 GetSlideStartPosition(AnnouncementStyle style)
    {
        if (style.AnimationEnhancements?.EnableSlide != true)
            return Vector2.Zero;

        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);

        return style.AnimationEnhancements.SlideFrom switch
        {
            SlideDirection.Left => new Vector2(-screenSize.X, 0),
            SlideDirection.Right => new Vector2(screenSize.X, 0),
            SlideDirection.Top => new Vector2(0, -screenSize.Y),
            SlideDirection.Bottom => new Vector2(0, screenSize.Y),
            _ => Vector2.Zero
        };
    }
}
