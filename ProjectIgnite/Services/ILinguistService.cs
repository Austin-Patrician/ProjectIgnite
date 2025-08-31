using ProjectIgnite.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// Linguist语言分析服务接口
    /// </summary>
    public interface ILinguistService
    {
        /// <summary>
        /// 分析项目目录的语言组成
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>语言分析结果</returns>
        Task<LinguistAnalysisResult> AnalyzeProjectAsync(
            string projectPath,
            IProgress<LinguistAnalysisResult>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查Linguist是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        Task<bool> IsLinguistAvailableAsync();

        /// <summary>
        /// 获取Linguist版本信息
        /// </summary>
        /// <returns>版本信息</returns>
        Task<string?> GetLinguistVersionAsync();

        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        /// <returns>支持的语言列表</returns>
        Task<List<string>> GetSupportedLanguagesAsync();

        /// <summary>
        /// 分析单个文件的语言类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>语言类型</returns>
        Task<string?> DetectFileLanguageAsync(string filePath);

        /// <summary>
        /// 获取项目的主要语言
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>主要语言</returns>
        Task<string?> GetPrimaryLanguageAsync(string projectPath);

        /// <summary>
        /// 获取语言统计信息
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>语言统计</returns>
        Task<Dictionary<string, LanguageStatistics>> GetLanguageStatisticsAsync(string projectPath);
    }

    /// <summary>
    /// 语言统计信息
    /// </summary>
    public class LanguageStatistics
    {
        /// <summary>
        /// 语言名称
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// 文件数量
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// 代码行数
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// 字节数
        /// </summary>
        public long ByteCount { get; set; }

        /// <summary>
        /// 百分比
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// 是否为主要语言
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// 语言类型（编程语言、标记语言、数据格式等）
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 语言颜色（用于UI显示）
        /// </summary>
        public string Color { get; set; } = "#cccccc";
    }

    /// <summary>
    /// Linguist分析状态
    /// </summary>
    public enum LinguistAnalysisStatus
    {
        /// <summary>
        /// 待开始
        /// </summary>
        Pending,

        /// <summary>
        /// 初始化中
        /// </summary>
        Initializing,

        /// <summary>
        /// 扫描文件中
        /// </summary>
        ScanningFiles,

        /// <summary>
        /// 分析语言中
        /// </summary>
        AnalyzingLanguages,

        /// <summary>
        /// 计算统计中
        /// </summary>
        CalculatingStatistics,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 出错
        /// </summary>
        Error,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Linguist分析结果
    /// </summary>
    public class LinguistAnalysisResult
    {
        /// <summary>
        /// 分析状态
        /// </summary>
        public LinguistAnalysisStatus Status { get; set; }

        /// <summary>
        /// 项目路径
        /// </summary>
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// 主要语言
        /// </summary>
        public string? PrimaryLanguage { get; set; }

        /// <summary>
        /// 语言统计
        /// </summary>
        public Dictionary<string, LanguageStatistics> Languages { get; set; } = new();

        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 总代码行数
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// 总字节数
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// 分析开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 分析结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 分析耗时
        /// </summary>
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// 当前操作描述
        /// </summary>
        public string? CurrentOperation { get; set; }

        /// <summary>
        /// 已处理文件数
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// 分析是否成功
        /// </summary>
        public bool Success => Status == LinguistAnalysisStatus.Completed;
    }
}