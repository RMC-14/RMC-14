using Content.Client.UserInterface.ControlExtensions;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.FloatSpinBox;

namespace Content.Client._RMC14.UserInterface;

public static class UIExtensions
{
    public static FloatSpinBox CreateDialSpinBox(float value = default, Action<FloatSpinBoxEventArgs>? onValueChanged = null, bool buttons = true, int minWidth = 130)
    {
        var spinBox = new FloatSpinBox(1, 0) { MinWidth = minWidth };
        spinBox.Value = value;
        spinBox.OnValueChanged += onValueChanged;
        if (!buttons)
        {
            foreach (var button in spinBox.GetControlOfType<Button>())
            {
                button.Visible = false;
            }
        }

        return spinBox;
    }
}
