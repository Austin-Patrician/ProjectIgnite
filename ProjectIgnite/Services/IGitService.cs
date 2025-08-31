using ProjectIgnite.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// Git服务接口
    /// </summary>
    public interface IGitService
    {
        /// <summary>
        /// 克隆Git仓库
        /// </summary>
        /// <param name="request">克隆请求</param>
        /// <param name="progress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>克隆结果</returns>
        Task<CloneResult> CloneRepositoryAsync(CloneRequest request, IProgress<CloneProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证Git URL是否有效
        /// </summary>
        /// <param name="gitUrl">Git URL</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        Task<GitValidationResult> ValidateGitUrlAsync(string gitUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取仓库信息
        /// </summary>
        /// <param name="gitUrl">Git URL</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>仓库信息</returns>
        Task<GitRepositoryInfo?> GetRepositoryInfoAsync(string gitUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取仓库分支列表
        /// </summary>
        /// <param name="gitUrl">Git URL</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分支列表</returns>
        Task<string[]> GetBranchesAsync(string gitUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查本地路径是否为Git仓库
        /// </summary>
        /// <param name="localPath">本地路径</param>
        /// <returns>是否为Git仓库</returns>
        bool IsGitRepository(string localPath);

        /// <summary>
        /// 获取本地仓库信息
        /// </summary>
        /// <param name="localPath">本地路径</param>
        /// <returns>本地仓库信息</returns>
        Task<LocalGitInfo?> GetLocalRepositoryInfoAsync(string localPath);

        /// <summary>
        /// 拉取最新代码
        /// </summary>
        /// <param name="localPath">本地路径</param>
        /// <param name="progress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>拉取结果</returns>
        Task<PullResult> PullRepositoryAsync(string localPath, IProgress<CloneProgress>? progress = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 克隆结果
    /// </summary>
    public class CloneResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? LocalPath { get; set; }
        public TimeSpan Duration { get; set; }
        public long TotalBytes { get; set; }
        public int TotalFiles { get; set; }
    }

    /// <summary>
    /// Git URL验证结果
    /// </summary>
    public class GitValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? NormalizedUrl { get; set; }
        public GitProviderType Provider { get; set; }
        public bool RequiresAuthentication { get; set; }
    }



    /// <summary>
    /// 本地Git仓库信息
    /// </summary>
    public class LocalGitInfo
    {
        public string RemoteUrl { get; set; } = string.Empty;
        public string CurrentBranch { get; set; } = string.Empty;
        public string[] Branches { get; set; } = Array.Empty<string>();
        public bool HasUncommittedChanges { get; set; }
        public DateTime LastCommitDate { get; set; }
        public string LastCommitHash { get; set; } = string.Empty;
        public string LastCommitMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 拉取结果
    /// </summary>
    public class PullResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int UpdatedFiles { get; set; }
        public bool HasConflicts { get; set; }
        public string[] ConflictFiles { get; set; } = Array.Empty<string>();
    }
}