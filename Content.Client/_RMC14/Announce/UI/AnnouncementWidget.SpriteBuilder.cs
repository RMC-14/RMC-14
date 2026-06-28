using System.Numerics;
using Content.Client.Resources;
using Content.Client._RMC14.Announce.Styling;
using Content.Shared._RMC14.Announce;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private sealed class SpriteBuilder
    {
        private readonly AnnouncementWidget _owner;
        private readonly DecalBuilder _decalBuilder;

        public SpriteBuilder(AnnouncementWidget owner, DecalBuilder decalBuilder)
        {
            _owner = owner;
            _decalBuilder = decalBuilder;
        }

        public Control? CreateSpriteContainer(AnnouncementDisplayData announcement, AnnouncementStyle style, Vector2 screenSize)
        {
            Control? decalControl = null;

            if (!string.IsNullOrEmpty(announcement.DecalRsi) && !string.IsNullOrEmpty(announcement.DecalState))
            {
                decalControl = _decalBuilder.CreateDecalContainer(announcement, screenSize);
                if (announcement.DecalPlacement == AnnouncementDecalPlacement.ReplaceSprite && decalControl != null)
                    return WrapWithCrtIfEnabled(decalControl, style, screenSize);
            }

            if (!announcement.ShowSprite)
            {
                if (decalControl == null)
                    return null;

                return WrapWithCrtIfEnabled(decalControl, style, screenSize);
            }

            if (!announcement.SpeakerEntity.HasValue ||
                !_owner._entityManager.TryGetEntity(announcement.SpeakerEntity.Value, out var speakerUid))
            {
                if (_owner.PreviewMode)
                    return WrapWithCrtIfEnabled(CreatePreviewSpritePlaceholder(style, screenSize), style, screenSize);

                if (decalControl == null)
                    return null;

                return WrapWithCrtIfEnabled(decalControl, style, screenSize);
            }

            var spriteScale = style.SpriteConfig.SpriteScale * announcement.SpriteScale;
            var screenScaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);

            var clipContainer = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                RectClipContent = true
            };

            var spriteView = new SpriteView(_owner._entityManager)
            {
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                Stretch = SpriteView.StretchMode.Fill
            };

            spriteView.SetEntity(speakerUid.Value);

            _owner.SetSpriteDisplayProperties(clipContainer, spriteView, style, spriteScale, screenScaleFactor);
            clipContainer.AddChild(spriteView);

            Control container = clipContainer;

            if (style.SpriteConfig.ShowSpriteBox)
            {
                var outerPanel = new Control
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
                    BackgroundColor = style.SpriteConfig.SpriteBoxColor,
                    BorderColor = style.SpriteConfig.SpriteBoxBorderColor,
                    BorderThickness = new Thickness(style.SpriteConfig.SpriteBoxBorderThickness)
                };

                var padding = style.LayoutConfig.SpriteDisplayMode == SpriteDisplayMode.TopHalf
                    ? Math.Max(style.SpriteConfig.SpriteBoxPadding * 0.3f, 5f * screenScaleFactor)
                    : Math.Max(style.SpriteConfig.SpriteBoxPadding, 15f * screenScaleFactor);

                styleBox.ContentMarginTopOverride = padding;
                styleBox.ContentMarginBottomOverride = padding;
                styleBox.ContentMarginLeftOverride = padding;
                styleBox.ContentMarginRightOverride = padding;

                panel.PanelOverride = styleBox;
                panel.AddChild(container);
                outerPanel.AddChild(panel);
                _owner.AddSpriteBoxShaderOverlay(style, outerPanel, underlay: false);
                container = outerPanel;
            }

            var placedContainer = _decalBuilder.ApplyDecalPlacement(container, decalControl, announcement, screenSize);
            if (placedContainer != null)
                container = placedContainer;
            container = WrapWithCrtIfEnabled(container, style, screenSize);

            if (style.TextConfig.ShowSpeakerName && !string.IsNullOrEmpty(announcement.SpeakerName))
            {
                var spriteWithMask = container;
                spriteWithMask.Measure(screenSize);
                var speakerWidth = MathF.Max(1f, spriteWithMask.DesiredSize.X);

                var nameContainer = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top,
                    SeparationOverride = CalculateSpeakerNameSeparation(style, screenScaleFactor)
                };

                var label = new Label
                {
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    Align = Label.AlignMode.Center,
                    VAlign = Label.VAlignMode.Center,
                    ClipText = true,
                    Text = announcement.SpeakerName,
                    MinWidth = speakerWidth,
                    SetWidth = speakerWidth,
                    MaxWidth = speakerWidth,
                    FontColorOverride = style.TextConfig.SpeakerNameColor
                };

                if (_owner._prototypeManager.TryIndex<FontPrototype>(style.TextConfig.Font, out var fontPrototype))
                {
                    label.FontOverride = _owner._resCache.GetFont(
                        fontPrototype.Path,
                        Math.Max(1, (int)MathF.Round(style.TextConfig.SpeakerNameFontSize)));
                }

                if (style.LayoutConfig.SpeakerNamePosition == AnnouncementSpeakerNamePosition.Above)
                {
                    nameContainer.AddChild(label);
                    nameContainer.AddChild(spriteWithMask);
                }
                else
                {
                    nameContainer.AddChild(spriteWithMask);
                    nameContainer.AddChild(label);
                }

                container = nameContainer;
            }

            return container;
        }

        private Control CreatePreviewSpritePlaceholder(AnnouncementStyle style, Vector2 screenSize)
        {
            var spriteScale = style.SpriteConfig.SpriteScale;
            var screenScaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);
            var clipContainer = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                RectClipContent = true
            };

            SetPlaceholderDisplayProperties(clipContainer, style, spriteScale, screenScaleFactor);

            var placeholder = new PanelContainer
            {
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalExpand = true,
                VerticalExpand = true,
                Margin = new Thickness(6f * screenScaleFactor)
            };

            placeholder.PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = style.SpriteConfig.SpriteBoxBorderColor.WithAlpha(0.15f),
                BorderColor = style.SpriteConfig.SpriteBoxBorderColor.WithAlpha(0.85f),
                BorderThickness = new Thickness(Math.Max(1f, style.SpriteConfig.SpriteBoxBorderThickness * 0.75f))
            };

            clipContainer.AddChild(placeholder);

            Control container = clipContainer;

            if (style.SpriteConfig.ShowSpriteBox)
            {
                var outerPanel = new Control
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
                    BackgroundColor = style.SpriteConfig.SpriteBoxColor,
                    BorderColor = style.SpriteConfig.SpriteBoxBorderColor,
                    BorderThickness = new Thickness(style.SpriteConfig.SpriteBoxBorderThickness)
                };

                var padding = style.LayoutConfig.SpriteDisplayMode == SpriteDisplayMode.TopHalf
                    ? Math.Max(style.SpriteConfig.SpriteBoxPadding * 0.3f, 5f * screenScaleFactor)
                    : Math.Max(style.SpriteConfig.SpriteBoxPadding, 15f * screenScaleFactor);

                styleBox.ContentMarginTopOverride = padding;
                styleBox.ContentMarginBottomOverride = padding;
                styleBox.ContentMarginLeftOverride = padding;
                styleBox.ContentMarginRightOverride = padding;

                panel.PanelOverride = styleBox;
                panel.AddChild(container);
                outerPanel.AddChild(panel);
                _owner.AddSpriteBoxShaderOverlay(style, outerPanel, underlay: false);
                container = outerPanel;
            }

            return container;
        }

        private static void SetPlaceholderDisplayProperties(Control clipContainer, AnnouncementStyle style, float spriteScale, float screenScaleFactor)
        {
            var baseContainerWidth = 120f * screenScaleFactor;
            var baseContainerHeight = 120f * screenScaleFactor;

            switch (style.LayoutConfig.SpriteDisplayMode)
            {
                case SpriteDisplayMode.TopHalf:
                    clipContainer.SetWidth = baseContainerWidth * spriteScale;
                    clipContainer.SetHeight = baseContainerHeight * spriteScale * 0.6f;
                    break;
                case SpriteDisplayMode.FullSprite:
                    clipContainer.SetWidth = baseContainerWidth * spriteScale;
                    clipContainer.SetHeight = baseContainerHeight * spriteScale * 2f;
                    break;
                case SpriteDisplayMode.HeadOnly:
                    clipContainer.SetWidth = baseContainerWidth * spriteScale;
                    clipContainer.SetHeight = baseContainerHeight * spriteScale * 0.5f;
                    break;
                case SpriteDisplayMode.CustomClip:
                    var clipSize = style.LayoutConfig.SpriteClipSize * spriteScale * screenScaleFactor;
                    clipContainer.SetWidth = Math.Max(clipSize.X, baseContainerWidth * spriteScale);
                    clipContainer.SetHeight = Math.Max(clipSize.Y, baseContainerHeight * spriteScale);
                    break;
                default:
                    clipContainer.SetWidth = baseContainerWidth * spriteScale;
                    clipContainer.SetHeight = baseContainerHeight * spriteScale;
                    break;
            }
        }

        private Control WrapWithCrtIfEnabled(Control container, AnnouncementStyle style, Vector2 screenSize)
        {
            if (!style.AnimationConfig.EnableCRT)
                return container;

            container.Measure(screenSize);
            var desiredSize = container.DesiredSize;
            var width = MathF.Max(1f, desiredSize.X);
            var height = MathF.Max(1f, desiredSize.Y);

            var crtWrapper = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                HorizontalExpand = false,
                VerticalExpand = false,
                MinWidth = width,
                MinHeight = height,
                SetWidth = width,
                SetHeight = height
            };

            container.HorizontalAlignment = HAlignment.Stretch;
            container.VerticalAlignment = VAlignment.Stretch;
            container.HorizontalExpand = true;
            container.VerticalExpand = true;
            crtWrapper.AddChild(container);

            var crtOverlay = new CRTOverlay
            {
                Settings = _owner.GetCRTSettingsFromStyle(style),
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalExpand = true,
                VerticalExpand = true
            };

            crtWrapper.AddChild(crtOverlay);
            return crtWrapper;
        }
    }
}

