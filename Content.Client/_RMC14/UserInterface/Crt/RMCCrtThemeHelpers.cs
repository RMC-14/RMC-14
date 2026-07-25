using Robust.Client.UserInterface;

namespace Content.Client._RMC14.UserInterface.Crt;

internal interface IRMCCrtThemedControl
{
    void ApplyCrtTheme(RMCCrtThemeContext context);
}

internal static class RMCCrtThemeHelpers
{
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
