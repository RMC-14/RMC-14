using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// An inline button used by paper tags ([form], [signature], [check], [date], [time]).
/// It is exactly one text line tall with no vertical padding or margin, so the rich text
/// label lays it out like a normal line of text and never pushes the following line down.
/// </summary>
[Virtual]
public class PaperTagButton : Button
{
    /// <summary>
    /// Height of one line of paper text, in UI units. Set by the paper window from its font.
    /// </summary>
    public static float LineHeight { get; private set; } = 16f;

    private readonly float _width;
    private StyleBox? _sourceStyleBox;

    /// <param name="width">Button width in UI units, or null to make it square.</param>
    public PaperTagButton(float? width = null)
    {
        _width = width ?? -1;
        Margin = new Thickness(1, 0, 1, 0);
        TextAlign = Label.AlignMode.Center;
        AddStyleClass("ButtonSquare");
        ApplySize();
    }

    private void ApplySize()
    {
        var width = _width < 0 ? LineHeight : _width;
        MinSize = new Vector2(width, LineHeight);
        MaxSize = new Vector2(width, LineHeight);
    }

    /// <summary>
    /// Updates the line height and resizes any paper tag buttons already under <paramref name="root"/>.
    /// </summary>
    public static void SetLineHeight(float lineHeight, Control root)
    {
        if (MathHelper.CloseTo(lineHeight, LineHeight))
            return;

        LineHeight = lineHeight;
        ResizeRecursive(root);
    }

    private static void ResizeRecursive(Control control)
    {
        if (control is PaperTagButton button)
            button.ApplySize();

        foreach (var child in control.Children)
        {
            ResizeRecursive(child);
        }
    }

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();

        // The square button style has padding above and below its label, which would make the button
        // taller than a text line. Use a copy of that style box with the vertical padding removed.
        if (!TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box) || box == _sourceStyleBox)
            return;

        _sourceStyleBox = box;

        if (box is not StyleBoxTexture texture)
        {
            StyleBoxOverride = null;
            return;
        }

        var copy = new StyleBoxTexture(texture);
        copy.SetContentMarginOverride(StyleBox.Margin.Vertical, 0);
        copy.SetPadding(StyleBox.Margin.Vertical, 0);
        StyleBoxOverride = copy;
    }
}
