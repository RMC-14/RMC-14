using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtSeparator : Control, IRMCCrtThemedControl
{
    private RMCCrtPalette _palette = RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue);
    private RMCCrtSeparatorOrientation _orientation;
    private float _thickness = 1;

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

    public void ApplyCrtTheme(RMCCrtPalette palette)
    {
        _palette = palette;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ApplyCrtTheme(RMCCrtThemeHelpers.FindPalette(this));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (Orientation == RMCCrtSeparatorOrientation.Vertical)
        {
            var left = Math.Max(0, (PixelWidth - Thickness * UIScale) / 2);
            handle.DrawRect(new UIBox2(left, 0, left + Thickness * UIScale, PixelHeight), _palette.Border);
            return;
        }

        var top = Math.Max(0, (PixelHeight - Thickness * UIScale) / 2);
        handle.DrawRect(new UIBox2(0, top, PixelWidth, top + Thickness * UIScale), _palette.Border);
    }

    private void UpdateMinimumSize()
    {
        MinSize = Orientation == RMCCrtSeparatorOrientation.Vertical
            ? new System.Numerics.Vector2(Thickness, 1)
            : new System.Numerics.Vector2(1, Thickness);
    }
}
