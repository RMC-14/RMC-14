using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

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
    private const float ScreenMargin = 16;
    private const int StackSeparation = 12;

    private readonly List<Stack> _stacks = new();

    public RMCAnnouncementArea()
    {
        MouseFilter = MouseFilterMode.Ignore;
        HorizontalExpand = true;
        VerticalExpand = true;
    }

    public void Add(Control view, Vector2 pivot, bool exclusive = false)
    {
        Stack? stack = null;
        if (!exclusive)
        {
            foreach (var existing in _stacks)
            {
                if (!existing.Exclusive && existing.Pivot == pivot)
                {
                    stack = existing;
                    break;
                }
            }
        }

        if (stack == null)
        {
            stack = new Stack(pivot, exclusive);
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
        view.HorizontalAlignment = GetAlignment(pivot);
        InvalidateArrange();
    }

    public Vector2 GetPivot(Vector2 globalTopLeft, Vector2 size)
    {
        var available = Size - size - new Vector2(ScreenMargin * 2);
        var position = globalTopLeft - GlobalPosition - new Vector2(ScreenMargin);
        var pivot = new Vector2(
            available.X > 0 ? position.X / available.X : 0,
            available.Y > 0 ? position.Y / available.Y : 0);

        return Vector2.Clamp(pivot, Vector2.Zero, Vector2.One);
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
        foreach (var stack in _stacks)
        {
            var size = Vector2.Min(stack.Box.DesiredSize, finalSize);
            var available = Vector2.Max(finalSize - size - new Vector2(ScreenMargin * 2), Vector2.Zero);
            var position = new Vector2(ScreenMargin) + available * stack.Pivot;
            stack.Box.Arrange(UIBox2.FromDimensions(position, size));
        }

        return finalSize;
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

    private sealed class Stack(Vector2 pivot, bool exclusive)
    {
        public Vector2 Pivot = pivot;
        public readonly bool Exclusive = exclusive;

        public readonly BoxContainer Box = new()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = StackSeparation,
            MouseFilter = MouseFilterMode.Ignore,
        };
    }
}
