using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementLayer : UIWidget
{
    public readonly RMCAnnouncementArea Area = new();

    public RMCAnnouncementLayer()
    {
        MouseFilter = MouseFilterMode.Ignore;
        HorizontalExpand = true;
        VerticalExpand = true;
        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);
        AddChild(Area);
    }
}

public sealed class RMCAnnouncementArea : Control
{
    private const float CustomMargin = 16;
    private const float DefaultSideMargin = 50;
    private const float DefaultCornerTopMargin = 100;
    private const float HudSeparation = 16;
    private const int StackSeparation = 12;
    private static readonly Vector2 ReferenceScreenSize = new(1920, 1080);

    private readonly List<Stack> _stacks = new();

    public event Action<FrameEventArgs>? FrameUpdated;
    public event Action? ScreenScaleChanged;

    public float ScreenScale { get; private set; } = 1;
    private (UIBox2 Viewport, UIBox2 Actions, UIBox2 Inventory) _hud;

    public RMCAnnouncementArea()
    {
        MouseFilter = MouseFilterMode.Ignore;
        HorizontalExpand = true;
        VerticalExpand = true;
    }

    public void Add(Control view, Vector2 pivot, bool avoidHud = false, bool exclusive = false)
    {
        Stack? stack = null;
        if (!exclusive)
        {
            foreach (var existing in _stacks)
            {
                if (!existing.Exclusive && existing.AvoidHud == avoidHud && existing.Pivot == pivot)
                {
                    stack = existing;
                    break;
                }
            }
        }

        if (stack == null)
        {
            stack = new Stack(pivot, avoidHud, exclusive);
            _stacks.Add(stack);
            AddChild(stack.Box);
        }

        view.HorizontalAlignment = GetAlignment(pivot);
        stack.Box.AddChild(view);
    }

    public void Remove(Control view)
    {
        var stack = FindStack(view);
        if (stack == null)
            return;

        stack.Box.RemoveChild(view);
        if (stack.Box.ChildCount > 0)
            return;

        _stacks.Remove(stack);
        RemoveChild(stack.Box);
    }

    public void MoveExclusive(Control view, Vector2 pivot)
    {
        if (FindStack(view) is not { Exclusive: true } stack)
            return;

        stack.Pivot = pivot;
        stack.AvoidHud = false;
        view.HorizontalAlignment = GetAlignment(pivot);
        InvalidateArrange();
    }

    public Vector2 GetPivot(Vector2 globalTopLeft, Vector2 size)
    {
        var bounds = GetBounds();
        var available = bounds.Size - size - new Vector2(CustomMargin * 2);
        var position = globalTopLeft - GlobalPosition - bounds.TopLeft - new Vector2(CustomMargin);
        var pivot = new Vector2(
            available.X > 0 ? position.X / available.X : 0,
            available.Y > 0 ? position.Y / available.Y : 0);

        return Vector2.Clamp(pivot, Vector2.Zero, Vector2.One);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Size.X > 0 && Size.Y > 0)
        {
            var screenScale = Math.Clamp((Size.X / ReferenceScreenSize.X + Size.Y / ReferenceScreenSize.Y) * 0.5f, 0.5f, 2f);
            if (!MathHelper.CloseTo(screenScale, ScreenScale, 0.01f))
            {
                ScreenScale = screenScale;
                ScreenScaleChanged?.Invoke();
            }
        }

        FrameUpdated?.Invoke(args);

        var bounds = GetBounds();
        var hud = (bounds, GetActionsBounds(), GetInventoryBounds(bounds));
        if (hud == _hud)
            return;

        _hud = hud;
        InvalidateArrange();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        foreach (var stack in _stacks)
        {
            stack.Box.Measure(availableSize);
        }

        return Vector2.Zero;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var bounds = GetBounds();
        var actions = GetActionsBounds();
        var inventory = GetInventoryBounds(bounds);

