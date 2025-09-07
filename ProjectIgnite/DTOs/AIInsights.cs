using System;
using System.Collections.Generic;

namespace ProjectIgnite.DTOs;

/// <summary>
/// AI 洞察分析结果
/// </summary>
public class AIInsights
{
    /// <summary>
    /// 高层架构总结
    /// </summary>
    public string HighLevelArchitecture { get; set; } = string.Empty;

    /// <summary>
    /// 模块洞察列表
    /// </summary>
    public List<ModuleInsight> Modules { get; set; } = new();

    /// <summary>
    /// 运行建议
    /// </summary>
    public RunRecommendation RunRecommendations { get; set; } = new();

    /// <summary>
    /// 依赖建议列表
    /// </summary>
    public List<DependencyAdviceItem> DependencyAdvice { get; set; } = new();

    /// <summary>
    /// 潜在风险列表
    /// </summary>
    public List<RiskItem> PotentialRisks { get; set; } = new();

    /// <summary>
    /// 置信度评分
    /// </summary>
    public ConfidenceScore ConfidenceScores { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;
}

/// <summary>
/// 模块洞察
/// </summary>
public class ModuleInsight
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块职责
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 关键文件列表
    /// </summary>
    public List<string> KeyFiles { get; set; } = new();

    /// <summary>
    /// 建议列表
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// 复杂度评分 (0-100)
    /// </summary>
    public double ComplexityScore { get; set; }
}

/// <summary>
/// 运行建议
/// </summary>
public class RunRecommendation
{
    /// <summary>
    /// 入口点列表
    /// </summary>
    public List<string> Entrypoints { get; set; } = new();

    /// <summary>
    /// 推荐使用的端口
    /// </summary>
    public List<int> PortsToUse { get; set; } = new();

    /// <summary>
    /// 推荐尝试的URL
    /// </summary>
    public List<string> UrlsToTry { get; set; } = new();

    /// <summary>
    /// 注意事项
    /// </summary>
    public List<string> Notes { get; set; } = new();

    /// <summary>
    /// 环境变量建议
    /// </summary>
    public List<string> EnvironmentVariables { get; set; } = new();
}

/// <summary>
/// 依赖建议项
/// </summary>
public class DependencyAdviceItem
{
    /// <summary>
    /// 包名称
    /// </summary>
    public string Package { get; set; } = string.Empty;

    /// <summary>
    /// 当前版本
    /// </summary>
    public string Current { get; set; } = string.Empty;

    /// <summary>
    /// 建议版本
    /// </summary>
    public string? Suggested { get; set; }

    /// <summary>
    /// 建议原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 建议类型
    /// </summary>
    public AdviceType Type { get; set; }
}

/// <summary>
/// 风险项
/// </summary>
public class RiskItem
{
    /// <summary>
    /// 风险标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 风险描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    public RiskSeverity Severity { get; set; }

    /// <summary>
    /// 置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 影响的文件或模块
    /// </summary>
    public List<string> AffectedItems { get; set; } = new();

    /// <summary>
    /// 建议的解决方案
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// 置信度评分
/// </summary>
public class ConfidenceScore
{
    /// <summary>
    /// 总体置信度 (0-1)
    /// </summary>
    public double Overall { get; set; }

    /// <summary>
    /// 推理说明
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// 数据完整性评分 (0-1)
    /// </summary>
    public double DataCompleteness { get; set; }

    /// <summary>
    /// 分析深度评分 (0-1)
    /// </summary>
    public double AnalysisDepth { get; set; }
}

/// <summary>
/// 建议类型枚举
/// </summary>
public enum AdviceType
{
    /// <summary>
    /// 升级建议
    /// </summary>
    Upgrade,
    
    /// <summary>
    /// 安全建议
    /// </summary>
    Security,
    
    /// <summary>
    /// 性能建议
    /// </summary>
    Performance,
    
    /// <summary>
    /// 兼容性建议
    /// </summary>
    Compatibility,
    
    /// <summary>
    /// 移除建议
    /// </summary>
    Remove
}

/// <summary>
/// 风险严重程度枚举
/// </summary>
public enum RiskSeverity
{
    /// <summary>
    /// 低风险
    /// </summary>
    Low,
    
    /// <summary>
    /// 中等风险
    /// </summary>
    Medium,
    
    /// <summary>
    /// 高风险
    /// </summary>
    High,
    
    /// <summary>
    /// 严重风险
    /// </summary>
    Critical
}