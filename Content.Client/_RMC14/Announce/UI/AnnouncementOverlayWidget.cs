using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementOverlayWidget : UIWidget
{
    private const float StackSeparation = 16f;

    private readonly LayoutContainer _layout;
    private readonly List<AnnouncementWidget> _announcements = new();
    private readonly List<AnnouncementWidget> _managedAnnouncements = new();

    public IReadOnlyList<AnnouncementWidget> Announcements => _announcements;

    public AnnouncementOverlayWidget()
    {
        MouseFilter = MouseFilterMode.Ignore;
        HorizontalExpand = true;
        VerticalExpand = true;
        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);

        _layout = new LayoutContainer
        {
            MouseFilter = MouseFilterMode.Ignore,
            HorizontalExpand = true,
            VerticalExpand = true
        };
        LayoutContainer.SetAnchorPreset(_layout, LayoutContainer.LayoutPreset.Wide);
        AddChild(_layout);
    }

    public void AddAnnouncement(AnnouncementWidget widget)
    {
        AddAnnouncement(widget, true);
    }

    public void AddStandaloneAnnouncement(AnnouncementWidget widget)
    {
        AddAnnouncement(widget, false);
    }

    private void AddAnnouncement(AnnouncementWidget widget, bool managed)
    {
        widget.PositionManaged = managed;
        _announcements.Add(widget);
        if (managed)
            _managedAnnouncements.Add(widget);

        _layout.AddChild(widget);
        Visible = true;
        if (managed)
            Reflow();
    }

    public void RemoveAnnouncement(AnnouncementWidget widget)
    {
        _announcements.Remove(widget);
        var managed = _managedAnnouncements.Remove(widget);
        widget.Parent?.RemoveChild(widget);
        Visible = _announcements.Count > 0;
        if (managed)
            Reflow();
    }

    public void ClearAnnouncements()
    {
        foreach (var widget in _announcements.ToArray())
        {
            widget.Parent?.RemoveChild(widget);
        }

        _announcements.Clear();
        _managedAnnouncements.Clear();
        Visible = false;
    }

    public void Reflow()
    {
        if (_managedAnnouncements.Count == 0)
            return;

        var sizes = new Vector2[_managedAnnouncements.Count];
        var stackWidth = 0f;
        var stackHeight = 0f;

        for (var i = 0; i < _managedAnnouncements.Count; i++)
        {
            var layout = _managedAnnouncements[i].ResolveLayout();
            sizes[i] = layout.Size;
            stackWidth = MathF.Max(stackWidth, layout.Size.X);
            stackHeight += layout.Size.Y;
        }

        stackHeight += StackSeparation * (_managedAnnouncements.Count - 1);
        var stackSize = new Vector2(stackWidth, stackHeight);
        var anchorLayout = _managedAnnouncements[0].ResolveLayout(stackSize);
        var alignment = _managedAnnouncements[0].ResolveStackAlignment();
        var positions = CalculateStackPositions(anchorLayout.Position, stackWidth, sizes, alignment, StackSeparation);

        for (var i = 0; i < _managedAnnouncements.Count; i++)
        {
            _managedAnnouncements[i].ApplyManagedLayout(positions[i], sizes[i]);
        }
    }

    internal static Vector2[] CalculateStackPositions(
        Vector2 anchor,
        float stackWidth,
        IReadOnlyList<Vector2> sizes,
        HAlignment alignment,
        float separation)
    {
        var positions = new Vector2[sizes.Count];
        var currentY = anchor.Y;
        for (var i = 0; i < sizes.Count; i++)
        {
            var size = sizes[i];
            var x = alignment switch
            {
                HAlignment.Right => anchor.X + stackWidth - size.X,
                HAlignment.Center => anchor.X + (stackWidth - size.X) * 0.5f,
                _ => anchor.X
            };

            positions[i] = new Vector2(x, currentY);
            currentY += size.Y + separation;
        }

        return positions;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        Reflow();
    }
}
