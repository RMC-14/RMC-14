using System.Text;
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
    private FormattedMessage[] _formattedLines = [];
    private string[] _plainLines = [];

    public TypewriterAnimation(TypewriterAnimationConfig config) => _config = config;

    public void Reset(AnnouncementAnimationContext context)
    {
        _currentLine = 0;
        _currentChar = 0;
        _timer = 0f;
        _formattedLines = new FormattedMessage[context.OriginalText.Length];
        _plainLines = new string[context.OriginalText.Length];

        for (var i = 0; i < context.OriginalText.Length; i++)
        {
            var formatted = FormattedMessage.FromMarkupPermissive(context.OriginalText[i]);
            _formattedLines[i] = formatted;
            _plainLines[i] = formatted.ToString();
        }

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

        if (_currentLine >= _plainLines.Length)
            return true;

        var lineText = _plainLines[_currentLine];
        if (_currentChar >= lineText.Length)
        {
            _currentLine++;
            _currentChar = 0;
            return _currentLine >= _plainLines.Length;
        }

        _currentChar++;
        printed = true;
        return false;
    }

    private void UpdateDisplay(AnnouncementAnimationContext context)
    {
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
                var currentLineText = _plainLines[textIndex];
                var maxLength = Math.Min(_currentChar, currentLineText.Length);
                var partialText = CreatePartialMarkup(_formattedLines[textIndex], maxLength);
                var message = context.FormatMessage(partialText, style);
                (context.Labels[i] as RichTextLabel)?.SetMessage(message);
            }
            else
            {
                (context.Labels[i] as RichTextLabel)?.SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
            }
        }
    }

    internal static string CreatePartialMarkup(FormattedMessage source, int visibleLength)
    {
        if (visibleLength <= 0)
            return string.Empty;

        var result = new StringBuilder();
        var openTags = new Stack<string>();
        var remaining = visibleLength;

        foreach (var node in source)
        {
            if (node.Name == null)
            {
                if (remaining <= 0)
                    break;

                var text = node.Value.StringValue ?? string.Empty;
                var length = Math.Min(remaining, text.Length);
                result.Append(FormattedMessage.EscapeText(text[..length]));
                remaining -= length;

                if (remaining <= 0)
                    break;

                continue;
            }

            if (node.Closing)
            {
                if (openTags.TryPop(out var tag))
                    AppendClosingTag(result, tag);

                continue;
            }

            result.Append(node);
            openTags.Push(node.Name);
        }

        while (openTags.TryPop(out var tag))
        {
            AppendClosingTag(result, tag);
        }

        return result.ToString();
    }

    private static void AppendClosingTag(StringBuilder builder, string tag)
    {
        builder.Append("[/");
        builder.Append(tag);
        builder.Append(']');
    }
}
