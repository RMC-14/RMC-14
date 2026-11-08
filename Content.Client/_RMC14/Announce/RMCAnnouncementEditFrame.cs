using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementEditFrame : PanelContainer
{
    public event Action<Vector2>? Dragged;
    public event Action<float>? Scrolled;

    private Vector2? _grabOffset;

    public RMCAnnouncementEditFrame(Control content)
    {
        MouseFilter = MouseFilterMode.Stop;
        DefaultCursorShape = CursorShape.Hand;
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.Black.WithAlpha(0.35f),
            BorderColor = Color.Gold,
            BorderThickness = new Thickness(1),
        };

        AddChild(content);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _grabOffset = args.RelativePosition;
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _grabOffset = null;
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (_grabOffset is { } offset)
            Dragged?.Invoke(args.GlobalPosition - offset);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        Scrolled?.Invoke(args.Delta.Y);
        args.Handle();
    }
}
