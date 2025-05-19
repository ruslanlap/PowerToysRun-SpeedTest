using System;
using System.Globalization;
using System.Windows.Data;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    /// <summary>
    /// Конвертер для заміни десяткового розділювача з крапки на кому, округлення до більшого і без дробових
    /// </summary>
    public class DecimalSeparatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Округлення до більшого цілого
                var ceilValue = Math.Ceiling(doubleValue);
                // Повертаємо як рядок, без дробової частини
                return ceilValue.ToString("F0", CultureInfo.InvariantCulture).Replace(".", ",");
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                string normalizedValue = stringValue.Replace(",", ".");
                if (double.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
            }
            return value;
        }
    }
}
