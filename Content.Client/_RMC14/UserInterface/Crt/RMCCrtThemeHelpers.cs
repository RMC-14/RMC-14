using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._RMC14.UserInterface.Crt;

internal interface IRMCCrtThemedControl
{
    void ApplyCrtTheme(RMCCrtThemeContext context);
}

/// <summary>
/// Keeps rich text measurement and its first baseline on the font actually used to draw the text.
/// Font tags alone leave those metrics on the control's default font.
/// </summary>
internal sealed class RMCCrtTextStyle
{
    private (bool Theme, int Size, bool Heading)? _key;

    public void Apply(RichTextLabel label, RMCCrtThemeContext context, int size, bool heading = false)
    {
        var key = (context.ThemeEnabled, size, heading);
        if (_key == key)
            return;

        var message = RMCCrtThemeHelpers.CreateTextMessage(null, context, size, heading);
        var node = message.Nodes[0];
        var fonts = new Stack<Font>();
        fonts.Push(IoCManager.Resolve<IUserInterfaceManager>().ThemeDefaults.DefaultFont);
        var font = FontTag.CreateFont(fonts, node, IoCManager.Resolve<IResourceCache>(),
            IoCManager.Resolve<IPrototypeManager>(), node.Value.StringValue!);
        label.Stylesheet = new Stylesheet(new StyleRule[]
        {
            Element<RichTextLabel>().Prop(Label.StylePropertyFont, font),
        });
        _key = key;
    }
}

internal static class RMCCrtThemeHelpers
{
    public static FormattedMessage CreateTextMessage(
        string? text,
        RMCCrtThemeContext context,
        int fontSize = 0,
        bool heading = false)
    {
        var message = new FormattedMessage();
        var nanoHeadingDefaults = !context.ThemeEnabled && heading && fontSize <= 0;
        var fontId = context.ThemeEnabled
            ? "Monospace"
            : heading
                ? nanoHeadingDefaults
                    ? "DefaultBold"
                    : "NotoSansDisplayBold"
                : "Default";
        var resolvedFontSize = nanoHeadingDefaults ? 16 : fontSize;
        Dictionary<string, MarkupParameter>? attributes = null;
        if (resolvedFontSize > 0)
        {
            attributes = new Dictionary<string, MarkupParameter>
            {
                ["size"] = new MarkupParameter(LongValue: resolvedFontSize),
            };
        }

        message.PushTag(new MarkupNode("font", new MarkupParameter(fontId), attributes));
        message.AddText(text ?? string.Empty);
        message.Pop();
        return message;
    }

    public static RMCCrtThemeContext FindContext(Control control)
    {
        for (var parent = control.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is RMCCrtThemeScope scope)
                return scope.ResolvedContext;
        }

        return new RMCCrtThemeContext(
            RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue),
            new RMCCrtAppearanceSettings(true, true));
    }

    public static void ApplyToDescendants(Control control, RMCCrtThemeContext context)
    {
        foreach (var child in control.Children)
        {
            if (child is RMCCrtThemeScope)
                continue;

            if (child is IRMCCrtThemedControl themed)
                themed.ApplyCrtTheme(context);

            ApplyToDescendants(child, context);
        }
    }
}
