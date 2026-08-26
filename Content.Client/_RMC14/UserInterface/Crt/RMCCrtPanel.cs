using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtPanel : PanelContainer, IRMCCrtThemedControl
{
    private readonly RMCCrtEffectRenderer _effectsRenderer = new();
    private readonly StyleBoxFlat _crtStyle = new();
    private readonly StyleBoxFlat _nanoWarningStyle = new();
    private Color? _backgroundOverride;
    private Color? _borderOverride;
    private float _backgroundOpacity = 0.72f;
    private float _borderThickness = 1;
    private RMCCrtThemeContext _context = new(
        RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue),
        new RMCCrtAppearanceSettings(true, true));
    private RMCCrtPanelVariant _variant = RMCCrtPanelVariant.Surface;

    public RMCCrtEffects Effects { get; set; }
    public float BackgroundOpacity
    {
        get => _backgroundOpacity;
        set
        {
            _backgroundOpacity = value;
            UpdateStyle();
        }
    }
    public float RgbOpacity { get; set; } = 0.06f;
    public float RgbWidth { get; set; } = 1;
    public float ScanlineOpacity { get; set; } = 0.25f;
    public float ScanlineSpacing { get; set; } = 2;
    public float ScanlineThickness { get; set; } = 1;
    public float StripeWidth { get; set; } = 18;

    public float BorderThickness
    {
        get => _borderThickness;
        set
        {
            _borderThickness = value;
            UpdateStyle();
        }
    }

    public RMCCrtPanelVariant Variant
    {
        get => _variant;
        set
        {
            _variant = value;
            UpdateStyle();
        }
    }

    public RMCCrtPanel()
    {
        PanelOverride = _crtStyle;
        UpdateStyle();
    }

    void IRMCCrtThemedControl.ApplyCrtTheme(RMCCrtThemeContext context)
    {
        ApplyAppearance(context);
    }

    internal void ApplyAppearance(RMCCrtThemeContext context)
    {
        _context = context;
        UpdateStyle();
    }

    internal void SetColorOverrides(Color? background, Color? border)
    {
        _backgroundOverride = background;
        _borderOverride = border;
        UpdateStyle();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ApplyAppearance(RMCCrtThemeHelpers.FindContext(this));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        _effectsRenderer.Draw(
            handle,
            PixelWidth,
            PixelHeight,
            UIScale,
            _context.ResolveEffects(Effects),
            ScanlineSpacing,
            ScanlineThickness,
            RgbWidth,
            StripeWidth,
            ScanlineOpacity,
            RgbOpacity,
            _context.Palette.Background.WithAlpha(0.3f));
    }

    private void UpdateStyle()
    {
        RemoveStyleClass(StyleNano.StyleClassBorderedWindowPanel);
        RemoveStyleClass(StyleNano.StyleClassInset);

        if (!_context.ThemeEnabled)
        {
            UpdateNanoStyle();
            return;
        }

        PanelOverride = _crtStyle;
        var palette = _context.Palette;
        var background = Variant switch
        {
            RMCCrtPanelVariant.Inset => palette.Background.WithAlpha(Math.Clamp(BackgroundOpacity + 0.15f, 0, 1)),
            RMCCrtPanelVariant.Surface => palette.Background.WithAlpha(Math.Clamp(BackgroundOpacity, 0, 1)),
            RMCCrtPanelVariant.Transparent => Color.Transparent,
            RMCCrtPanelVariant.Warning => palette.Warning.WithAlpha(0.72f),
            _ => palette.Background.WithAlpha(Math.Clamp(BackgroundOpacity, 0, 1)),
        };
        var border = Variant == RMCCrtPanelVariant.Warning ? palette.Warning : palette.Border;

        _crtStyle.BackgroundColor = _backgroundOverride ?? background;
        _crtStyle.BorderColor = _borderOverride ?? border;
        _crtStyle.BorderThickness = new Thickness(BorderThickness);
    }

    private void UpdateNanoStyle()
    {
        switch (Variant)
        {
            case RMCCrtPanelVariant.Inset:
                PanelOverride = null;
                AddStyleClass(StyleNano.StyleClassInset);
                break;
            case RMCCrtPanelVariant.Surface:
                PanelOverride = null;
                AddStyleClass(StyleNano.StyleClassBorderedWindowPanel);
                break;
            case RMCCrtPanelVariant.Transparent:
                PanelOverride = null;
                break;
            case RMCCrtPanelVariant.Warning:
                _nanoWarningStyle.BackgroundColor = StyleNano.PanelDark;
                _nanoWarningStyle.BorderColor = StyleNano.ConcerningOrangeFore;
                _nanoWarningStyle.BorderThickness = new Thickness(BorderThickness);
                PanelOverride = _nanoWarningStyle;
                break;
        }
    }
}
