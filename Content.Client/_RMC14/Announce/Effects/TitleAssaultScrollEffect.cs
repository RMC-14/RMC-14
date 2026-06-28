using System;
using System.Text;
using Content.Client._RMC14.Announce.Styling;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class TitleAssaultScrollEffect : IAnnouncementVisualEffect
{
    private string _cachedTitleText = string.Empty;
    private int _cachedSpacerChars = -1;
    private int _cachedApproxVisibleChars = -1;
    private string _cachedLoop = string.Empty;
    private int _cachedPeriod;

    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        if (!context.HasTitle || context.Labels.Count == 0)
            return;

        var titleLabel = context.Labels[0];
        var titleText = context.Output.TitleText;
        if (string.IsNullOrEmpty(titleText))
            return;

        titleLabel.Modulate = context.Style.TitleConfig.TitleColor;

        var viewportWidth = MathF.Max(context.Output.TitleViewportWidth, titleLabel.DesiredSize.X);
        var fontSize = MathF.Max(1f, context.Output.TitleRenderedFontSize);
        var approxVisibleChars = context.Output.TitleContentWidth <= viewportWidth
            ? titleText.Length
            : Math.Max(8, (int) MathF.Floor(viewportWidth / MathF.Max(1f, fontSize * 1.25f)));

        var spacerChars = Math.Max(4, (int) MathF.Ceiling(context.Style.TitleConfig.Effect.Gap / MathF.Max(1f, fontSize * 0.55f)));

        if (titleText != _cachedTitleText || spacerChars != _cachedSpacerChars || approxVisibleChars != _cachedApproxVisibleChars)
            RebuildCache(titleText, spacerChars, approxVisibleChars);

        var charsPerSecond = MathF.Max(1f, context.Style.TitleConfig.Effect.Speed / MathF.Max(1f, fontSize * 0.55f));
        var elapsed = (float) (currentTime - context.Output.StartTime).TotalSeconds;
        var startIndex = (int) (elapsed * charsPerSecond) % _cachedPeriod;

        var visible = _cachedLoop[startIndex..(startIndex + approxVisibleChars)];
        titleLabel.SetMessage(AnnouncementStyling.CreateFormattedMessage(
            visible,
            fontSize,
            context.Style.TitleConfig.TitleColor,
            context.Style.TitleConfig.TitleFont));
    }

    private void RebuildCache(string titleText, int spacerChars, int approxVisibleChars)
    {
        _cachedTitleText = titleText;
        _cachedSpacerChars = spacerChars;
        _cachedApproxVisibleChars = approxVisibleChars;

        var spacer = new string(' ', spacerChars);
        _cachedPeriod = titleText.Length + spacerChars;

        var minLength = _cachedPeriod + approxVisibleChars;
        var sb = new StringBuilder(minLength + _cachedPeriod);
        while (sb.Length < minLength)
            sb.Append(titleText).Append(spacer);

        _cachedLoop = sb.ToString().Replace(' ', ' ');
    }
}
