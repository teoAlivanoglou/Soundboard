using System.Windows;
using System.Windows.Controls;

namespace Soundboard.UI;

// TODO: Lots of duplicate code...
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

        var width = availableSize.Width;
        var columns = Math.Max(1, (int)(width / (MinItemWidth + (2 * ItemGap))));

        // Stretch item width evenly across all columns
        var itemWidth = Math.Max(1, (width - (columns * 2 * ItemGap)) / columns);
        var itemHeight = itemWidth;

        var visibleCount = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child is null or { Visibility: Visibility.Collapsed }) continue;
            child.Measure(new Size(itemWidth, itemHeight));
            visibleCount++;
        }

        var rows = (int)Math.Ceiling((double)visibleCount / columns);
        var totalHeight = rows * (itemHeight + (2 * ItemGap));

        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize.Width <= 0) return finalSize;

        var width = finalSize.Width;
        var columns = Math.Max(1, (int)(width / (MinItemWidth + (2 * ItemGap))));
        var itemWidth = Math.Max(1, (width - (columns * 2 * ItemGap)) / columns);
        var itemHeight = itemWidth;

        var index = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child is null or { Visibility: Visibility.Collapsed })
                continue;

            var col = index % columns;
            var row = index / columns;

            var x = (col * (itemWidth + (2 * ItemGap))) + ItemGap;
            var y = (row * (itemHeight + (2 * ItemGap))) + ItemGap;

            child.Arrange(new Rect(x, y, itemWidth, itemHeight));
            index++;
        }

        return finalSize;
    }
}