using Avalonia;
using Avalonia.Controls;
using System;

namespace Soundboard.Avalonia.UI.Controls;

public class UniformSquarePanel : Panel
{
    public static readonly StyledProperty<double> MinItemSizeProperty =
        AvaloniaProperty.Register<UniformSquarePanel, double>(
            nameof(MinItemSize),
            defaultValue: 100.0);

    public double MinItemSize
    {
        get => GetValue(MinItemSizeProperty);
        set => SetValue(MinItemSizeProperty, value);
    }

    public static readonly StyledProperty<double> ItemGapProperty =
        AvaloniaProperty.Register<UniformSquarePanel, double>(
            nameof(ItemGap),
            defaultValue: 8.0);

    public double ItemGap
    {
        get => GetValue(ItemGapProperty);
        set => SetValue(ItemGapProperty, value);
    }

    static UniformSquarePanel()
    {
        AffectsMeasure<UniformSquarePanel>(MinItemSizeProperty);
        AffectsMeasure<UniformSquarePanel>(ItemGapProperty);
    }

    public UniformSquarePanel()
    {
        ClipToBounds = false;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var gap = Math.Max(0.0, ItemGap);
        var minSize = Math.Max(1.0, MinItemSize);

        if (double.IsInfinity(width) || double.IsNaN(width) || width <= 0)
        {
            var itemSize = minSize;
            var visible = 0;
            var children = Children;
            var count = children.Count;
            for (int i = 0; i < count; i++)
            {
                var child = children[i];
                if (child.IsVisible)
                {
                    child.Measure(new Size(itemSize, itemSize));
                    visible++;
                }
            }

            double totalW = visible > 0 ? visible * (itemSize + gap) + gap : 0;
            double totalH = visible > 0 ? itemSize + 2 * gap : 0;
            return new Size(totalW, totalH);
        }

        int columns = Math.Max(1, (int)((width - gap) / (minSize + gap)));
        double itemWidth = Math.Max(1.0, (width - (columns + 1) * gap) / columns);
        double itemHeight = itemWidth;

        var childConstraint = new Size(itemWidth, itemHeight);
        var panelChildren = Children;
        var totalCount = panelChildren.Count;
        int visibleCount = 0;

        for (int i = 0; i < totalCount; i++)
        {
            var child = panelChildren[i];
            if (child.IsVisible)
            {
                child.Measure(childConstraint);
                visibleCount++;
            }
        }

        if (visibleCount == 0)
            return new Size(width, 0);

        int rows = (visibleCount + columns - 1) / columns;
        double totalHeight = rows * (itemHeight + gap) + gap;

        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width;
        if (width <= 0)
            return finalSize;

        var gap = Math.Max(0.0, ItemGap);
        var minSize = Math.Max(1.0, MinItemSize);

        int columns = Math.Max(1, (int)((width - gap) / (minSize + gap)));
        double itemWidth = Math.Max(1.0, (width - (columns + 1) * gap) / columns);
        double itemHeight = itemWidth;

        var children = Children;
        var count = children.Count;
        int index = 0;

        for (int i = 0; i < count; i++)
        {
            var child = children[i];
            if (!child.IsVisible)
                continue;

            int col = index % columns;
            int row = index / columns;

            double x = gap + col * (itemWidth + gap);
            double y = gap + row * (itemHeight + gap);

            child.Arrange(new Rect(x, y, itemWidth, itemHeight));
            index++;
        }

        return finalSize;
    }
}