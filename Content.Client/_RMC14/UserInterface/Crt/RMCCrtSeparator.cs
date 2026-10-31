using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtSeparator : Control, IRMCCrtThemedControl
{
    private RMCCrtThemeContext _context = new(
        RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue),
        new RMCCrtAppearanceSettings(true, true));
    private RMCCrtSeparatorOrientation _orientation;
    private float _thickness = 1;

    internal Color ResolvedColor =>
        _context.ThemeEnabled ? _context.Palette.Border : StyleNano.NanoGold;

    public RMCCrtSeparatorOrientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            UpdateMinimumSize();
        }
    }

    public float Thickness
    {
        get => _thickness;
        set
        {
            _thickness = value;
            UpdateMinimumSize();
        }
    }

    public RMCCrtSeparator()
    {
        UpdateMinimumSize();
    }

    void IRMCCrtThemedControl.ApplyCrtTheme(RMCCrtThemeContext context)
    {
        ApplyAppearance(context);
    }

    internal void ApplyAppearance(RMCCrtThemeContext context)
    {
        _context = context;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ApplyAppearance(RMCCrtThemeHelpers.FindContext(this));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (Orientation == RMCCrtSeparatorOrientation.Vertical)
        {
            var left = Math.Max(0, (PixelWidth - Thickness * UIScale) / 2);
            handle.DrawRect(new UIBox2(left, 0, left + Thickness * UIScale, PixelHeight), ResolvedColor);
            return;
        }

        var top = Math.Max(0, (PixelHeight - Thickness * UIScale) / 2);
        handle.DrawRect(new UIBox2(0, top, PixelWidth, top + Thickness * UIScale), ResolvedColor);
    }

    private void UpdateMinimumSize()
    {
        MinSize = Orientation == RMCCrtSeparatorOrientation.Vertical
            ? new System.Numerics.Vector2(Thickness, 1)
            : new System.Numerics.Vector2(1, Thickness);
    }
}
