using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsBui
{
    private static readonly AttachedProperty<TextState?> TextStateProperty =
        AttachedProperty<TextState?>.CreateNull("RequisitionsTextState", typeof(RequisitionsBui));

    private static void SetText(Label label, string text)
    {
        if (label.Text != text)
            label.Text = text;
    }

    private static void SetText(RichTextLabel label, string text, bool markup = false, Color? color = null)
    {
        var state = label.GetValue(TextStateProperty);
        if (state == null)
        {
            state = new TextState();
            label.SetValue(TextStateProperty, state);
        }
        if (state.Text == text && state.Markup == markup && state.Color == color)
            return;

        label.SetMessage(markup ? FormattedMessage.FromMarkupOrThrow(text) : FormattedMessage.FromUnformatted(text),
            defaultColor: color);
        state.Text = text;
        state.Markup = markup;
        state.Color = color;
    }

    private sealed class TextState
    {
        public string? Text;
        public bool Markup;
        public Color? Color;
    }

    private static void ClearTextCache(Control control)
    {
        if (control is RichTextLabel label && label.GetValue(TextStateProperty) != null)
            label.SetValue(TextStateProperty, null);
        foreach (var child in control.Children)
            ClearTextCache(child);
    }
}
