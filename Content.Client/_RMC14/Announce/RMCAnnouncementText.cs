using System.Text;
using Content.Shared._RMC14.Announce;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce;

public static class RMCAnnouncementText
{
    private const string DecryptGlyphs = "!#$%&*+-/<=>?@0123456789ABCDEF";
    public const string Cursor = "_";

    public static List<(string Text, Color Color)> BuildBody(
        string text,
        string shown,
        int start,
        int end,
        int fade,
        bool cursor,
        bool cursorVisible,
        bool trailingCursor,
        Color color)
    {
        var transparent = color.WithAlpha(0);
        var cursorColor = cursorVisible ? color : transparent;
        var cursorAt = GetCursorIndex(text, shown.Length, cursor);
        var segments = BuildRange(text, shown, start, end, fade, cursorAt, color, cursorColor);
        if (cursor && trailingCursor)
            segments.Add((Cursor, shown.Length >= text.Length ? cursorColor : transparent));

        return segments;
    }

    private static List<(string Text, Color Color)> BuildRange(
        string text,
        string shown,
        int start,
        int end,
        int fade,
        int cursorAt,
        Color color,
        Color cursorColor)
    {
        var segments = new List<(string Text, Color Color)>();
        var builder = new StringBuilder();
        Color? current = null;
        for (var i = start; i < end; i++)
        {
            char character;
            Color characterColor;
            if (i == cursorAt)
            {
                character = Cursor[0];
                characterColor = cursorColor;
            }
            else
            {
                character = i < shown.Length ? shown[i] : text[i];
                characterColor = color.WithAlpha(color.A * GetCharacterAlpha(i - shown.Length, fade));
            }

            if (current is { } previous && previous != characterColor)
            {
                segments.Add((builder.ToString(), previous));
                builder.Clear();
            }

            current = characterColor;
            builder.Append(character);
        }

        if (current is { } last && builder.Length > 0)
            segments.Add((builder.ToString(), last));

        return segments;
    }

    private static int GetCursorIndex(string text, int revealed, bool cursor)
    {
        return cursor && revealed < text.Length && !char.IsWhiteSpace(text[revealed]) ? revealed : -1;
    }

    private static float GetCharacterAlpha(int offset, int fade)
    {
        if (offset < 0)
            return 1;

        return offset < fade ? (fade - offset) / (float) (fade + 1) : 0;
    }

    public static float GetLengthScale(RMCAnnouncementPresetComponent preset, int length)
    {
        if (length <= preset.TextScaleReference)
            return 1;

        return MathF.Max(preset.MinTextScale, MathF.Sqrt(preset.TextScaleReference / (float) length));
    }

    public static string Decrypt(string text, float progress, IRobustRandom random)
    {
        if (progress >= 1)
            return text;

        var characters = text.ToCharArray();
        for (var i = (int) (characters.Length * Math.Max(0, progress)); i < characters.Length; i++)
        {
            if (!char.IsWhiteSpace(characters[i]))
                characters[i] = DecryptGlyphs[random.Next(DecryptGlyphs.Length)];
        }

        return new string(characters);
    }

    public static FormattedMessage Build(string text, Color color)
    {
        return Build(new[] { (text, color) });
    }

    public static FormattedMessage Build(IReadOnlyList<(string Text, Color Color)> segments)
    {
        var message = new FormattedMessage();
        foreach (var (text, color) in segments)
        {
            if (text.Length == 0)
                continue;

            message.PushColor(color);
            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    message.PushNewline();

                if (lines[i].Length > 0)
                    message.AddText(lines[i]);
            }

            message.Pop();
        }

        return message;
    }

    public static Font GetFont(IPrototypeManager prototype, IResourceCache resourceCache, string fontId, int size)
    {
        if (!prototype.TryIndex<FontPrototype>(fontId, out var font))
            font = prototype.Index<FontPrototype>(FontTag.DefaultFont);

        return new VectorFont(resourceCache.GetResource<FontResource>(font.Path), size);
    }

    public static List<(int Start, int Length)> WrapLines(string text, Font font, int size, float maxWidth)
    {
        var lines = new List<(int Start, int Length)>();
        var paragraphStart = 0;
        while (paragraphStart <= text.Length)
        {
            var paragraphEnd = text.IndexOf('\n', paragraphStart);
            if (paragraphEnd < 0)
                paragraphEnd = text.Length;

            var start = paragraphStart;
            var width = 0f;
            var space = -1;
            var i = start;
            while (i < paragraphEnd)
            {
                var length = 1;
                var advance = (float) size;
                if (Rune.TryGetRuneAt(text, i, out var rune))
                {
                    length = rune.Utf16SequenceLength;
                    if (font.GetCharMetrics(rune, 1f) is { } metrics)
                        advance = metrics.Advance;
                }

                if (width + advance > maxWidth && i > start)
                {
                    var breakAt = text[i] == ' ' ? i : space;
                    if (breakAt > start)
                    {
                        lines.Add((start, breakAt - start));
                        start = breakAt + 1;
                    }
                    else
                    {
                        lines.Add((start, i - start));
                        start = i;
                    }

                    i = start;
                    width = 0;
                    space = -1;
                    continue;
                }

                if (text[i] == ' ')
                    space = i;

                width += advance;
                i += length;
            }

            lines.Add((start, paragraphEnd - start));
            paragraphStart = paragraphEnd + 1;
        }

        return lines;
    }

    public static void SetFont(
        RichTextLabel label,
        IPrototypeManager prototype,
        IResourceCache resourceCache,
        string fontId,
        int size)
    {
        var vectorFont = GetFont(prototype, resourceCache, fontId, size);
        label.Stylesheet = new Stylesheet(new[]
        {
            new StyleRule(
                new SelectorElement(typeof(RichTextLabel), null, null, null),
                new[] { new StyleProperty("font", vectorFont) }),
        });
    }
}
