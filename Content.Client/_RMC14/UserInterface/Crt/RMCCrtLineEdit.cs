using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Crt;

/// <summary>
/// A single-line input with the surrounding CRT palette and a visible focus border.
/// </summary>
public sealed class RMCCrtLineEdit : LineEdit, IRMCCrtThemedControl
{
    private readonly StyleBoxFlat _style = new();
    private RMCCrtThemeContext _context;

    public RMCCrtLineEdit()
    {
        AddStyleClass(RMCCrtStyleClasses.Input);
        MinHeight = 30;
        _style.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        _style.SetContentMarginOverride(StyleBox.Margin.Vertical, 5);
        OnFocusEnter += _ => UpdateStyle();
        OnFocusExit += _ => UpdateStyle();
    }

    void IRMCCrtThemedControl.ApplyCrtTheme(RMCCrtThemeContext context)
    {
        _context = context;
        UpdateStyle();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ((IRMCCrtThemedControl) this).ApplyCrtTheme(RMCCrtThemeHelpers.FindContext(this));
    }

    private void UpdateStyle()
    {
        StyleBoxOverride = _context.ThemeEnabled ? _style : null;
        _style.BackgroundColor = _context.Palette.Background;
        _style.BorderColor = HasKeyboardFocus() ? _context.Palette.Foreground : _context.Palette.Border;
        _style.BorderThickness = new Thickness(1);
    }
}
