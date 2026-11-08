using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce;

public static class RMCAnnouncementText
{
    public static FormattedMessage Build(string text, Color color)
    {
        var message = new FormattedMessage();
        message.PushColor(color);

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                message.PushNewline();

            message.AddText(lines[i]);
        }

        message.Pop();
        return message;
    }

    public static void SetFont(
        RichTextLabel label,
        IPrototypeManager prototype,
        IResourceCache resourceCache,
        string fontId,
        int size)
    {
        if (!prototype.TryIndex<FontPrototype>(fontId, out var font))
            font = prototype.Index<FontPrototype>(FontTag.DefaultFont);

        var vectorFont = new VectorFont(resourceCache.GetResource<FontResource>(font.Path), size);
        label.Stylesheet = new Stylesheet(new[]
        {
            new StyleRule(
                new SelectorElement(typeof(RichTextLabel), null, null, null),
                new[] { new StyleProperty("font", vectorFont) }),
        });
    }
}
