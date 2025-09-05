using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 启动项目记录实体类
    /// </summary>
    public class LaunchedProject
    {
        /// <summary>
        /// 记录ID，主键
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 关联的项目源ID
        /// </summary>
        public int ProjectSourceId { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// 项目本地路径
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型：DotNet, NodeJs, Python, Docker等
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ProjectType { get; set; } = string.Empty;

        /// <summary>
        /// 当前运行状态：Stopped, Starting, Running, Error, Stopping
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Stopped";

        /// <summary>
        /// 当前使用的端口
        /// </summary>
        public int? CurrentPort { get; set; }

        /// <summary>
        /// 当前环境
        /// </summary>
        [MaxLength(50)]
        public string? CurrentEnvironment { get; set; }

        /// <summary>
        /// 进程ID
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 停止时间
        /// </summary>
        public DateTime? StoppedAt { get; set; }

        /// <summary>
        /// 最后健康检查时间
        /// </summary>
        public DateTime? LastHealthCheckAt { get; set; }

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
        /// 导航属性：关联的项目源
        /// </summary>
        public virtual ProjectSource? ProjectSource { get; set; }
    }
}
