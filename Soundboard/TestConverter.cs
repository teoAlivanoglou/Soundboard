using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Soundboard
{
    class TestConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
                return new Thickness(i - 5, i - 2, i - 5, i - 2);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    class CategoryColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush(CategoryColors.GetColorForCategory((string)value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class EnabledBrushConverter : IMultiValueConverter
    {
        private Thickness enabledThickness = new Thickness(2, 2, 2, 0);
        private Thickness disabledThickness = new Thickness(0);
        private SolidColorBrush enabledBrush = new SolidColorBrush(Colors.Green);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = false;
            if (values[0] is bool b)
                enabled = b;

            var tab = values[1] as Border;

            return enabled ? enabledBrush : tab.Background;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class NoCategoryConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value.ToString()) ? "All" : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TextBlockTextWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)((int)values[0] - (int)values[1] - (int)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LerpConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)(values[0]) * (double)(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultByTenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                double d => d * 10,
                int i => i * 10,
                _ => value
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d / 10;

            if (value is int i)
                return i / 10;

            return value;
        }
    }

    class MultiTestConverter : IMultiValueConverter
    {
        public bool IsScrollbarVisible(ScrollViewer scrollViewer)
        {
            if (scrollViewer is null) return false;
            return scrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible;
        }


        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            UniformGrid? grid = values[0] as UniformGrid;
            Button? button = values[1] as Button;
            var listView = VisualTreeHelper.GetParent(grid);

            if (grid is null || button is null || listView is null) return values;

            var buttonWidth = button.MinWidth + button.Margin.Left + button.Margin.Right;
            var gridWidth = grid.ActualWidth;

            var columns = 0;
            var minTotalSize = 1000000.0;

            do
            {
                columns++;
                minTotalSize = columns * buttonWidth;
            } while (minTotalSize < gridWidth);

            grid.Columns = columns;


            return values;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { 1, 2, 3 };
        }
    }
}