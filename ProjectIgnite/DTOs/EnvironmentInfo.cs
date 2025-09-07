using System.Collections.Generic;

namespace ProjectIgnite.DTOs;

/// <summary>
/// 项目环境信息
/// </summary>
public class EnvironmentInfo
{
    /// <summary>
    /// .NET SDK 类型
    /// </summary>
    public string? DotNetSdk { get; set; }

    /// <summary>
    /// 目标框架
    /// </summary>
    public List<string> TargetFrameworks { get; set; } = new();

    /// <summary>
    /// 运行时版本
    /// </summary>
    public List<string> RuntimeVersions { get; set; } = new();

    /// <summary>
    /// Node.js 版本要求
    /// </summary>
    public string? NodeVersion { get; set; }

    /// <summary>
    /// 包管理器类型
    /// </summary>
    public PackageManagerType? PackageManager { get; set; }

    /// <summary>
    /// 语言版本
    /// </summary>
    public string? LanguageVersion { get; set; }

    /// <summary>
    /// 是否启用可空引用类型
    /// </summary>
    public bool? NullableEnabled { get; set; }

    /// <summary>
    /// 是否启用隐式using
    /// </summary>
    public bool? ImplicitUsings { get; set; }
}

/// <summary>
/// 包管理器类型枚举
/// </summary>
public enum PackageManagerType
{
    /// <summary>
    /// NPM
    /// </summary>
    Npm,
    
    /// <summary>
    /// Yarn
    /// </summary>
    Yarn,
    
    /// <summary>
    /// PNPM
    /// </summary>
    Pnpm,
    
    /// <summary>
    /// NuGet
    /// </summary>
    NuGet
}