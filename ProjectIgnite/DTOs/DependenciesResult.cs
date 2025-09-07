using System.Collections.Generic;
using System.Linq;

namespace ProjectIgnite.DTOs;

/// <summary>
/// 依赖分析结果
/// </summary>
public class DependenciesResult
{
    /// <summary>
    /// C# 项目依赖
    /// </summary>
    public List<PackageEntry> CSharp { get; set; } = new();

    /// <summary>
    /// Node 项目依赖
    /// </summary>
    public List<PackageEntry> Node { get; set; } = new();

    /// <summary>
    /// 总依赖数量
    /// </summary>
    public int TotalCount => CSharp.Count + Node.Count;

    /// <summary>
    /// 开发依赖数量
    /// </summary>
    public int DevDependencyCount => CSharp.Where(p => p.DevDependency).Count() + Node.Where(p => p.DevDependency).Count();
}

/// <summary>
/// 包条目信息
/// </summary>
public class PackageEntry
{
    /// <summary>
    /// 包名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 是否为开发依赖
    /// </summary>
    public bool DevDependency { get; set; }

    /// <summary>
    /// 依赖来源
    /// </summary>
    public PackageSource Source { get; set; }

    /// <summary>
    /// 是否过时
    /// </summary>
    public bool IsOutdated { get; set; }

    /// <summary>
    /// 风险评分 (0-100)
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// 许可证信息
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// 描述信息
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 包来源枚举
/// </summary>
public enum PackageSource
{
    /// <summary>
    /// .csproj 文件
    /// </summary>
    CsProj,
    
    /// <summary>
    /// package.json 文件
    /// </summary>
    PackageJson,
    
    /// <summary>
    /// 锁文件
    /// </summary>
    LockFile,
    
    /// <summary>
    /// 其他来源
    /// </summary>
    Other
}