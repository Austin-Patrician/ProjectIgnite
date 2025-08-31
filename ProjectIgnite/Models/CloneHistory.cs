using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 克隆历史记录实体类，对应CLONE_HISTORY表
    /// </summary>
    public class CloneHistory
    {
        /// <summary>
        /// 历史记录ID，主键
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 关联的项目ID，外键
        /// </summary>
        [Required]
        public int ProjectId { get; set; }

        /// <summary>
        /// 克隆开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 克隆结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 克隆状态：started, progress, completed, failed, cancelled
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "started";

        /// <summary>
        /// 克隆进度百分比 (0-100)
        /// </summary>
        public int Progress { get; set; } = 0;

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 克隆的Git URL
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>
        /// 目标本地路径
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// 克隆的分支名称
        /// </summary>
        [MaxLength(100)]
        public string? BranchName { get; set; }

        /// <summary>
        /// 克隆耗时（秒）
        /// </summary>
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// 克隆的文件总数
        /// </summary>
        public int? TotalFiles { get; set; }

        /// <summary>
        /// 克隆的总大小（字节）
        /// </summary>
        public long? TotalSize { get; set; }

        /// <summary>
        /// 导航属性：关联的项目源
        /// </summary>
        [ForeignKey("ProjectId")]
        public virtual ProjectSource? ProjectSource { get; set; }
    }
}