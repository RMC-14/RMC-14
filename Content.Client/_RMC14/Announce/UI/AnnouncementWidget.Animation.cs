using Content.Shared._RMC14.Announce;
using System.Numerics;
using System.Linq;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private void UpdateAnnouncement(float deltaTime, TimeSpan currentTime)
    {
        if (ActiveAnnouncement == null)
            return;

        var elapsed = (float) (currentTime - ActiveAnnouncement.StartTime).TotalSeconds;

        UpdateTimers(deltaTime);

        switch (ActiveAnnouncement.State)
        {
            case AnnouncementState.Animating:
                UpdateAnimatingState(deltaTime);
                break;
            case AnnouncementState.Holding:
                UpdateHoldingState(elapsed);
                break;
        }

        ApplyVisualEffects(currentTime);
    }

    private void UpdateTimers(float deltaTime)
    {
        if (ActiveAnnouncement == null)
            return;

        ActiveAnnouncement.TypewriterTimer += deltaTime;
        ActiveAnnouncement.GlitchTimer += deltaTime;
        ActiveAnnouncement.SlideTimer += deltaTime;
        ActiveAnnouncement.ZoomTimer += deltaTime;
        ActiveAnnouncement.BounceTimer += deltaTime;
        ActiveAnnouncement.FadeTimer += deltaTime;
        ActiveAnnouncement.PulseTimer += deltaTime;
    }

    private void UpdateAnimatingState(float deltaTime)
    {
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;

        switch (style.Animation)
        {
            case AnnouncementAnimation.Typewriter:
                UpdateTypewriterAnimation();
                break;
            case AnnouncementAnimation.Glitch:
                UpdateGlitchAnimation();
                break;
            case AnnouncementAnimation.Slide:
                UpdateSlideAnimation();
                break;
            case AnnouncementAnimation.Zoom:
                UpdateZoomAnimation();
                break;
            case AnnouncementAnimation.Bounce:
                UpdateBounceAnimation();
                break;
            case AnnouncementAnimation.Fade:
                UpdateFadeAnimation();
                break;
            case AnnouncementAnimation.Pulse:
                UpdatePulseAnimation();
                break;
            default:
                ActiveAnnouncement.State = AnnouncementState.Holding;
                break;
        }
    }

    private void UpdateTypewriterAnimation()
    {
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;
        var printSpeed = style.PrintSpeed;

        if (ActiveAnnouncement.TypewriterTimer >= printSpeed)
        {
            ActiveAnnouncement.TypewriterTimer = 0f;
            AdvanceTypewriter();
        }
    }

    private void AdvanceTypewriter()
    {
        if (ActiveAnnouncement == null)
            return;

        var cleanText = ActiveAnnouncement.CleanText;
        var currentLine = ActiveAnnouncement.CurrentLine;
        var currentChar = ActiveAnnouncement.CurrentChar;

        if (currentLine >= cleanText.Length)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            return;
        }

        var lineText = cleanText[currentLine];
        if (currentChar >= lineText.Length)
        {
            ActiveAnnouncement.CurrentLine++;
            ActiveAnnouncement.CurrentChar = 0;
            return;
        }

        ActiveAnnouncement.CurrentChar++;
        UpdateTypewriterDisplay();
    }

    private void UpdateTypewriterDisplay()
    {
        if (ActiveAnnouncement == null)
            return;

        var cleanText = ActiveAnnouncement.CleanText;
        var originalText = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.Data.Style;

        for (var i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < ActiveAnnouncement.CurrentLine)
            {
                var message = CreateFormattedMessage(originalText[textIndex], style);
                _richTextLabels[i].SetMessage(message);
            }
            else if (textIndex == ActiveAnnouncement.CurrentLine)
            {
                var currentLineText = originalText[textIndex];
                var maxLength = Math.Min(ActiveAnnouncement.CurrentChar, currentLineText.Length);
                var partialText = currentLineText.Substring(0, maxLength);
                var message = CreateFormattedMessage(partialText, style);
                _richTextLabels[i].SetMessage(message);
            }
            else
            {
                _richTextLabels[i].SetMessage(FormattedMessage.FromMarkupPermissive(""));
            }
        }
    }

    private void UpdateGlitchAnimation()
    {
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;
        var printSpeed = style.PrintSpeed * 0.5f;

        if (ActiveAnnouncement.GlitchTimer >= printSpeed)
        {
            ActiveAnnouncement.GlitchTimer = 0f;
            AdvanceGlitch();
        }

        if (RandomChance(style.GlitchChance))
            ApplyGlitchEffect();
    }

    private void AdvanceGlitch()
    {
        if (ActiveAnnouncement == null)
            return;

        var cleanText = ActiveAnnouncement.CleanText;
        var currentLine = ActiveAnnouncement.CurrentLine;
        var currentChar = ActiveAnnouncement.CurrentChar;

        if (currentLine >= cleanText.Length)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            return;
        }

        var lineText = cleanText[currentLine];
        if (currentChar >= lineText.Length)
        {
            ActiveAnnouncement.CurrentLine++;
            ActiveAnnouncement.CurrentChar = 0;
            return;
        }

        ActiveAnnouncement.CurrentChar++;
        UpdateGlitchDisplay();
    }

    private void UpdateGlitchDisplay()
    {
        if (ActiveAnnouncement == null)
            return;

        var cleanText = ActiveAnnouncement.CleanText;
        var originalText = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.Data.Style;

        for (var i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < ActiveAnnouncement.CurrentLine)
            {
                var message = CreateFormattedMessage(originalText[textIndex], style);
                _richTextLabels[i].SetMessage(message);
            }
            else if (textIndex == ActiveAnnouncement.CurrentLine)
            {
                var currentLineText = originalText[textIndex];
                var maxLength = Math.Min(ActiveAnnouncement.CurrentChar, currentLineText.Length);
                var partialText = currentLineText.Substring(0, maxLength);
                var message = CreateFormattedMessage(partialText, style);
                _richTextLabels[i].SetMessage(message);
            }
            else
            {
                _richTextLabels[i].SetMessage(FormattedMessage.FromMarkupPermissive(""));
            }
        }
    }

    private void ApplyGlitchEffect()
    {
        if (ActiveAnnouncement == null)
            return;

        var glitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~".ToCharArray();
        var style = ActiveAnnouncement.Data.Style;

        for (var i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex <= ActiveAnnouncement.CurrentLine && RandomChance(0.1f))
            {
                var originalText = ActiveAnnouncement.Data.Text[textIndex];
                var glitchedText = string.Join("", originalText.Select(c =>
                    RandomChance(0.05f) ? glitchChars[_random.Next(glitchChars.Length)] : c));

                var message = CreateFormattedMessage(glitchedText, style);
                _richTextLabels[i].SetMessage(message);
            }
        }
    }

    private void UpdateSlideAnimation()
    {
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;
        var enhancements = style.AnimationEnhancements;
        if (enhancements?.EnableSlide != true)
            return;

        var duration = enhancements.SlideDuration;
        var progress = Math.Min(ActiveAnnouncement.SlideTimer / duration, 1.0f);

        var startPos = ActiveAnnouncement.SlideStartPosition;
        var currentOffset = Vector2.Lerp(startPos, Vector2.Zero, progress);
        ActiveAnnouncement.CurrentSlideOffset = currentOffset;

        if (progress >= 1.0f)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            SetAllLabelsText();
        }
    }

    private void UpdateZoomAnimation()
    {
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;
        var enhancements = style.AnimationEnhancements;
        if (enhancements?.EnableZoom != true)
            return;

        var duration = enhancements.ZoomDuration;
        var progress = Math.Min(ActiveAnnouncement.ZoomTimer / duration, 1.0f);

        var startScale = enhancements.ZoomStartScale;
        var currentScale = startScale + (1.0f - startScale) * progress;
        ActiveAnnouncement.ZoomCurrentScale = currentScale;

        if (progress >= 1.0f)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            SetAllLabelsText();
        }
    }

    private void UpdateBounceAnimation()
    {
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;
        var enhancements = style.AnimationEnhancements;
        if (enhancements?.EnableBounce != true)
            return;

        var bounceCount = enhancements.BounceCount;
        var bounceHeight = enhancements.BounceHeight;
        const float cycleDuration = 0.5f;
        var totalDuration = bounceCount * cycleDuration;

        if (ActiveAnnouncement.BounceTimer >= totalDuration)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            ActiveAnnouncement.CurrentBounceOffset = Vector2.Zero;
            SetAllLabelsText();
            return;
        }

        var cycleProgress = (ActiveAnnouncement.BounceTimer % cycleDuration) / cycleDuration;
        var bounceY = MathF.Sin(cycleProgress * MathF.PI) * bounceHeight;
        ActiveAnnouncement.CurrentBounceOffset = new Vector2(0, -bounceY);
    }

    private void UpdateFadeAnimation()
    {
        if (ActiveAnnouncement == null)
            return;

        const float duration = 2.0f;
        var progress = Math.Min(ActiveAnnouncement.FadeTimer / duration, 1.0f);

        ActiveAnnouncement.FadeAlpha = progress;

        if (progress >= 1.0f)
        {
            ActiveAnnouncement.State = AnnouncementState.Holding;
            SetAllLabelsText();
        }
    }

    private void UpdatePulseAnimation()
    {
        if (ActiveAnnouncement == null)
            return;

        const float pulseSpeed = 2.0f;
        const float pulseIntensity = 0.3f;

        var pulseValue = MathF.Sin(ActiveAnnouncement.PulseTimer * pulseSpeed);
        ActiveAnnouncement.PulseScale = 1.0f + (pulseValue * pulseIntensity);
        ActiveAnnouncement.PulseAlpha = 0.7f + (pulseValue * 0.3f);

        SetAllLabelsText();
    }

    private void UpdateHoldingState(float elapsed)
    {
        if (ActiveAnnouncement == null)
            return;

        var holdDuration = ActiveAnnouncement.Data.Style.HoldDuration;
        if (elapsed >= GetAnimationDuration() + holdDuration)
            FinishAnnouncement();
    }

    private void SetAllLabelsText()
    {
        if (ActiveAnnouncement == null)
            return;

        var originalText = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.Data.Style;

        for (var i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < originalText.Length)
            {
                var message = CreateFormattedMessage(originalText[textIndex], style);
                _richTextLabels[i].SetMessage(message);
            }
        }
    }

    private void ApplyVisualEffects(TimeSpan currentTime)
    {
        if (ActiveAnnouncement == null)
            return;

        var style = ActiveAnnouncement.Data.Style;

        foreach (var label in _richTextLabels)
        {
            var baseColor = style.PrimaryColor;

            if (style.SpriteGlow)
                baseColor = ApplyGlow(baseColor, style.SpriteGlowIntensity, currentTime);

            if (style.FlickerChance > 0)
                baseColor = ApplyFlicker(baseColor, style.FlickerChance, currentTime);

            if (ActiveAnnouncement.Data.Style.Animation == AnnouncementAnimation.Fade)
                baseColor = new Color(baseColor.R, baseColor.G, baseColor.B, baseColor.A * ActiveAnnouncement.FadeAlpha);

            if (ActiveAnnouncement.Data.Style.Animation == AnnouncementAnimation.Pulse)
                baseColor = new Color(baseColor.R, baseColor.G, baseColor.B, baseColor.A * ActiveAnnouncement.PulseAlpha);

            label.Modulate = baseColor;
        }
    }

    private static Color ApplyGlow(Color baseColor, float intensity, TimeSpan currentTime)
    {
        var time = (float) currentTime.TotalSeconds;
        var glow = MathF.Sin(time * 3f) * 0.5f + 0.5f;
        var glowFactor = 1.0f + (glow * intensity);

        return new Color(
            Math.Min(baseColor.R * glowFactor, 1.0f),
            Math.Min(baseColor.G * glowFactor, 1.0f),
            Math.Min(baseColor.B * glowFactor, 1.0f),
            baseColor.A
        );
    }

    private static Color ApplyFlicker(Color baseColor, float flickerChance, TimeSpan currentTime)
    {
        var time = (float) currentTime.TotalSeconds;
        var noise = MathF.Sin(time * 100f) * 0.5f + 0.5f;
        if (noise < flickerChance)
        {
            return new Color(
                baseColor.R * 0.3f,
                baseColor.G * 0.3f,
                baseColor.B * 0.3f,
                baseColor.A
            );
        }

        return baseColor;
    }

    private bool RandomChance(float probability)
    {
        return _random.NextFloat() < probability;
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

        if (ActiveAnnouncement.Data.Style.AnimationEnhancements?.EnableZoom == true)
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

    private float GetAnimationDuration()
    {
        if (ActiveAnnouncement == null)
            return 0f;

        var style = ActiveAnnouncement.Data.Style;
        var cleanText = ActiveAnnouncement.CleanText;

        return style.Animation switch
        {
            AnnouncementAnimation.Typewriter => cleanText.Sum(line => line.Length) * style.PrintSpeed,
            AnnouncementAnimation.Glitch => cleanText.Sum(line => line.Length) * style.PrintSpeed * 0.5f,
            AnnouncementAnimation.Slide => style.AnimationEnhancements?.SlideDuration ?? 1.0f,
            AnnouncementAnimation.Zoom => style.AnimationEnhancements?.ZoomDuration ?? 1.0f,
            AnnouncementAnimation.Bounce => (style.AnimationEnhancements?.BounceCount ?? 3) * 0.5f,
            _ => 0f
        };
    }

    private Vector2 GetSlideStartPosition(AnnouncementStyle style)
    {
        if (style.AnimationEnhancements?.EnableSlide != true)
            return Vector2.Zero;

        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);

        return style.AnimationEnhancements.SlideFrom switch
        {
            SlideDirection.Left => new Vector2(-screenSize.X, 0),
            SlideDirection.Right => new Vector2(screenSize.X, 0),
            SlideDirection.Top => new Vector2(0, -screenSize.Y),
            SlideDirection.Bottom => new Vector2(0, screenSize.Y),
            _ => Vector2.Zero
        };
    }
}
