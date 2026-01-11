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
using Robust.Client.ResourceManagement;
using Robust.Shared.Graphics.RSI;

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

        // Build sprite first so text width calculations can account for its size.
        CreateSpriteContainer(announcement);
        CreateTextLabels(announcement.Text, titleText, style);

        if (_spriteContainer != null)
        {
            var spritePos = style.SpritePosition;

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
        ApplyUIScale(style.UIScale);
    }

    private void CreateTextLabels(string[] text, string? titleText, AnnouncementStyle style)
    {
        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);
        var baseMaxWidth = AnnouncementStyling.CalculateMaxTextWidth(screenSize, style.Position);
        var optimalWidth = AnnouncementStyling.CalculateOptimalTextWidth(text, style, screenSize);
        var maxAllowedWidth = Math.Min(baseMaxWidth, optimalWidth * 1.1f);
        // Expand available width when the sprite is laid out horizontally with text.
        if (_spriteContainer != null &&
            (style.SpritePosition == AnnouncementSpritePosition.Left || style.SpritePosition == AnnouncementSpritePosition.Right))
        {
            _spriteContainer.Measure(screenSize);
            var spriteWidth = _spriteContainer.DesiredSize.X;
            maxAllowedWidth = baseMaxWidth + spriteWidth;
            Logger.Info($"[AnnouncementWidget] Sizing -> screen {screenSize}, baseMaxWidth {baseMaxWidth}, spriteWidth {spriteWidth}, adjustedMaxWidth {maxAllowedWidth}, hasTitle {_hasTitle}");
        }
        else
        {
            Logger.Info($"[AnnouncementWidget] Sizing -> screen {screenSize}, maxAllowedWidth {maxAllowedWidth}, hasTitle {_hasTitle}");
        }
        optimalWidth = Math.Min(maxAllowedWidth, optimalWidth);

        var totalLabels = text.Length + _titleOffset;
        _richTextLabels = new RichTextLabel[totalLabels];

        var scaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);
        var labelIndex = 0;
        RichTextLabel? titleLabelRef = null;
        Control? titleUnderlineRef = null;

        var outerContainer = new Control
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top,
            HorizontalExpand = false,
            SetWidth = optimalWidth,
            MinWidth = optimalWidth
        };
        if (ActiveAnnouncement != null)
        {
            var textOffset = ActiveAnnouncement.Data.TextOffset * scaleFactor;
            outerContainer.Margin = new Thickness(textOffset.X, textOffset.Y, 0, 0);
        }
        var container = new PanelContainer
        {
            HorizontalAlignment = HAlignment.Stretch,
            VerticalAlignment = VAlignment.Stretch,
            HorizontalExpand = false
        };
        container.SetWidth = optimalWidth;
        container.MinWidth = optimalWidth;

        var textAlign = GetTextAlignment(style);
        var textContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalAlignment = textAlign,
            VerticalAlignment = VAlignment.Top,
            HorizontalExpand = false
        };
        textContainer.MaxWidth = optimalWidth;
        textContainer.MinWidth = optimalWidth;
        textContainer.SetWidth = optimalWidth;

        if (_hasTitle && !string.IsNullOrEmpty(titleText))
        {
            var titleLabel = new RichTextLabel
            {
                HorizontalAlignment = textAlign,
                VerticalAlignment = VAlignment.Center,
                MaxWidth = maxAllowedWidth,
                HorizontalExpand = false
            };

            var titleMessage = CreateFormattedTitleMessage(titleText, style, screenSize, maxAllowedWidth);
            titleLabel.SetMessage(titleMessage);
            var titleResponsive = AnnouncementStyling.CalculateResponsiveFontSize(new[] { titleText }, style.TitleFontSize, maxAllowedWidth, screenSize, style);
            Logger.Info($"[AnnouncementWidget] Title label MaxWidth {titleLabel.MaxWidth}, text \"{titleText}\", titleFont {style.TitleFontSize}, responsive {titleResponsive}, fontResource {style.TitleFont}");

            if (style.TitleUnderline)
            {
                var underlineThickness = Math.Max(1f, style.TitleUnderlineThickness * scaleFactor);
                titleLabel.Measure(new Vector2(maxAllowedWidth, float.PositiveInfinity));
                var underlineWidth = MathF.Min(maxAllowedWidth, titleLabel.DesiredSize.X);
                var titleStack = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = textAlign,
                    VerticalAlignment = VAlignment.Top,
                    SeparationOverride = Math.Max(2, (int)MathF.Ceiling(underlineThickness))
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
                underline.PanelOverride = new StyleBoxFlat { BackgroundColor = style.TitleColor };

                titleStack.AddChild(titleLabel);
                titleStack.AddChild(underline);
                textContainer.AddChild(titleStack);
                var spacerHeight = Math.Max(2f, underlineThickness * 1.3f);
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
            _richTextLabels[labelIndex] = titleLabel;
            titleLabelRef = titleLabel;
            labelIndex++;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var label = new RichTextLabel
            {
                HorizontalAlignment = textAlign,
                VerticalAlignment = VAlignment.Center,
                MaxWidth = maxAllowedWidth,
                HorizontalExpand = false
            };

            textContainer.AddChild(label);
            _richTextLabels[labelIndex] = label;
            labelIndex++;
        }

        container.AddChild(textContainer);
        outerContainer.AddChild(container);

        CRTOverlay? crtOverlayRef = null;

        if (style.AnimationEnhancements?.EnableCRT == true)
        {
            var crtSettings = GetCRTSettingsFromStyle(style);
            var crtOverlay = new CRTOverlay
            {
                Settings = crtSettings,
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalExpand = true,
                MinWidth = optimalWidth,
                SetWidth = optimalWidth
            };
            outerContainer.AddChild(crtOverlay);
            crtOverlayRef = crtOverlay;
        }

        _textContainers.Add(outerContainer);
        ApplyTextStyling();

        // Measure to understand actual sizes after layout
        outerContainer.Measure(screenSize);
        container.Measure(screenSize);
        textContainer.Measure(screenSize);
        titleLabelRef?.Measure(screenSize);
        titleUnderlineRef?.Measure(screenSize);
        crtOverlayRef?.Measure(screenSize);
        Logger.Info($"[AnnouncementWidget] Measured outer width {outerContainer.DesiredSize.X}, panel width {container.DesiredSize.X}, text stack width {textContainer.DesiredSize.X}");
        Logger.Info($"[AnnouncementWidget] Title desired width {(titleLabelRef?.DesiredSize.X ?? 0)}, maxAllowed {maxAllowedWidth}, Size {titleLabelRef?.Size ?? Vector2.Zero}, MinSize {titleLabelRef?.MinSize ?? Vector2.Zero}, MaxSize {titleLabelRef?.MaxSize ?? Vector2.Zero}");
        Logger.Info($"[AnnouncementWidget] Widget desired size {DesiredSize}, UI scale {style.UIScale}");
        if (crtOverlayRef != null)
        {
            Logger.Info($"[AnnouncementWidget] CRT overlay desired {crtOverlayRef.DesiredSize}, MinWidth {crtOverlayRef.MinWidth}");
        }
    }

    private void ApplyIncognitoFinal(AnnouncementNetData announcement, Vector2 screenSize)
    {
        var wantsEyeBand = string.Equals(announcement.ConfigId, "PMC", StringComparison.OrdinalIgnoreCase);
        var applyMask = announcement.IncognitoMask;

        if ((!applyMask && !wantsEyeBand) || _spriteContainer == null)
        {
            Logger.Info($"[AnnouncementWidget] Incognito early-exit: mask={announcement.IncognitoMask}, eyeBand={wantsEyeBand}, spriteContainer={_spriteContainer != null}");
            return;
        }

        _spriteContainer.Measure(screenSize);
        var spriteSize = _spriteContainer.DesiredSize;
        var spriteContent = _spriteContainer;
        Logger.Info($"[AnnouncementWidget] Incognito measure pre-wrap: sprite Desired {spriteSize}, Type={spriteContent.GetType().Name}");

        // Wrap sprite and mask in a fixed-size container so the overlay fully covers the sprite.
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

        // Ensure children match the wrapper bounds.
        spriteContent.HorizontalAlignment = HAlignment.Stretch;
        spriteContent.VerticalAlignment = VAlignment.Stretch;
        spriteContent.HorizontalExpand = true;
        spriteContent.VerticalExpand = true;
        spriteContent.MinWidth = spriteSize.X;
        spriteContent.MinHeight = spriteSize.Y;

        wrapper.AddChild(spriteContent);
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

        if (applyMask || wantsEyeBand)
        {
            // Separate eye band overlay so it can be toggled/adjusted independently from static.
            var eyeBand = new EyeBandOverlay
            {
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalExpand = true,
                VerticalExpand = true
            };
            wrapper.AddChild(eyeBand);
        }

        wrapper.Measure(screenSize);
        Logger.Info($"[AnnouncementWidget] Incognito overlay applied; sprite Desired {spriteContent.DesiredSize}, wrapper {wrapper.DesiredSize}");
        LogSpriteTree("Incognito tree", wrapper, 1);

        _spriteContainer = wrapper;
    }

    private void CreateSpriteContainer(AnnouncementNetData announcement)
    {
        Control? decalControl = null;
        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);
        Logger.Info($"[AnnouncementWidget] CreateSpriteContainer start; incognito={announcement.IncognitoMask}, decal={announcement.DecalRsi}:{announcement.DecalState}, showSprite={announcement.ShowSprite}, speakerEntity={announcement.SpeakerEntity}");

        if (!string.IsNullOrEmpty(announcement.DecalRsi) && !string.IsNullOrEmpty(announcement.DecalState))
        {
            TryCreateDecalContainer(announcement, screenSize, out decalControl);
            if (announcement.DecalPlacement == AnnouncementDecalPlacement.ReplaceSprite && decalControl != null)
            {
                _spriteContainer = decalControl;
                ApplyIncognitoFinal(announcement, screenSize);
                return;
            }
        }

        if (!announcement.SpeakerEntity.HasValue ||
            !announcement.ShowSprite ||
            !_entityManager.TryGetEntity(announcement.SpeakerEntity.Value, out var speakerUid))
        {
            Logger.Info($"[AnnouncementWidget] Sprite skipped: hasSpeaker={announcement.SpeakerEntity.HasValue}, showSprite={announcement.ShowSprite}, speakerLookup={(announcement.SpeakerEntity.HasValue && _entityManager.TryGetEntity(announcement.SpeakerEntity.Value, out _))}");
            if (!string.IsNullOrEmpty(announcement.DecalRsi) && !string.IsNullOrEmpty(announcement.DecalState))
            {
                TryCreateDecalContainer(announcement, screenSize, out _spriteContainer);
                ApplyIncognitoFinal(announcement, screenSize);
            }
            return;
        }

        var style = announcement.Style;
        var spriteScale = style.SpriteScale * announcement.SpriteScale;
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
            panel.AddChild(container);
            outerPanel.AddChild(panel);
            AddSpriteBoxShaderOverlay(style, outerPanel, underlay: false);
            container = outerPanel;
        }

        if (style.AnimationEnhancements?.EnableCRT == true)
        {
            var crtWrapper = new Control
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top
            };

            crtWrapper.AddChild(container);

            var crtOverlay = new CRTOverlay
            {
                Settings = GetCRTSettingsFromStyle(style),
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch
            };

            crtWrapper.AddChild(crtOverlay);
            container = crtWrapper;
        }

        if (style.ShowSpeakerName && !string.IsNullOrEmpty(announcement.SpeakerName))
        {
            // Apply incognito before attaching the speaker name so the mask only covers the sprite.
            _spriteContainer = container;
            ApplyIncognitoFinal(announcement, screenSize);
            container = _spriteContainer!;

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

            var message = CreateFormattedMessage(announcement.SpeakerName, new AnnouncementStyle
            {
                PrimaryColor = style.SpeakerNameColor,
                FontSize = style.SpeakerNameFontSize,
                Font = style.Font
            });

            label.SetMessage(message);

            if (style.SpeakerNamePosition == AnnouncementSpeakerNamePosition.Above)
            {
                nameContainer.AddChild(label);
                nameContainer.AddChild(container);
            }
            else
            {
                nameContainer.AddChild(container);
                nameContainer.AddChild(label);
            }

            container = nameContainer;
        }

        // If no speaker name, still apply incognito before decal placement.
        if (!style.ShowSpeakerName || string.IsNullOrEmpty(announcement.SpeakerName))
        {
            _spriteContainer = container;
            ApplyIncognitoFinal(announcement, screenSize);
            container = _spriteContainer!;
        }

        _spriteContainer = container;
        Logger.Info($"[AnnouncementWidget] Sprite container built: type={_spriteContainer.GetType().Name}, Desired={_spriteContainer.DesiredSize}");
        ApplyDecalPlacement(decalControl, announcement, screenSize);
    }

    private void LogSpriteTree(string label, Control node, int depth)
    {
        var pad = new string(' ', depth * 2);
        Logger.Info($"[AnnouncementWidget] {label} depth={depth} type={node.GetType().Name} Desired={node.DesiredSize} Pixel={node.PixelSize} Global={node.GlobalPixelRect} RectClip={node.RectClipContent} Visible={node.Visible}");
        foreach (var child in node.Children)
        {
            LogSpriteTree(label, child, depth + 1);
        }
    }


    private void ApplyDecalPlacement(Control? decalControl, AnnouncementNetData announcement, Vector2 screenSize)
    {
        if (decalControl == null || _spriteContainer == null)
            return;

        Control? finalContainer = null;
        switch (announcement.DecalPlacement)
        {
            case AnnouncementDecalPlacement.BehindSprite:
                _spriteContainer.Measure(screenSize);
                var spriteSize = _spriteContainer.DesiredSize;
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

                // Center the decal behind the sprite so it sits more to the right instead of hugging the left.
                decalControl.HorizontalAlignment = HAlignment.Center;
                decalControl.VerticalAlignment = VAlignment.Center;
                decalControl.HorizontalExpand = false;
                decalControl.VerticalExpand = false;
                _spriteContainer.HorizontalAlignment = HAlignment.Stretch;
                _spriteContainer.VerticalAlignment = VAlignment.Stretch;

                overlay.AddChild(decalControl);
                overlay.AddChild(_spriteContainer);
                overlay.Measure(screenSize);
                finalContainer = overlay;
                Logger.Info($"[AnnouncementWidget] Decal behind sprite applied: overlay size {overlay.DesiredSize}, sprite {spriteSize}");
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
                leftBox.AddChild(_spriteContainer);
                leftBox.Measure(screenSize);
                finalContainer = leftBox;
                Logger.Info($"[AnnouncementWidget] Decal left placement: decal {decalControl.DesiredSize}, sprite {_spriteContainer.DesiredSize}, combined {leftBox.DesiredSize}");
                break;
            case AnnouncementDecalPlacement.Right:
                var rightBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top,
                    SeparationOverride = 0
                };
                rightBox.AddChild(_spriteContainer);
                rightBox.AddChild(decalControl);
                rightBox.Measure(screenSize);
                finalContainer = rightBox;
                Logger.Info($"[AnnouncementWidget] Decal right placement: decal {decalControl.DesiredSize}, sprite {_spriteContainer.DesiredSize}, combined {rightBox.DesiredSize}");
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
                aboveBox.AddChild(_spriteContainer);
                aboveBox.Measure(screenSize);
                finalContainer = aboveBox;
                Logger.Info($"[AnnouncementWidget] Decal above placement: decal {decalControl.DesiredSize}, sprite {_spriteContainer.DesiredSize}, combined {aboveBox.DesiredSize}");
                break;
            case AnnouncementDecalPlacement.Below:
                var belowBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Top,
                    SeparationOverride = 0
                };
                belowBox.AddChild(_spriteContainer);
                belowBox.AddChild(decalControl);
                belowBox.Measure(screenSize);
                finalContainer = belowBox;
                Logger.Info($"[AnnouncementWidget] Decal below placement: decal {decalControl.DesiredSize}, sprite {_spriteContainer.DesiredSize}, combined {belowBox.DesiredSize}");
                break;
        }

        if (finalContainer != null)
            _spriteContainer = finalContainer;
    }

    private void TryCreateDecalContainer(AnnouncementNetData announcement, Vector2 screenSize, out Control? containerOut)
    {
        containerOut = null;
        try
        {
            var resPath = new ResPath(announcement.DecalRsi!);
            var rsi = _resCache.GetResource<RSIResource>(resPath);
            if (!rsi.RSI.TryGetState(announcement.DecalState!, out var state) || state == null)
            {
                Logger.Info($"[AnnouncementWidget] Decal missing state {announcement.DecalState} in {announcement.DecalRsi}");
                return;
            }

            var frames = state.GetFrames(RsiDirection.South);
            if (frames.Length == 0)
            {
                Logger.Info($"[AnnouncementWidget] Decal state has no frames {announcement.DecalState} in {announcement.DecalRsi}");
                return;
            }

            var texture = frames[0];
            var screenScaleFactor = AnnouncementStyling.CalculateScreenScaleFactor(screenSize);
            var decalTestScale = Math.Max(0.1f, announcement.DecalScale * screenScaleFactor);

            var animatedRect = new AnimatedTextureRect
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                HorizontalExpand = true,
                VerticalExpand = true
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
                SetWidth = width,
                SetHeight = height
            };
            clipContainer.AddChild(animatedRect);

            containerOut = clipContainer;
            Logger.Info($"[AnnouncementWidget] Decal container created for {announcement.DecalRsi}:{announcement.DecalState} size {width}x{height} (scaled x{decalTestScale})");
        }
        catch (Exception ex)
        {
            Logger.Error($"[AnnouncementWidget] Failed to load decal {announcement.DecalRsi}:{announcement.DecalState}: {ex}");
        }
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

    private static HAlignment GetTextAlignment(AnnouncementStyle style)
    {
        return style.Position switch
        {
            AnnouncementPosition.TopLeft or AnnouncementPosition.MiddleLeft or AnnouncementPosition.BottomLeft => HAlignment.Left,
            AnnouncementPosition.TopRight or AnnouncementPosition.MiddleRight or AnnouncementPosition.BottomRight => HAlignment.Right,
            _ => HAlignment.Center
        };
    }

    private void AddSpriteBoxShaderOverlay(AnnouncementStyle style, Control container, bool underlay)
    {
        if (string.IsNullOrWhiteSpace(style.SpriteBoxShader))
            return;

        if (!_prototypeManager.TryIndex<ShaderPrototype>(style.SpriteBoxShader, out var shaderPrototype))
        {
            Logger.Warning($"[AnnouncementWidget] Sprite box shader '{style.SpriteBoxShader}' not found.");
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
        var maxAllowedWidth = AnnouncementStyling.CalculateMaxTextWidth(screenSize, style.Position);
        var responsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(ActiveAnnouncement?.Data.Text ?? new[] { text }, style.FontSize, maxAllowedWidth, screenSize, style);

        return AnnouncementStyling.CreateFormattedMessage(text, responsiveFontSize, style.PrimaryColor, style.Font);
    }

    private FormattedMessage CreateFormattedTitleMessage(string text, AnnouncementStyle style, Vector2 screenSize, float maxAllowedWidth)
    {
        var responsiveFontSize = AnnouncementStyling.CalculateResponsiveFontSize(new[] { text }, style.TitleFontSize, maxAllowedWidth, screenSize, style);
        // Keep titles slightly smaller than body to avoid overflow and give hierarchy.
        var cappedFontSize = Math.Min(responsiveFontSize, style.FontSize * 0.9f);
        return AnnouncementStyling.CreateFormattedMessage(text, cappedFontSize, style.TitleColor, style.TitleFont);
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

        LayoutContainer.SetPosition(this, position);

        if (style.AnimationEnhancements?.EnableZoom == true)
        {
            SetWidth = widgetSize.X * ActiveAnnouncement.ZoomCurrentScale;
            SetHeight = widgetSize.Y * ActiveAnnouncement.ZoomCurrentScale;
        }
    }

    private static Vector2 CalculatePosition(Vector2 screenSize, Vector2 widgetSize, AnnouncementStyle style)
    {
        const float padding = 50f;
        const float topPadding = 100f;

        return style.Position switch
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
}
