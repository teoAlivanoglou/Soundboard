using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Soundboard.Utils;

namespace Soundboard
{
    class TestConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
                return new Thickness(i);
            // return new Thickness(i - 2, i - 2, i - 2, i - 2);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    class CategoryColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {

            if (targetType == typeof(Brush) || targetType.IsSubclassOf(typeof(Brush)))
            {
                return value switch
                {
                    Soundboard.Discovery.SoundModel soundModel => soundModel.BackgroundBrush,
                    Soundboard.Discovery.CategoryModel categoryModel => categoryModel.BackgroundBrush,
                    _ => Brushes.Transparent
                };
            }
            else if (targetType == typeof(Color))
            {
                return value switch
                {
                    Soundboard.Discovery.SoundModel soundModel => (soundModel.BackgroundBrush as SolidColorBrush)?.Color ?? Colors.Transparent,
                    Soundboard.Discovery.CategoryModel categoryModel => (categoryModel.BackgroundBrush as SolidColorBrush)?.Color ?? Colors.Transparent,
                    _ => Colors.Transparent
                };
            }

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class EnabledBrushConverter : IMultiValueConverter
    {
        private Thickness _enabledThickness = new Thickness(2, 2, 2, 0);
        private Thickness _disabledThickness = new Thickness(0);
        private SolidColorBrush? _enabledBrush; // = new SolidColorBrush(Colors.Green);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = false;
            if (values[0] is bool b)
                enabled = b;

            var tab = values[1] as Border;

            if (_enabledBrush is null)
            {
                if (parameter is Brush)
                    _enabledBrush = (SolidColorBrush)parameter;
                else _enabledBrush = new SolidColorBrush(Colors.Green);
            }

            if (parameter is SolidColorBrush)
                return enabled ? (Brush)parameter : tab.Background;

            return enabled ? _enabledBrush : tab.Background;
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


    [ValueConversion(typeof(Enum), typeof(IEnumerable<ValueDescription>))]
    public class EnumToCollectionConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return EnumHelper.GetAllValuesAndDescriptions(value.GetType());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }


    public class LogScaleConverter : IValueConverter
    {
        private double ExpScale(double inputValue, double midValue, double maxValue)
        {
            double returnValue = 0;
            if (inputValue < 0 || inputValue > 1)
                throw new ArgumentOutOfRangeException("Input value must be between 0 and 1.0");
            if (midValue <= 0 || midValue >= maxValue)
                throw new ArgumentOutOfRangeException("MidValue must be greater than 0 and less than MaxValue");
            // returnValue = A + B * Math.Exp(C * inputValue);
            double M = maxValue / midValue;
            double C = Math.Log(Math.Pow(M - 1, 2));
            double B = maxValue / (Math.Exp(C) - 1);
            double A = -1 * B;
            returnValue = A + B * Math.Exp(C * inputValue);
            return returnValue;
        }

        private static double min = 0;
        private static double mid = 100;
        private static double max = 500;

        private double A;
        private double C;
        private double B;

        public LogScaleConverter()
        {
            A = (min * max - mid * mid) / (min - 2 * mid + max);
            B = (mid - min) * (mid - min) / (min - 2 * mid + max);
            C = 2 * Math.Log((max - mid) / (mid - min));
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = (double)value;

            return Math.Log((t - A) / B) / C;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = (double)value;

            return (int)(A + B * Math.Exp(C * t));
        }
    }

    public class BoldForEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? FontWeights.DemiBold : FontWeights.Normal;
            }

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}