using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ProjectIgnite.Converters
{
    /// <summary>
    /// 取反布尔转换器：将 true -> false，false -> true
    /// </summary>
    public class BooleanNegationConverter : IValueConverter
    {
        public static readonly BooleanNegationConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }
    }
}
