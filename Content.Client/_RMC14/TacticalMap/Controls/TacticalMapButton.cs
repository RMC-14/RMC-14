using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap.Controls;

public enum TacticalMapButtonKind
{
    Normal,
    Primary,
    Danger,
    Flat,
}

public sealed class TacticalMapButton : ContainerButton, ITacticalMapThemed
{
    private readonly StyleBoxFlat _box = new();
    private readonly BoxContainer? _content;
    private readonly TextureRect? _icon;
    private readonly Label? _label;
    private TacticalMapTheme _theme = TacticalMapTheme.Marine;
    private TacticalMapButtonKind _kind;
    private TacticalMapTextStyle _textStyle = TacticalMapTextStyle.Normal;
    private bool _active;
    private bool _alignLeft;
    private Color? _swatch;

    public TacticalMapButton()
    {
        StyleBoxOverride = _box;
        _box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);
        _box.SetContentMarginOverride(StyleBox.Margin.Vertical, 1);

        _icon = new TextureRect
        {
            Visible = false,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(14, 14),
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };

        _label = new Label
        {
            Visible = false,
            VAlign = Label.VAlignMode.Center,
            VerticalAlignment = VAlignment.Center,
        };

        _content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };

        _content.AddChild(_icon);
        _content.AddChild(_label);
        AddChild(_content);
        UpdateStyle();
    }

    public string? Text
    {
        get => _label?.Text;
        set
        {
            if (_label == null)
                return;

            _label.Text = value;
            _label.Visible = !string.IsNullOrEmpty(value);
        }
    }

    public Texture? Icon
    {
        get => _icon?.Texture;
        set
        {
            if (_icon == null)
                return;

            _icon.Texture = value;
            _icon.Visible = value != null;
            _icon.Margin = string.IsNullOrEmpty(Text) ? new Thickness(0) : new Thickness(0, 0, 4, 0);
        }
    }

    public bool TintIcon { get; set; }

    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value)
                return;

            _active = value;
            UpdateStyle();
        }
    }

    public TacticalMapButtonKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            UpdateStyle();
        }
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

    public bool AlignLeft
    {
        get => _alignLeft;
        set
        {
            _alignLeft = value;
            if (_content != null)
                _content.HorizontalAlignment = value ? HAlignment.Left : HAlignment.Center;
        }
    }

    public Color? Swatch
    {
        get => _swatch;
        set
        {
            _swatch = value;
            UpdateStyle();
        }
    }

    public void ApplyTheme(TacticalMapTheme theme)
    {
        _theme = theme;
        UpdateStyle();
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateStyle();
    }

    private void UpdateStyle()
    {
        if (_label == null || _icon == null)
            return;

        var theme = _theme;
        var mode = DrawMode;
        var hover = mode == DrawModeEnum.Hover;
        var pressed = mode == DrawModeEnum.Pressed;
        var disabled = mode == DrawModeEnum.Disabled;

        Color background;
        Color border;
        Color text;
        var thickness = 1f;

        if (_swatch is { } swatch)
        {
            background = disabled ? swatch.WithAlpha(0.35f) : swatch;
            border = _active ? theme.Text : hover ? theme.TextMuted : theme.Border;
            thickness = _active ? 2f : 1f;
            text = theme.Text;
        }
        else if (disabled)
        {
            background = _kind == TacticalMapButtonKind.Flat ? Color.Transparent : theme.Surface;
            border = _kind == TacticalMapButtonKind.Flat ? Color.Transparent : theme.Border.WithAlpha(0.5f);
            text = theme.TextDisabled;
        }
        else
        {
            switch (_kind)
            {
                case TacticalMapButtonKind.Primary:
                    background = pressed || _active
                        ? theme.Selected
                        : hover
                            ? Color.InterpolateBetween(theme.Primary, theme.Text, 0.2f)
                            : theme.Primary;
                    border = theme.Text.WithAlpha(0.6f);
                    text = pressed || _active ? theme.TextInverse : theme.PrimaryText;
                    break;
                case TacticalMapButtonKind.Danger:
                    background = pressed ? theme.Danger : hover ? theme.Danger.WithAlpha(0.25f) : theme.Surface;
                    border = theme.Danger.WithAlpha(0.8f);
                    text = pressed ? theme.TextInverse : theme.Danger;
                    break;
                case TacticalMapButtonKind.Flat:
                    background = pressed || _active ? theme.Hover : hover ? theme.Hover.WithAlpha(theme.Hover.A * 0.6f) : Color.Transparent;
                    border = Color.Transparent;
                    text = hover || pressed || _active ? theme.Text : theme.TextMuted;
                    break;
                default:
                    if (pressed || _active)
                    {
                        background = theme.Selected;
                        border = theme.Selected;
                        text = theme.TextInverse;
                    }
                    else
                    {
                        background = hover ? Color.InterpolateBetween(theme.Surface, theme.Text, theme.Hover.A) : theme.Surface;
                        border = hover ? theme.TextMuted : theme.Border;
                        text = theme.Text;
                    }

                    break;
            }
        }

        _box.BackgroundColor = background;
        _box.BorderColor = border;
        _box.BorderThickness = new Thickness(thickness);
        _label.FontOverride = TacticalMapStyle.Font(theme, _textStyle);
        _label.FontColorOverride = text;
        _icon.ModulateSelfOverride = TintIcon ? text : null;
    }
}
