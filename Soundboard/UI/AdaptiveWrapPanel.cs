using System;
using System.Windows;
using System.Windows.Controls;

namespace Soundboard.UI;

public class AdaptiveWrapPanel : Panel
{
    public static readonly DependencyProperty MinItemWidthProperty =
        DependencyProperty.Register(nameof(MinItemWidth), typeof(double), typeof(AdaptiveWrapPanel),
            new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ItemGapProperty =
        DependencyProperty.Register(nameof(ItemGap), typeof(double), typeof(AdaptiveWrapPanel),
            new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double ItemGap
    {
        get => (double)GetValue(ItemGapProperty);
        set => SetValue(ItemGapProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width) || availableSize.Width <= 0)
            return base.MeasureOverride(availableSize);

        double width = availableSize.Width;
        int columns = Math.Max(1, (int)(width / (MinItemWidth + (2 * ItemGap))));

        // Flexbox math: stretch item width evenly across all columns
        double itemWidth = Math.Max(1, (width - (columns * 2 * ItemGap)) / columns);
        double itemHeight = itemWidth;

        int visibleCount = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child != null && child.Visibility != Visibility.Collapsed)
            {
                child.Measure(new Size(itemWidth, itemHeight));
                visibleCount++;
            }
        }

        int rows = (int)Math.Ceiling((double)visibleCount / columns);
        double totalHeight = rows * (itemHeight + (2 * ItemGap));

        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize.Width <= 0) return finalSize;

        double width = finalSize.Width;
        int columns = Math.Max(1, (int)(width / (MinItemWidth + (2 * ItemGap))));
        double itemWidth = Math.Max(1, (width - (columns * 2 * ItemGap)) / columns);
        double itemHeight = itemWidth;

        int index = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child == null || child.Visibility == Visibility.Collapsed)
                continue;

            int col = index % columns;
            int row = index / columns;

            double x = (col * (itemWidth + (2 * ItemGap))) + ItemGap;
            double y = (row * (itemHeight + (2 * ItemGap))) + ItemGap;

            child.Arrange(new Rect(x, y, itemWidth, itemHeight));
            index++;
        }

        return finalSize;
    }
}
