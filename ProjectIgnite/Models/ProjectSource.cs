using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 项目源实体类，对应PROJECT_SOURCE表
    /// </summary>
    public class ProjectSource
    {
        /// <summary>
        /// 项目ID，主键
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Git仓库URL
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>
        /// 本地路径
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string LocalPath { get; set; } = string.Empty;

        /// <summary>
        /// 项目描述
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// 主要编程语言
        /// </summary>
        [MaxLength(50)]
        public string? PrimaryLanguage { get; set; }

        /// <summary>
        /// 项目状态：pending, cloning, analyzing, completed, error
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// 克隆进度百分比 (0-100)
        /// </summary>
        public int CloneProgress { get; set; } = 0;

        /// <summary>
        /// 语言分析进度百分比 (0-100)
        /// </summary>
        public int AnalysisProgress { get; set; } = 0;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后克隆时间
        /// </summary>
        public DateTime? LastClonedAt { get; set; }

        /// <summary>
        /// 最后分析时间
        /// </summary>
        public DateTime? LastAnalyzedAt { get; set; }
        
    }
}