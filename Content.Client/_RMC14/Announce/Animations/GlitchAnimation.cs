using Content.Shared._RMC14.Announce;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using System;
using System.Linq;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class GlitchAnimation : IAnnouncementAnimation
{
    private static readonly char[] GlitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~".ToCharArray();

    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.CurrentLine = 0;
        context.State.CurrentChar = 0;
        context.State.GlitchTimer = 0f;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            context.Labels[i].SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
        }
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var style = context.Style;
        var printSpeed = style.PrintSpeed * 0.5f;

        context.State.GlitchTimer += deltaTime;
        if (context.State.GlitchTimer >= printSpeed)
        {
            context.State.GlitchTimer = 0f;
            var finished = Advance(context);
            if (finished)
                return AnnouncementAnimationStatus.Finished;
        }

        if (RandomChance(context.Random, style.GlitchChance))
            ApplyGlitchEffect(context);

        return AnnouncementAnimationStatus.Running;
    }

    private static bool Advance(AnnouncementAnimationContext context)
    {
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
        var originalText = context.OriginalText;
        var cleanText = context.CleanText;
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

    private static void ApplyGlitchEffect(AnnouncementAnimationContext context)
    {
        var style = context.Style;
        var cleanText = context.CleanText;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            var textIndex = i - context.TitleOffset;
            if (textIndex <= context.State.CurrentLine && RandomChance(context.Random, 0.1f))
            {
                var originalText = cleanText[textIndex];
                var glitchedText = string.Join("", originalText.Select(c =>
                    RandomChance(context.Random, 0.05f) ? GlitchChars[context.Random.Next(GlitchChars.Length)] : c));

                var message = context.FormatMessage(glitchedText, style);
                context.Labels[i].SetMessage(message);
            }
        }
    }

    private static bool RandomChance(IRobustRandom random, float probability)
    {
        return random.NextFloat() < probability;
    }
}
