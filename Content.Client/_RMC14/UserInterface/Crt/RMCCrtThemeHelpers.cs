using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UserInterface.Crt;

internal interface IRMCCrtThemedControl
{
    void ApplyCrtTheme(RMCCrtThemeContext context);
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
