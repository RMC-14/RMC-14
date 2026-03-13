using System;
using Content.Client._RMC14.Announce.Styling;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class TitleAssaultScrollEffect : IAnnouncementVisualEffect
{
    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        if (!context.HasTitle || context.Labels.Count == 0)
            return;

        var titleLabel = context.Labels[0];
        var titleText = context.State.TitleText;
        if (string.IsNullOrEmpty(titleText))
            return;

        titleLabel.Modulate = context.Style.TitleConfig.TitleColor;

        var viewportWidth = MathF.Max(context.State.TitleViewportWidth, titleLabel.DesiredSize.X);
        var fontSize = MathF.Max(1f, context.State.TitleRenderedFontSize);
        var approxVisibleChars = context.State.TitleContentWidth <= viewportWidth
            ? titleText.Length
            : Math.Max(8, (int) MathF.Floor(viewportWidth / MathF.Max(1f, fontSize * 1.25f)));

        var spacerChars = Math.Max(4, (int) MathF.Ceiling(context.Style.TitleConfig.Effect.Gap / MathF.Max(1f, fontSize * 0.55f)));
        var spacer = new string(' ', spacerChars);
        var loop = titleText + spacer + titleText + spacer;

        var charsPerSecond = MathF.Max(1f, context.Style.TitleConfig.Effect.Speed / MathF.Max(1f, fontSize * 0.55f));
        var elapsed = (float) (currentTime - context.State.StartTime).TotalSeconds;
        var startIndex = (int) (elapsed * charsPerSecond) % (titleText.Length + spacerChars);

        while (loop.Length < startIndex + approxVisibleChars)
        {
            loop += titleText + spacer;
        }

        var visible = loop.Substring(startIndex, approxVisibleChars).Replace(' ', '\u00A0');
        titleLabel.SetMessage(AnnouncementStyling.CreateFormattedMessage(
            visible,
            fontSize,
            context.Style.TitleConfig.TitleColor,
            context.Style.TitleConfig.TitleFont));
    }
}
