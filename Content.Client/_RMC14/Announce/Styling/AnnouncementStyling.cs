using Content.Shared._RMC14.Announce;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using System.Numerics;
using static Robust.Client.UserInterface.Control;

namespace Content.Client._RMC14.Announce.Styling;

public static class AnnouncementStyling
{
    public static AnnouncementStyle CreateDisplayStyle(AnnouncementStyle baseStyle, float visualScale)
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();
        var style = serialization.CreateCopy(baseStyle, notNullableOverride: true)!;
        var scale = MathF.Max(0.1f, visualScale);

        style.LayoutConfig.UIScale = baseStyle.LayoutConfig.UIScale;
        style.LayoutConfig.SpriteSpacing *= scale;
        style.LayoutConfig.SpriteClipOffset *= scale;
        style.LayoutConfig.SpriteClipSize *= scale;

        style.TextConfig.FontSize *= scale;
        style.TextConfig.LineHeight *= scale;
        style.TextConfig.SpeakerNameFontSize *= scale;

        style.SpriteConfig.SpriteBoxBorderThickness *= scale;
        style.SpriteConfig.SpriteBoxPadding *= scale;
        style.SpriteConfig.SpriteScale *= scale;

        style.TitleConfig.TitleFontSize *= scale;
        style.TitleConfig.TitleUnderlineThickness *= scale;

        style.AnimationConfig.AnimationEnhancements = serialization.CreateCopy(baseStyle.AnimationConfig.AnimationEnhancements, notNullableOverride: true)!;
        style.AnimationConfig.AnimationEnhancements.BounceHeight *= scale;

