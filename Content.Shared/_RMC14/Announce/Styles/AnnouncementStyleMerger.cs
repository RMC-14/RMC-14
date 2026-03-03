using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.Announce;

public static class AnnouncementStyleMerger
{
    public static AnnouncementStyle Merge(AnnouncementStyle baseStyle, AnnouncementStyleOverride? overrideStyle)
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();
        var merged = serialization.CreateCopy(baseStyle, notNullableOverride: true)!;

        if (overrideStyle != null)
        {
            merged.AnimationConfig.Animation = overrideStyle.Animation ?? merged.AnimationConfig.Animation;
            merged.AnimationConfig.AnimationEnhancements =
                overrideStyle.AnimationEnhancements == null
                    ? merged.AnimationConfig.AnimationEnhancements
                    : serialization.CreateCopy(overrideStyle.AnimationEnhancements, notNullableOverride: true)!;

            merged.TextConfig.PrimaryColor = overrideStyle.PrimaryColor ?? merged.TextConfig.PrimaryColor;
            merged.TitleConfig.TitleColor = overrideStyle.TitleColor ?? merged.TitleConfig.TitleColor;
            merged.BackgroundConfig.BackgroundColor = overrideStyle.BackgroundColor ?? merged.BackgroundConfig.BackgroundColor;
            merged.BackgroundConfig.BackgroundAlpha = overrideStyle.BackgroundAlpha ?? merged.BackgroundConfig.BackgroundAlpha;

            merged.LayoutConfig.Position = overrideStyle.Position ?? merged.LayoutConfig.Position;
            merged.LayoutConfig.SpritePosition = overrideStyle.SpritePosition ?? merged.LayoutConfig.SpritePosition;

            merged.TextConfig.ShowSpeakerName = overrideStyle.ShowSpeakerName ?? merged.TextConfig.ShowSpeakerName;
            merged.TextConfig.SpeakerNameColor = overrideStyle.SpeakerNameColor ?? merged.TextConfig.SpeakerNameColor;
            merged.TextConfig.SpeakerNameFontSize = overrideStyle.SpeakerNameFontSize ?? merged.TextConfig.SpeakerNameFontSize;
            merged.LayoutConfig.SpeakerNamePosition = overrideStyle.SpeakerNamePosition ?? merged.LayoutConfig.SpeakerNamePosition;

            merged.SpriteConfig.SpriteScale = overrideStyle.SpriteScale ?? merged.SpriteConfig.SpriteScale;
            merged.LayoutConfig.SpriteSpacing = overrideStyle.SpriteSpacing ?? merged.LayoutConfig.SpriteSpacing;
        }

        return merged;
    }
}
