using System.Numerics;
using Content.Client._RMC14.Announce.Styling;
using Content.Shared._RMC14.Announce;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
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

        public Control? CreateSpriteContainer(AnnouncementNetData announcement, Vector2 screenSize)
        {
            var style = announcement.Style;
            Control? decalControl = null;

            if (!string.IsNullOrEmpty(announcement.DecalRsi) && !string.IsNullOrEmpty(announcement.DecalState))
            {
                decalControl = _decalBuilder.CreateDecalContainer(announcement, screenSize);
                if (announcement.DecalPlacement == AnnouncementDecalPlacement.ReplaceSprite && decalControl != null)
                    return WrapWithCrtIfEnabled(decalControl, style, screenSize);
            }

            if (!announcement.SpeakerEntity.HasValue ||
                !announcement.ShowSprite ||
                !_owner._entityManager.TryGetEntity(announcement.SpeakerEntity.Value, out var speakerUid))
            {
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

                var nameContainer = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top,
                    SeparationOverride = 0
                };

                var label = new RichTextLabel
                {
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                };

                var message = _owner.CreateFormattedMessageWithOverrides(
                    announcement.SpeakerName,
                    style,
                    style.TextConfig.SpeakerNameFontSize,
                    style.TextConfig.SpeakerNameColor,
                    style.TextConfig.Font);

                label.SetMessage(message);

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
            else
            {
            }

            return container;
        }

        private Control WrapWithCrtIfEnabled(Control container, AnnouncementStyle style, Vector2 screenSize)
        {
            if (style.AnimationConfig.AnimationEnhancements?.EnableCRT != true)
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

