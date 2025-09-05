using Microsoft.EntityFrameworkCore;
using ProjectIgnite.Data;
using ProjectIgnite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 端口管理服务实现
    /// </summary>
    public class PortManagementService : IPortManagementService
    {
        private readonly ProjectIgniteDbContext _context;

        public PortManagementService(ProjectIgniteDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 分配端口
        /// </summary>
        public async Task<PortAllocation?> AllocatePortAsync(string projectName, int? preferredPort = null, int? portRangeStart = null, int? portRangeEnd = null)
        {
            var startPort = portRangeStart ?? 3000;
            var endPort = portRangeEnd ?? 9999;

            // 先查找项目源ID
            var projectSource = await _context.ProjectSources
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (projectSource == null)
            {
                throw new InvalidOperationException($"Project '{projectName}' not found");
            }

            var projectSourceId = projectSource.Id;

            // 如果指定了首选端口，先检查是否可用
            if (preferredPort.HasValue && preferredPort.Value >= startPort && preferredPort.Value <= endPort)
            {
                if (await IsPortAvailableAsync(preferredPort.Value))
                {
                    return await CreatePortAllocationAsync(preferredPort.Value, projectSourceId);
                }
            }

            // 查找项目的历史端口使用记录，优先使用之前用过的端口
            var historyPorts = await _context.PortAllocations
                .Where(p => p.ProjectSourceId == projectSourceId && p.Status == "Available")
                .OrderByDescending(p => p.LastUsedAt)
                .Select(p => p.Port)
                .ToListAsync();

            foreach (var port in historyPorts)
            {
                if (port >= startPort && port <= endPort && await IsPortAvailableAsync(port))
                {
                    return await UpdatePortAllocationAsync(port, projectSourceId, "InUse");
                }
            }

            // 寻找可用端口
            for (int port = startPort; port <= endPort; port++)
            {
                if (await IsPortAvailableAsync(port))
                {
                    return await CreatePortAllocationAsync(port, projectSourceId);
                }
            }

            throw new InvalidOperationException($"无法在范围 {startPort}-{endPort} 内找到可用端口");
        }

        /// <summary>
        /// 释放端口
        /// </summary>
        public async Task<bool> ReleasePortAsync(int port)
        {
            try
            {
                var allocation = await _context.PortAllocations
                    .FirstOrDefaultAsync(p => p.Port == port);

                if (allocation != null)
                {
                    allocation.Status = "Available";
                    allocation.ReleasedAt = DateTime.Now;
                    allocation.LastUsedAt = DateTime.Now;
                    allocation.UpdatedAt = DateTime.Now;
                    allocation.LaunchedProjectId = null;

                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 检查端口是否可用
        /// </summary>
        public async Task<bool> IsPortAvailableAsync(int port)
        {
            // 检查数据库中的端口状态
            var allocation = await _context.PortAllocations
                .FirstOrDefaultAsync(p => p.Port == port);

            if (allocation != null)
            {
                // 如果是系统保留端口，不可用
                if (allocation.IsSystemReserved)
                    return false;

                // 如果状态为被占用，不可用
                if (allocation.Status == "InUse" || allocation.Status == "Reserved")
                    return false;
            }

            // 检查系统级端口是否被占用
            return IsPortAvailableInSystem(port);
        }

        /// <summary>
        /// 获取端口使用状态
        /// </summary>
        public async Task<PortAllocation?> GetPortStatusAsync(int port)
        {
            return await _context.PortAllocations
                .Include(p => p.ProjectSource)
                .Include(p => p.LaunchedProject)
                .FirstOrDefaultAsync(p => p.Port == port);
        }

        /// <summary>
        /// 获取项目的端口分配历史
        /// </summary>
        public async Task<List<PortAllocation>> GetProjectPortHistoryAsync(int projectSourceId)
        {
            return await _context.PortAllocations
                .Where(p => p.ProjectSourceId == projectSourceId)
                .OrderByDescending(p => p.LastUsedAt ?? p.AllocatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取可用端口列表
        /// </summary>
        public async Task<List<int>> GetAvailablePortsAsync(int startPort, int endPort, int count = 10)
        {
            var availablePorts = new List<int>();
            var checkedCount = 0;
            var maxCheck = (endPort - startPort + 1) * 2; // 避免无限循环

            for (int port = startPort; port <= endPort && availablePorts.Count < count && checkedCount < maxCheck; port++)
            {
                checkedCount++;
                if (await IsPortAvailableAsync(port))
                {
                    availablePorts.Add(port);
                }
            }

            return availablePorts;
        }

        /// <summary>
        /// 保留系统端口
        /// </summary>
        public async Task<int> ReserveSystemPortsAsync(List<int> ports)
        {
            int reservedCount = 0;

            foreach (var port in ports)
            {
                try
                {
                    var existing = await _context.PortAllocations
                        .FirstOrDefaultAsync(p => p.Port == port);

                    if (existing == null)
                    {
                        var allocation = new PortAllocation
                        {
                            Port = port,
                            Status = "Reserved",
                            IsSystemReserved = true,
                            Description = "系统保留端口",
                            AllocatedAt = DateTime.Now
                        };

                        _context.PortAllocations.Add(allocation);
                        reservedCount++;
                    }
                    else if (!existing.IsSystemReserved)
                    {
                        existing.IsSystemReserved = true;
                        existing.Status = "Reserved";
                        existing.Description = "系统保留端口";
                        existing.UpdatedAt = DateTime.Now;
                        reservedCount++;
                    }
                }
                catch (Exception)
                {
                    // 忽略单个端口的保留失败
                }
            }

            if (reservedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return reservedCount;
        }

        /// <summary>
        /// 获取端口使用统计
        /// </summary>
        public async Task<Dictionary<string, object>> GetPortUsageStatisticsAsync()
        {
            var stats = new Dictionary<string, object>();

            var totalAllocations = await _context.PortAllocations.CountAsync();
            var inUseCount = await _context.PortAllocations.CountAsync(p => p.Status == "InUse");
            var reservedCount = await _context.PortAllocations.CountAsync(p => p.Status == "Reserved");
            var availableCount = await _context.PortAllocations.CountAsync(p => p.Status == "Available");
            var systemReservedCount = await _context.PortAllocations.CountAsync(p => p.IsSystemReserved);

            var mostUsedPorts = await _context.PortAllocations
                .Where(p => p.UsageCount > 0)
                .OrderByDescending(p => p.UsageCount)
                .Take(10)
                .Select(p => new { p.Port, p.UsageCount, p.LastUsedAt })
                .ToListAsync();

            var portRangeUsage = await _context.PortAllocations
                .GroupBy(p => (p.Port / 1000) * 1000) // 按千位分组
                .Select(g => new 
                { 
                    Range = $"{g.Key}-{g.Key + 999}",
                    Count = g.Count(),
                    InUse = g.Count(p => p.Status == "InUse")
                })
                .OrderBy(g => g.Range)
                .ToListAsync();

            stats["TotalAllocations"] = totalAllocations;
            stats["InUseCount"] = inUseCount;
            stats["ReservedCount"] = reservedCount;
            stats["AvailableCount"] = availableCount;
            stats["SystemReservedCount"] = systemReservedCount;
            stats["MostUsedPorts"] = mostUsedPorts;
            stats["PortRangeUsage"] = portRangeUsage;

            return stats;
        }

        /// <summary>
        /// 获取活动的端口分配记录
        /// </summary>
        public async Task<List<PortAllocation>> GetActivePortAllocationsAsync()
        {
            return await _context.PortAllocations
                .Where(p => p.Status == "InUse" || p.Status == "Reserved")
                .Include(p => p.ProjectSource)
                .Include(p => p.LaunchedProject)
                .OrderBy(p => p.Port)
                .ToListAsync();
        }

        /// <summary>
        /// 清理未使用的端口分配记录
        /// </summary>
        public async Task<int> CleanupUnusedPortAllocationsAsync()
        {
            var cutoffDate = DateTime.Now.AddDays(-30); // 30天前

            var unusedAllocations = await _context.PortAllocations
                .Where(p => p.Status == "Available" && 
                           p.UsageCount == 0 && 
                           !p.IsSystemReserved &&
                           p.CreatedAt < cutoffDate)
                .ToListAsync();

            if (unusedAllocations.Any())
            {
                _context.PortAllocations.RemoveRange(unusedAllocations);
                await _context.SaveChangesAsync();
            }

            return unusedAllocations.Count;
        }

        /// <summary>
        /// 创建端口分配记录
        /// </summary>
        private async Task<PortAllocation> CreatePortAllocationAsync(int port, int projectSourceId)
        {
            var allocation = new PortAllocation
            {
                Port = port,
                ProjectSourceId = projectSourceId,
                Status = "InUse",
                AllocatedAt = DateTime.Now,
                LastUsedAt = DateTime.Now,
                UsageCount = 1,
                Description = "自动分配"
            };

            _context.PortAllocations.Add(allocation);
            await _context.SaveChangesAsync();
            return allocation;
        }

        /// <summary>
        /// 更新端口分配记录
        /// </summary>
        private async Task<PortAllocation?> UpdatePortAllocationAsync(int port, int projectSourceId, string status)
        {
            var allocation = await _context.PortAllocations
                .FirstOrDefaultAsync(p => p.Port == port);

            if (allocation != null)
            {
                allocation.ProjectSourceId = projectSourceId;
                allocation.Status = status;
                allocation.AllocatedAt = DateTime.Now;
                allocation.LastUsedAt = DateTime.Now;
                allocation.UsageCount++;
                allocation.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return allocation;
            }

            return null;
        }

        /// <summary>
        /// 检查系统级端口是否可用
        /// </summary>
        private bool IsPortAvailableInSystem(int port)
        {
            try
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
                var udpListeners = ipGlobalProperties.GetActiveUdpListeners();

                // 检查TCP端口
                if (tcpListeners.Any(listener => listener.Port == port))
                    return false;

                // 检查UDP端口
                if (udpListeners.Any(listener => listener.Port == port))
                    return false;

                return true;
            }
            catch (Exception)
            {
                // 如果无法检查，假设端口不可用以确保安全
                return false;
            }
        }
    }
}
