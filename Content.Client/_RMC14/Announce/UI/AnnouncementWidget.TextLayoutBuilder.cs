using System.Numerics;
using Content.Client._RMC14.Announce.Styling;
using Content.Shared._RMC14.Announce;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private sealed class TextLayoutBuilder
    {
        private readonly AnnouncementWidget _owner;

        public TextLayoutBuilder(AnnouncementWidget owner)
        {
            _owner = owner;
        }

        public TextLayoutBuildResult BuildTextLayout(
            string[] text,
            string? titleText,
            AnnouncementStyle style,
            Control? spriteContainer,
            bool hasTitle,
            int titleOffset,
            Vector2 textOffset,
            Vector2 screenSize)
        {
            var baseMaxWidth = AnnouncementStyling.CalculateMaxTextWidth(screenSize, style.LayoutConfig.Position);
            var maxAllowedWidth = baseMaxWidth;
            var screenScaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);

            if (spriteContainer != null &&
                (style.LayoutConfig.SpritePosition == AnnouncementSpritePosition.Left || style.LayoutConfig.SpritePosition == AnnouncementSpritePosition.Right))
            {
                spriteContainer.Measure(screenSize);
                var spriteWidth = spriteContainer.DesiredSize.X;
                var spriteSpacing = style.LayoutConfig.SpriteSpacing * screenScaleFactor;
                var horizontalTextBudget = Math.Max(baseMaxWidth * 0.45f, baseMaxWidth - spriteWidth - spriteSpacing);
                maxAllowedWidth = Math.Min(maxAllowedWidth, horizontalTextBudget);
            }

            var effectiveTextWidth = CalculateContentDrivenTextWidth(
                text,
                titleText,
                hasTitle,
                style,
                screenSize,
                screenScaleFactor,
                maxAllowedWidth);
            var bodyResponsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(
                text.Length > 0 ? text : new[] { string.Empty },
                style.TextConfig.FontSize,
                effectiveTextWidth,
                screenSize,
                style);

            var totalLabels = text.Length + titleOffset;
            var labels = new RichTextLabel[totalLabels];

            var scaleFactor = screenScaleFactor;
            var labelIndex = 0;
            RichTextLabel? titleLabelRef = null;
            RichTextLabel[] titleLabels = Array.Empty<RichTextLabel>();
            Control? titleTrackRef = null;
            var titleViewportWidth = 0f;
            var titleContentWidth = 0f;
            var titleScrollGap = 0f;
            Control? titleUnderlineRef = null;

            var outerContainer = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                HorizontalExpand = false,
                SetWidth = effectiveTextWidth,
                MinWidth = effectiveTextWidth,
                Margin = new Thickness(textOffset.X * scaleFactor, textOffset.Y * scaleFactor, 0, 0)
            };

            var container = new PanelContainer
            {
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalExpand = false
            };
            container.SetWidth = effectiveTextWidth;
            container.MinWidth = effectiveTextWidth;

            var textAlign = AnnouncementWidget.GetTextAlignment(style, spriteContainer != null);
            var textContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalAlignment = textAlign,
                VerticalAlignment = VAlignment.Top,
                HorizontalExpand = false
            };
            textContainer.MaxWidth = effectiveTextWidth;
            textContainer.MinWidth = effectiveTextWidth;
            textContainer.SetWidth = effectiveTextWidth;

            if (hasTitle && !string.IsNullOrEmpty(titleText))
            {
                var titleMessage = _owner.CreateFormattedTitleMessage(titleText, style, screenSize, effectiveTextWidth);
                var enableAssaultScroll = false;
                var titleLabel = CreateTitleLabel(textAlign, effectiveTextWidth);
                titleLabel.SetMessage(titleMessage);

                if (enableAssaultScroll)
                {
                    var marqueeMeasureSize = new Vector2(MathF.Max(screenSize.X * 2f, effectiveTextWidth * 2f), float.PositiveInfinity);
                    titleLabel.Measure(marqueeMeasureSize);
                    titleContentWidth = MathF.Max(titleLabel.DesiredSize.X, effectiveTextWidth);
                    titleViewportWidth = effectiveTextWidth;
                    titleScrollGap = Math.Max(style.TitleConfig.Effect.Gap * scaleFactor, 24f * scaleFactor);

                    var titleViewport = new LayoutContainer
                    {
                        InheritChildMeasure = false,
                        HorizontalAlignment = textAlign,
                        VerticalAlignment = VAlignment.Center,
                        HorizontalExpand = false,
                        RectClipContent = true,
                        MinWidth = effectiveTextWidth,
                        SetWidth = effectiveTextWidth
                    };

                    var duplicateTitleLabel = CreateTitleLabel(HAlignment.Left, float.PositiveInfinity);
                    duplicateTitleLabel.SetMessage(titleMessage);
                    duplicateTitleLabel.Measure(marqueeMeasureSize);

                    var titleHeight = MathF.Max(titleLabel.DesiredSize.Y, duplicateTitleLabel.DesiredSize.Y);
                    titleViewport.MinHeight = titleHeight;
                    titleViewport.SetHeight = titleHeight;
                    titleLabel.MinWidth = titleContentWidth;
                    titleLabel.SetWidth = titleContentWidth;
                    titleLabel.MinHeight = titleHeight;
                    titleLabel.SetHeight = titleHeight;
                    duplicateTitleLabel.MinWidth = titleContentWidth;
                    duplicateTitleLabel.SetWidth = titleContentWidth;
                    duplicateTitleLabel.MinHeight = titleHeight;
                    duplicateTitleLabel.SetHeight = titleHeight;

                    LayoutContainer.SetAnchorPreset(titleLabel, LayoutContainer.LayoutPreset.TopLeft);
                    LayoutContainer.SetGrowHorizontal(titleLabel, LayoutContainer.GrowDirection.Constrain);
                    LayoutContainer.SetGrowVertical(titleLabel, LayoutContainer.GrowDirection.Constrain);
                    LayoutContainer.SetAnchorPreset(duplicateTitleLabel, LayoutContainer.LayoutPreset.TopLeft);
                    LayoutContainer.SetGrowHorizontal(duplicateTitleLabel, LayoutContainer.GrowDirection.Constrain);
                    LayoutContainer.SetGrowVertical(duplicateTitleLabel, LayoutContainer.GrowDirection.Constrain);

                    SetLayoutRect(titleLabel, Vector2.Zero, new Vector2(titleContentWidth, titleHeight));
                    SetLayoutRect(duplicateTitleLabel, new Vector2(titleContentWidth + titleScrollGap, 0f), new Vector2(titleContentWidth, titleHeight));

                    titleViewport.AddChild(titleLabel);
                    titleViewport.AddChild(duplicateTitleLabel);

                    titleLabels = new[] { titleLabel, duplicateTitleLabel };
                    titleTrackRef = titleViewport;

                    if (style.TitleConfig.TitleUnderline)
                    {
                        var underlineThickness = Math.Max(1f, style.TitleConfig.TitleUnderlineThickness * scaleFactor);
                        var titleStack = new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            HorizontalAlignment = textAlign,
                            VerticalAlignment = VAlignment.Top,
                            SeparationOverride = Math.Max(2, (int) MathF.Ceiling(underlineThickness))
                        };

                        var underline = new PanelContainer
                        {
                            HorizontalAlignment = textAlign,
                            VerticalAlignment = VAlignment.Top,
                            HorizontalExpand = false,
                            VerticalExpand = false,
                            MinWidth = effectiveTextWidth,
                            SetWidth = effectiveTextWidth,
                            MinHeight = underlineThickness,
                            SetHeight = underlineThickness
                        };
                        underline.PanelOverride = new StyleBoxFlat { BackgroundColor = style.TitleConfig.TitleColor };

                        titleStack.AddChild(titleViewport);
                        titleStack.AddChild(underline);
                        textContainer.AddChild(titleStack);

                        var spacerHeight = Math.Max(underlineThickness * 1.5f, bodyResponsiveFontSize * 0.35f);
                        var titleSpacer = new Control
                        {
                            MinHeight = spacerHeight,
                            SetHeight = spacerHeight
                        };
                        textContainer.AddChild(titleSpacer);
                        titleUnderlineRef = underline;
                    }
                    else
                    {
                        textContainer.AddChild(titleViewport);
                    }
                }
                else
                {
                    titleLabels = new[] { titleLabel };
                    titleLabel.Measure(new Vector2(effectiveTextWidth, float.PositiveInfinity));
                    titleViewportWidth = effectiveTextWidth;
                    titleContentWidth = titleLabel.DesiredSize.X;
                    titleScrollGap = Math.Max(style.TitleConfig.Effect.Gap * scaleFactor, 24f * scaleFactor);
                    if (style.TitleConfig.TitleUnderline)
                    {
                        var underlineThickness = Math.Max(1f, style.TitleConfig.TitleUnderlineThickness * scaleFactor);
                        var underlineWidth = MathF.Min(effectiveTextWidth, titleLabel.DesiredSize.X);
                        var titleStack = new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            HorizontalAlignment = textAlign,
                            VerticalAlignment = VAlignment.Top,
                            SeparationOverride = Math.Max(2, (int) MathF.Ceiling(underlineThickness))
                        };

                        var underline = new PanelContainer
                        {
                            HorizontalAlignment = textAlign,
                            VerticalAlignment = VAlignment.Top,
                            HorizontalExpand = false,
                            VerticalExpand = false,
                            MinWidth = underlineWidth,
                            SetWidth = underlineWidth,
                            MinHeight = underlineThickness,
                            SetHeight = underlineThickness
                        };
                        underline.PanelOverride = new StyleBoxFlat { BackgroundColor = style.TitleConfig.TitleColor };

                        titleStack.AddChild(titleLabel);
                        titleStack.AddChild(underline);
                        textContainer.AddChild(titleStack);

                        var spacerHeight = Math.Max(underlineThickness * 1.5f, bodyResponsiveFontSize * 0.35f);
                        var titleSpacer = new Control
                        {
                            MinHeight = spacerHeight,
                            SetHeight = spacerHeight
                        };
                        textContainer.AddChild(titleSpacer);
                        titleUnderlineRef = underline;
                    }
                    else
                    {
                        textContainer.AddChild(titleLabel);
                    }
                }

                labels[labelIndex] = titleLabel;
                titleLabelRef = titleLabel;
                labelIndex++;
            }

            for (var i = 0; i < text.Length; i++)
            {
                var label = new RichTextLabel
                {
                    HorizontalAlignment = textAlign,
                    VerticalAlignment = VAlignment.Center,
                    MaxWidth = effectiveTextWidth,
                    HorizontalExpand = false
                };

                textContainer.AddChild(label);
                labels[labelIndex] = label;
                labelIndex++;
            }

            container.AddChild(textContainer);
            outerContainer.AddChild(container);

            CRTOverlay? crtOverlayRef = null;
            if (style.AnimationConfig.AnimationEnhancements?.EnableCRT == true)
            {
                var crtSettings = _owner.GetCRTSettingsFromStyle(style);
                var crtOverlay = new CRTOverlay
                {
                    Settings = crtSettings,
                    HorizontalAlignment = HAlignment.Stretch,
                    VerticalAlignment = VAlignment.Stretch,
                    HorizontalExpand = true,
                    MinWidth = effectiveTextWidth,
                    SetWidth = effectiveTextWidth
                };
                outerContainer.AddChild(crtOverlay);
                crtOverlayRef = crtOverlay;
            }

            outerContainer.Measure(screenSize);
            container.Measure(screenSize);
            textContainer.Measure(screenSize);
            titleLabelRef?.Measure(screenSize);
            titleUnderlineRef?.Measure(screenSize);
            crtOverlayRef?.Measure(screenSize);

            if (titleTrackRef is LayoutContainer titleViewportControl && titleLabels.Length > 0)
            {
                var measuredTitleHeight = 0f;
                foreach (var title in titleLabels)
                {
                    measuredTitleHeight = MathF.Max(measuredTitleHeight, MathF.Max(title.DesiredSize.Y, title.Size.Y));
                }

                if (measuredTitleHeight > 0f)
                {
                    titleViewportControl.MinHeight = measuredTitleHeight;
                    titleViewportControl.SetHeight = measuredTitleHeight;
                }
            }

            return new TextLayoutBuildResult(
                labels,
                outerContainer,
                effectiveTextWidth,
                titleLabels,
                titleTrackRef,
                titleViewportWidth,
                titleContentWidth,
                titleScrollGap,
                CalculateTitleFontSize(style, screenSize, effectiveTextWidth, titleText ?? string.Empty));
        }

        private static RichTextLabel CreateTitleLabel(HAlignment alignment, float maxWidth)
        {
            return new RichTextLabel
            {
                HorizontalAlignment = alignment,
                VerticalAlignment = VAlignment.Center,
                MaxWidth = maxWidth,
                HorizontalExpand = false
            };
        }

        private static void SetLayoutRect(Control control, Vector2 position, Vector2 size)
        {
            LayoutContainer.SetMarginLeft(control, position.X);
            LayoutContainer.SetMarginTop(control, position.Y);
            LayoutContainer.SetMarginRight(control, position.X + size.X);
            LayoutContainer.SetMarginBottom(control, position.Y + size.Y);
        }

        private static float CalculateContentDrivenTextWidth(
            string[] text,
            string? titleText,
            bool hasTitle,
            AnnouncementStyle style,
            Vector2 screenSize,
            float screenScaleFactor,
            float maxAllowedWidth)
        {
            var longestBodyLine = 0;
            var longestBodyMeasured = 0f;

            var responsiveBodyFontSize = AnnouncementStyling.CalculateResponsiveFontSize(
                text.Length > 0 ? text : new[] { string.Empty },
                style.TextConfig.FontSize,
                maxAllowedWidth,
                screenSize,
                style);

            foreach (var line in text)
            {
                var plain = AnnouncementWidget.StripMarkup(line);
                if (plain.Length > longestBodyLine)
                    longestBodyLine = plain.Length;

                if (plain.Length == 0)
                    continue;

                var measured = MeasureFormattedTextWidth(
                    plain,
                    responsiveBodyFontSize,
                    style.TextConfig.PrimaryColor,
                    style.TextConfig.Font,
                    screenSize);
                if (measured > longestBodyMeasured)
                    longestBodyMeasured = measured;
            }

            var titleMeasured = 0f;
            var longestTitleLine = 0;

            if (hasTitle && !string.IsNullOrEmpty(titleText))
            {
                var plainTitle = AnnouncementWidget.StripMarkup(titleText);
                longestTitleLine = plainTitle.Length;

                if (plainTitle.Length > 0)
                {
                    var responsiveTitleFont = AnnouncementStyling.CalculateResponsiveFontSize(
                        new[] { plainTitle },
                        style.TitleConfig.TitleFontSize,
                        maxAllowedWidth,
                        screenSize,
                        style);
                    var titleFontSize = Math.Min(responsiveTitleFont, style.TextConfig.FontSize * 0.9f);
                    titleMeasured = MeasureFormattedTextWidth(
                        plainTitle,
                        titleFontSize,
                        style.TitleConfig.TitleColor,
                        style.TitleConfig.TitleFont,
                        screenSize);
                }
            }

            var longestVisibleLine = Math.Max(longestBodyLine, longestTitleLine);
            var measuredContentWidth = MathF.Max(longestBodyMeasured, titleMeasured);
            var horizontalPadding = style.BackgroundConfig.ShowBackground ? 24f * screenScaleFactor : 10f * screenScaleFactor;
            var minReadableWidth = MathF.Max(120f * screenScaleFactor, responsiveBodyFontSize * 6f);
            var minWidth = MathF.Min(maxAllowedWidth, minReadableWidth);

            var fillRatio = longestVisibleLine switch
            {
                <= 16 => 0.88f,
                <= 28 => 0.90f,
                <= 42 => 0.92f,
                _ => 0.94f
            };

            var basePreferredWidth = measuredContentWidth + horizontalPadding;
            var preferredWidth = basePreferredWidth / fillRatio;

            var maxExtraHeadroom = MathF.Max(20f * screenScaleFactor, responsiveBodyFontSize * 1.15f);
            preferredWidth = MathF.Min(preferredWidth, basePreferredWidth + maxExtraHeadroom);

            preferredWidth = MathF.Max(preferredWidth, basePreferredWidth + (4f * screenScaleFactor));
            preferredWidth = MathF.Max(preferredWidth, minWidth);
            return MathHelper.Clamp(preferredWidth, minWidth, maxAllowedWidth);
        }

        private static float MeasureFormattedTextWidth(
            string text,
            float fontSize,
            Color color,
            string? font,
            Vector2 screenSize)
        {
            var label = new RichTextLabel
            {
                HorizontalExpand = false,
                MaxWidth = float.MaxValue
            };

            label.SetMessage(AnnouncementStyling.CreateFormattedMessage(text, fontSize, color, font));
            label.Measure(screenSize);

            return label.DesiredSize.X;
        }

        private static float CalculateTitleFontSize(AnnouncementStyle style, Vector2 screenSize, float maxAllowedWidth, string titleText)
        {
            var responsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(new[] { titleText }, style.TitleConfig.TitleFontSize, maxAllowedWidth, screenSize, style);
            return Math.Min(responsiveFontSize, style.TextConfig.FontSize * 0.9f);
        }
    }

    private readonly record struct TextLayoutBuildResult(
        RichTextLabel[] Labels,
        Control Container,
        float MaxAllowedWidth,
        RichTextLabel[] TitleLabels,
        Control? TitleTrack,
        float TitleViewportWidth,
        float TitleContentWidth,
        float TitleScrollGap,
        float TitleRenderedFontSize);
}

