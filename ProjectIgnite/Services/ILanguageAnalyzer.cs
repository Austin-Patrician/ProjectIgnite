using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.Services;

/// <summary>
/// 语言分析器接口
/// </summary>
public interface ILanguageAnalyzer
{
    /// <summary>
    /// 支持的语言类型
    /// </summary>
    string LanguageType { get; }

    /// <summary>
    /// 检测项目是否为该语言类型
    /// </summary>
    /// <param name="rootPath">项目根路径</param>
    /// <returns>检测结果和置信度</returns>
    Task<LanguageDetectionResult> DetectAsync(string rootPath);

    /// <summary>
    /// 扫描项目并提取语言特定信息
    /// </summary>
    /// <param name="request">扫描请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>扫描结果</returns>
    Task<LanguageSpecificResult> ScanAsync(ProjectScanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提取运行信息
    /// </summary>
    /// <param name="rootPath">项目根路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>运行信息</returns>
    Task<RunInfo> ExtractRunInfoAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提取依赖信息
    /// </summary>
    /// <param name="rootPath">项目根路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>依赖信息</returns>
    Task<DependenciesResult> ExtractDependenciesAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成项目摘要
    /// </summary>
    /// <param name="rootPath">项目根路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目摘要</returns>
    Task<ProjectSummary> SummarizeAsync(string rootPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// 语言检测结果
/// </summary>
public class LanguageDetectionResult
{
    /// <summary>
    /// 是否检测到该语言
    /// </summary>
    public bool IsDetected { get; set; }

    /// <summary>
    /// 置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 检测到的关键文件
    /// </summary>
    public List<string> KeyFiles { get; set; } = new();

    /// <summary>
    /// 检测原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 语言特定扫描结果
/// </summary>
public class LanguageSpecificResult
{
    /// <summary>
    /// 语言类型
    /// </summary>
    public string LanguageType { get; set; } = string.Empty;

    /// <summary>
    /// 环境信息
    /// </summary>
    public EnvironmentInfo Environment { get; set; } = new();

    /// <summary>
    /// 依赖信息
    /// </summary>
    public DependenciesResult Dependencies { get; set; } = new();

    /// <summary>
    /// 运行信息
    /// </summary>
    public RunInfo RunInfo { get; set; } = new();

    /// <summary>
    /// 结构图节点
    /// </summary>
    public List<GraphNode> StructureNodes { get; set; } = new();

    /// <summary>
    /// 结构图边
    /// </summary>
    public List<GraphEdge> StructureEdges { get; set; } = new();

    /// <summary>
    /// 关键代码片段
    /// </summary>
    public List<CodeSnippet> KeySnippets { get; set; } = new();
}

/// <summary>
/// 项目摘要
/// </summary>
public class ProjectSummary
{
    /// <summary>
    /// 项目名称
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 项目类型
    /// </summary>
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>
    /// 框架信息
    /// </summary>
    public string Framework { get; set; } = string.Empty;

    /// <summary>
    /// 主要模块
    /// </summary>
    public List<string> MainModules { get; set; } = new();

    /// <summary>
    /// 关键文件
    /// </summary>
    public List<string> KeyFiles { get; set; } = new();

    /// <summary>
    /// 顶级依赖
    /// </summary>
    public List<string> TopDependencies { get; set; } = new();

    /// <summary>
    /// 复杂度指标
    /// </summary>
    public Dictionary<string, object> ComplexityMetrics { get; set; } = new();
}

/// <summary>
/// 代码片段
/// </summary>
public class CodeSnippet
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 起始行号
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// 结束行号
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// 代码内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 片段类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 重要性评分 (0-1)
    /// </summary>
    public double Importance { get; set; }

    /// <summary>
    /// 上下文信息
    /// </summary>
    public string? Context { get; set; }
}