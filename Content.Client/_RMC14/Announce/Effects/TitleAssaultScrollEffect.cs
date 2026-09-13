using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce.Effects;

public sealed class TitleAssaultScrollEffect : IAnnouncementVisualEffect
{
    public void Apply(AnnouncementEffectContext context, TimeSpan currentTime)
    {
        var titleLabels = context.Output.TitleLabels;
        if (titleLabels.Length < 1)
            return;

        var contentWidth = context.Output.TitleContentWidth;
        var gap = context.Output.TitleScrollGap;
        var n = titleLabels.Length;
        var slotWidth = contentWidth + gap;
        var period = n * slotWidth;
        if (period <= 0f)
            return;

        var speed = context.Style.TitleConfig.Effect.Speed;
        var elapsed = (float)(currentTime - context.Output.StartTime).TotalSeconds;
        var offset = elapsed * speed % period;

        for (var i = 0; i < n; i++)
        {
            var xi = i * slotWidth - offset;
            // When a label scrolls fully off the left, wrap it to the right end of the belt
            if (xi <= -contentWidth)
                xi += period;
            LayoutContainer.SetMarginLeft(titleLabels[i], xi);
            LayoutContainer.SetMarginRight(titleLabels[i], xi + contentWidth);
        }
    }
}