        foreach (var stack in _stacks)
        {
            var size = Vector2.Min(stack.Box.DesiredSize, bounds.Size);
            Vector2 position;
            if (stack.AvoidHud)
            {
                var topMargin = stack.Pivot.Y == 0 && !MathHelper.CloseTo(stack.Pivot.X, 0.5f)
                    ? DefaultCornerTopMargin
                    : DefaultSideMargin;

                var available = Vector2.Max(
                    bounds.Size - size - new Vector2(DefaultSideMargin * 2, topMargin + DefaultSideMargin),
                    Vector2.Zero);

                position = bounds.TopLeft + new Vector2(DefaultSideMargin, topMargin) + available * stack.Pivot;

                if (stack.Pivot == new Vector2(0, 1) && inventory.Width > 0 && inventory.Height > 0)
                    position = PlaceOutsideBottomLeftHudArea(position, size, inventory, bounds);

                if (actions.Width > 0 && actions.Height > 0)
                    position = AvoidOverlap(position, size, actions, bounds);
            }
            else
            {
                var available = Vector2.Max(bounds.Size - size - new Vector2(CustomMargin * 2), Vector2.Zero);
                position = bounds.TopLeft + new Vector2(CustomMargin) + available * stack.Pivot;
            }

            stack.Box.Arrange(UIBox2.FromDimensions(position, size));
        }

        return finalSize;
    }

    private UIBox2 GetBounds()
    {
        var viewport = UserInterfaceManager.ActiveScreen?.GetWidget<MainViewport>();
        if (viewport is { VisibleInTree: true, Size.X: > 0, Size.Y: > 0 })
            return UIBox2.FromDimensions(viewport.GlobalPosition - GlobalPosition, viewport.Size);

        return UIBox2.FromDimensions(Vector2.Zero, Size);
    }

    private UIBox2 GetActionsBounds()
    {
        var actions = UserInterfaceManager.ActiveScreen?.GetWidget<ActionsBar>()?.ActionsContainer;
        if (actions is not { VisibleInTree: true } || actions.Size.X <= 0 || actions.Size.Y <= 0)
            return default;

        return UIBox2.FromDimensions(actions.GlobalPosition - GlobalPosition, actions.Size);
    }

    private UIBox2 GetInventoryBounds(UIBox2 bounds)
    {
        var inventory = UserInterfaceManager.ActiveScreen?.GetWidget<InventoryGui>();
        if (inventory == null)
            return default;

        var expanded = inventory.InventoryHotbar.GetExpandedDesiredSize(bounds.Size);
        if (expanded.X <= 0 || expanded.Y <= 0)
            return default;

        var position = inventory.GlobalPosition - GlobalPosition;
        var bottom = position.Y + inventory.Size.Y;
        return UIBox2.FromDimensions(new Vector2(position.X, bottom - expanded.Y), expanded);
    }

    private static Vector2 PlaceOutsideBottomLeftHudArea(Vector2 position, Vector2 size, UIBox2 reserved, UIBox2 bounds)
    {
        var right = new Vector2(Math.Max(position.X, reserved.Right + HudSeparation), position.Y);
        if (right.X + size.X <= bounds.Right)
            return right;

        var above = new Vector2(
            Math.Clamp(position.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - size.X)),
            reserved.Top - size.Y - HudSeparation);

        if (above.Y >= bounds.Top)
            return above;

        return new Vector2(
            Math.Clamp(right.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - size.X)),
            Math.Clamp(position.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - size.Y)));
    }

    private static Vector2 AvoidOverlap(Vector2 position, Vector2 size, UIBox2 obstacle, UIBox2 bounds)
    {
        if (!UIBox2.FromDimensions(position, size).Intersects(obstacle))
            return position;

        var right = new Vector2(obstacle.Right + HudSeparation, position.Y);
        if (right.X + size.X <= bounds.Right)
            return right;

        var below = new Vector2(position.X, obstacle.Bottom + HudSeparation);
        if (below.Y + size.Y <= bounds.Bottom)
            return below;

        return right;
    }

    private Stack? FindStack(Control view)
    {
        foreach (var stack in _stacks)
        {
            if (view.Parent == stack.Box)
                return stack;
        }

        return null;
    }

    private static HAlignment GetAlignment(Vector2 pivot)
    {
        return pivot.X switch
        {
            < 0.34f => HAlignment.Left,
            > 0.66f => HAlignment.Right,
            _ => HAlignment.Center,
        };
    }

    private sealed class Stack(Vector2 pivot, bool avoidHud, bool exclusive)
    {
        public Vector2 Pivot = pivot;
        public bool AvoidHud = avoidHud;
        public readonly bool Exclusive = exclusive;

        public readonly BoxContainer Box = new()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = StackSeparation,
            MouseFilter = MouseFilterMode.Ignore,
        };
    }
}
