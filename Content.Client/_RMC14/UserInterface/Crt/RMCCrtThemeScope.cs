using System.Linq;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._RMC14.UserInterface.Crt;

/// <summary>
/// Applies an isolated CM-inspired CRT theme to its descendants.
/// </summary>
public sealed class RMCCrtThemeScope : PanelContainer
{
    private readonly RMCCrtEffectRenderer _effectsRenderer = new();
    private readonly StyleBoxFlat _style = new();
    private readonly IResourceCache _resourceCache;
    private readonly IStylesheetManager _stylesheet;

    private float _backgroundOpacity = 1;
    private float _borderThickness = 1;
    private Color _customBackground = Color.FromHex("#00000F");
    private Color _customBorder = Color.FromHex("#8ACBFF");
    private Color _customDanger = Color.FromHex("#F04B43");
    private Color _customDisabledBackground = Color.FromHex("#00000F80");
    private Color _customDisabledForeground = Color.FromHex("#55768D");
    private Color _customFill = Color.FromHex("#82C5F2");
    private Color _customFillForeground = Color.FromHex("#00000F");
    private Color _customForeground = Color.FromHex("#8ACBFF");
    private Color _customGood = Color.FromHex("#00C957");
    private Color _customMuted = Color.FromHex("#55768D");
    private Color _customWarning = Color.FromHex("#D3B400");
    private RMCCrtEffects _effects = RMCCrtEffects.HorizontalScanlines;
    private RMCCrtPalettePreset _palette = RMCCrtPalettePreset.Blue;

    public RMCCrtPalette ResolvedPalette { get; private set; }

    public RMCCrtPalettePreset Palette
    {
        get => _palette;
        set
        {
            _palette = value;
            RefreshTheme();
        }
    }

    public RMCCrtEffects Effects
    {
        get => _effects;
        set => _effects = value;
    }

    public float BorderThickness
    {
        get => _borderThickness;
        set
        {
            _borderThickness = value;
            UpdatePanel();
        }
    }

    public float BackgroundOpacity
    {
        get => _backgroundOpacity;
        set
        {
            _backgroundOpacity = value;
            UpdatePanel();
        }
    }

    public float RgbOpacity { get; set; } = 0.06f;
    public float RgbWidth { get; set; } = 1;
    public float ScanlineOpacity { get; set; } = 0.25f;
    public float ScanlineSpacing { get; set; } = 2;
    public float ScanlineThickness { get; set; } = 1;
    public float StripeWidth { get; set; } = 18;

    public Color CustomBackground { get => _customBackground; set { _customBackground = value; RefreshTheme(); } }
    public Color CustomBorder { get => _customBorder; set { _customBorder = value; RefreshTheme(); } }
    public Color CustomDanger { get => _customDanger; set { _customDanger = value; RefreshTheme(); } }
    public Color CustomDisabledBackground { get => _customDisabledBackground; set { _customDisabledBackground = value; RefreshTheme(); } }
    public Color CustomDisabledForeground { get => _customDisabledForeground; set { _customDisabledForeground = value; RefreshTheme(); } }
    public Color CustomFill { get => _customFill; set { _customFill = value; RefreshTheme(); } }
    public Color CustomFillForeground { get => _customFillForeground; set { _customFillForeground = value; RefreshTheme(); } }
    public Color CustomForeground { get => _customForeground; set { _customForeground = value; RefreshTheme(); } }
    public Color CustomGood { get => _customGood; set { _customGood = value; RefreshTheme(); } }
    public Color CustomMuted { get => _customMuted; set { _customMuted = value; RefreshTheme(); } }
    public Color CustomWarning { get => _customWarning; set { _customWarning = value; RefreshTheme(); } }

    public RMCCrtThemeScope()
    {
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        _stylesheet = IoCManager.Resolve<IStylesheetManager>();
        PanelOverride = _style;
        ResolvedPalette = RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue);
        RefreshTheme();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        RefreshTheme();
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
            ResolvedPalette.Warning.WithAlpha(0.3f));
    }

    private void RefreshTheme()
    {
        ResolvedPalette = Palette == RMCCrtPalettePreset.Custom
            ? new RMCCrtPalette(
                CustomForeground,
                CustomBackground,
                CustomBorder,
                CustomFill,
                CustomFillForeground,
                CustomGood,
                CustomWarning,
                CustomDanger,
                CustomMuted,
                CustomDisabledBackground,
                CustomDisabledForeground)
            : RMCCrtPalettes.Get(Palette);

        UpdatePanel();
        Stylesheet = CreateStylesheet();
        RMCCrtThemeHelpers.ApplyToDescendants(this, ResolvedPalette);
    }

    private void UpdatePanel()
    {
        _style.BackgroundColor = ResolvedPalette.Background.WithAlpha(Math.Clamp(BackgroundOpacity, 0, 1));
        _style.BorderColor = ResolvedPalette.Border;
        _style.BorderThickness = new Thickness(BorderThickness);
    }

    private Stylesheet CreateStylesheet()
    {
        var mono = _resourceCache.GetFont("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf", 12);
        var rules = _stylesheet.SheetNano.Rules.Concat(new StyleRule[]
        {
            Element<Label>().Class(RMCCrtStyleClasses.Text)
                .Prop(Label.StylePropertyFont, mono)
                .Prop(Label.StylePropertyFontColor, ResolvedPalette.Foreground),
            Element<Label>().Class(RMCCrtStyleClasses.Heading)
                .Prop(Label.StylePropertyFont, mono)
                .Prop(Label.StylePropertyFontColor, ResolvedPalette.Foreground),
            Element<RichTextLabel>().Class(RMCCrtStyleClasses.Text)
                .Prop(Control.StylePropertyModulateSelf, ResolvedPalette.Foreground),
        }).ToArray();

        return new Stylesheet(rules);
    }
}
