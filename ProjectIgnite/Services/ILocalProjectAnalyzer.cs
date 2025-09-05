using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.Models;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 本地项目分析器接口
    /// </summary>
    public interface ILocalProjectAnalyzer
    {
        /// <summary>
        /// 分析本地项目结构
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="customInstructions">自定义指令</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>项目分析结果</returns>
        Task<ProjectAnalysisResult> AnalyzeProjectAsync(
            string projectPath,
            string projectName,
            string? customInstructions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检测项目类型
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>项目类型</returns>
        ProjectType DetectProjectType(string projectPath);

        /// <summary>
        /// 扫描文件系统结构
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="maxDepth">最大深度</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文件系统结构</returns>
        Task<FileSystemStructure> ScanFileSystemAsync(
            string projectPath,
            int maxDepth = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 分析项目依赖
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="projectType">项目类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>依赖信息</returns>
        Task<ProjectDependencies> AnalyzeDependenciesAsync(
            string projectPath,
            ProjectType projectType,
            CancellationToken cancellationToken = default);
    }
}
