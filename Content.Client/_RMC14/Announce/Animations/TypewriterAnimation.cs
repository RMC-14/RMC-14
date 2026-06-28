using System;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class TypewriterAnimation : IAnnouncementAnimation
{
    private int _currentLine;
    private int _currentChar;
    private float _timer;

    public void Reset(AnnouncementAnimationContext context)
    {
        _currentLine = 0;
        _currentChar = 0;
        _timer = 0f;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            context.Labels[i].SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
        }
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var style = context.Style;

        _timer += deltaTime;
        if (_timer < style.AnimationConfig.PrintSpeed)
            return AnnouncementAnimationStatus.Running;

        _timer = 0f;

        var cleanText = context.CleanText;

        if (_currentLine >= cleanText.Length)
            return AnnouncementAnimationStatus.Finished;

        var lineText = cleanText[_currentLine];
        if (_currentChar >= lineText.Length)
        {
            _currentLine++;
            _currentChar = 0;
            return _currentLine >= cleanText.Length
                ? AnnouncementAnimationStatus.Finished
                : AnnouncementAnimationStatus.Running;
        }

        _currentChar++;
        UpdateDisplay(context);
        return AnnouncementAnimationStatus.Running;
    }

    private void UpdateDisplay(AnnouncementAnimationContext context)
    {
        var cleanText = context.CleanText;
        var originalText = context.OriginalText;
        var style = context.Style;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            var textIndex = i - context.TitleOffset;
            if (textIndex < _currentLine)
            {
                var message = context.FormatMessage(originalText[textIndex], style);
                context.Labels[i].SetMessage(message);
            }
            else if (textIndex == _currentLine)
            {
                var currentLineText = cleanText[textIndex];
                var maxLength = Math.Min(_currentChar, currentLineText.Length);
                var partialText = currentLineText[..maxLength];
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
