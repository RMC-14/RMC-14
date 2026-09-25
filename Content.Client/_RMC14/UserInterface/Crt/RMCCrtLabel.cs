using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtLabel : RichTextLabel, IRMCCrtThemedControl
{
    private RMCCrtThemeContext _context = new(
        RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue),
        new RMCCrtAppearanceSettings(true, true));
    private bool _heading;
    private string? _text;
    private int _textFontSize;
    private RMCCrtTone _tone = RMCCrtTone.Default;

    public new string? Text
    {
        get => _text;
        set
        {
            if (_text == value)
                return;

            _text = value;
            UpdateAppearance();
        }
    }

    public RMCCrtTone Tone
    {
        get => _tone;
        set
        {
            _tone = value;
            UpdateAppearance();
        }
    }

    public bool Heading
    {
        get => _heading;
        set
        {
            _heading = value;
            UpdateAppearance();
        }
    }

    /// <summary>
    /// Overrides only this label's text size. Zero keeps the active theme's default font.
    /// </summary>
    public int TextFontSize
    {
        get => _textFontSize;
        set
        {
            _textFontSize = value;
            UpdateAppearance();
        }
    }

    public RMCCrtLabel()
    {
        UpdateAppearance();
    }

    void IRMCCrtThemedControl.ApplyCrtTheme(RMCCrtThemeContext context)
    {
        ApplyAppearance(context);
    }

    internal void ApplyAppearance(RMCCrtThemeContext context)
    {
        _context = context;
        UpdateAppearance();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ApplyAppearance(RMCCrtThemeHelpers.FindContext(this));
    }

    private void UpdateAppearance()
    {
        SetMessage(RMCCrtThemeHelpers.CreateTextMessage(
            _text,
            _context,
            TextFontSize,
            Heading));

        var palette = _context.Palette;
        Color? color = Tone switch
        {
            RMCCrtTone.Danger => _context.ThemeEnabled ? palette.Danger : StyleNano.DangerousRedFore,
            RMCCrtTone.Good => _context.ThemeEnabled ? palette.Good : StyleNano.GoodGreenFore,
            RMCCrtTone.Muted => _context.ThemeEnabled ? palette.Muted : StyleNano.DisabledFore,
            RMCCrtTone.Warning => _context.ThemeEnabled ? palette.Warning : StyleNano.ConcerningOrangeFore,
            _ => _context.ThemeEnabled
                ? palette.Foreground
                : Heading
                    ? StyleNano.NanoGold
                    : null,
        };
        ModulateSelfOverride = color;
    }
}
