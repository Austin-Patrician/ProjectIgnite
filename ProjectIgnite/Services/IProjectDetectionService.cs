using ProjectIgnite.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 项目检测服务接口
    /// </summary>
    public interface IProjectDetectionService
    {
        /// <summary>
        /// 检测项目类型
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>项目类型</returns>
        Task<string> DetectProjectTypeAsync(string projectPath);

        /// <summary>
        /// 生成项目配置
        /// </summary>
        /// <param name="projectSource">项目源</param>
        /// <returns>项目配置列表</returns>
        Task<List<ProjectConfiguration>> GenerateProjectConfigurationsAsync(ProjectSource projectSource);

        /// <summary>
        /// 检测项目的启动配置
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="projectType">项目类型</param>
        /// <returns>启动配置信息</returns>
        Task<Dictionary<string, object>> DetectLaunchSettingsAsync(string projectPath, string projectType);

        /// <summary>
        /// 获取推荐的端口范围
        /// </summary>
        /// <param name="projectType">项目类型</param>
        /// <returns>端口范围 (起始端口, 结束端口)</returns>
        (int startPort, int endPort) GetRecommendedPortRange(string projectType);

        /// <summary>
        /// 检测环境配置文件
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="projectType">项目类型</param>
        /// <returns>环境配置文件路径列表</returns>
        Task<List<string>> DetectEnvironmentConfigFilesAsync(string projectPath, string projectType);
    }
}
