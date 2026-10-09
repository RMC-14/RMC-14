using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UserInterface.Crt;

public sealed class RMCCrtIcon : TextureRect, IRMCCrtThemedControl
{
    public static readonly ResPath DefaultRsiPath = new("/Textures/_RMC14/Interface/CRT/crt_icons.rsi");

    private static readonly ISawmill Log = Logger.GetSawmill("rmc-crt");

    private readonly IResourceCache _resourceCache;
    private string? _iconState;
    private ResPath _rsiPath = DefaultRsiPath;
    private RMCCrtThemeContext _context = new(
        RMCCrtPalettes.Get(RMCCrtPalettePreset.Blue),
        new RMCCrtAppearanceSettings(true, true));
    private RMCCrtTone _tone = RMCCrtTone.Default;

    public string? IconState
    {
        get => _iconState;
        set
        {
            _iconState = value;
            UpdateIcon();
        }
    }

    public ResPath RsiPath
    {
        get => _rsiPath;
        set
        {
            _rsiPath = value;
            UpdateIcon();
        }
    }

    public RMCCrtTone Tone
    {
        get => _tone;
        set
        {
            _tone = value;
            UpdateColor();
        }
    }

    public RMCCrtIcon()
    {
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        Stretch = StretchMode.Scale;
        UpdateColor();
    }

    void IRMCCrtThemedControl.ApplyCrtTheme(RMCCrtThemeContext context)
    {
        ApplyAppearance(context);
    }

    internal void ApplyAppearance(RMCCrtThemeContext context)
    {
        _context = context;
        UpdateColor();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ApplyAppearance(RMCCrtThemeHelpers.FindContext(this));
    }

    private void UpdateColor()
    {
        if (!_context.ThemeEnabled)
        {
            ModulateSelfOverride = Tone switch
            {
                RMCCrtTone.Danger => StyleNano.DangerousRedFore,
                RMCCrtTone.Good => StyleNano.GoodGreenFore,
                RMCCrtTone.Muted => StyleNano.DisabledFore,
                RMCCrtTone.Warning => StyleNano.ConcerningOrangeFore,
                _ => null,
            };
            return;
        }

        var palette = _context.Palette;
        ModulateSelfOverride = Tone switch
        {
            RMCCrtTone.Danger => palette.Danger,
            RMCCrtTone.Good => palette.Good,
            RMCCrtTone.Muted => palette.Muted,
            RMCCrtTone.Warning => palette.Warning,
            _ => palette.Foreground,
        };
    }

    private void UpdateIcon()
    {
        if (string.IsNullOrWhiteSpace(IconState))
        {
            Texture = null;
            return;
        }

        try
        {
            var rsi = _resourceCache.GetResource<RSIResource>(RsiPath).RSI;
            if (rsi.TryGetState(new RSI.StateId(IconState), out var state))
            {
                Texture = state.Frame0;
                return;
            }

            Texture = null;
            Log.Warning($"CRT icon state '{IconState}' does not exist in '{RsiPath}'.");
        }
        catch (Exception exception)
        {
            Texture = null;
            Log.Error($"Failed to load CRT icon state '{IconState}' from '{RsiPath}': {exception}");
        }
    }
}
