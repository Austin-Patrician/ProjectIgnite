using ProjectIgnite.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 进程管理服务接口
    /// </summary>
    public interface IProcessManagementService
    {
        /// <summary>
        /// 启动项目
        /// </summary>
        /// <param name="projectSource">项目源</param>
        /// <param name="configuration">项目配置</param>
        /// <returns>启动的项目信息</returns>
        Task<LaunchedProject> StartProjectAsync(ProjectSource projectSource, ProjectConfiguration configuration);

        /// <summary>
        /// 停止项目
        /// </summary>
        /// <param name="launchedProjectId">启动项目ID</param>
        /// <returns>是否成功停止</returns>
        Task<bool> StopProjectAsync(int launchedProjectId);

        /// <summary>
        /// 重启项目
        /// </summary>
        /// <param name="launchedProjectId">启动项目ID</param>
        /// <returns>是否成功重启</returns>
        Task<bool> RestartProjectAsync(int launchedProjectId);

        /// <summary>
        /// 获取项目状态
        /// </summary>
        /// <param name="launchedProjectId">启动项目ID</param>
        /// <returns>项目状态信息</returns>
        Task<Dictionary<string, object>> GetProjectStatusAsync(int launchedProjectId);

        /// <summary>
        /// 获取所有运行中的项目
        /// </summary>
        /// <returns>运行中的项目列表</returns>
        Task<List<LaunchedProject>> GetRunningProjectsAsync();

        /// <summary>
        /// 健康检查项目
        /// </summary>
        /// <param name="launchedProjectId">启动项目ID</param>
        /// <returns>健康检查结果</returns>
        Task<bool> HealthCheckProjectAsync(int launchedProjectId);

        /// <summary>
        /// 获取项目日志
        /// </summary>
        /// <param name="launchedProjectId">启动项目ID</param>
        /// <param name="lines">获取的行数</param>
        /// <returns>日志内容</returns>
        Task<List<string>> GetProjectLogsAsync(int launchedProjectId, int lines = 100);

        /// <summary>
        /// 清理僵尸进程
        /// </summary>
        /// <returns>清理的进程数</returns>
        Task<int> CleanupZombieProcessesAsync();

        /// <summary>
        /// 进程输出事件
        /// </summary>
        event EventHandler<ProcessOutputEventArgs> ProcessOutput;

        /// <summary>
        /// 进程状态变化事件
        /// </summary>
        event EventHandler<ProcessStatusEventArgs> ProcessStatusChanged;
    }

    /// <summary>
    /// 进程输出事件参数
    /// </summary>
    public class ProcessOutputEventArgs : EventArgs
    {
        public int LaunchedProjectId { get; set; }
        public string Output { get; set; } = string.Empty;
        public bool IsError { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 进程状态变化事件参数
    /// </summary>
    public class ProcessStatusEventArgs : EventArgs
    {
        public int LaunchedProjectId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
