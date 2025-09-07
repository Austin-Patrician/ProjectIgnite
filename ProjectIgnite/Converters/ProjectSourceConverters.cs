using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.Converters
{
    /// <summary>
    /// 状态颜色转换器
    /// </summary>
    public class StatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "completed" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // 绿色
                    "cloning" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // 橙色
                    "analyzing" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // 蓝色
                    "error" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // 红色
                    "cancelled" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // 灰色
                    "pending" => new SolidColorBrush(Color.FromRgb(96, 125, 139)), // 蓝灰色
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // 默认灰色
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 状态文本转换器
    /// </summary>
    public class StatusTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "completed" => "已完成",
                    "cloning" => "克隆中",
                    "analyzing" => "分析中",
                    "error" => "错误",
                    "cancelled" => "已取消",
                    "pending" => "待处理",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 进度可见性转换器
    /// </summary>
    public class ProgressVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "cloning" or "analyzing" => true,
                    _ => false
                };
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 文件大小转换器
    /// </summary>
    public class FileSizeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return FormatFileSize(bytes);
            }
            if (value is int intBytes)
            {
                return FormatFileSize(intBytes);
            }
            return "0 B";
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 数字格式转换器
    /// </summary>
    public class NumberFormatConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int number)
            {
                return number.ToString("N0");
            }
            if (value is long longNumber)
            {
                return longNumber.ToString("N0");
            }
            return "0";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字符串到可见性转换器
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value?.ToString());
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 零值到可见性转换器
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 计数到可见性转换器
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 空值到可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 对象到布尔值转换器
    /// </summary>
    public class ObjectToBooleanConverter : IValueConverter
    {
        public static readonly ObjectToBooleanConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 检查是否需要反转结果
            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
            
            bool result = value != null;
            
            return invert ? !result : result;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到字符串转换器
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        public static readonly BooleanToStringConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string parameterString)
            {
                var parts = parameterString.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 项目源相关的静态转换器集合
    /// </summary>
    public static class ProjectSourceConverters
    {
        /// <summary>
        /// 判断文件节点类型是否为目录
        /// </summary>
        public static readonly IValueConverter IsDirectory = new FuncValueConverter<FileNodeType, bool>(
            type => type == FileNodeType.Directory);

        /// <summary>
        /// 判断文件节点类型是否为文件
        /// </summary>
        public static readonly IValueConverter IsFile = new FuncValueConverter<FileNodeType, bool>(
            type => type == FileNodeType.File);

        /// <summary>
        /// 从完整路径中提取文件名
        /// </summary>
        public static readonly IValueConverter GetFileName = new FuncValueConverter<string, string>(
            path => string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileName(path));

        /// <summary>
        /// 将字符串列表连接为单个字符串
        /// </summary>
        public static readonly IValueConverter JoinStrings = new FuncValueConverter<IEnumerable<string>, string>(
            strings => strings == null ? "" : string.Join(", ", strings));

        /// <summary>
        /// 将整数列表连接为单个字符串
        /// </summary>
        public static readonly IValueConverter JoinIntegers = new FuncValueConverter<IEnumerable<int>, string>(
            integers => integers == null ? "" : string.Join(", ", integers));

        /// <summary>
        /// 判断数值是否大于零
        /// </summary>
        public static readonly IValueConverter IsGreaterThanZero = new FuncValueConverter<int, bool>(
            value => value > 0);

        /// <summary>
        /// 将布尔值转换为状态颜色
        /// </summary>
        public static readonly IValueConverter BoolToStatusColor = new FuncValueConverter<bool, IBrush>(
            isAvailable => isAvailable ? Brushes.Green : Brushes.Red);

        /// <summary>
        /// 将严重程度转换为颜色
        /// </summary>
        public static readonly IValueConverter SeverityToColor = new FuncValueConverter<string, IBrush>(severity =>
        {
            return severity?.ToLower() switch
            {
                "high" or "critical" => Brushes.Red,
                "medium" or "warning" => Brushes.Orange,
                "low" or "info" => Brushes.Blue,
                _ => Brushes.Gray
            };
        });
    }

    /// <summary>
    /// 通用函数转换器
    /// </summary>
    /// <typeparam name="TIn">输入类型</typeparam>
    /// <typeparam name="TOut">输出类型</typeparam>
    public class FuncValueConverter<TIn, TOut> : IValueConverter
    {
        private readonly Func<TIn, TOut> _converter;

        public FuncValueConverter(Func<TIn, TOut> converter)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TIn input)
            {
                return _converter(input);
            }
            return default(TOut);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported.");
        }
    }
}