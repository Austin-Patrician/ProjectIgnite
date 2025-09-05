using ProjectIgnite.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 端口管理服务接口
    /// </summary>
    public interface IPortManagementService
    {
        /// <summary>
        /// 分配端口
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <param name="preferredPort">首选端口</param>
        /// <param name="portRangeStart">端口范围起始</param>
        /// <param name="portRangeEnd">端口范围结束</param>
        /// <returns>分配的端口分配信息</returns>
        Task<PortAllocation?> AllocatePortAsync(string projectName, int? preferredPort = null, int? portRangeStart = null, int? portRangeEnd = null);

        /// <summary>
        /// 释放端口
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否成功释放</returns>
        Task<bool> ReleasePortAsync(int port);

        /// <summary>
        /// 检查端口是否可用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否可用</returns>
        Task<bool> IsPortAvailableAsync(int port);

        /// <summary>
        /// 获取端口使用状态
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>端口分配信息</returns>
        Task<PortAllocation?> GetPortStatusAsync(int port);

        /// <summary>
        /// 获取项目的端口分配历史
        /// </summary>
        /// <param name="projectSourceId">项目源ID</param>
        /// <returns>端口分配历史</returns>
        Task<List<PortAllocation>> GetProjectPortHistoryAsync(int projectSourceId);

        /// <summary>
        /// 获取可用端口列表
        /// </summary>
        /// <param name="startPort">起始端口</param>
        /// <param name="endPort">结束端口</param>
        /// <param name="count">需要的端口数量</param>
        /// <returns>可用端口列表</returns>
        Task<List<int>> GetAvailablePortsAsync(int startPort, int endPort, int count = 10);

        /// <summary>
        /// 保留系统端口
        /// </summary>
        /// <param name="ports">要保留的端口列表</param>
        /// <returns>保留成功的端口数量</returns>
        Task<int> ReserveSystemPortsAsync(List<int> ports);

        /// <summary>
        /// 获取端口使用统计
        /// </summary>
        /// <returns>端口使用统计信息</returns>
        Task<Dictionary<string, object>> GetPortUsageStatisticsAsync();

        /// <summary>
        /// 获取活动的端口分配记录
        /// </summary>
        /// <returns>活动的端口分配记录列表</returns>
        Task<List<PortAllocation>> GetActivePortAllocationsAsync();

        /// <summary>
        /// 清理未使用的端口分配记录
        /// </summary>
        /// <returns>清理的记录数</returns>
        Task<int> CleanupUnusedPortAllocationsAsync();
    }
}
