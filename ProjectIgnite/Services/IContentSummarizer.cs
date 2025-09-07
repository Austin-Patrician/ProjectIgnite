using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.Services;

/// <summary>
/// 内容摘要器接口
/// </summary>
public interface IContentSummarizer
{
    /// <summary>
    /// 构建项目摘要
    /// </summary>
    /// <param name="scanResult">扫描结果</param>
    /// <param name="tokenBudget">Token 预算</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>摘要内容</returns>
    Task<ProjectContentSummary> BuildSummaryAsync(
        ProjectScanResult scanResult, 
        int tokenBudget, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 提取关键代码片段
    /// </summary>
    /// <param name="scanResult">扫描结果</param>
    /// <param name="maxSnippets">最大片段数</param>
    /// <param name="maxLinesPerSnippet">每个片段最大行数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关键片段列表</returns>
    Task<List<CodeSnippet>> ExtractKeySnippetsAsync(
        ProjectScanResult scanResult,
        int maxSnippets = 10,
        int maxLinesPerSnippet = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 估算内容的 Token 数量
    /// </summary>
    /// <param name="content">内容文本</param>
    /// <returns>估算的 Token 数量</returns>
    int EstimateTokenCount(string content);

    /// <summary>
    /// 截断内容以适应 Token 预算
    /// </summary>
    /// <param name="content">原始内容</param>
    /// <param name="maxTokens">最大 Token 数</param>
    /// <returns>截断后的内容和截断信息</returns>
    (string truncatedContent, string truncationNote) TruncateContent(string content, int maxTokens);
}

/// <summary>
/// 项目内容摘要
/// </summary>
public class ProjectContentSummary
{
    /// <summary>
    /// 项目基本信息
    /// </summary>
    public ProjectBasicInfo BasicInfo { get; set; } = new();

    /// <summary>
    /// 文件树摘要
    /// </summary>
    public string FileTreeSummary { get; set; } = string.Empty;

    /// <summary>
    /// 依赖摘要
    /// </summary>
    public string DependenciesSummary { get; set; } = string.Empty;

    /// <summary>
    /// 环境信息摘要
    /// </summary>
    public string EnvironmentSummary { get; set; } = string.Empty;

    /// <summary>
    /// 运行信息摘要
    /// </summary>
    public string RunInfoSummary { get; set; } = string.Empty;

    /// <summary>
    /// 结构图摘要
    /// </summary>
    public string StructureSummary { get; set; } = string.Empty;

    /// <summary>
    /// 关键代码片段
    /// </summary>
    public List<CodeSnippet> KeySnippets { get; set; } = new();

    /// <summary>
    /// 总 Token 估算
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// 摘要生成时间
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;
}

/// <summary>
/// 项目基本信息
/// </summary>
public class ProjectBasicInfo
{
    /// <summary>
    /// 项目名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 项目类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 检测到的语言
    /// </summary>
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// 主要框架
    /// </summary>
    public string Framework { get; set; } = string.Empty;

    /// <summary>
    /// 文件统计
    /// </summary>
    public FileStatistics FileStats { get; set; } = new();

    /// <summary>
    /// 复杂度指标
    /// </summary>
    public Dictionary<string, object> ComplexityMetrics { get; set; } = new();
}

/// <summary>
/// 文件统计信息
/// </summary>
public class FileStatistics
{
    /// <summary>
    /// 总文件数
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// 总目录数
    /// </summary>
    public int TotalDirectories { get; set; }

    /// <summary>
    /// 按语言分组的文件数
    /// </summary>
    public Dictionary<string, int> FilesByLanguage { get; set; } = new();

    /// <summary>
    /// 关键文件数量
    /// </summary>
    public int KeyFilesCount { get; set; }
}