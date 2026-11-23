using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap.Controls;

public enum TacticalMapPanelKind
{
    None,
    Window,
    Surface,
    Header,
    Footer,
    Frame,
    Popup,
}

public sealed class TacticalMapPanel : PanelContainer, ITacticalMapThemed
{
    private TacticalMapTheme _theme = TacticalMapTheme.Marine;
    private TacticalMapPanelKind _kind = TacticalMapPanelKind.Surface;

    public TacticalMapPanel()
    {
        UpdateStyle();
    }

    public TacticalMapPanelKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            UpdateStyle();
        }
    }

    public void ApplyTheme(TacticalMapTheme theme)
    {
        _theme = theme;
        UpdateStyle();
    }

    private void UpdateStyle()
    {
        var theme = _theme;
        PanelOverride = _kind switch
        {
            TacticalMapPanelKind.None => TacticalMapStyle.Box(Color.Transparent),
            TacticalMapPanelKind.Window => TacticalMapStyle.Box(theme.Background, theme.Border),
            TacticalMapPanelKind.Header => TacticalMapStyle.Rule(theme, true),
            TacticalMapPanelKind.Footer => TacticalMapStyle.Rule(theme, false),
            TacticalMapPanelKind.Frame => TacticalMapStyle.Box(Color.Transparent, theme.Border),
            TacticalMapPanelKind.Popup => TacticalMapStyle.Box(theme.Background.WithAlpha(0.96f), theme.Border, padding: 6),
            _ => TacticalMapStyle.Box(theme.Surface, theme.Border),
        };
    }
}
