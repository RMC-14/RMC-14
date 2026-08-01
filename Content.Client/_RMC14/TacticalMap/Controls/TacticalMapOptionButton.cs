using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed class TacticalMapOptionButton : OptionButton
{
    private static readonly Color BackgroundColor = Color.FromHex("#1A1F27").WithAlpha(0.75f);
    private static readonly Color BorderColor = Color.FromHex("#2C3440").WithAlpha(0.8f);

    private readonly Label? _label;
    private readonly TextureRect? _triangle;

    public TacticalMapOptionButton()
    {
        StyleBoxOverride = CreateStyleBox();

        _label = FindChild<Label>(this);
        if (_label != null)
        {
            _label.ClipText = true;
            _label.Align = Label.AlignMode.Center;
        }

        _triangle = FindChild<TextureRect>(this);
        UpdateVisualState();
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateVisualState();
    }

    public override void ButtonOverride(Button button)
    {
        button.StyleBoxOverride = CreateStyleBox();
        button.TextAlign = Label.AlignMode.Center;
        button.ClipText = true;
        button.Label.FontColorOverride = TacticalMapInnerButton.DefaultTextColor;
    }

    private void UpdateVisualState()
    {
        var color = Disabled
            ? TacticalMapInnerButton.DefaultDisabledTextColor
            : (IsHovered ? TacticalMapInnerButton.DefaultHoveredTextColor : TacticalMapInnerButton.DefaultTextColor);

        if (_label != null)
            _label.FontColorOverride = color;

        if (_triangle != null)
            _triangle.ModulateSelfOverride = color;
    }

    private static StyleBoxFlat CreateStyleBox()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = BackgroundColor,
            BorderColor = BorderColor,
            BorderThickness = new Thickness(1),
        };
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
