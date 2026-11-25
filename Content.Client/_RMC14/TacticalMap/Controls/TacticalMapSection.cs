using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap.Controls;

[Virtual]
public class TacticalMapSection : BoxContainer, ITacticalMapThemed
{
    private const float Padding = 6;

    private readonly TacticalMapPanel _header;
    private readonly TacticalMapLabel _title;
    private readonly TacticalMapLabel _detail;
    private TacticalMapTheme _theme = TacticalMapTheme.Marine;

    public TacticalMapSection()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;

        _title = new TacticalMapLabel
        {
            TextStyle = TacticalMapTextStyle.Heading,
            HorizontalExpand = true,
            ClipText = true,
            VAlign = Label.VAlignMode.Center,
        };

        _detail = new TacticalMapLabel
        {
            TextStyle = TacticalMapTextStyle.Small,
            Visible = false,
            VAlign = Label.VAlignMode.Center,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin = new Thickness(Padding, 2),
            MinHeight = 18,
        };

        row.AddChild(_title);
        row.AddChild(_detail);

        _header = new TacticalMapPanel { Kind = TacticalMapPanelKind.Header };
        _header.AddChild(row);
        AddChild(_header);
    }

    public string? Title
    {
        get => _title.Text;
        set => _title.Text = value?.ToUpperInvariant();
    }

    public string? Detail
    {
        get => _detail.Text;
        set
        {
            _detail.Text = value;
            _detail.Visible = !string.IsNullOrEmpty(value);
        }
    }

    public virtual void ApplyTheme(TacticalMapTheme theme)
    {
        _theme = theme;
    }

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);
        if (newChild == _header)
            return;

        newChild.Margin = new Thickness(Padding, 0, Padding, Padding);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var box = UIBox2.FromDimensions(Vector2.Zero, PixelSize);
        handle.DrawRect(box, _theme.Surface);
        handle.DrawRect(box, _theme.Border, false);
    }
}
