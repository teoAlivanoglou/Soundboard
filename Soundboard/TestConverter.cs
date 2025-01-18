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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UniformGrid? grid = value as UniformGrid;
            //grid.Columns = 
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 5;
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
            Button? button= values[1] as Button;
            var listView = VisualTreeHelper.GetParent(grid) ;

            if (grid is null || button is null || listView is null) return values;

            var buttonWidth = button.MinWidth + button.Margin.Left + button.Margin.Right;
            var gridWidth = grid.ActualWidth;

            var columns = 0;
            var minTotalSize = 1000000.0;

            do
            {
                columns++;
                minTotalSize = columns * buttonWidth ;

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
