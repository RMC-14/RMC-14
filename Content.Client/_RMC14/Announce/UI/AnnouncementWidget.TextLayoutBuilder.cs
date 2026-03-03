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
            var optimalWidth = AnnouncementStyling.CalculateOptimalTextWidth(text, style, screenSize);
            var maxAllowedWidth = Math.Min(baseMaxWidth, optimalWidth * 1.1f);
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

            optimalWidth = Math.Min(maxAllowedWidth, optimalWidth);
            var effectiveTextWidth = CalculateContentDrivenTextWidth(
                text,
                titleText,
                hasTitle,
                style,
                screenSize,
                screenScaleFactor,
                optimalWidth,
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
                var titleLabel = new RichTextLabel
                {
                    HorizontalAlignment = textAlign,
                    VerticalAlignment = VAlignment.Center,
                    MaxWidth = effectiveTextWidth,
                    HorizontalExpand = false
                };

                var titleMessage = _owner.CreateFormattedTitleMessage(titleText, style, screenSize, effectiveTextWidth);
                titleLabel.SetMessage(titleMessage);

                if (style.TitleConfig.TitleUnderline)
                {
                    var underlineThickness = Math.Max(1f, style.TitleConfig.TitleUnderlineThickness * scaleFactor);
                    titleLabel.Measure(new Vector2(effectiveTextWidth, float.PositiveInfinity));
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

            return new TextLayoutBuildResult(labels, outerContainer, effectiveTextWidth);
        }

        private static float CalculateContentDrivenTextWidth(
            string[] text,
            string? titleText,
            bool hasTitle,
            AnnouncementStyle style,
            Vector2 screenSize,
            float screenScaleFactor,
            float optimalWidth,
            float maxAllowedWidth)
        {
            var longestBodyLine = 0;
            foreach (var line in text)
            {
                var plain = AnnouncementWidget.StripMarkup(line);
                if (plain.Length > longestBodyLine)
                    longestBodyLine = plain.Length;
            }

            var bodyEstimate = longestBodyLine * style.TextConfig.FontSize * 0.60f;
            var titleEstimate = 0f;

            if (hasTitle && !string.IsNullOrEmpty(titleText))
            {
                var plainTitle = AnnouncementWidget.StripMarkup(titleText);
                var titleBaseFont = Math.Min(style.TitleConfig.TitleFontSize, style.TextConfig.FontSize * 0.9f);
                titleEstimate = plainTitle.Length * titleBaseFont * 0.60f;
            }

            var estimatedContentWidth = MathF.Max(bodyEstimate, titleEstimate);
            var horizontalPadding = style.BackgroundConfig.ShowBackground ? 24f * screenScaleFactor : 10f * screenScaleFactor;
            var minReadableWidth = MathF.Max(120f * screenScaleFactor, style.TextConfig.FontSize * 6f);
            var minOptimalWidth = optimalWidth * 0.72f;
            var minWidth = MathF.Min(maxAllowedWidth, MathF.Max(minReadableWidth, minOptimalWidth));

            var preferredWidth = estimatedContentWidth + horizontalPadding;
            preferredWidth = MathF.Max(preferredWidth, minWidth);
            return MathHelper.Clamp(preferredWidth, minWidth, maxAllowedWidth);
        }
    }

    private readonly record struct TextLayoutBuildResult(RichTextLabel[] Labels, Control Container, float MaxAllowedWidth);
}

