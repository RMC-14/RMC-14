using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class TitleAssaultScrollEffect : IAnnouncementVisualEffect
{
    private int _debugFrameCount;

    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        var titleLabels = context.Output.TitleLabels;
        if (titleLabels.Length < 2)
            return;

        var contentWidth = context.Output.TitleContentWidth;
        var gap = context.Output.TitleScrollGap;
        var period = contentWidth + gap;
        if (period <= 0f)
            return;

        var speed = context.Style.TitleConfig.Effect.Speed;
        var elapsed = (float)(currentTime - context.Output.StartTime).TotalSeconds;
        var offset = elapsed * speed % period;

        var x1 = -offset;
        LayoutContainer.SetMarginLeft(titleLabels[0], x1);
        LayoutContainer.SetMarginRight(titleLabels[0], x1 + contentWidth);

        var x2 = period - offset;
        LayoutContainer.SetMarginLeft(titleLabels[1], x2);
        LayoutContainer.SetMarginRight(titleLabels[1], x2 + contentWidth);

        _debugFrameCount++;
        if (_debugFrameCount <= 5 || _debugFrameCount % 60 == 0)
        {
            var expectedSingleLineHeight = context.Output.TitleRenderedFontSize * 1.4f;
            var label0Height = titleLabels[0].Size.Y > 0f ? titleLabels[0].Size.Y : titleLabels[0].DesiredSize.Y;
            var isWrapping = label0Height > expectedSingleLineHeight;
            var trackSize = context.Output.TitleTrack?.Size ?? default;
            var label0MaxWidth = titleLabels[0].MaxWidth;

            Logger.Debug(
                $"[AssaultScroll] frame={_debugFrameCount}" +
                $" contentWidth={contentWidth:F1}" +
                $" gap={gap:F1}" +
                $" period={period:F1}" +
                $" offset={offset:F1}" +
                $" x1={x1:F1} x2={x2:F1}" +
                $" label0.Size={titleLabels[0].Size}" +
                $" label0.DesiredSize={titleLabels[0].DesiredSize}" +
                $" label0.MaxWidth={label0MaxWidth:F1}" +
                $" label1.Size={titleLabels[1].Size}" +
                $" label1.MaxWidth={titleLabels[1].MaxWidth:F1}" +
                $" TitleTrack.Size={context.Output.TitleTrack?.Size}" +
                $" TitleRenderedFontSize={context.Output.TitleRenderedFontSize:F1}" +
                $" TitleViewportWidth={context.Output.TitleViewportWidth:F1}" +
                $" expectedSingleLineH={expectedSingleLineHeight:F1}" +
                $" WRAPPING={isWrapping}" +
                $" label0RightEdge={x1 + contentWidth:F1} viewportW={trackSize.X:F1}" +
                $" labelExceedsViewport={x1 + contentWidth > trackSize.X && x1 >= 0f}");
        }
    }
}
