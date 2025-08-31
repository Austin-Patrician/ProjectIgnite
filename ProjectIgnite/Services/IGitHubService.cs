using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.Models;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// GitHub 服务接口
    /// 负责与 GitHub API 交互，获取仓库信息和文件结构
    /// </summary>
    public interface IGitHubService
    {
        /// <summary>
        /// 获取仓库基本信息
        /// </summary>
        /// <param name="repositoryUrl">仓库 URL</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>仓库信息</returns>
        Task<RepositoryInfo> GetRepositoryInfoAsync(
            string repositoryUrl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取仓库文件树结构
        /// </summary>
        /// <param name="owner">仓库所有者</param>
        /// <param name="repo">仓库名称</param>
        /// <param name="branch">分支名称（可选，默认为主分支）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文件树根节点</returns>
        Task<FileTreeNode> GetFileTreeAsync(
            string owner,
            string repo,
            string? branch = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取 README 文件内容
        /// </summary>
        /// <param name="owner">仓库所有者</param>
        /// <param name="repo">仓库名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>README 内容</returns>
        Task<string> GetReadmeContentAsync(
            string owner,
            string repo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取文件内容
        /// </summary>
        /// <param name="owner">仓库所有者</param>
        /// <param name="repo">仓库名称</param>
        /// <param name="path">文件路径</param>
        /// <param name="branch">分支名称（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文件内容</returns>
        Task<string> GetFileContentAsync(
            string owner,
            string repo,
            string path,
            string? branch = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析仓库 URL
        /// </summary>
        /// <param name="repositoryUrl">仓库 URL</param>
        /// <returns>解析结果（所有者和仓库名）</returns>
        (string owner, string repo) ParseRepositoryUrl(string repositoryUrl);

        /// <summary>
        /// 验证仓库是否存在且可访问
        /// </summary>
        /// <param name="repositoryUrl">仓库 URL</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否可访问</returns>
        Task<bool> ValidateRepositoryAsync(
            string repositoryUrl,
            CancellationToken cancellationToken = default);
    }
}