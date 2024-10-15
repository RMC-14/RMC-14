using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.FloatSpinBox;

namespace Content.Client._RMC14.UserInterface;

public static class UIExtensions
{
    public static FloatSpinBox CreateDialSpinBox(float value = default, Action<FloatSpinBoxEventArgs>? onValueChanged = null)
    {
        var spinBox = new FloatSpinBox(1, 0) { MinWidth = 130 };
        spinBox.Value = value;
        spinBox.OnValueChanged += onValueChanged;
        return spinBox;
    }
}
