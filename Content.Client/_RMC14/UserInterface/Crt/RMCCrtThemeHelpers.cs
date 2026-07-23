using Robust.Client.UserInterface;

namespace Content.Client._RMC14.UserInterface.Crt;

internal interface IRMCCrtThemedControl
{
    void ApplyCrtTheme(RMCCrtPalette palette);
}

internal static class RMCCrtThemeHelpers
{
    public static RMCCrtPalette FindPalette(Control control)
    {
        for (var parent = control.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is RMCCrtThemeScope scope)
                return scope.ResolvedPalette;
        }

        return RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue);
    }

    public static void ApplyToDescendants(Control control, RMCCrtPalette palette)
    {
        foreach (var child in control.Children)
        {
            if (child is RMCCrtThemeScope)
                continue;

            if (child is IRMCCrtThemedControl themed)
                themed.ApplyCrtTheme(palette);

            ApplyToDescendants(child, palette);
        }
    }
}
