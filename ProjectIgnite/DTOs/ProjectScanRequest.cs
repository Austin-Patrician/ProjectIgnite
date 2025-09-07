using System.ComponentModel.DataAnnotations;

namespace ProjectIgnite.DTOs;

/// <summary>
/// 项目扫描请求配置
/// </summary>
public class ProjectScanRequest
{
    /// <summary>
    /// 项目根路径
    /// </summary>
    [Required]
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// 指定要分析的语言类型，空表示自动检测
    /// </summary>
    public string[]? Languages { get; set; }

    /// <summary>
    /// 最大扫描深度，默认6层
    /// </summary>
    public int MaxDepth { get; set; } = 6;

    /// <summary>
    /// 忽略的目录模式
    /// </summary>
    public string[] IgnorePatterns { get; set; } = 
    {
        ".git", "bin", "obj", "node_modules", "dist", "build", 
        ".cache", ".vs", ".idea", "packages", "target"
    };

    /// <summary>
    /// 最大文件数限制
    /// </summary>
    public int MaxFiles { get; set; } = 10000;

    /// <summary>
    /// 最大目录数限制
    /// </summary>
    public int MaxDirs { get; set; } = 5000;

    /// <summary>
    /// 是否启用AI分析
    /// </summary>
    public bool EnableAI { get; set; } = true;

    /// <summary>
    /// AI Token预算限制
    /// </summary>
    public int TokenBudget { get; set; } = 15000;

    /// <summary>
    /// 扫描超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}