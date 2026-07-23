using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtLabel : Label, IRMCCrtThemedControl
{
    private RMCCrtPalette _palette = RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue);
    private bool _heading;
    private RMCCrtTone _tone = RMCCrtTone.Default;

    public RMCCrtTone Tone
    {
        get => _tone;
        set
        {
            _tone = value;
            UpdateColor();
        }
    }

    public bool Heading
    {
        get => _heading;
        set
        {
            _heading = value;
            RemoveStyleClass(RMCCrtStyleClasses.Text);
            RemoveStyleClass(RMCCrtStyleClasses.Heading);
            AddStyleClass(value ? RMCCrtStyleClasses.Heading : RMCCrtStyleClasses.Text);
        }
    }

    public RMCCrtLabel()
    {
        AddStyleClass(RMCCrtStyleClasses.Text);
        AddStyleClass("monospace");
        UpdateColor();
    }

    public void ApplyCrtTheme(RMCCrtPalette palette)
    {
        _palette = palette;
        UpdateColor();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ApplyCrtTheme(RMCCrtThemeHelpers.FindPalette(this));
    }

    private void UpdateColor()
    {
        FontColorOverride = Tone switch
        {
            RMCCrtTone.Danger => _palette.Danger,
            RMCCrtTone.Good => _palette.Good,
            RMCCrtTone.Muted => _palette.Muted,
            RMCCrtTone.Warning => _palette.Warning,
            _ => _palette.Foreground,
        };
    }
}
