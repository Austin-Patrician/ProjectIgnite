using System;
using System.Collections.Generic;

namespace ProjectIgnite.DTOs;

/// <summary>
/// 项目扫描结果
/// </summary>
public class ProjectScanResult
{
    /// <summary>
    /// 检测到的语言列表（按置信度排序）
    /// </summary>
    public List<string> LanguagesDetected { get; set; } = new();

    /// <summary>
    /// 文件树摘要
    /// </summary>
    public List<FileNodeSummary> FileTreeSummary { get; set; } = new();

    /// <summary>
    /// 依赖分析结果
    /// </summary>
    public DependenciesResult Dependencies { get; set; } = new();

    /// <summary>
    /// 环境信息
    /// </summary>
    public EnvironmentInfo Environments { get; set; } = new();

    /// <summary>
    /// 运行信息
    /// </summary>
    public RunInfo RunInfo { get; set; } = new();

    /// <summary>
    /// 项目结构图
    /// </summary>
    public StructureGraph StructureGraph { get; set; } = new();

    /// <summary>
    /// 警告列表
    /// </summary>
    public List<WarningItem> Warnings { get; set; } = new();

    /// <summary>
    /// 日志列表
    /// </summary>
    public List<LogItem> Logs { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 扫描耗时（毫秒）
    /// </summary>
    public long ScanDurationMs { get; set; }

    /// <summary>
    /// 扫描的文件总数
    /// </summary>
    public int TotalFilesScanned { get; set; }

    /// <summary>
    /// 扫描的目录总数
    /// </summary>
    public int TotalDirectoriesScanned { get; set; }

    /// <summary>
    /// 是否扫描完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 警告项
/// </summary>
public class WarningItem
{
    /// <summary>
    /// 警告标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 警告消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 警告级别
    /// </summary>
    public WarningLevel Level { get; set; }

    /// <summary>
    /// 相关文件路径
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}

/// <summary>
/// 日志项
/// </summary>
public class LogItem
{
    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 来源
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? Exception { get; set; }
}

/// <summary>
/// 警告级别枚举
/// </summary>
public enum WarningLevel
{
    /// <summary>
    /// 信息
    /// </summary>
    Info,
    
    /// <summary>
    /// 警告
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error
}

/// <summary>
/// 日志级别枚举
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// 调试
    /// </summary>
    Debug,
    
    /// <summary>
    /// 信息
    /// </summary>
    Info,
    
    /// <summary>
    /// 警告
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error,
    
    /// <summary>
    /// 严重错误
    /// </summary>
    Critical
}