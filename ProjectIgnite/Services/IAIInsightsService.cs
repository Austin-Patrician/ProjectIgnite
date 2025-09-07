using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.Services;

/// <summary>
/// AI 洞察服务接口
/// </summary>
public interface IAIInsightsService
{
    /// <summary>
    /// 生成项目洞察
    /// </summary>
    /// <param name="scanResult">项目扫描结果</param>
    /// <param name="tokenBudget">Token 预算限制</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 洞察结果</returns>
    Task<AIInsights> GenerateInsightsAsync(
        ProjectScanResult scanResult, 
        int tokenBudget = 15000, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 预览将要发送给 AI 的内容
    /// </summary>
    /// <param name="scanResult">项目扫描结果</param>
    /// <param name="tokenBudget">Token 预算限制</param>
    /// <returns>预览内容和预算评估</returns>
    Task<AIContentPreview> DryRunPreviewAsync(
        ProjectScanResult scanResult, 
        int tokenBudget = 15000);

    /// <summary>
    /// 检查 AI 服务是否可用
    /// </summary>
    /// <returns>服务可用性状态</returns>
    Task<AIServiceStatus> CheckServiceStatusAsync();

    /// <summary>
    /// 获取支持的 AI 模型列表
    /// </summary>
    /// <returns>模型列表</returns>
    Task<string[]> GetAvailableModelsAsync();
}

/// <summary>
/// AI 内容预览
/// </summary>
public class AIContentPreview
{
    /// <summary>
    /// 将要发送的摘要内容
    /// </summary>
    public string SummaryContent { get; set; } = string.Empty;

    /// <summary>
    /// 关键代码片段列表
    /// </summary>
    public List<CodeSnippet> KeySnippets { get; set; } = new();

    /// <summary>
    /// 预估 Token 数量
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Token 预算使用率 (0-1)
    /// </summary>
    public double BudgetUsageRatio { get; set; }

    /// <summary>
    /// 内容截断信息
    /// </summary>
    public List<string> TruncationNotes { get; set; } = new();

    /// <summary>
    /// 数据完整性评分 (0-1)
    /// </summary>
    public double DataCompletenessScore { get; set; }
}

/// <summary>
/// AI 服务状态
/// </summary>
public class AIServiceStatus
{
    /// <summary>
    /// 服务是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 当前使用的模型
    /// </summary>
    public string? CurrentModel { get; set; }

    /// <summary>
    /// 剩余配额（如果适用）
    /// </summary>
    public int? RemainingQuota { get; set; }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTimeOffset LastChecked { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    public string? ErrorMessage { get; set; }
}