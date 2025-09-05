using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ProjectIgnite.Converters
{
    /// <summary>
    /// 字符串相等转换器：比较字符串值是否相等
    /// </summary>
    public class StringEqualsConverter : IValueConverter
    {
        public static readonly StringEqualsConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var valueString = value.ToString();
            var parameterString = parameter.ToString();

            return string.Equals(valueString, parameterString, StringComparison.OrdinalIgnoreCase);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StringEqualsConverter does not support ConvertBack");
        }
    }
}
