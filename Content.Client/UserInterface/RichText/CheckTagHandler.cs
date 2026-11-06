using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Client.Paper.UI;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Converts [check] tags into clickable buttons that toggle between ✔ and ✖.
/// </summary>
public sealed class CheckTagHandler : IMarkupTagHandler
{
    public string Name => "check";
    private static int _checkCounter = 0;

    private static int GetCheckIndex(MarkupNode node)
    {
        return _checkCounter++;
    }

    /// <summary>
    /// Resets the check counter to ensure consistent indexing across renders.
    /// </summary>
    public static void ResetCheckCounter()
    {
        _checkCounter = 0;
    }

    /// <summary>
    /// Counts check buttons before the clicked button to determine which [check] tag it represents.
    /// </summary>
    private static int CountCheckButtonsBefore(Control clickedButton)
    {
        var count = 0;
        var root = clickedButton;

        // Find the root container
        while (root.Parent != null)
            root = root.Parent;

        // Count check buttons in document order
        var found = false;
        CountCheckButtonsRecursive(root, clickedButton, ref count, ref found);
        return found ? count : 0;
    }

    private static void CountCheckButtonsRecursive(Control control, Control target, ref int count, ref bool found)
    {
        if (found) return;

        if (control is Button btn && (btn.Text == "☐" || btn.Text == "✔" || btn.Text == "✖"))
        {
            if (control == target)
            {
                found = true;
                return;
            }
            count++;
        }

        foreach (Control child in control.Children)
        {
            CountCheckButtonsRecursive(child, target, ref count, ref found);
        }
    }

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public string TextBefore(MarkupNode node) => "";
    public string TextAfter(MarkupNode node) => "";

    /// <summary>
    /// Creates a clickable button to replace the [check] tag.
    /// </summary>
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var btn = new PaperTagButton { Text = "☐" };

        var checkIndex = GetCheckIndex(node);
        btn.Name = $"check_{checkIndex}";

        btn.OnPressed += _ =>
        {
            // Find the PaperWindow parent
            var parent = btn.Parent;
            while (parent != null && parent is not PaperWindow)
                parent = parent.Parent;

            if (parent is PaperWindow paperWindow)
            {
                // Count buttons to determine which [check] tag this represents
                var buttonIndex = CountCheckButtonsBefore(btn);
                paperWindow.OpenCheckDialog(buttonIndex);
            }
        };

        control = btn;
        return true;
    }

    /// <summary>
    /// Replaces the nth occurrence of [check] tag with replacement symbol.
    /// </summary>
    private static string ReplaceNthCheckTag(string text, int index, string replacement)
    {
        const string checkTag = "[check]";
        var currentIndex = 0;
        var pos = 0;

        while (pos < text.Length)
        {
            var foundPos = text.IndexOf(checkTag, pos);
            if (foundPos == -1) break;

            if (currentIndex == index)
            {
                return text.Substring(0, foundPos) + replacement + text.Substring(foundPos + checkTag.Length);
            }

            currentIndex++;
            pos = foundPos + checkTag.Length;
        }

        return text;
    }
}