// File: ProgressToWidthConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val && targetType == typeof(double))
            {
                FrameworkElement element = parameter as FrameworkElement;
                if (element is ProgressBar progressBar)
                {
                    double max = progressBar.Maximum;
                    if (max <= 0) max = 100;

                    double percent = val / max;
                    return element.ActualWidth * percent;
                }
                return 0;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}