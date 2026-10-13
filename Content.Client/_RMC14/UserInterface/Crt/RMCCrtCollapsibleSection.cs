using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Crt;

/// <summary>
/// A wrapping disclosure button with an optional summary. Its contents inherit the surrounding theme.
/// </summary>
public sealed class RMCCrtCollapsibleSection : BoxContainer
{
    public readonly RMCCrtLabel Summary = new()
    {
        TextFontSize = 10,
        HorizontalExpand = true,
        MouseFilter = MouseFilterMode.Ignore,
        Visible = false,
    };
    public readonly BoxContainer Body = new()
    {
        Orientation = LayoutOrientation.Vertical,
        SeparationOverride = 6,
        HorizontalExpand = true,
        Margin = new Thickness(30, 0, 9, 8),
        Visible = false,
    };
    private readonly RMCCrtActionButton _header = new()
    {
        ContentAlignment = RMCCrtContentAlignment.Left,
        TextFontSize = 10,
        HorizontalExpand = true,
    };
    private readonly RMCCrtLabel _indicator = new()
    {
        TextFontSize = 10,
        SetWidth = 14,
        VerticalAlignment = VAlignment.Top,
        MouseFilter = MouseFilterMode.Ignore,
    };
    private string _title = string.Empty;
    private bool _expanded;

    public event Action<bool>? OnToggled;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            UpdateHeader();
        }
    }

    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
                return;
            _expanded = value;
            Body.Visible = value;
            UpdateHeader();
            OnToggled?.Invoke(value);
        }
    }

    public RMCCrtCollapsibleSection()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        HorizontalExpand = true;
        _header.Background.ScanlineOpacity = 0.08f;
        // Keep the indicator outside the wrapping text so every line starts at the same position.
        _header.Layout.RemoveChild(_header.TextLabel);
        var content = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
            MouseFilter = MouseFilterMode.Ignore,
        };
        _header.TextLabel.HorizontalExpand = true;
        content.AddChild(_header.TextLabel);
        content.AddChild(Summary);
        _header.Layout.AddChild(_indicator);
        _header.Layout.AddChild(content);
        AddChild(_header);
        AddChild(Body);
        _header.OnPressed += _ => Expanded = !Expanded;
        UpdateHeader();
    }

    private void UpdateHeader()
    {
        _header.Text = Title;
        _indicator.Text = Expanded ? "▾" : "▸";
    }
}
