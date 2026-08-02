using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtLabel : Label, IRMCCrtThemedControl
{
    private readonly IResourceCache _resourceCache;
    private RMCCrtThemeContext _context = new(
        RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue),
        new RMCCrtAppearanceSettings(true, true));
    private bool _heading;
    private int _textFontSize;
    private RMCCrtTone _tone = RMCCrtTone.Default;

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
        _resourceCache = IoCManager.Resolve<IResourceCache>();
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

    private void UpdateColor()
    {
        var palette = _context.Palette;
        FontColorOverride = Tone switch
        {
            RMCCrtTone.Danger => palette.Danger,
            RMCCrtTone.Good => palette.Good,
            RMCCrtTone.Muted => palette.Muted,
            RMCCrtTone.Warning => palette.Warning,
            _ => palette.Foreground,
        };
    }

    private void UpdateAppearance()
    {
        RemoveStyleClass(RMCCrtStyleClasses.Text);
        RemoveStyleClass(RMCCrtStyleClasses.Heading);
        RemoveStyleClass("monospace");
        RemoveStyleClass(StyleBase.StyleClassLabelHeading);

        FontOverride = TextFontSize > 0
            ? _context.ThemeEnabled
                ? _resourceCache.GetFont("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf", TextFontSize)
                : _resourceCache.NotoStack(
                    variation: Heading ? "Bold" : "Regular",
                    size: TextFontSize,
                    display: Heading)
            : null;

        if (_context.ThemeEnabled)
        {
            AddStyleClass(Heading ? RMCCrtStyleClasses.Heading : RMCCrtStyleClasses.Text);
            AddStyleClass("monospace");
            UpdateColor();
            return;
        }

        if (Heading)
            AddStyleClass(StyleBase.StyleClassLabelHeading);

        FontColorOverride = Tone switch
        {
            RMCCrtTone.Danger => StyleNano.DangerousRedFore,
            RMCCrtTone.Good => StyleNano.GoodGreenFore,
            RMCCrtTone.Muted => StyleNano.DisabledFore,
            RMCCrtTone.Warning => StyleNano.ConcerningOrangeFore,
            _ => null,
        };
    }
}
