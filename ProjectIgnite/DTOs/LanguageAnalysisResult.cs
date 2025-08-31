using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectIgnite.DTOs
{
    /// <summary>
    /// 语言分析结果
    /// </summary>
    public class LanguageAnalysisResult
    {
        /// <summary>
        /// 主要编程语言
        /// </summary>
        public string PrimaryLanguage { get; set; } = string.Empty;

        /// <summary>
        /// 语言百分比分布
        /// </summary>
        public Dictionary<string, double> LanguagePercentages { get; set; } = new();

        /// <summary>
        /// 语言字节数分布
        /// </summary>
        public Dictionary<string, long> LanguageBytes { get; set; } = new();

        /// <summary>
        /// 文件扩展名统计
        /// </summary>
        public Dictionary<string, int> FileExtensions { get; set; } = new();

        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 代码文件数
        /// </summary>
        public int CodeFiles { get; set; }

        /// <summary>
        /// 总代码行数
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// 代码字节数
        /// </summary>
        public long CodeBytes { get; set; }

        /// <summary>
        /// 分析开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 分析结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 分析耗时（毫秒）
        /// </summary>
        public long AnalysisDuration => (long)(EndTime - StartTime).TotalMilliseconds;

        /// <summary>
        /// 是否分析成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 忽略的文件数
        /// </summary>
        public int IgnoredFiles { get; set; }

        /// <summary>
        /// 二进制文件数
        /// </summary>
        public int BinaryFiles { get; set; }

        /// <summary>
        /// 大文件数（超过阈值）
        /// </summary>
        public int LargeFiles { get; set; }

        /// <summary>
        /// 获取格式化的语言分布字符串
        /// </summary>
        /// <returns></returns>
        public string GetFormattedLanguageDistribution()
        {
            if (!LanguagePercentages.Any())
                return "未知";

            var top3 = LanguagePercentages
                .OrderByDescending(x => x.Value)
                .Take(3)
                .Select(x => $"{x.Key} ({x.Value:F1}%)")
                .ToArray();

            return string.Join(", ", top3);
        }

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// 获取格式化的文件统计信息
        /// </summary>
        /// <returns></returns>
        public string GetFormattedFileStats()
        {
            return $"总计 {TotalFiles} 个文件，{CodeFiles} 个代码文件，{TotalLines:N0} 行代码";
        }
    }

    /// <summary>
    /// 单个语言的详细分析结果
    /// </summary>
    public class LanguageDetail
    {
        /// <summary>
        /// 语言名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件数量
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// 代码行数
        /// </summary>
        public long LineCount { get; set; }

        /// <summary>
        /// 字节数
        /// </summary>
        public long ByteCount { get; set; }

        /// <summary>
        /// 占项目的百分比
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// 语言颜色（用于图表显示）
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FormattedSize
        {
            get
            {
                if (ByteCount < 1024)
                    return $"{ByteCount} B";
                else if (ByteCount < 1024 * 1024)
                    return $"{ByteCount / 1024.0:F1} KB";
                else
                    return $"{ByteCount / (1024.0 * 1024.0):F1} MB";
            }
        }

        /// <summary>
        /// 格式化的代码行数
        /// </summary>
        public string FormattedLineCount
        {
            get
            {
                if (LineCount < 1000)
                    return LineCount.ToString();
                else if (LineCount < 1000000)
                    return $"{LineCount / 1000.0:F1}K";
                else
                    return $"{LineCount / 1000000.0:F1}M";
            }
        }
    }
}