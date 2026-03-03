using System.Numerics;
using Content.Client._RMC14.Announce.Styling;
using Content.Shared._RMC14.Announce;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private sealed class DecalBuilder
    {
        private readonly AnnouncementWidget _owner;

        public DecalBuilder(AnnouncementWidget owner)
        {
            _owner = owner;
        }

        public Control? ApplyDecalPlacement(Control? spriteContainer, Control? decalControl, AnnouncementNetData announcement, Vector2 screenSize)
        {
            if (decalControl == null || spriteContainer == null)
                return spriteContainer;

            Control? finalContainer = null;
            switch (announcement.DecalPlacement)
            {
                case AnnouncementDecalPlacement.BehindSprite:
                    spriteContainer.Measure(screenSize);
                    var spriteSize = spriteContainer.DesiredSize;
                    var overlay = new Control
                    {
                        HorizontalAlignment = HAlignment.Center,
                        VerticalAlignment = VAlignment.Top,
                        HorizontalExpand = true,
                        VerticalExpand = true,
                        SetWidth = spriteSize.X,
                        SetHeight = spriteSize.Y,
                        MinWidth = spriteSize.X,
                        MinHeight = spriteSize.Y,
                        RectClipContent = true
                    };

                    decalControl.HorizontalAlignment = HAlignment.Center;
                    decalControl.VerticalAlignment = VAlignment.Center;
                    decalControl.HorizontalExpand = false;
                    decalControl.VerticalExpand = false;
                    spriteContainer.HorizontalAlignment = HAlignment.Stretch;
                    spriteContainer.VerticalAlignment = VAlignment.Stretch;

                    overlay.AddChild(decalControl);
                    overlay.AddChild(spriteContainer);
                    overlay.Measure(screenSize);
                    finalContainer = overlay;
                    break;
                case AnnouncementDecalPlacement.Left:
                    var leftBox = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        HorizontalAlignment = HAlignment.Center,
                        VerticalAlignment = VAlignment.Top,
                        SeparationOverride = 0
                    };
                    leftBox.AddChild(decalControl);
                    leftBox.AddChild(spriteContainer);
                    leftBox.Measure(screenSize);
                    finalContainer = leftBox;
                    break;
                case AnnouncementDecalPlacement.Right:
                    var rightBox = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        HorizontalAlignment = HAlignment.Center,
                        VerticalAlignment = VAlignment.Top,
                        SeparationOverride = 0
                    };
                    rightBox.AddChild(spriteContainer);
                    rightBox.AddChild(decalControl);
                    rightBox.Measure(screenSize);
                    finalContainer = rightBox;
                    break;
                case AnnouncementDecalPlacement.Above:
                    var aboveBox = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        HorizontalAlignment = HAlignment.Center,
                        VerticalAlignment = VAlignment.Top,
                        SeparationOverride = 0
                    };
                    aboveBox.AddChild(decalControl);
                    aboveBox.AddChild(spriteContainer);
                    aboveBox.Measure(screenSize);
                    finalContainer = aboveBox;
                    break;
                case AnnouncementDecalPlacement.Below:
                    var belowBox = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        HorizontalAlignment = HAlignment.Center,
                        VerticalAlignment = VAlignment.Top,
                        SeparationOverride = 0
                    };
                    belowBox.AddChild(spriteContainer);
                    belowBox.AddChild(decalControl);
                    belowBox.Measure(screenSize);
                    finalContainer = belowBox;
                    break;
            }

            return finalContainer ?? spriteContainer;
        }

        public Control? CreateDecalContainer(AnnouncementNetData announcement, Vector2 screenSize)
        {
            try
            {
                var resPath = new ResPath(announcement.DecalRsi!);
                var rsi = _owner._resCache.GetResource<RSIResource>(resPath);
                if (!rsi.RSI.TryGetState(announcement.DecalState!, out var state) || state == null)
                    return null;

                var frames = state.GetFrames(RsiDirection.South);
                if (frames.Length == 0)
                    return null;

                var texture = frames[0];
                var screenScaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);
                var decalTestScale = Math.Max(0.1f, announcement.DecalScale * screenScaleFactor);

                var animatedRect = new AnimatedTextureRect
                {
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top,
                    HorizontalExpand = false,
                    VerticalExpand = false
                };
                animatedRect.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(resPath, announcement.DecalState!));
                animatedRect.DisplayRect.Stretch = TextureRect.StretchMode.Scale;
                animatedRect.DisplayRect.TextureScale = new Vector2(decalTestScale, decalTestScale);

                var width = texture.Width * decalTestScale;
                var height = texture.Height * decalTestScale;
                animatedRect.SetWidth = width;
                animatedRect.SetHeight = height;

                var offset = announcement.DecalOffset * screenScaleFactor;
                animatedRect.Margin = new Thickness(offset.X, offset.Y, 0, 0);
                animatedRect.DisplayRect.Modulate = animatedRect.DisplayRect.Modulate.WithAlpha(MathHelper.Clamp(announcement.DecalAlpha, 0f, 1f));

                var clipContainer = new Control
                {
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top,
                    RectClipContent = true,
                    HorizontalExpand = false,
                    VerticalExpand = false,
                    MinWidth = width,
                    MinHeight = height,
                    SetWidth = width,
                    SetHeight = height
                };
                clipContainer.AddChild(animatedRect);

                return clipContainer;
            }
            catch (Exception ex)
            {
                Logger.Error($"[AnnouncementWidget] Failed to load decal {announcement.DecalRsi}:{announcement.DecalState}: {ex}");
                return null;
            }
        }
    }
}
