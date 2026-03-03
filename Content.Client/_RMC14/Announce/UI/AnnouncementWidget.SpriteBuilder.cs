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
            Control? decalControl = null;

            if (!string.IsNullOrEmpty(announcement.DecalRsi) && !string.IsNullOrEmpty(announcement.DecalState))
            {
                decalControl = _decalBuilder.CreateDecalContainer(announcement, screenSize);
                if (announcement.DecalPlacement == AnnouncementDecalPlacement.ReplaceSprite && decalControl != null)
                    return ApplyIncognitoFinal(announcement, screenSize, decalControl);
            }

            if (!announcement.SpeakerEntity.HasValue ||
                !announcement.ShowSprite ||
                !_owner._entityManager.TryGetEntity(announcement.SpeakerEntity.Value, out var speakerUid))
            {
                if (decalControl == null)
                    return null;

                return ApplyIncognitoFinal(announcement, screenSize, decalControl);
            }

            var style = announcement.Style;
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

            if (style.AnimationConfig.AnimationEnhancements?.EnableCRT == true)
            {
                var crtWrapper = new Control
                {
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top
                };

                crtWrapper.AddChild(container);

                var crtOverlay = new CRTOverlay
                {
                    Settings = _owner.GetCRTSettingsFromStyle(style),
                    HorizontalAlignment = HAlignment.Stretch,
                    VerticalAlignment = VAlignment.Stretch
                };

                crtWrapper.AddChild(crtOverlay);
                container = crtWrapper;
            }

            if (style.TextConfig.ShowSpeakerName && !string.IsNullOrEmpty(announcement.SpeakerName))
            {
                var spriteWithMask = ApplyIncognitoFinal(announcement, screenSize, container);

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
                container = ApplyIncognitoFinal(announcement, screenSize, container);
            }

            return _decalBuilder.ApplyDecalPlacement(container, decalControl, announcement, screenSize);
        }

        private static Control ApplyIncognitoFinal(AnnouncementNetData announcement, Vector2 screenSize, Control spriteContainer)
        {
            var applyMask = announcement.IncognitoMask;

            if (!applyMask)
                return spriteContainer;

            spriteContainer.Measure(screenSize);
            var spriteSize = spriteContainer.DesiredSize;

            var wrapper = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                HorizontalExpand = true,
                VerticalExpand = true,
                MinWidth = spriteSize.X,
                MinHeight = spriteSize.Y,
                SetWidth = spriteSize.X,
                SetHeight = spriteSize.Y,
                RectClipContent = true
            };

            spriteContainer.HorizontalAlignment = HAlignment.Stretch;
            spriteContainer.VerticalAlignment = VAlignment.Stretch;
            spriteContainer.HorizontalExpand = true;
            spriteContainer.VerticalExpand = true;
            spriteContainer.MinWidth = spriteSize.X;
            spriteContainer.MinHeight = spriteSize.Y;

            wrapper.AddChild(spriteContainer);
            if (applyMask)
            {
                var mask = new IncognitoOverlay
                {
                    HorizontalAlignment = HAlignment.Stretch,
                    VerticalAlignment = VAlignment.Stretch,
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    MinWidth = spriteSize.X,
                    MinHeight = spriteSize.Y,
                    SetWidth = spriteSize.X,
                    SetHeight = spriteSize.Y
                };
                wrapper.AddChild(mask);
            }

            wrapper.Measure(screenSize);
            return wrapper;
        }
    }
}

