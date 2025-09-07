using System.Collections.Generic;

namespace ProjectIgnite.DTOs;

/// <summary>
/// 项目结构图
/// </summary>
public class StructureGraph
{
    /// <summary>
    /// 图节点列表
    /// </summary>
    public List<GraphNode> Nodes { get; set; } = new();

    /// <summary>
    /// 图边列表
    /// </summary>
    public List<GraphEdge> Edges { get; set; } = new();

    /// <summary>
    /// 布局信息
    /// </summary>
    public GraphLayout? Layout { get; set; }
}

/// <summary>
/// 图节点
/// </summary>
public class GraphNode
{
    /// <summary>
    /// 节点唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    public GraphNodeType Type { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 风险评分 (0-100)
    /// </summary>
    public double? RiskScore { get; set; }

    /// <summary>
    /// 关联文件引用
    /// </summary>
    public List<string> FileRefs { get; set; } = new();

    /// <summary>
    /// 节点位置
    /// </summary>
    public NodePosition? Position { get; set; }

    /// <summary>
    /// 节点大小
    /// </summary>
    public NodeSize? Size { get; set; }
}

/// <summary>
/// 图边
/// </summary>
public class GraphEdge
{
    /// <summary>
    /// 源节点ID
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点ID
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// 关系类型
    /// </summary>
    public GraphRelationType RelationType { get; set; }

    /// <summary>
    /// 边权重
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// 边标签
    /// </summary>
    public string? Label { get; set; }
}

/// <summary>
/// 图节点类型枚举
/// </summary>
public enum GraphNodeType
{
    /// <summary>
    /// 解决方案
    /// </summary>
    Solution,
    
    /// <summary>
    /// 项目
    /// </summary>
    Project,
    
    /// <summary>
    /// 模块
    /// </summary>
    Module,
    
    /// <summary>
    /// 服务
    /// </summary>
    Service,
    
    /// <summary>
    /// 控制器
    /// </summary>
    Controller,
    
    /// <summary>
    /// 文件
    /// </summary>
    File,
    
    /// <summary>
    /// 包
    /// </summary>
    Package
}

/// <summary>
/// 图关系类型枚举
/// </summary>
public enum GraphRelationType
{
    /// <summary>
    /// 依赖关系
    /// </summary>
    DependsOn,
    
    /// <summary>
    /// 引用关系
    /// </summary>
    References,
    
    /// <summary>
    /// 调用关系
    /// </summary>
    Calls,
    
    /// <summary>
    /// 导入关系
    /// </summary>
    Imports,
    
    /// <summary>
    /// 包含关系
    /// </summary>
    Contains
}

/// <summary>
/// 节点位置
/// </summary>
public class NodePosition
{
    /// <summary>
    /// X坐标
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y坐标
    /// </summary>
    public double Y { get; set; }
}

/// <summary>
/// 节点大小
/// </summary>
public class NodeSize
{
    /// <summary>
    /// 宽度
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public double Height { get; set; }
}

/// <summary>
/// 图布局信息
/// </summary>
public class GraphLayout
{
    /// <summary>
    /// 布局算法类型
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// 画布宽度
    /// </summary>
    public double CanvasWidth { get; set; }

    /// <summary>
    /// 画布高度
    /// </summary>
    public double CanvasHeight { get; set; }

    /// <summary>
    /// 缩放级别
    /// </summary>
    public double ZoomLevel { get; set; } = 1.0;
}