        return style;
    }

    public static void ApplyLocalAppearanceOverrides(AnnouncementStyle style, AnnouncementDisplayData display)
    {
        if (display.ShowTitleOverride is { } showTitle)
            style.TitleConfig.ShowTitle = showTitle;

        if (display.BodyTextScaleOverride is { } bodyTextScale)
        {
            style.TextConfig.FontSize *= MathF.Max(0.1f, bodyTextScale);
            style.TextConfig.LineHeight *= MathF.Max(0.1f, bodyTextScale);
        }

        if (display.TitleTextScaleOverride is { } titleTextScale)
        {
            style.TitleConfig.TitleFontSize *= MathF.Max(0.1f, titleTextScale);
            style.TitleConfig.TitleUnderlineThickness *= MathF.Max(0.1f, titleTextScale);
        }

        if (display.TextColorOverride is { } textColor)
        {
            style.TextConfig.PrimaryColor = textColor;
            style.TextConfig.SpeakerNameColor = textColor;
        }

        if (display.TitleColorOverride is { } titleColor)
            style.TitleConfig.TitleColor = titleColor;

        if (display.SpriteBoxColorOverride is { } spriteBoxColor)
            style.SpriteConfig.SpriteBoxColor = spriteBoxColor;

        if (display.SpriteBoxBorderColorOverride is { } spriteBoxBorderColor)
            style.SpriteConfig.SpriteBoxBorderColor = spriteBoxBorderColor;

        if (display.BackgroundColorOverride is { } bgColor)
            style.BackgroundConfig.BackgroundColor = bgColor;

        if (display.CRTGlowColorOverride is { } crtGlowColor)
        {
            var enhancements = style.AnimationConfig.AnimationEnhancements;
            if (enhancements != null && enhancements.EnableCRT)
            {
                enhancements.CRTSettings ??= new CRTSettings();
                enhancements.CRTSettings.GlowColor = crtGlowColor;
            }
        }
    }

    public static AnnouncementStyle CreateResponsiveStyle(AnnouncementStyle baseStyle, float responsiveFontSize, Vector2 screenSize)
    {
        var scaleFactor = CalculateScreenScaleFactor(screenSize);
        var serialization = IoCManager.Resolve<ISerializationManager>();
        var style = serialization.CreateCopy(baseStyle, notNullableOverride: true)!;

        style.TextConfig.FontSize = responsiveFontSize;
        style.TextConfig.LineHeight = baseStyle.TextConfig.LineHeight * scaleFactor;
        style.LayoutConfig.SpriteSpacing = baseStyle.LayoutConfig.SpriteSpacing * scaleFactor;
        style.SpriteConfig.SpriteBoxBorderThickness = baseStyle.SpriteConfig.SpriteBoxBorderThickness * scaleFactor;
        style.SpriteConfig.SpriteBoxPadding = baseStyle.SpriteConfig.SpriteBoxPadding * scaleFactor;
        style.TextConfig.SpeakerNameFontSize = baseStyle.TextConfig.SpeakerNameFontSize * scaleFactor;
        style.AnimationConfig.AnimationEnhancements = serialization.CreateCopy(baseStyle.AnimationConfig.AnimationEnhancements, notNullableOverride: true)!;

        return style;
    }

    public static float CalculateScreenScaleFactor(Vector2 screenSize)
    {
        var baseResolution = new Vector2(1920f, 1080f);
        var scaleX = screenSize.X / baseResolution.X;
        var scaleY = screenSize.Y / baseResolution.Y;
        var avgScale = (scaleX + scaleY) * 0.5f;

        return MathHelper.Clamp(avgScale, 0.5f, 2.0f);
    }

    public static Vector2 GetPositionFromStyle(AnnouncementPosition position, Vector2 screenSize, Vector2 contentSize)
    {
        return position switch
        {
            AnnouncementPosition.TopLeft => new Vector2(50f, 100f),
            AnnouncementPosition.TopCenter => new Vector2((screenSize.X - contentSize.X) * 0.5f, 50f),
            AnnouncementPosition.TopRight => new Vector2(screenSize.X - contentSize.X - 50f, 100f),
            AnnouncementPosition.MiddleLeft => new Vector2(50f, (screenSize.Y - contentSize.Y) * 0.5f),
            AnnouncementPosition.MiddleCenter => new Vector2((screenSize.X - contentSize.X) * 0.5f, (screenSize.Y - contentSize.Y) * 0.5f),
            AnnouncementPosition.MiddleRight => new Vector2(screenSize.X - contentSize.X - 50f, (screenSize.Y - contentSize.Y) * 0.5f),
            AnnouncementPosition.BottomLeft => new Vector2(50f, screenSize.Y - contentSize.Y - 50f),
            AnnouncementPosition.BottomCenter => new Vector2((screenSize.X - contentSize.X) * 0.5f, screenSize.Y - contentSize.Y - 50f),
            AnnouncementPosition.BottomRight => new Vector2(screenSize.X - contentSize.X - 50f, screenSize.Y - contentSize.Y - 50f),
            _ => new Vector2((screenSize.X - contentSize.X) * 0.5f, (screenSize.Y - contentSize.Y) * 0.5f)
        };
    }

    public static float CalculateMaxTextWidth(Vector2 screenSize, AnnouncementPosition position)
    {
        return position switch
        {
            AnnouncementPosition.FullScreen => screenSize.X * 0.9f,
            AnnouncementPosition.TopCenter or
            AnnouncementPosition.MiddleCenter or
            AnnouncementPosition.BottomCenter => screenSize.X * 0.6f,
            _ => screenSize.X * 0.4f
        };
    }

    public static float CalculateResponsiveFontSize(string[] text, float baseFontSize, float maxWidth, Vector2? screenSize, AnnouncementStyle? style = null)
    {
        if (text.Length == 0)
            return baseFontSize;

        if (style?.ScalingConfig.EnableResponsiveScaling == false)
            return baseFontSize;

        var totalWordCount = 0;
        var totalCharCount = 0;
        var longestLineLength = 0;
        foreach (var line in text)
        {
            totalWordCount += CountWords(line);
            totalCharCount += line.Length;
            if (line.Length > longestLineLength)
                longestLineLength = line.Length;
        }

        var estimatedWidth = longestLineLength * baseFontSize * 0.6f;

        var widthScaleFactor = 1.0f;
        if (estimatedWidth > maxWidth)
        {
            widthScaleFactor = maxWidth / estimatedWidth;
        }

        var wordCountScaleFactor = CalculateWordCountScaleFactor(totalWordCount, totalCharCount);

        // Width is the primary constraint; word-count pressure is intentionally softened.
        var softenedWordCountScale = 1f - ((1f - wordCountScaleFactor) * 0.10f);
        var combinedScaleFactor = widthScaleFactor * softenedWordCountScale;

        if (screenSize.HasValue)
        {
            var screenScaleFactor = CalculateScreenScaleFactor(screenSize.Value);
            combinedScaleFactor *= screenScaleFactor;
        }

        var scalingFactor = style?.ScalingConfig.ResponsiveScaleFactor ?? 1.0f;
        combinedScaleFactor *= scalingFactor;

        var finalFontSize = baseFontSize * combinedScaleFactor;
        var minScale = style?.ScalingConfig.MinScale ?? 0.4f;
        var maxScale = style?.ScalingConfig.MaxScale ?? 1.5f;
        var minFontSize = baseFontSize * minScale;
        var maxFontSize = baseFontSize * maxScale;

        return MathHelper.Clamp(finalFontSize, minFontSize, maxFontSize);
    }

    private static readonly char[] WordSeparators = { ' ', '\t', '\n', '\r' };

    private static float CalculateWordCountScaleFactor(int wordCount, int charCount)
    {
        if (wordCount <= 0 || charCount <= 0)
            return 1.0f;

        var wordFactor = 1f + (wordCount / 18f);
        var charFactor = 1f + (charCount / 240f);
        var combined = MathF.Pow(wordFactor * charFactor, 0.35f);
        var scale = 1f / combined;

        return MathHelper.Clamp(scale, 0.65f, 1.0f);
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static float CalculateOptimalTextWidth(string[] text, AnnouncementStyle style, Vector2 screenSize)
    {
        var totalWordCount = 0;
        foreach (var line in text)
            totalWordCount += CountWords(line);
        var maxLineWidth = CalculateMaxTextWidth(screenSize, style.LayoutConfig.Position);

        if (totalWordCount <= 5)
        {
            return Math.Min(maxLineWidth, screenSize.X * 0.3f);
        }

        if (totalWordCount <= 20)
        {
            return Math.Min(maxLineWidth, screenSize.X * 0.5f);
        }

        return maxLineWidth;
    }

    public static string CreateFormattedTextWithSize(string text, float fontSize, Color color, string? font = null)
    {
        var colorHex = ColorToHex(color);
        if (!string.IsNullOrEmpty(font))
            return $"[font=\"{font}\" size={(int)fontSize}][color={colorHex}]{text}[/color][/font]";

        return $"[font size={(int)fontSize}][color={colorHex}]{text}[/color][/font]";
    }

    private static string ColorToHex(Color color)
    {
        var r = (int)(color.R * 255);
        var g = (int)(color.G * 255);
        var b = (int)(color.B * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public static FormattedMessage CreateFormattedMessage(string text, float fontSize, Color color, string? font = null)
    {
        var formattedText = CreateFormattedTextWithSize(text, fontSize, color, font);
        return FormattedMessage.FromMarkupPermissive(formattedText);
    }
}

