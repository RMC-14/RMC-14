using Content.Shared._RMC14.Announce;
using Robust.Shared.Utility;
using System;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class TypewriterAnimation : IAnnouncementAnimation
{
    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.CurrentLine = 0;
        context.State.CurrentChar = 0;
        context.State.TypewriterTimer = 0f;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            context.Labels[i].SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
        }
    }

    public bool Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var style = context.Style;

        context.State.TypewriterTimer += deltaTime;
        if (context.State.TypewriterTimer < style.PrintSpeed)
            return false;

        context.State.TypewriterTimer = 0f;

        var cleanText = context.CleanText;
        var currentLine = context.State.CurrentLine;
        var currentChar = context.State.CurrentChar;

        if (currentLine >= cleanText.Length)
            return true;

        var lineText = cleanText[currentLine];
        if (currentChar >= lineText.Length)
        {
            context.State.CurrentLine++;
            context.State.CurrentChar = 0;
            return context.State.CurrentLine >= cleanText.Length;
        }

        context.State.CurrentChar++;
        UpdateDisplay(context);
        return false;
    }

    private static void UpdateDisplay(AnnouncementAnimationContext context)
    {
        var cleanText = context.CleanText;
        var originalText = context.OriginalText;
        var style = context.Style;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            var textIndex = i - context.TitleOffset;
            if (textIndex < context.State.CurrentLine)
            {
                var message = context.FormatMessage(originalText[textIndex], style);
                context.Labels[i].SetMessage(message);
            }
            else if (textIndex == context.State.CurrentLine)
            {
                var currentLineText = cleanText[textIndex];
                var maxLength = Math.Min(context.State.CurrentChar, currentLineText.Length);
                var partialText = currentLineText.Substring(0, maxLength);
                var message = context.FormatMessage(partialText, style);
                context.Labels[i].SetMessage(message);
            }
            else
            {
                context.Labels[i].SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
            }
        }
    }
}
