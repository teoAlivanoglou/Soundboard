using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using System;
using System.Runtime.CompilerServices;

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
                if (IsChildVisible(child))
                {
                    visible++;
                }
            }

            double totalW = visible > 0 ? visible * itemSize + (visible - 1) * gap : 0;
            double totalH = visible > 0 ? itemSize : 0;
            return new Size(totalW, totalH);
        }

        var (columns, itemWidth) = CalculateLayout(width, minSize, gap);
        double itemHeight = itemWidth;

        var panelChildren = Children;
        var totalCount = panelChildren.Count;
        int visibleCount = 0;

        for (int i = 0; i < totalCount; i++)
        {
            var child = panelChildren[i];
            if (IsChildVisible(child))
            {
                visibleCount++;
            }
        }

        if (visibleCount == 0)
            return new Size(width, 0);

        int rows = (visibleCount + columns - 1) / columns;
        double totalHeight = rows * itemHeight + (rows - 1) * gap;

        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width;
        if (width <= 0)
            return finalSize;

        var gap = Math.Max(0.0, ItemGap);
        var minSize = Math.Max(1.0, MinItemSize);

        var (columns, itemWidth) = CalculateLayout(width, minSize, gap);
        double itemHeight = itemWidth;

        var children = Children;
        var count = children.Count;
        int index = 0;

        for (int i = 0; i < count; i++)
        {
            var child = children[i];
            if (!IsChildVisible(child))
                continue;

            int col = index % columns;
            int row = index / columns;

            double x = col * (itemWidth + gap);
            double y = row * (itemHeight + gap);

            child.Arrange(new Rect(x, y, itemWidth, itemHeight));
            index++;
        }

        return finalSize;
    }

    private static bool IsChildVisible(Control child)
    {
        if (!child.IsVisible) return false;
        if (child is ContentPresenter { Child: { } innerChild } && !innerChild.IsVisible)
            return false;
        return true;
    }

    private static (int columns, double itemSize) CalculateLayout(double width, double minSize, double gap)
    {
        int columns = Math.Max(1, (int)((width + gap) / (minSize + gap)));
        double itemSize = Math.Max(1.0, (width - (columns - 1) * gap) / columns);
        return (columns, itemSize);
    }
}