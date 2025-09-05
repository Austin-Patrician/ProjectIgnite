using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 端口分配实体类
    /// </summary>
    public class PortAllocation
    {
        /// <summary>
        /// 分配ID，主键
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 关联的项目源ID
        /// </summary>
        public int? ProjectSourceId { get; set; }

        /// <summary>
        /// 关联的启动项目ID
        /// </summary>
        public int? LaunchedProjectId { get; set; }

        /// <summary>
        /// 端口状态：Available, Reserved, InUse, Blocked
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Available";

        /// <summary>
        /// 分配原因/描述
        /// </summary>
        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// 分配时间
        /// </summary>
        public DateTime? AllocatedAt { get; set; }

        /// <summary>
        /// 释放时间
        /// </summary>
        public DateTime? ReleasedAt { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; } = 0;

        /// <summary>
        /// 是否为系统保留端口
        /// </summary>
        public bool IsSystemReserved { get; set; } = false;

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

        /// <summary>
        /// 导航属性：关联的启动项目
        /// </summary>
        public virtual LaunchedProject? LaunchedProject { get; set; }
    }
}
