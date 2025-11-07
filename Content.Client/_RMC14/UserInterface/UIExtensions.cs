using Content.Client.UserInterface.ControlExtensions;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
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

    public static T CreatePopOutableWindow<T>(this BoundUserInterface bui) where T : RMCPopOutWindow, new()
    {
        var window = bui.CreateDisposableControl<T>();
        window.OnFinalClose += bui.Close;

        if (IoCManager.Resolve<IEntityManager>().System<UserInterfaceSystem>().TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCentered();
        }

        return window;
    }

    public static void RemoveChildExcept(this Control parent, Control except)
    {
        for (var i = parent.ChildCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child != except)
                parent.RemoveChild(i);
        }
    }

    public static void RemoveChildrenAfter(this Control parent, int after)
    {
        for (var i = parent.ChildCount - 1; i >= after; i--)
        {
            parent.RemoveChild(i);
        }
    }

    public static void SetTabVisibleAfter(this Control parent, int after, bool visible)
    {
        for (var i = parent.ChildCount - 1; i >= after; i--)
        {
            var child = parent.GetChild(i);
            TabContainer.SetTabVisible(child, visible);
        }
    }

    public static void SetVisibleAfter(this Control parent, int after, bool visible)
    {
        for (var i = parent.ChildCount - 1; i >= after; i--)
        {
            var child = parent.GetChild(i);
            child.Visible = visible;
        }
    }
}
