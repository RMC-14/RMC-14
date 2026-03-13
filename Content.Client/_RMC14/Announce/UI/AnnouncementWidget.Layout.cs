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
using Robust.Shared.Log;

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

        var titleText = !string.IsNullOrEmpty(announcement.Title) ? announcement.Title : style.TitleConfig.Title;
        _hasTitle = style.TitleConfig.ShowTitle && !string.IsNullOrEmpty(titleText);
        _titleOffset = _hasTitle ? 1 : 0;

        var contentContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top,
            SeparationOverride = 0
        };

        var screenSize = ResolveScreenSize();
        _spriteContainer = _spriteBuilder.CreateSpriteContainer(announcement, screenSize);

        var textLayout = _textLayoutBuilder.BuildTextLayout(
            announcement.Text,
            titleText,
            style,
            _spriteContainer,
            _hasTitle,
            _titleOffset,
            announcement.TextOffset,
            screenSize);

        _richTextLabels = textLayout.Labels;
        _textContainers.Add(textLayout.Container);
        _activeTextMaxWidth = textLayout.MaxAllowedWidth;
        ActiveAnnouncement.TitleLabels = textLayout.TitleLabels;
        ActiveAnnouncement.TitleTrack = textLayout.TitleTrack;
        ActiveAnnouncement.TitleViewportWidth = textLayout.TitleViewportWidth;
        ActiveAnnouncement.TitleContentWidth = textLayout.TitleContentWidth;
        ActiveAnnouncement.TitleScrollGap = textLayout.TitleScrollGap;
        ActiveAnnouncement.TitleText = titleText;
        ActiveAnnouncement.TitleRenderedFontSize = textLayout.TitleRenderedFontSize;
        ApplyTextStyling();

        if (_spriteContainer != null)
        {
            var spritePos = style.LayoutConfig.SpritePosition;

            var spriteWrapper = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                HorizontalExpand = false,
                VerticalExpand = false
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
        ApplyUIScale(style.LayoutConfig.UIScale);
    }

    private void SetSpriteDisplayProperties(Control clipContainer, SpriteView spriteView, AnnouncementStyle style, float spriteScale, float screenScaleFactor)
    {
        var baseContainerWidth = 120f * screenScaleFactor;
        var baseContainerHeight = 120f * screenScaleFactor;

        var spriteMultiplier = 2.0f;

        switch (style.LayoutConfig.SpriteDisplayMode)
        {
            case SpriteDisplayMode.TopHalf:
                var topHalfWidth = baseContainerWidth * spriteScale;
                var topHalfHeight = baseContainerHeight * spriteScale * 0.6f;

                clipContainer.SetWidth = topHalfWidth;
                clipContainer.SetHeight = topHalfHeight;
                spriteView.SetWidth = topHalfWidth * spriteMultiplier;
                spriteView.SetHeight = topHalfHeight * spriteMultiplier;
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
                var clipSize = style.LayoutConfig.SpriteClipSize * spriteScale * screenScaleFactor;
                clipContainer.SetWidth = Math.Max(clipSize.X, baseContainerWidth * spriteScale);
                clipContainer.SetHeight = Math.Max(clipSize.Y, baseContainerHeight * spriteScale);
                spriteView.SetWidth = clipContainer.Width * spriteMultiplier;
                spriteView.SetHeight = clipContainer.Height * spriteMultiplier;

                var offset = style.LayoutConfig.SpriteClipOffset * screenScaleFactor;
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

    private static HAlignment GetTextAlignment(AnnouncementStyle style, bool hasSpriteContent)
    {
        if (hasSpriteContent)
        {
            if (style.LayoutConfig.SpritePosition == AnnouncementSpritePosition.Left)
                return HAlignment.Left;

            if (style.LayoutConfig.SpritePosition == AnnouncementSpritePosition.Right)
                return HAlignment.Right;
        }

        return style.LayoutConfig.Position switch
        {
            AnnouncementPosition.TopLeft or AnnouncementPosition.MiddleLeft or AnnouncementPosition.BottomLeft => HAlignment.Left,
            AnnouncementPosition.TopRight or AnnouncementPosition.MiddleRight or AnnouncementPosition.BottomRight => HAlignment.Right,
            _ => HAlignment.Center
        };
    }

    private void AddSpriteBoxShaderOverlay(AnnouncementStyle style, Control container, bool underlay)
    {
        if (string.IsNullOrWhiteSpace(style.SpriteConfig.SpriteBoxShader))
            return;

        if (!_prototypeManager.TryIndex<ShaderPrototype>(style.SpriteConfig.SpriteBoxShader, out var shaderPrototype))
        {
            Logger.Warning($"[AnnouncementWidget] Sprite box shader '{style.SpriteConfig.SpriteBoxShader}' not found.");
            return;
        }

        var overlay = new TextureRect
        {
            Texture = Texture.White,
            Stretch = TextureRect.StretchMode.Scale,
            ShaderOverride = shaderPrototype.Instance(),
            MouseFilter = Control.MouseFilterMode.Ignore,
            HorizontalAlignment = HAlignment.Stretch,
            VerticalAlignment = VAlignment.Stretch,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        container.AddChild(overlay);
        if (underlay)
            overlay.SetPositionFirst();
    }

    private void ApplyUIScale(float uiScale)
    {
        if (MathHelper.CloseTo(uiScale, 1.0f))
            return;

        var screenSize = ResolveScreenSize();
        Measure(screenSize);
        var desired = DesiredSize;
        if (desired.X <= 0f || desired.Y <= 0f)
            return;

        var scaledWidth = desired.X * uiScale;
        var scaledHeight = desired.Y * uiScale;
        SetWidth = scaledWidth;
        SetHeight = scaledHeight;
        MinWidth = scaledWidth;
        MinHeight = scaledHeight;
    }

    private CRTSettings GetCRTSettingsFromStyle(AnnouncementStyle style)
    {
        if (style.AnimationConfig.AnimationEnhancements?.EnableCRT == true &&
            style.AnimationConfig.AnimationEnhancements.CRTSettings != null)
        {
            return style.AnimationConfig.AnimationEnhancements.CRTSettings;
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

                if (style.BackgroundConfig.ShowBackground)
                {
                    styleBox.BackgroundColor = style.BackgroundConfig.BackgroundColor.WithAlpha(style.BackgroundConfig.BackgroundAlpha);
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

        var animation = ActiveAnnouncement.Data.Style.AnimationConfig.Animation;

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
        return CreateFormattedMessageWithOverrides(text, style, null, null, null);
    }

    private FormattedMessage CreateFormattedMessageWithOverrides(
        string text,
        AnnouncementStyle style,
        float? fontSizeOverride,
        Color? colorOverride,
        string? fontOverride)
    {
        var screenSize = ResolveScreenSize();
        var maxAllowedWidth = _activeTextMaxWidth > 0f
            ? _activeTextMaxWidth
            : AnnouncementStyling.CalculateMaxTextWidth(screenSize, style.LayoutConfig.Position);
        var baseFontSize = fontSizeOverride ?? style.TextConfig.FontSize;
        var responsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(
            ActiveAnnouncement?.Data.Text ?? new[] { text },
            baseFontSize,
            maxAllowedWidth,
            screenSize,
            style);

        return AnnouncementStyling.CreateFormattedMessage(
            text,
            responsiveFontSize,
            colorOverride ?? style.TextConfig.PrimaryColor,
            fontOverride ?? style.TextConfig.Font);
    }

    private FormattedMessage CreateFormattedTitleMessage(string text, AnnouncementStyle style, Vector2 screenSize, float maxAllowedWidth)
    {
        var responsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(new[] { text }, style.TitleConfig.TitleFontSize, maxAllowedWidth, screenSize, style);
        return AnnouncementStyling.CreateFormattedMessage(text, responsiveFontSize, style.TitleConfig.TitleColor, style.TitleConfig.TitleFont);
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

        UpdateLayoutRect(position, widgetSize);

        if (style.AnimationConfig.Animation == AnnouncementAnimation.Zoom)
        {
            SetWidth = widgetSize.X * ActiveAnnouncement.ZoomCurrentScale;
            SetHeight = widgetSize.Y * ActiveAnnouncement.ZoomCurrentScale;
        }
    }

    private static Vector2 CalculatePosition(Vector2 screenSize, Vector2 widgetSize, AnnouncementStyle style)
    {
        const float padding = 50f;
        const float topPadding = 100f;

        return style.LayoutConfig.Position switch
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

    private void UpdateLayoutRect(Vector2 position, Vector2 size)
    {
        LayoutContainer.SetMarginLeft(this, position.X);
        LayoutContainer.SetMarginTop(this, position.Y);
        LayoutContainer.SetMarginRight(this, position.X + size.X);
        LayoutContainer.SetMarginBottom(this, position.Y + size.Y);
    }
}

