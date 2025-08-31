using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 日志消息模型
    /// </summary>
    public partial class LogMessage : ObservableObject
    {
        [ObservableProperty]
        private DateTime _timestamp;

        [ObservableProperty]
        private LogLevel _level;

        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private string _source = string.Empty;

        public LogMessage(LogLevel level, string message, string source = "")
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
            Source = source;
        }

        public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] [{Level}] {Source}: {Message}";
    }

    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}