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
        var style = ActiveAnnouncement.ResolvedStyle;
        var screenSize = ResolveScreenSize();
        var screenScaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);
        var spriteSeparation = CalculateSpriteSeparation(style, screenScaleFactor);

        var titleText = !string.IsNullOrEmpty(announcement.Title) ? announcement.Title : style.TitleConfig.Title;
        _hasTitle = style.TitleConfig.ShowTitle && !string.IsNullOrEmpty(titleText);
        _titleOffset = _hasTitle ? 1 : 0;

        _spriteContainer = _spriteBuilder.CreateSpriteContainer(announcement, style, screenSize);
        var contentAlignment = GetTextAlignment(style, _spriteContainer != null);

        var contentContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = contentAlignment,
            VerticalAlignment = VAlignment.Top,
            SeparationOverride = spriteSeparation
        };

        var titleSpansAnnouncement = _hasTitle &&
            (style.LayoutConfig.SpritePosition == AnnouncementSpritePosition.Left || style.LayoutConfig.SpritePosition == AnnouncementSpritePosition.Right) &&
            style.LayoutConfig.TitlePosition is AnnouncementTitlePosition.Above or AnnouncementTitlePosition.Below;

        var textLayout = _textLayoutBuilder.BuildTextLayout(
            announcement.Text,
            titleSpansAnnouncement ? null : titleText,
            style,
            _spriteContainer,
            _hasTitle && !titleSpansAnnouncement,
            titleSpansAnnouncement ? 0 : _titleOffset,
            announcement.TextOffset,
            screenSize);

        var resolvedTextWidth = textLayout.MaxAllowedWidth;
        _textContainers.Add(textLayout.Container);
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
                        SeparationOverride = spriteSeparation,
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
                    SeparationOverride = spriteSeparation,
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

        if (titleSpansAnnouncement)
        {
            contentContainer.Measure(screenSize);
            var maxAnnouncementWidth = CalculateMaxAnnouncementWidth(screenSize, style, _spriteContainer, spriteSeparation);
            var preferredAnnouncementWidth = _textLayoutBuilder.CalculateStandaloneTitlePreferredWidth(
                titleText,
                style,
                screenSize,
                contentContainer.DesiredSize.X,
                maxAnnouncementWidth);

            if (preferredAnnouncementWidth > contentContainer.DesiredSize.X)
            {
                resolvedTextWidth += preferredAnnouncementWidth - contentContainer.DesiredSize.X;
                _textLayoutBuilder.ExpandTextLayoutWidth(textLayout, resolvedTextWidth);
                contentContainer.Measure(screenSize);
            }

            _activeTextMaxWidth = resolvedTextWidth;
            var titleAlignment = GetTextAlignment(style, _spriteContainer != null);
            var titleBuild = _textLayoutBuilder.BuildStandaloneTitleLayout(
                titleText,
                style,
                screenSize,
                MathF.Max(contentContainer.DesiredSize.X, textLayout.MaxAllowedWidth),
                titleAlignment);

            _richTextLabels = new[] { titleBuild.PrimaryLabel }.Concat(textLayout.Labels).ToArray();
            ActiveAnnouncement.TitleLabels = titleBuild.TitleLabels;
            ActiveAnnouncement.TitleTrack = titleBuild.TitleTrack;
            ActiveAnnouncement.TitleViewportWidth = titleBuild.TitleViewportWidth;
            ActiveAnnouncement.TitleContentWidth = titleBuild.TitleContentWidth;
            ActiveAnnouncement.TitleScrollGap = titleBuild.TitleScrollGap;
            ActiveAnnouncement.TitleText = titleText;
            ActiveAnnouncement.TitleRenderedFontSize = titleBuild.TitleRenderedFontSize;

            var root = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalAlignment = contentAlignment,
                VerticalAlignment = VAlignment.Top,
                SeparationOverride = Math.Max(2, (int) MathF.Ceiling(style.TextConfig.LineHeight * 0.15f))
            };

            if (style.LayoutConfig.TitlePosition == AnnouncementTitlePosition.Above)
            {
                root.AddChild(titleBuild.Container);
                root.AddChild(contentContainer);
            }
            else
            {
                root.AddChild(contentContainer);
                root.AddChild(titleBuild.Container);
            }

            AddChild(root);
        }
        else
        {
            _activeTextMaxWidth = resolvedTextWidth;
            _richTextLabels = textLayout.Labels;
            ActiveAnnouncement.TitleLabels = textLayout.TitleLabels;
            ActiveAnnouncement.TitleTrack = textLayout.TitleTrack;
            ActiveAnnouncement.TitleViewportWidth = textLayout.TitleViewportWidth;
            ActiveAnnouncement.TitleContentWidth = textLayout.TitleContentWidth;
            ActiveAnnouncement.TitleScrollGap = textLayout.TitleScrollGap;
            ActiveAnnouncement.TitleText = titleText;
            ActiveAnnouncement.TitleRenderedFontSize = textLayout.TitleRenderedFontSize;
            AddChild(contentContainer);
        }

        SetInitialVisibility();
        ApplyUIScale(style.LayoutConfig.UIScale);
    }

    private static float CalculateMaxAnnouncementWidth(
        Vector2 screenSize,
        AnnouncementStyle style,
        Control? spriteContainer,
        int spriteSeparation)
    {
        var maxTextWidth = AnnouncementStyling.CalculateMaxTextWidth(screenSize, style.LayoutConfig.Position);
        if (spriteContainer == null)
            return maxTextWidth;

        if (style.LayoutConfig.SpritePosition != AnnouncementSpritePosition.Left &&
            style.LayoutConfig.SpritePosition != AnnouncementSpritePosition.Right)
        {
            return maxTextWidth;
        }

        spriteContainer.Measure(screenSize);
        return maxTextWidth + spriteContainer.DesiredSize.X + spriteSeparation;
    }

    private static int CalculateSpriteSeparation(AnnouncementStyle style, float screenScaleFactor)
    {
        var configured = Math.Max(0f, style.LayoutConfig.SpriteSpacing);
        var fontDriven = Math.Max(
            style.TextConfig.FontSize * 0.20f,
            style.TextConfig.ShowSpeakerName ? style.TextConfig.SpeakerNameFontSize * 0.50f : 0f);

        if (style.SpriteConfig.ShowSpriteBox)
            fontDriven = Math.Max(fontDriven, style.SpriteConfig.SpriteBoxBorderThickness * 2f);

        var spacing = MathF.Max(configured, MathF.Max(2f * screenScaleFactor, fontDriven));
        return Math.Max(0, (int)MathF.Ceiling(spacing));
    }

    private static int CalculateSpeakerNameSeparation(AnnouncementStyle style, float screenScaleFactor)
    {
        var configured = Math.Max(0f, style.LayoutConfig.SpriteSpacing * 0.5f);
        var fontDriven = Math.Max(
            style.TextConfig.SpeakerNameFontSize * 0.35f,
            style.TextConfig.FontSize * 0.15f);

        var spacing = MathF.Max(configured, MathF.Max(2f * screenScaleFactor, fontDriven));
        return Math.Max(0, (int)MathF.Ceiling(spacing));
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

        var style = ActiveAnnouncement.ResolvedStyle;

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

        var animation = ActiveAnnouncement.ResolvedStyle.AnimationConfig.Animation;

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
        if (Parent is not Control parent || ActiveAnnouncement == null)
            return;

        var screenSize = parent.Size.X > 0f && parent.Size.Y > 0f
            ? parent.Size
            : ResolveScreenSize();
        Measure(screenSize);
        var widgetSize = DesiredSize;
        if (Width > 0f && Height > 0f)
            widgetSize = new Vector2(Width, Height);
        var announcement = ActiveAnnouncement.Data;
        var style = ActiveAnnouncement.ResolvedStyle;

        var position = CalculatePosition(screenSize, widgetSize, announcement, style);
        position += ActiveAnnouncement.CurrentSlideOffset + ActiveAnnouncement.CurrentBounceOffset;

        UpdateLayoutRect(position, widgetSize);

        if (style.AnimationConfig.Animation == AnnouncementAnimation.Zoom)
        {
            SetWidth = widgetSize.X * ActiveAnnouncement.ZoomCurrentScale;
            SetHeight = widgetSize.Y * ActiveAnnouncement.ZoomCurrentScale;
        }
    }

    private static Vector2 CalculatePosition(Vector2 screenSize, Vector2 widgetSize, AnnouncementDisplayData announcement, AnnouncementStyle style)
    {
        if (announcement.ScreenPositionOverride is { } normalizedPosition)
            return CalculateCustomPosition(screenSize, widgetSize, normalizedPosition);

        return CalculateStylePosition(screenSize, widgetSize, style);
    }

    private static Vector2 CalculateStylePosition(Vector2 screenSize, Vector2 widgetSize, AnnouncementStyle style)
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

    private static Vector2 CalculateCustomPosition(Vector2 screenSize, Vector2 widgetSize, Vector2 normalizedPosition)
    {
        var clamped = new Vector2(
            Math.Clamp(normalizedPosition.X, 0f, 1f),
            Math.Clamp(normalizedPosition.Y, 0f, 1f));

        var position = new Vector2(screenSize.X * clamped.X, screenSize.Y * clamped.Y);

        const float minVisible = 48f;
        position.X = ClampPositionAxis(position.X, widgetSize.X, screenSize.X, minVisible);
        position.Y = ClampPositionAxis(position.Y, widgetSize.Y, screenSize.Y, minVisible);
        return position;
    }

    private static float ClampPositionAxis(float position, float size, float screenSize, float minVisible)
    {
        var min = minVisible - size;
        var max = screenSize - minVisible;
        if (min > max)
            return (screenSize - size) * 0.5f;

        return Math.Clamp(position, min, max);
    }

    private void UpdateLayoutRect(Vector2 position, Vector2 size)
    {
        LayoutContainer.SetMarginLeft(this, position.X);
        LayoutContainer.SetMarginTop(this, position.Y);
        LayoutContainer.SetMarginRight(this, position.X + size.X);
        LayoutContainer.SetMarginBottom(this, position.Y + size.Y);
    }
}

