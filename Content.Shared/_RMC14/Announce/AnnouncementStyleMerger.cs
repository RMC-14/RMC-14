using System;
using System.Collections.Generic;

namespace Content.Shared._RMC14.Announce;

public static class AnnouncementStyleMerger
{
    public static AnnouncementStyle Merge(AnnouncementStyle baseStyle, AnnouncementStyle overrideStyle)
    {
        return baseStyle with
        {
            Animation = Pick(baseStyle.Animation, overrideStyle.Animation, v => v != AnnouncementAnimation.Typewriter),
            Position = Pick(baseStyle.Position, overrideStyle.Position, v => v != AnnouncementPosition.MiddleCenter),
            ShowBackground = Pick(baseStyle.ShowBackground, overrideStyle.ShowBackground),
            BackgroundAlpha = Pick(baseStyle.BackgroundAlpha, overrideStyle.BackgroundAlpha),
            BackgroundColor = Pick(baseStyle.BackgroundColor, overrideStyle.BackgroundColor),
            PrimaryColor = Pick(baseStyle.PrimaryColor, overrideStyle.PrimaryColor),
            SecondaryColor = overrideStyle.SecondaryColor ?? baseStyle.SecondaryColor,
            AccentColor = overrideStyle.AccentColor ?? baseStyle.AccentColor,
            FontSize = Pick(baseStyle.FontSize, overrideStyle.FontSize),
            LineHeight = Pick(baseStyle.LineHeight, overrideStyle.LineHeight),
            PrintSpeed = Pick(baseStyle.PrintSpeed, overrideStyle.PrintSpeed),
            HoldDuration = Pick(baseStyle.HoldDuration, overrideStyle.HoldDuration),
            ShakeIntensity = Pick(baseStyle.ShakeIntensity, overrideStyle.ShakeIntensity),
            FlickerChance = Pick(baseStyle.FlickerChance, overrideStyle.FlickerChance),
            GlitchChance = Pick(baseStyle.GlitchChance, overrideStyle.GlitchChance),
            ShowSpriteBox = Pick(baseStyle.ShowSpriteBox, overrideStyle.ShowSpriteBox),
            SpriteBoxColor = Pick(baseStyle.SpriteBoxColor, overrideStyle.SpriteBoxColor),
            SpriteBoxBorderColor = Pick(baseStyle.SpriteBoxBorderColor, overrideStyle.SpriteBoxBorderColor),
            SpriteBoxBorderThickness = Pick(baseStyle.SpriteBoxBorderThickness, overrideStyle.SpriteBoxBorderThickness),
            SpriteBoxPadding = Pick(baseStyle.SpriteBoxPadding, overrideStyle.SpriteBoxPadding),
            SpriteGlow = Pick(baseStyle.SpriteGlow, overrideStyle.SpriteGlow),
            SpriteGlowColor = Pick(baseStyle.SpriteGlowColor, overrideStyle.SpriteGlowColor),
            SpriteGlowIntensity = Pick(baseStyle.SpriteGlowIntensity, overrideStyle.SpriteGlowIntensity),
            ShowSpeakerName = Pick(baseStyle.ShowSpeakerName, overrideStyle.ShowSpeakerName),
            SpeakerNameColor = Pick(baseStyle.SpeakerNameColor, overrideStyle.SpeakerNameColor),
            SpeakerNameFontSize = Pick(baseStyle.SpeakerNameFontSize, overrideStyle.SpeakerNameFontSize),
            SpeakerNamePosition = Pick(baseStyle.SpeakerNamePosition, overrideStyle.SpeakerNamePosition),
            SpritePosition = Pick(baseStyle.SpritePosition, overrideStyle.SpritePosition),
            SpriteSpacing = Pick(baseStyle.SpriteSpacing, overrideStyle.SpriteSpacing),
            AnimationEnhancements = overrideStyle.AnimationEnhancements ?? baseStyle.AnimationEnhancements,
            TextEnhancements = overrideStyle.TextEnhancements ?? baseStyle.TextEnhancements,
            BackgroundStyle = overrideStyle.BackgroundStyle ?? baseStyle.BackgroundStyle,
            EnableScreenShake = Pick(baseStyle.EnableScreenShake, overrideStyle.EnableScreenShake),
            ShakeDuration = Pick(baseStyle.ShakeDuration, overrideStyle.ShakeDuration),
            EnableFlash = Pick(baseStyle.EnableFlash, overrideStyle.EnableFlash),
            FlashColor = Pick(baseStyle.FlashColor, overrideStyle.FlashColor),
            FlashDuration = Pick(baseStyle.FlashDuration, overrideStyle.FlashDuration),
            FlashCount = Pick(baseStyle.FlashCount, overrideStyle.FlashCount)
        };
    }

    private static T Pick<T>(T baseValue, T overrideValue)
    {
        return !EqualityComparer<T>.Default.Equals(baseValue, overrideValue) ? overrideValue : baseValue;
    }

    private static T Pick<T>(T baseValue, T overrideValue, Func<T, bool> useOverridePredicate)
    {
        return useOverridePredicate(overrideValue) ? overrideValue : baseValue;
    }
}
