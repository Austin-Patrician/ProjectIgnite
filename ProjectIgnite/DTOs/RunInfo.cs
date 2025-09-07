using System.Collections.Generic;

namespace ProjectIgnite.DTOs;

/// <summary>
/// 项目运行信息
/// </summary>
public class RunInfo
{
    /// <summary>
    /// 端口候选列表
    /// </summary>
    public List<PortCandidate> Ports { get; set; } = new();

    /// <summary>
    /// URL候选列表
    /// </summary>
    public List<UrlCandidate> Urls { get; set; } = new();

    /// <summary>
    /// 启动命令列表
    /// </summary>
    public List<string> StartCommands { get; set; } = new();

    /// <summary>
    /// 环境变量文件
    /// </summary>
    public List<string> EnvironmentFiles { get; set; } = new();
}

/// <summary>
/// 端口候选信息
/// </summary>
public class PortCandidate
{
    /// <summary>
    /// 端口号
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// 置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 来源类型
    /// </summary>
    public RunInfoSource Source { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool? IsAvailable { get; set; }
}

/// <summary>
/// URL候选信息
/// </summary>
public class UrlCandidate
{
    /// <summary>
    /// URL地址
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 来源类型
    /// </summary>
    public RunInfoSource Source { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 运行信息来源枚举
/// </summary>
public enum RunInfoSource
{
    /// <summary>
    /// launchSettings.json
    /// </summary>
    LaunchSettings,
    
    /// <summary>
    /// appsettings.json
    /// </summary>
    AppSettings,
    
    /// <summary>
    /// package.json scripts
    /// </summary>
    Scripts,
    
    /// <summary>
    /// 代码推断
    /// </summary>
    CodeInference,
    
    /// <summary>
    /// 框架默认值
    /// </summary>
    Default
}