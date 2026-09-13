using System.Linq;
using System.Numerics;
using Content.Client.Resources;
using Content.Client._RMC14.Announce.Styling;
using Content.Shared._RMC14.Announce;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Log;
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
                var spriteSpacing = CalculateSpriteSeparation(style, screenScaleFactor);
                var horizontalTextBudget = Math.Max(baseMaxWidth * 0.45f, baseMaxWidth - spriteWidth - spriteSpacing);
                maxAllowedWidth = Math.Min(maxAllowedWidth, horizontalTextBudget);
            }

            var titleScrolls = hasTitle && !string.IsNullOrEmpty(titleText) &&
                style.TitleConfig.Effect.Type == AnnouncementTitleEffectType.AssaultScroll;

            var effectiveTextWidth = CalculateContentDrivenTextWidth(
                text,
                titleScrolls ? null : titleText,
                titleScrolls ? false : hasTitle,
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
            var labels = new Control[totalLabels];

            var scaleFactor = screenScaleFactor;
            var bodyLineHeight = MathF.Max(1f, style.TextConfig.LineHeight);
            var labelIndex = 0;
            Control? titleLabelRef = null;
            Control[] titleLabels = Array.Empty<Control>();
            Control? titleTrackRef = null;
            var titleViewportWidth = 0f;
            var titleContentWidth = 0f;
            var titleScrollGap = 0f;
            Control? titleUnderlineRef = null;

            var textAlign = AnnouncementWidget.GetTextAlignment(style, spriteContainer != null);
            var titleAlign = AnnouncementWidget.GetTextAlignment(style, spriteContainer != null);
            var outerContainer = new Control
            {
                HorizontalAlignment = textAlign,
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
                if (titleScrolls)
                {
                    var titleFontSize = CalculateTitleFontSize(style, screenSize, effectiveTextWidth, titleText);
                    var titleFont = ResolveFont(style.TitleConfig.TitleFont, (int)titleFontSize);
                    var measureLabel = CreateMarqueeTitleLabel(titleAlign, titleText, titleFont, style.TitleConfig.TitleColor);

                    var marqueeMeasureSize = new Vector2(MathF.Max(screenSize.X * 2f, effectiveTextWidth * 2f), float.PositiveInfinity);
                    measureLabel.Measure(marqueeMeasureSize);
                    titleViewportWidth = effectiveTextWidth;
                    titleScrollGap = Math.Max(style.TitleConfig.Effect.Gap * scaleFactor, 24f * scaleFactor);

                    // Use natural text width so copies tile continuously with only the gap between them
                    var naturalTextWidth = MathF.Max(measureLabel.DesiredSize.X, 1f);
                    titleContentWidth = naturalTextWidth;
                    var slotWidth = titleContentWidth + titleScrollGap;
                    var copiesNeeded = Math.Max((int)MathF.Ceiling(effectiveTextWidth / slotWidth) + 1, 2);

                    var titleViewport = new LayoutContainer
                    {
                        InheritChildMeasure = false,
                        HorizontalAlignment = titleAlign,
                        VerticalAlignment = VAlignment.Center,
                        HorizontalExpand = false,
                        RectClipContent = true,
                        MinWidth = effectiveTextWidth,
                        SetWidth = effectiveTextWidth
                    };

                    var titleHeight = MathF.Max(measureLabel.DesiredSize.Y, MathF.Ceiling(titleFontSize));
                    var titlePadding = CalculateTitleVerticalPadding(style, scaleFactor);
                    var titleViewportHeight = titleHeight + titlePadding;
                    var titleOffsetY = titlePadding * 0.5f;
                    titleViewport.MinHeight = titleViewportHeight;
                    titleViewport.SetHeight = titleViewportHeight;

                    var scrollLabels = new Control[copiesNeeded];
                    for (var ci = 0; ci < copiesNeeded; ci++)
                    {
                        var lbl = ci == 0
                            ? measureLabel
                            : CreateMarqueeTitleLabel(titleAlign, titleText, titleFont, style.TitleConfig.TitleColor);
                        lbl.MinWidth = titleContentWidth;
                        lbl.SetWidth = titleContentWidth;
                        lbl.MinHeight = titleHeight;
                        lbl.SetHeight = titleHeight;
                        LayoutContainer.SetAnchorPreset(lbl, LayoutContainer.LayoutPreset.TopLeft);
                        LayoutContainer.SetGrowHorizontal(lbl, LayoutContainer.GrowDirection.Constrain);
                        LayoutContainer.SetGrowVertical(lbl, LayoutContainer.GrowDirection.Constrain);
                        SetLayoutRect(lbl, new Vector2(ci * slotWidth, titleOffsetY), new Vector2(titleContentWidth, titleHeight));
                        titleViewport.AddChild(lbl);
                        scrollLabels[ci] = lbl;
                    }

                    titleLabels = scrollLabels;
                    titleTrackRef = titleViewport;

                    if (style.TitleConfig.TitleUnderline)
                    {
                        var underlineThickness = Math.Max(1f, style.TitleConfig.TitleUnderlineThickness * scaleFactor);
                        var titleStack = new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            HorizontalAlignment = titleAlign,
                            VerticalAlignment = VAlignment.Top,
                            SeparationOverride = Math.Max(2, (int) MathF.Ceiling(underlineThickness))
                        };

                        var underline = new PanelContainer
                        {
                            HorizontalAlignment = titleAlign,
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

                    labels[labelIndex] = scrollLabels[0];
                    titleLabelRef = scrollLabels[0];
                    labelIndex++;
                }
                else
                {
                    var titleMeasureSize = new Vector2(MathF.Max(screenSize.X * 2f, effectiveTextWidth * 2f), float.PositiveInfinity);
                    titleViewportWidth = effectiveTextWidth;
                    titleScrollGap = Math.Max(style.TitleConfig.Effect.Gap * scaleFactor, 24f * scaleFactor);
                    var titleFontSize = CalculateTitleFontSize(style, screenSize, effectiveTextWidth, titleText);
                    var renderLabel = CreateStandaloneTitleLabel(titleText, style, titleFontSize, titleAlign);
                    renderLabel.Measure(titleMeasureSize);
                    titleContentWidth = renderLabel.DesiredSize.X;
                    titleLabels = Array.Empty<Control>();

                    var titleHeight = MathF.Max(renderLabel.DesiredSize.Y, 1f);
                    var titlePadding = CalculateTitleVerticalPadding(style, scaleFactor);
                    var titleViewportHeight = titleHeight + titlePadding;
                    var titleOffsetY = titlePadding * 0.5f;
                    var titleViewport = new LayoutContainer
                    {
                        InheritChildMeasure = false,
                        HorizontalAlignment = titleAlign,
                        VerticalAlignment = VAlignment.Center,
                        HorizontalExpand = false,
                        RectClipContent = true,
                        MinWidth = effectiveTextWidth,
                        SetWidth = effectiveTextWidth,
                        MinHeight = titleViewportHeight,
                        SetHeight = titleViewportHeight
                    };

                    LayoutContainer.SetAnchorPreset(renderLabel, LayoutContainer.LayoutPreset.TopLeft);
                    LayoutContainer.SetGrowHorizontal(renderLabel, LayoutContainer.GrowDirection.Constrain);
                    LayoutContainer.SetGrowVertical(renderLabel, LayoutContainer.GrowDirection.Constrain);

                    renderLabel.MinWidth = effectiveTextWidth;
                    renderLabel.SetWidth = effectiveTextWidth;
                    renderLabel.MinHeight = titleHeight;
                    renderLabel.SetHeight = titleHeight;
                    SetLayoutRect(renderLabel, new Vector2(0f, titleOffsetY), new Vector2(effectiveTextWidth, titleHeight));
                    titleViewport.AddChild(renderLabel);
                    titleTrackRef = titleViewport;

                    if (style.TitleConfig.TitleUnderline)
                    {
                        var underlineThickness = Math.Max(1f, style.TitleConfig.TitleUnderlineThickness * scaleFactor);
                        var underlineWidth = MathF.Min(effectiveTextWidth, renderLabel.DesiredSize.X);
                        var titleStack = new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            HorizontalAlignment = titleAlign,
                            VerticalAlignment = VAlignment.Top,
                            SeparationOverride = Math.Max(2, (int) MathF.Ceiling(underlineThickness))
                        };

                        var underline = new PanelContainer
                        {
                            HorizontalAlignment = titleAlign,
                            VerticalAlignment = VAlignment.Top,
                            HorizontalExpand = false,
                            VerticalExpand = false,
                            MinWidth = underlineWidth,
                            SetWidth = underlineWidth,
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

                    var titlePlaceholder = new RichTextLabel();
                    labels[labelIndex] = titlePlaceholder;
                    titleLabelRef = titlePlaceholder;
                    labelIndex++;
                }
            }

            for (var i = 0; i < text.Length; i++)
            {
                var label = new RichTextLabel
                {
                    HorizontalAlignment = textAlign,
                    VerticalAlignment = VAlignment.Top,
                    MaxWidth = effectiveTextWidth,
                    HorizontalExpand = false,
                    MinHeight = bodyLineHeight
                };

                textContainer.AddChild(label);
                labels[labelIndex] = label;
                labelIndex++;
            }

            container.AddChild(textContainer);
            outerContainer.AddChild(container);

            outerContainer.Measure(screenSize);
            container.Measure(screenSize);
            textContainer.Measure(screenSize);
            titleLabelRef?.Measure(screenSize);
            titleUnderlineRef?.Measure(screenSize);

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

        public TitleLayoutBuildResult BuildStandaloneTitleLayout(
            string titleText,
            AnnouncementStyle style,
            Vector2 screenSize,
            float titleWidth,
            HAlignment alignment)
        {
            var scaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);
            var enableAssaultScroll = style.TitleConfig.Effect.Type == AnnouncementTitleEffectType.AssaultScroll;

            Control[] titleLabels;
            Control titleTrack;
            Control primaryLabel;
            var titleViewportWidth = titleWidth;
            float titleContentWidth;
            var titleScrollGap = Math.Max(style.TitleConfig.Effect.Gap * scaleFactor, 24f * scaleFactor);

            if (enableAssaultScroll)
            {
                var titleFontSize = CalculateTitleFontSize(style, screenSize, titleWidth, titleText);
                var titleFont = ResolveFont(style.TitleConfig.TitleFont, (int)titleFontSize);
                var measureLabel = CreateMarqueeTitleLabel(alignment, titleText, titleFont, style.TitleConfig.TitleColor);
                var marqueeMeasureSize = new Vector2(MathF.Max(screenSize.X * 2f, titleWidth * 2f), float.PositiveInfinity);
                measureLabel.Measure(marqueeMeasureSize);

                // Use natural text width so copies tile continuously with only the gap between them
                var naturalTextWidth = MathF.Max(measureLabel.DesiredSize.X, 1f);
                titleContentWidth = naturalTextWidth;
                var slotWidth = titleContentWidth + titleScrollGap;
                var copiesNeeded = Math.Max((int)MathF.Ceiling(titleWidth / slotWidth) + 1, 2);

                var titleHeight = MathF.Max(measureLabel.DesiredSize.Y, MathF.Ceiling(titleFontSize));
                var titlePadding = CalculateTitleVerticalPadding(style, scaleFactor);
                var titleViewportHeight = titleHeight + titlePadding;
                var titleOffsetY = titlePadding * 0.5f;

                var titleViewport = new LayoutContainer
                {
                    InheritChildMeasure = false,
                    HorizontalAlignment = alignment,
                    VerticalAlignment = VAlignment.Center,
                    HorizontalExpand = false,
                    RectClipContent = true,
                    MinWidth = titleWidth,
                    SetWidth = titleWidth,
                    MinHeight = titleViewportHeight,
                    SetHeight = titleViewportHeight
                };

                var scrollLabels = new Control[copiesNeeded];
                for (var ci = 0; ci < copiesNeeded; ci++)
                {
                    var lbl = ci == 0
                        ? measureLabel
                        : CreateMarqueeTitleLabel(alignment, titleText, titleFont, style.TitleConfig.TitleColor);
                    lbl.MinWidth = titleContentWidth;
                    lbl.SetWidth = titleContentWidth;
                    lbl.MinHeight = titleHeight;
                    lbl.SetHeight = titleHeight;
                    LayoutContainer.SetAnchorPreset(lbl, LayoutContainer.LayoutPreset.TopLeft);
                    LayoutContainer.SetGrowHorizontal(lbl, LayoutContainer.GrowDirection.Constrain);
                    LayoutContainer.SetGrowVertical(lbl, LayoutContainer.GrowDirection.Constrain);
                    SetLayoutRect(lbl, new Vector2(ci * slotWidth, titleOffsetY), new Vector2(titleContentWidth, titleHeight));
                    titleViewport.AddChild(lbl);
                    scrollLabels[ci] = lbl;
                }

                titleLabels = scrollLabels;
                primaryLabel = scrollLabels[0];
                titleTrack = titleViewport;
            }
            else
            {
                var titleMessage = _owner.CreateFormattedTitleMessage(titleText, style, screenSize, titleWidth);
                var titleLabel = CreateTitleLabel(alignment, float.PositiveInfinity);
                titleLabel.SetMessage(titleMessage);
                primaryLabel = titleLabel;

                var titleMeasureSize = new Vector2(MathF.Max(screenSize.X * 2f, titleWidth * 2f), float.PositiveInfinity);
                titleLabel.Measure(titleMeasureSize);
                var titleFontSize = CalculateTitleFontSize(style, screenSize, titleWidth, titleText);
                var renderLabel = CreateStandaloneTitleLabel(titleText, style, titleFontSize, alignment);
                renderLabel.Measure(titleMeasureSize);
                titleContentWidth = renderLabel.DesiredSize.X;
                var titleHeight = MathF.Max(renderLabel.DesiredSize.Y, 1f);
                var titlePadding = CalculateTitleVerticalPadding(style, scaleFactor);
                var titleViewportHeight = titleHeight + titlePadding;
                var titleOffsetY = titlePadding * 0.5f;

                var titleViewport = new LayoutContainer
                {
                    InheritChildMeasure = false,
                    HorizontalAlignment = alignment,
                    VerticalAlignment = VAlignment.Center,
                    HorizontalExpand = false,
                    RectClipContent = true,
                    MinWidth = titleWidth,
                    SetWidth = titleWidth,
                    MinHeight = titleViewportHeight,
                    SetHeight = titleViewportHeight
                };

                LayoutContainer.SetAnchorPreset(renderLabel, LayoutContainer.LayoutPreset.TopLeft);
                LayoutContainer.SetGrowHorizontal(renderLabel, LayoutContainer.GrowDirection.Constrain);
                LayoutContainer.SetGrowVertical(renderLabel, LayoutContainer.GrowDirection.Constrain);

                renderLabel.MinWidth = titleWidth;
                renderLabel.SetWidth = titleWidth;
                renderLabel.MinHeight = titleHeight;
                renderLabel.SetHeight = titleHeight;
                SetLayoutRect(renderLabel, new Vector2(0f, titleOffsetY), new Vector2(titleWidth, titleHeight));
                titleViewport.AddChild(renderLabel);
                titleLabels = Array.Empty<Control>();
                titleTrack = titleViewport;
            }

            Control container = titleTrack;
            if (style.TitleConfig.TitleUnderline)
            {
                var underlineThickness = Math.Max(1f, style.TitleConfig.TitleUnderlineThickness * scaleFactor);
                var underlineWidth = enableAssaultScroll ? titleWidth : MathF.Min(titleWidth, titleContentWidth);
                var titleStack = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = alignment,
                    VerticalAlignment = VAlignment.Top,
                    SeparationOverride = Math.Max(2, (int) MathF.Ceiling(underlineThickness))
                };

                var underline = new PanelContainer
                {
                    HorizontalAlignment = alignment,
                    VerticalAlignment = VAlignment.Top,
                    HorizontalExpand = false,
                    VerticalExpand = false,
                    MinWidth = underlineWidth,
                    SetWidth = underlineWidth,
                    MinHeight = underlineThickness,
                    SetHeight = underlineThickness
                };
                underline.PanelOverride = new StyleBoxFlat { BackgroundColor = style.TitleConfig.TitleColor };

                titleStack.AddChild(titleTrack);
                titleStack.AddChild(underline);
                container = titleStack;
            }

            container.Measure(screenSize);
            if (titleTrack is LayoutContainer titleViewportControl)
            {
                var measuredTitleHeight = 0f;
                foreach (var title in titleLabels)
                {
                    measuredTitleHeight = MathF.Max(measuredTitleHeight, MathF.Max(title.DesiredSize.Y, title.Size.Y));
                }

                if (measuredTitleHeight > 0f)
                {
                    var paddedHeight = measuredTitleHeight + CalculateTitleVerticalPadding(style, scaleFactor);
                    titleViewportControl.MinHeight = paddedHeight;
                    titleViewportControl.SetHeight = paddedHeight;
                }
            }

            return new TitleLayoutBuildResult(
                container,
                primaryLabel,
                titleLabels,
                titleTrack,
                titleViewportWidth,
                titleContentWidth,
                titleScrollGap,
                CalculateTitleFontSize(style, screenSize, titleWidth, titleText));
        }

        public float CalculateStandaloneTitlePreferredWidth(
            string titleText,
            AnnouncementStyle style,
            Vector2 screenSize,
            float minWidth,
            float maxWidth)
        {
            if (string.IsNullOrWhiteSpace(titleText))
                return minWidth;

            var scaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);
            var plainTitle = AnnouncementWidget.StripMarkup(titleText);
            if (plainTitle.Length == 0)
                return minWidth;

            var titleFontSize = CalculateTitleFontSize(style, screenSize, maxWidth, plainTitle);
            var measuredWidth = MeasureFormattedTextWidth(
                plainTitle,
                titleFontSize,
                style.TitleConfig.TitleColor,
                style.TitleConfig.TitleFont,
                screenSize);

            var horizontalPadding = style.BackgroundConfig.ShowBackground ? 24f * scaleFactor : 10f * scaleFactor;
            var extraHeadroom = MathF.Max(16f * scaleFactor, titleFontSize * 0.8f);
            var preferredWidth = measuredWidth + horizontalPadding + extraHeadroom;

            return MathHelper.Clamp(MathF.Max(preferredWidth, minWidth), minWidth, maxWidth);
        }

        public void ExpandTextLayoutWidth(TextLayoutBuildResult layout, float width)
        {
            if (width <= 0f)
                return;

            layout.Container.SetWidth = width;
            layout.Container.MinWidth = width;

            if (layout.Container.Children.FirstOrDefault() is not PanelContainer panel)
                return;

            panel.SetWidth = width;
            panel.MinWidth = width;

            if (panel.Children.FirstOrDefault() is not BoxContainer textContainer)
                return;

            textContainer.SetWidth = width;
            textContainer.MinWidth = width;
            textContainer.MaxWidth = width;

            foreach (var label in layout.Labels)
            {
                label.MaxWidth = width;
            }
        }

        private static RichTextLabel CreateTitleLabel(HAlignment alignment, float maxWidth)
        {
            return new RichTextLabel
            {
                HorizontalAlignment = alignment,
                VerticalAlignment = VAlignment.Top,
                MaxWidth = maxWidth,
                HorizontalExpand = false
            };
        }

        private static Label CreateMarqueeTitleLabel(HAlignment hAlignment, string text, Font font, Color color)
        {
            var alignMode = hAlignment switch
            {
                HAlignment.Center => Label.AlignMode.Center,
                HAlignment.Right => Label.AlignMode.Right,
                _ => Label.AlignMode.Left
            };
            return new Label
            {
                Text = text,
                FontOverride = font,
                FontColorOverride = color,
                Align = alignMode,
                VAlign = Label.VAlignMode.Top,
                HorizontalExpand = false
            };
        }

        private Font ResolveFont(string? fontId, int size)
        {
            var id = string.IsNullOrEmpty(fontId) ? FontTag.DefaultFont : fontId;
            if (!_owner._prototypeManager.TryIndex<FontPrototype>(id, out var prototype))
                prototype = _owner._prototypeManager.Index<FontPrototype>(FontTag.DefaultFont);
            var fontResource = _owner._resCache.GetResource<FontResource>(prototype.Path);
            return new VectorFont(fontResource, size);
        }

        private static void SetLayoutRect(Control control, Vector2 position, Vector2 size)
        {
            LayoutContainer.SetMarginLeft(control, position.X);
            LayoutContainer.SetMarginTop(control, position.Y);
            LayoutContainer.SetMarginRight(control, position.X + size.X);
            LayoutContainer.SetMarginBottom(control, position.Y + size.Y);
        }

        private static float CalculateTitleVerticalPadding(AnnouncementStyle style, float scaleFactor)
        {
            return Math.Max(5f * scaleFactor, style.TitleConfig.TitleFontSize * 0.25f);
        }

        private Label CreateStandaloneTitleLabel(
            string titleText,
            AnnouncementStyle style,
            float titleFontSize,
            HAlignment alignment)
        {
            var label = new Label
            {
                Text = titleText,
                ClipText = true,
                FontColorOverride = style.TitleConfig.TitleColor,
                HorizontalAlignment = alignment,
                VerticalAlignment = VAlignment.Center,
                Align = alignment switch
                {
                    HAlignment.Left => Label.AlignMode.Left,
                    HAlignment.Right => Label.AlignMode.Right,
                    _ => Label.AlignMode.Center
                },
                VAlign = Label.VAlignMode.Center
            };

            if (_owner._prototypeManager.TryIndex<FontPrototype>(style.TitleConfig.TitleFont, out var fontPrototype))
            {
                label.FontOverride = _owner._resCache.GetFont(fontPrototype.Path, Math.Max(1, (int)MathF.Round(titleFontSize)));
            }

            return label;
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
                    var titleFontSize = CalculateTitleFontSize(style, screenSize, maxAllowedWidth, plainTitle);
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
            return AnnouncementStyling.CalculateResponsiveFontSize(
                new[] { titleText },
                style.TitleConfig.TitleFontSize,
                maxAllowedWidth,
                screenSize,
                style);
        }
    }

    private readonly record struct TextLayoutBuildResult(
        Control[] Labels,
        Control Container,
        float MaxAllowedWidth,
        Control[] TitleLabels,
        Control? TitleTrack,
        float TitleViewportWidth,
        float TitleContentWidth,
        float TitleScrollGap,
        float TitleRenderedFontSize);

    private readonly record struct TitleLayoutBuildResult(
        Control Container,
        Control PrimaryLabel,
        Control[] TitleLabels,
        Control TitleTrack,
        float TitleViewportWidth,
        float TitleContentWidth,
        float TitleScrollGap,
        float TitleRenderedFontSize);
}
