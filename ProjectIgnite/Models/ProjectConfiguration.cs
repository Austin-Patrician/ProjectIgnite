using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 项目配置实体类
    /// </summary>
    public class ProjectConfiguration
    {
        /// <summary>
        /// 配置ID，主键
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 关联的项目源ID
        /// </summary>
        public int ProjectSourceId { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 环境名称：Development, Staging, Production
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// 启动命令
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string StartCommand { get; set; } = string.Empty;

        /// <summary>
        /// 工作目录
        /// </summary>
        [MaxLength(500)]
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// 默认端口
        /// </summary>
        public int? DefaultPort { get; set; }

        /// <summary>
        /// 端口范围起始
        /// </summary>
        public int? PortRangeStart { get; set; }

        /// <summary>
        /// 端口范围结束
        /// </summary>
        public int? PortRangeEnd { get; set; }

        /// <summary>
        /// 环境变量 (JSON格式)
        /// </summary>
        public string? EnvironmentVariables { get; set; }

        /// <summary>
        /// 启动参数
        /// </summary>
        [MaxLength(1000)]
        public string? Arguments { get; set; }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        [MaxLength(500)]
        public string? ConfigFilePath { get; set; }

        /// <summary>
        /// 健康检查URL
        /// </summary>
        [MaxLength(500)]
        public string? HealthCheckUrl { get; set; }

        /// <summary>
        /// 健康检查间隔(秒)
        /// </summary>
        public int HealthCheckInterval { get; set; } = 30;

        /// <summary>
        /// 是否自动重启
        /// </summary>
        public bool AutoRestart { get; set; } = false;

        /// <summary>
        /// 最大重启次数
        /// </summary>
        public int MaxRestartCount { get; set; } = 3;

        /// <summary>
        /// 是否默认配置
        /// </summary>
        public bool IsDefault { get; set; } = false;

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
