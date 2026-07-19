using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtPanel : PanelContainer, IRMCCrtThemedControl
{
    private readonly RMCCrtEffectRenderer _effectsRenderer = new();
    private readonly StyleBoxFlat _style = new();
    private Color? _backgroundOverride;
    private Color? _borderOverride;
    private float _backgroundOpacity = 0.72f;
    private float _borderThickness = 1;
    private RMCCrtPalette _palette = RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue);
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
        PanelOverride = _style;
        UpdateStyle();
    }

    public void ApplyCrtTheme(RMCCrtPalette palette)
    {
        _palette = palette;
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
        ApplyCrtTheme(RMCCrtThemeHelpers.FindPalette(this));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        _effectsRenderer.Draw(
            handle,
            PixelWidth,
            PixelHeight,
            UIScale,
            Effects,
            ScanlineSpacing,
            ScanlineThickness,
            RgbWidth,
            StripeWidth,
            ScanlineOpacity,
            RgbOpacity,
            _palette.Background.WithAlpha(0.3f));
    }

    private void UpdateStyle()
    {
        var background = Variant switch
        {
            RMCCrtPanelVariant.Inset => _palette.Background.WithAlpha(Math.Clamp(BackgroundOpacity + 0.15f, 0, 1)),
            RMCCrtPanelVariant.Surface => _palette.Background.WithAlpha(Math.Clamp(BackgroundOpacity, 0, 1)),
            RMCCrtPanelVariant.Transparent => Color.Transparent,
            RMCCrtPanelVariant.Warning => _palette.Warning.WithAlpha(0.72f),
            _ => _palette.Background.WithAlpha(Math.Clamp(BackgroundOpacity, 0, 1)),
        };
        var border = Variant == RMCCrtPanelVariant.Warning ? _palette.Warning : _palette.Border;

        _style.BackgroundColor = _backgroundOverride ?? background;
        _style.BorderColor = _borderOverride ?? border;
        _style.BorderThickness = new Thickness(BorderThickness);
    }
}
