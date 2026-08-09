using Content.Shared._RMC14.Announce.Animations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class TypewriterAnimation : IAnnouncementAnimation
{
    private const float MinTickInterval = 0.005f;
    private const int MaxAdvancePerUpdate = 8;

    private readonly TypewriterAnimationConfig _config;
    private int _currentLine;
    private int _currentChar;
    private float _timer;

    public TypewriterAnimation(TypewriterAnimationConfig config) => _config = config;

    public void Reset(AnnouncementAnimationContext context)
    {
        _currentLine = 0;
        _currentChar = 0;
        _timer = 0f;

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            (context.Labels[i] as RichTextLabel)?.SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
        }
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var printSpeed = MathF.Max(MinTickInterval, _config.PrintSpeed);

        _timer += deltaTime;
        if (_timer < printSpeed)
            return AnnouncementAnimationStatus.Running;

        var advanced = 0;
        var changed = false;
        while (_timer >= printSpeed && advanced < MaxAdvancePerUpdate)
        {
            _timer -= printSpeed;
            advanced++;

            var finished = Advance(context, out var printed);
            changed |= printed;

            if (finished)
            {
                if (changed)
                    UpdateDisplay(context);

                return AnnouncementAnimationStatus.Finished;
            }
        }

        if (changed)
            UpdateDisplay(context);

        return AnnouncementAnimationStatus.Running;
    }

    private bool Advance(AnnouncementAnimationContext context, out bool printed)
    {
        printed = false;

        var cleanText = context.CleanText;
        if (_currentLine >= cleanText.Length)
            return true;

        var lineText = cleanText[_currentLine];
        if (_currentChar >= lineText.Length)
        {
            _currentLine++;
            _currentChar = 0;
            return _currentLine >= cleanText.Length;
        }

        _currentChar++;
        printed = true;
        return false;
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
                (context.Labels[i] as RichTextLabel)?.SetMessage(message);
            }
            else if (textIndex == _currentLine)
            {
                var currentLineText = cleanText[textIndex];
                var maxLength = Math.Min(_currentChar, currentLineText.Length);
                var partialText = currentLineText[..maxLength];
                var message = context.FormatMessage(partialText, style);
                (context.Labels[i] as RichTextLabel)?.SetMessage(message);
            }
            else
            {
                (context.Labels[i] as RichTextLabel)?.SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
            }
        }
    }
}
