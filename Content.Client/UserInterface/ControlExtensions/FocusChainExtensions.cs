using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.ControlExtensions;

public static class FocusChainExtensions
{
    /// Works with <see cref="LineEdit"/>, <see cref="FloatSpinBox"/>, and any other control
    /// that contains a <see cref="LineEdit"/> or can receive keyboard focus directly.
    public static void LinkTabFocus(params Control[] controls)
    {
        if (controls.Length < 2)
            return;

        var targets = new Control?[controls.Length];
        for (var i = 0; i < controls.Length; i++)
        {
            targets[i] = GetFocusTarget(controls[i]);
        }

        for (var i = 0; i < targets.Length; i++)
        {
            if (targets[i] is not { } current)
                continue;

            var next = targets[(i + 1) % targets.Length];
            var prev = targets[(i - 1 + targets.Length) % targets.Length];

            current.OnKeyBindDown += args =>
            {
                if (args.Function == EngineKeyFunctions.GuiTabNavigateNext)
                {
                    next?.GrabKeyboardFocus();
                    if (next is LineEdit nextEdit)
                        nextEdit.CursorPosition = nextEdit.Text.Length;
                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.GuiTabNavigatePrev)
                {
                    prev?.GrabKeyboardFocus();
                    if (prev is LineEdit prevEdit)
                        prevEdit.CursorPosition = prevEdit.Text.Length;
                    args.Handle();
                }
            };
        }
    }

    private static Control? GetFocusTarget(Control control)
    {
        if (control is LineEdit)
            return control;

        foreach (var edit in control.GetControlOfType<LineEdit>())
            return edit;

        return control.CanKeyboardFocus ? control : null;
    }
}
