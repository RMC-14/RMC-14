using System.Diagnostics.CodeAnalysis;
using Content.Client.Paper.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Base for paper tags that render as a button which, when clicked, asks the server
/// to replace the tag with the current in-game date or time.
/// </summary>
public abstract class PaperTimeStampTagHandler : IMarkupTagHandler
{
    public abstract string Name { get; }

    protected abstract PaperTimeStampType Type { get; }

    protected abstract string ButtonText { get; }

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var btn = new PaperTimeStampButton
        {
            Type = Type,
            Text = ButtonText,
        };

        btn.OnPressed += _ =>
        {
            var parent = btn.Parent;
            while (parent != null && parent is not PaperWindow)
                parent = parent.Parent;

            if (parent is not PaperWindow paperWindow)
                return;

            var index = CountButtonsBefore(paperWindow, btn);
            paperWindow.SendTimeStampRequest(btn.Type, index);
        };

        control = btn;
        return true;
    }

    /// <summary>
    /// Counts buttons of the same type that come before the clicked one in document order,
    /// which tells us which [date] or [time] tag in the text it represents.
    /// </summary>
    private static int CountButtonsBefore(Control root, PaperTimeStampButton target)
    {
        var count = 0;
        var found = false;
        CountRecursive(root, target, ref count, ref found);
        return found ? count : 0;
    }

    private static void CountRecursive(Control control, PaperTimeStampButton target, ref int count, ref bool found)
    {
        if (found)
            return;

        if (control is PaperTimeStampButton btn && btn.Type == target.Type)
        {
            if (btn == target)
            {
                found = true;
                return;
            }

            count++;
        }

        foreach (var child in control.Children)
        {
            CountRecursive(child, target, ref count, ref found);
        }
    }

    private sealed class PaperTimeStampButton : PaperTagButton
    {
        public PaperTimeStampType Type;

        public PaperTimeStampButton() : base(48)
        {
        }
    }
}
