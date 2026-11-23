using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed class TacticalMapLabel : Label, ITacticalMapThemed
{
    private TacticalMapTheme _theme = TacticalMapTheme.Marine;
    private TacticalMapTextStyle _textStyle = TacticalMapTextStyle.Normal;
    private Color? _color;

    public TacticalMapLabel()
    {
        UpdateStyle();
    }

    public TacticalMapTextStyle TextStyle
    {
        get => _textStyle;
        set
        {
            _textStyle = value;
            UpdateStyle();
        }
    }

    public Color? ColorOverride
    {
        get => _color;
        set
        {
            _color = value;
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
        FontOverride = TacticalMapStyle.Font(_theme, _textStyle);
        FontColorOverride = _color ?? TacticalMapStyle.TextColor(_theme, _textStyle);
    }
}
