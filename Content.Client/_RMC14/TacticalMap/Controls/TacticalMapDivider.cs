using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed class TacticalMapDivider : PanelContainer, ITacticalMapThemed
{
    private TacticalMapTheme _theme = TacticalMapTheme.Marine;
    private bool _vertical;

    public TacticalMapDivider()
    {
        UpdateStyle();
    }

    public bool Vertical
    {
        get => _vertical;
        set
        {
            _vertical = value;
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
        PanelOverride = TacticalMapStyle.Box(_theme.Divider);
        if (_vertical)
        {
            MinSize = new Vector2(1, 0);
            HorizontalExpand = false;
            VerticalExpand = true;
        }
        else
        {
            MinSize = new Vector2(0, 1);
            HorizontalExpand = true;
            VerticalExpand = false;
        }
    }
}
