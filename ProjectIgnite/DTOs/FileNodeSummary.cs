using System;
using System.Collections.Generic;

namespace ProjectIgnite.DTOs;

/// <summary>
/// 文件节点摘要信息
/// </summary>
public class FileNodeSummary
{
    /// <summary>
    /// 文件或目录路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    public FileNodeType Type { get; set; }

    /// <summary>
    /// 是否为关键文件（如.csproj、package.json等）
    /// </summary>
    public bool IsKey { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// 子节点列表
    /// </summary>
    public List<FileNodeSummary> Children { get; set; } = new();

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// 检测到的语言类型
    /// </summary>
    public string? Language { get; set; }
}

/// <summary>
/// 文件节点类型枚举
/// </summary>
public enum FileNodeType
{
    /// <summary>
    /// 目录
    /// </summary>
    Directory,
    
    /// <summary>
    /// 文件
    /// </summary>
    File
}