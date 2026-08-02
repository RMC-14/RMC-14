using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface.Crt;

/// <summary>
/// Lays out two content columns around a divider and measures both columns with the same available width.
/// The visible children must be ordered as the first column, the divider, and the second column.
/// </summary>
public sealed class RMCCrtTwoColumnContainer : Container
{
    private float _columnSeparation = 6;

    public float ColumnSeparation
    {
        get => _columnSeparation;
        set
        {
            if (MathHelper.CloseTo(_columnSeparation, value))
                return;

            _columnSeparation = Math.Max(0, value);
            InvalidateMeasure();
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (!TryGetColumns(out var first, out var divider, out var second))
            return base.MeasureOverride(availableSize);

        divider.Measure(availableSize);
        var dividerWidth = divider.DesiredSize.X;
        var availableColumnsWidth = Math.Max(0, availableSize.X - dividerWidth - ColumnSeparation * 2);
        var columnWidth = availableColumnsWidth / 2;
        var columnAvailableSize = new Vector2(columnWidth, availableSize.Y);

        first.Measure(columnAvailableSize);
        second.Measure(columnAvailableSize);

        return new Vector2(
            first.DesiredSize.X + dividerWidth + second.DesiredSize.X + ColumnSeparation * 2,
            Math.Max(divider.DesiredSize.Y, Math.Max(first.DesiredSize.Y, second.DesiredSize.Y)));
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (!TryGetColumns(out var first, out var divider, out var second))
            return base.ArrangeOverride(finalSize);

        // Windows are initially measured with infinite space before being opened. Measure again with the real
        // arranged width so wrapping controls have their final line breaks on the first visible frame.
        divider.Measure(finalSize);
        var dividerWidth = Math.Min(divider.DesiredSize.X, finalSize.X);
        var availableColumnsWidth = Math.Max(0, finalSize.X - dividerWidth - ColumnSeparation * 2);
        var firstWidth = availableColumnsWidth / 2;
        var secondWidth = availableColumnsWidth - firstWidth;
        var dividerLeft = firstWidth + ColumnSeparation;
        var secondLeft = dividerLeft + dividerWidth + ColumnSeparation;

        first.Measure(new Vector2(firstWidth, finalSize.Y));
        second.Measure(new Vector2(secondWidth, finalSize.Y));

        first.Arrange(UIBox2.FromDimensions(Vector2.Zero, new Vector2(firstWidth, finalSize.Y)));
        divider.Arrange(UIBox2.FromDimensions(
            new Vector2(dividerLeft, 0),
            new Vector2(dividerWidth, finalSize.Y)));
        second.Arrange(UIBox2.FromDimensions(
            new Vector2(secondLeft, 0),
            new Vector2(secondWidth, finalSize.Y)));

        return finalSize;
    }

    private bool TryGetColumns(
        out Control first,
        out Control divider,
        out Control second)
    {
        first = default!;
        divider = default!;
        second = default!;

        var visibleIndex = 0;
        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            switch (visibleIndex++)
            {
                case 0:
                    first = child;
                    break;
                case 1:
                    divider = child;
                    break;
                case 2:
                    second = child;
                    break;
                default:
                    return false;
            }
        }

        return visibleIndex == 3;
    }
}
