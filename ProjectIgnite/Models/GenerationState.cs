using System;
using System.ComponentModel;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 图表生成状态枚举
    /// </summary>
    public enum GenerationState
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        [Description("空闲")]
        Idle,

        /// <summary>
        /// 验证仓库 URL
        /// </summary>
        [Description("验证仓库")]
        ValidatingRepository,

        /// <summary>
        /// 获取仓库信息
        /// </summary>
        [Description("获取仓库信息")]
        FetchingRepositoryInfo,

        /// <summary>
        /// 获取文件树
        /// </summary>
        [Description("获取文件树")]
        FetchingFileTree,

        /// <summary>
        /// 获取 README 内容
        /// </summary>
        [Description("获取 README")]
        FetchingReadme,

        /// <summary>
        /// 生成架构说明
        /// </summary>
        [Description("生成架构说明")]
        GeneratingExplanation,

        /// <summary>
        /// 生成组件映射
        /// </summary>
        [Description("生成组件映射")]
        GeneratingMapping,

        /// <summary>
        /// 生成图表代码
        /// </summary>
        [Description("生成图表")]
        GeneratingDiagram,

        /// <summary>
        /// 完成
        /// </summary>
        [Description("完成")]
        Completed,

        /// <summary>
        /// 错误
        /// </summary>
        [Description("错误")]
        Error,

        /// <summary>
        /// 已取消
        /// </summary>
        [Description("已取消")]
        Cancelled
    }

    /// <summary>
    /// 生成进度信息
    /// </summary>
    public class GenerationProgress
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        public GenerationState State { get; set; } = GenerationState.Idle;

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public int Percentage { get; set; } = 0;

        /// <summary>
        /// 当前步骤描述
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 详细信息
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 当前步骤开始时间
        /// </summary>
        public DateTime CurrentStepStartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted => State == GenerationState.Completed;

        /// <summary>
        /// 是否出错
        /// </summary>
        public bool IsError => State == GenerationState.Error;

        /// <summary>
        /// 是否已取消
        /// </summary>
        public bool IsCancelled => State == GenerationState.Cancelled;

        /// <summary>
        /// 是否正在进行中
        /// </summary>
        public bool IsInProgress => State != GenerationState.Idle && 
                                   State != GenerationState.Completed && 
                                   State != GenerationState.Error && 
                                   State != GenerationState.Cancelled;

        /// <summary>
        /// 获取已用时间
        /// </summary>
        /// <returns>已用时间</returns>
        public TimeSpan GetElapsedTime()
        {
            return DateTime.UtcNow - StartTime;
        }

        /// <summary>
        /// 获取当前步骤已用时间
        /// </summary>
        /// <returns>当前步骤已用时间</returns>
        public TimeSpan GetCurrentStepElapsedTime()
        {
            return DateTime.UtcNow - CurrentStepStartTime;
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="state">新状态</param>
        /// <param name="percentage">进度百分比</param>
        /// <param name="message">消息</param>
        /// <param name="details">详细信息</param>
        public void Update(GenerationState state, int percentage, string message, string? details = null)
        {
            if (State != state)
            {
                CurrentStepStartTime = DateTime.UtcNow;
            }
            
            State = state;
            Percentage = Math.Clamp(percentage, 0, 100);
            Message = message;
            Details = details;
            ErrorMessage = null; // 清除之前的错误信息
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="details">错误详细信息</param>
        public void SetError(string errorMessage, string? details = null)
        {
            State = GenerationState.Error;
            Message = "生成失败";
            ErrorMessage = errorMessage;
            Details = details;
        }

        /// <summary>
        /// 设置取消状态
        /// </summary>
        public void SetCancelled()
        {
            State = GenerationState.Cancelled;
            Message = "已取消";
            ErrorMessage = null;
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public void Reset()
        {
            State = GenerationState.Idle;
            Percentage = 0;
            Message = string.Empty;
            Details = null;
            ErrorMessage = null;
            StartTime = DateTime.UtcNow;
            CurrentStepStartTime = DateTime.UtcNow;
        }
    }
}