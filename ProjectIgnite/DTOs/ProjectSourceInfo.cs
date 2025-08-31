using System;
using System.Collections.Generic;

namespace ProjectIgnite.DTOs
{
    /// <summary>
    /// 项目源信息
    /// </summary>
    public class ProjectSourceInfo
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Git仓库URL
        /// </summary>
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>
        /// 本地路径
        /// </summary>
        public string LocalPath { get; set; } = string.Empty;

        /// <summary>
        /// 分支名称
        /// </summary>
        public string Branch { get; set; } = string.Empty;

        /// <summary>
        /// 项目描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 项目状态
        /// </summary>
        public ProjectStatus Status { get; set; }

        /// <summary>
        /// 克隆进度
        /// </summary>
        public double CloneProgress { get; set; }

        /// <summary>
        /// 分析进度
        /// </summary>
        public double AnalysisProgress { get; set; }

        /// <summary>
        /// 主要编程语言
        /// </summary>
        public string PrimaryLanguage { get; set; } = string.Empty;

        /// <summary>
        /// 语言分析结果
        /// </summary>
        public Dictionary<string, double> LanguagePercentages { get; set; } = new();

        /// <summary>
        /// 文件总数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 代码行数
        /// </summary>
        public int LinesOfCode { get; set; }

        /// <summary>
        /// 项目大小（字节）
        /// </summary>
        public long ProjectSize { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 是否自动分析
        /// </summary>
        public bool AutoAnalyze { get; set; } = true;

        /// <summary>
        /// 最后克隆时间
        /// </summary>
        public DateTime? LastClonedAt { get; set; }

        /// <summary>
        /// 最后分析时间
        /// </summary>
        public DateTime? LastAnalyzedAt { get; set; }

        /// <summary>
        /// 语言详情列表
        /// </summary>
        public List<LanguageDetail> Languages { get; set; } = new();

        /// <summary>
        /// 总行数
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// 总大小（字节）
        /// </summary>
        public long TotalSize { get; set; }
    }

    /// <summary>
    /// 项目状态枚举
    /// </summary>
    public enum ProjectStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending,

        /// <summary>
        /// 克隆中
        /// </summary>
        Cloning,

        /// <summary>
        /// 分析中
        /// </summary>
        Analyzing,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }
}