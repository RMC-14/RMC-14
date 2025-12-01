using System.Numerics;
using Content.Client._RMC14.Announce.Styling;
using Content.Shared._RMC14.Announce;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using System.Linq;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private void SetupUI()
    {
        if (ActiveAnnouncement == null)
            return;

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

            if (spritePos == AnnouncementSpritePosition.Left || spritePos == AnnouncementSpritePosition.Above)
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

        for (var i = 0; i < text.Length; i++)
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
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;

        foreach (var outerContainer in _textContainers)
        {
            if (outerContainer.Children.FirstOrDefault() is PanelContainer panel)
            {
                var styleBox = new StyleBoxFlat();

                if (style.ShowBackground)
                {
                    styleBox.BackgroundColor = style.BackgroundColor.WithAlpha(style.BackgroundAlpha);
                    const float padding = 10f;
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
        if (ActiveAnnouncement == null)
            return;

        var animation = ActiveAnnouncement.Data.Style.Animation;

        if (animation == AnnouncementAnimation.Typewriter || animation == AnnouncementAnimation.Glitch)
        {
            for (var i = _titleOffset; i < _richTextLabels.Length; i++)
            {
                _richTextLabels[i].SetMessage(FormattedMessage.FromMarkupPermissive(""));
            }
        }
        else
        {
            SetAllLabelsText();
        }
    }

    private FormattedMessage CreateFormattedMessage(string text, AnnouncementStyle style)
    {
        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);
        var optimalWidth = AnnouncementStyling.CalculateOptimalTextWidth(ActiveAnnouncement?.Data.Text ?? new[] { text }, style, screenSize);
        var responsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(ActiveAnnouncement?.Data.Text ?? new[] { text }, style.FontSize, optimalWidth, screenSize);

        return AnnouncementStyling.CreateFormattedMessage(text, responsiveFontSize, style.PrimaryColor);
    }
}
