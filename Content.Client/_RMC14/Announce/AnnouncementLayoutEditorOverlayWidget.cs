using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementLayoutEditorOverlayWidget : LayoutContainer
{
    public event Action<Vector2>? PreviewPositionChanged;
    public event Action<float>? PreviewScaleChanged;

    public Vector2? CurrentPosition => ResolveCurrentNormalizedPosition();
    public float CurrentScale => _previewWidget.ActiveAnnouncement?.Data.LayoutScale ?? 1f;

    private readonly AnnouncementWidget _previewWidget;
    private bool _dragging;
    private Vector2 _dragGrabRatio = new(0.5f, 0.5f);

    public AnnouncementLayoutEditorOverlayWidget()
    {
        MouseFilter = MouseFilterMode.Pass;
        Visible = false;
        HorizontalExpand = true;
        VerticalExpand = true;
        LayoutContainer.SetAnchorPreset(this, LayoutPreset.TopLeft);
        LayoutContainer.SetGrowHorizontal(this, GrowDirection.Constrain);
        LayoutContainer.SetGrowVertical(this, GrowDirection.Constrain);

        _previewWidget = new AnnouncementWidget
        {
            PreviewMode = true,
            MouseFilter = MouseFilterMode.Ignore,
        };

        AddChild(_previewWidget);
    }

    public void ShowPreview(UIScreen screen, AnnouncementDisplayData announcement)
    {
        SyncToScreen(screen);
        _previewWidget.ForcedScreenSize = Size.X > 0f && Size.Y > 0f ? Size : screen.Size;
        _previewWidget.ShowAnnouncement(announcement);
        Visible = true;
    }

    public void HidePreview()
    {
        _dragging = false;
        Visible = false;
        _previewWidget.ForcedScreenSize = null;
        _previewWidget.PreviewMode = false;
        _previewWidget.Visible = false;
        _previewWidget.PreviewMode = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!Visible)
            return;

        if (IoCManager.Resolve<IUserInterfaceManager>().ActiveScreen is { } screen)
        {
            SyncToScreen(screen);
            _previewWidget.ForcedScreenSize = Size.X > 0f && Size.Y > 0f ? Size : screen.Size;
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!Visible || args.Function != EngineKeyFunctions.UIClick)
            return;

        if (!TryBeginDrag(args.RelativePosition))
            return;

        _dragging = true;
        UpdateDraggedPosition(args.RelativePosition);
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragging = false;
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_dragging || !Visible)
            return;

        UpdateDraggedPosition(args.RelativePosition);
        args.Handle();
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (!Visible || _previewWidget.ActiveAnnouncement == null || args.Delta.Y == 0f)
            return;

        var nextScale = Math.Clamp(_previewWidget.ActiveAnnouncement.Data.LayoutScale + args.Delta.Y * 0.05f, 0.1f, 2.5f);
        _previewWidget.ActiveAnnouncement.Data.LayoutScale = nextScale;
        _previewWidget.ShowAnnouncement(_previewWidget.ActiveAnnouncement.Data);
        PreviewScaleChanged?.Invoke(nextScale);
        args.Handle();
    }

    private void SyncToScreen(Control screen)
    {
        var position = screen.Position;
        var size = screen.Size;

        LayoutContainer.SetMarginLeft(this, position.X);
        LayoutContainer.SetMarginTop(this, position.Y);
        LayoutContainer.SetMarginRight(this, position.X + size.X);
        LayoutContainer.SetMarginBottom(this, position.Y + size.Y);
        SetWidth = size.X;
        SetHeight = size.Y;
        MinWidth = size.X;
        MinHeight = size.Y;
    }

    private bool TryBeginDrag(Vector2 localPosition)
    {
        var widgetSize = ResolveWidgetSize();
        if (widgetSize.X <= 0f || widgetSize.Y <= 0f)
            return false;

        var widgetPosition = _previewWidget.Position;
        var widgetRect = UIBox2.FromDimensions(widgetPosition, widgetSize);
        if (!widgetRect.Contains(localPosition))
            return false;

        var widgetLocalPosition = localPosition - widgetPosition;
        _dragGrabRatio = new Vector2(
            Math.Clamp(widgetLocalPosition.X / widgetSize.X, 0f, 1f),
            Math.Clamp(widgetLocalPosition.Y / widgetSize.Y, 0f, 1f));

        return Width > 0f && Height > 0f;
    }

    private void UpdateDraggedPosition(Vector2 localPosition)
    {
        if (_previewWidget.ActiveAnnouncement == null)
            return;

        var boundsSize = Size;
        if (boundsSize.X <= 0f || boundsSize.Y <= 0f)
            return;

        var widgetSize = ResolveWidgetSize();
        if (widgetSize.X <= 0f || widgetSize.Y <= 0f)
            return;

        var grabOffset = widgetSize * _dragGrabRatio;
        var topLeft = localPosition - grabOffset;
        var normalized = new Vector2(
            Math.Clamp(topLeft.X / boundsSize.X, 0f, 1f),
            Math.Clamp(topLeft.Y / boundsSize.Y, 0f, 1f));

        _previewWidget.ActiveAnnouncement.Data.ScreenPositionOverride = normalized;
        PreviewPositionChanged?.Invoke(normalized);
    }

    private Vector2 ResolveWidgetSize()
    {
        var size = _previewWidget.Size;
        if (size.X > 0f && size.Y > 0f)
            return size;

        var desired = _previewWidget.DesiredSize;
        if (desired.X > 0f && desired.Y > 0f)
            return desired;

        return Vector2.Zero;
    }

    private Vector2? ResolveCurrentNormalizedPosition()
    {
        if (_previewWidget.ActiveAnnouncement == null)
            return null;

        var boundsSize = Size;
        if (boundsSize.X <= 0f || boundsSize.Y <= 0f)
            return _previewWidget.ActiveAnnouncement.Data.ScreenPositionOverride;

        var widgetPosition = _previewWidget.Position;
        return new Vector2(
            Math.Clamp(widgetPosition.X / boundsSize.X, 0f, 1f),
            Math.Clamp(widgetPosition.Y / boundsSize.Y, 0f, 1f));
    }
}
