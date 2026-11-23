using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed class TacticalMapOptionButton : OptionButton, ITacticalMapThemed
{
    private readonly Label? _label;
    private readonly TextureRect? _triangle;
    private TacticalMapTheme _theme = TacticalMapTheme.Marine;

    public TacticalMapOptionButton()
    {
        _label = FindChild<Label>(this);
        if (_label != null)
        {
            _label.ClipText = true;
            _label.Align = Label.AlignMode.Left;
        }

        _triangle = FindChild<TextureRect>(this);
        UpdateVisualState();
    }

    public void ApplyTheme(TacticalMapTheme theme)
    {
        _theme = theme;
        UpdateVisualState();
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateVisualState();
    }

    public override void ButtonOverride(Button button)
    {
        button.StyleBoxOverride = TacticalMapStyle.Box(_theme.Surface, _theme.Border, padding: 3);
        button.TextAlign = Label.AlignMode.Left;
        button.ClipText = true;
        button.Label.FontOverride = TacticalMapStyle.Font(_theme);
        button.Label.FontColorOverride = _theme.Text;
    }

    private void UpdateVisualState()
    {
        var hovered = DrawMode == DrawModeEnum.Hover;
        var color = Disabled ? _theme.TextDisabled : _theme.Text;
        var box = TacticalMapStyle.Box(
            hovered ? Color.InterpolateBetween(_theme.Surface, _theme.Text, _theme.Hover.A) : _theme.Surface,
            hovered ? _theme.TextMuted : _theme.Border);
        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, 1);
        StyleBoxOverride = box;

        if (_label != null)
        {
            _label.FontOverride = TacticalMapStyle.Font(_theme);
            _label.FontColorOverride = color;
        }

        if (_triangle != null)
            _triangle.ModulateSelfOverride = color;
    }

    private static T? FindChild<T>(Control root) where T : Control
    {
        var queue = new Queue<Control>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T match)
                return match;

            foreach (var child in current.Children)
            {
                queue.Enqueue(child);
            }
        }

        return null;
    }
}
