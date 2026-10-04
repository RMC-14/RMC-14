using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;

/// <summary>
/// Lays out ghost targets at their natural width and wraps them onto new rows.
/// </summary>
internal sealed class RMCGhostTargetWrapContainer : Container
{
    private const float HorizontalSeparation = 4;
    private const float VerticalSeparation = 4;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var width = float.IsFinite(availableSize.X)
            ? availableSize.X
            : float.PositiveInfinity;
        var lineWidth = 0f;
        var lineHeight = 0f;
        var measuredWidth = 0f;
        var measuredHeight = 0f;

        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.Measure(availableSize);
            var childSize = child.DesiredSize;
            var requiredWidth = lineWidth == 0
                ? childSize.X
                : lineWidth + HorizontalSeparation + childSize.X;

            if (lineWidth > 0 && requiredWidth > width)
            {
                measuredWidth = Math.Max(measuredWidth, lineWidth);
                measuredHeight += lineHeight + VerticalSeparation;
                lineWidth = childSize.X;
                lineHeight = childSize.Y;
                continue;
            }

            lineWidth = requiredWidth;
            lineHeight = Math.Max(lineHeight, childSize.Y);
        }

        measuredWidth = Math.Max(measuredWidth, lineWidth);
        measuredHeight += lineHeight;
        return new Vector2(
            Math.Min(measuredWidth, availableSize.X),
            Math.Min(measuredHeight, availableSize.Y));
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var x = 0f;
        var y = 0f;
        var lineHeight = 0f;

        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            var childSize = child.DesiredSize;
            var childWidth = Math.Min(childSize.X, finalSize.X);
            if (x > 0 && x + childWidth > finalSize.X)
            {
                x = 0;
                y += lineHeight + VerticalSeparation;
                lineHeight = 0;
            }

            child.Arrange(UIBox2.FromDimensions(
                new Vector2(x, y),
                new Vector2(childWidth, childSize.Y)));
            x += childWidth + HorizontalSeparation;
            lineHeight = Math.Max(lineHeight, childSize.Y);
        }

        return finalSize;
    }
}
