using System;

namespace ProjectIgnite.DTOs
{
    /// <summary>
    /// 克隆进度信息
    /// </summary>
    public class CloneProgress
    {

        public int ProjectId { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Git仓库URL
        /// </summary>
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>
        /// 目标路径
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// 进度值 (0-100)
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// 当前操作描述
        /// </summary>
        public string CurrentOperation { get; set; } = string.Empty;

        /// <summary>
        /// 已处理的对象数
        /// </summary>
        public int ProcessedObjects { get; set; }

        /// <summary>
        /// 是否可以取消
        /// </summary>
        public bool CanCancel { get; set; } = true;

        /// <summary>
        /// 当前状态描述
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 详细消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 已接收的对象数
        /// </summary>
        public int ReceivedObjects { get; set; }

        /// <summary>
        /// 总对象数
        /// </summary>
        public int TotalObjects { get; set; }

        /// <summary>
        /// 已接收的字节数
        /// </summary>
        public long ReceivedBytes { get; set; }

        /// <summary>
        /// 总字节数
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// 当前阶段
        /// </summary>
        public CloneStage Stage { get; set; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 估计剩余时间（秒）
        /// </summary>
        public double? EstimatedRemainingSeconds { get; set; }

        /// <summary>
        /// 传输速度（字节/秒）
        /// </summary>
        public double TransferSpeed { get; set; }

        /// <summary>
        /// 传输速度（对象/秒）
        /// </summary>
        public double Speed { get; set; }
    }

    /// <summary>
    /// 克隆阶段枚举
    /// </summary>
    public enum CloneStage
    {
        /// <summary>
        /// 初始化
        /// </summary>
        Initializing,

        /// <summary>
        /// 连接远程仓库
        /// </summary>
        Connecting,

        /// <summary>
        /// 接收对象
        /// </summary>
        ReceivingObjects,

        /// <summary>
        /// 解析增量
        /// </summary>
        ResolvingDeltas,

        /// <summary>
        /// 检出文件
        /// </summary>
        CheckingOut,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }
}