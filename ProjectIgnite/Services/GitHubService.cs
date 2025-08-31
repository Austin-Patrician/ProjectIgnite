using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using ProjectIgnite.Models;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// GitHub服务实现
    /// </summary>
    public class GitHubService : IGitHubService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly ILogger<GitHubService> _logger;
        private static readonly Regex RepositoryUrlRegex = new(
            @"^https://github\.com/([^/]+)/([^/]+?)(?:\.git)?/?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public GitHubService(ILogger<GitHubService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 创建GitHub客户端（匿名访问）
            _gitHubClient = new GitHubClient(new ProductHeaderValue("ProjectIgnite"));
            
            // TODO: 如果需要认证，可以在这里设置token
            // _gitHubClient.Credentials = new Credentials("your-token");
        }

        /// <summary>
        /// 获取仓库信息
        /// </summary>
        public async Task<RepositoryInfo> GetRepositoryInfoAsync(
            string repositoryUrl,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (owner, name) = ParseRepositoryUrl(repositoryUrl);
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("无效的仓库URL: {RepositoryUrl}", repositoryUrl);
                    throw new ArgumentException("无效的仓库URL", nameof(repositoryUrl));
                }

                _logger.LogInformation("获取仓库信息: {Owner}/{Name}", owner, name);

                var repository = await _gitHubClient.Repository.Get(owner, name);
                var languagesResponse = await _gitHubClient.Repository.GetAllLanguages(owner, name);
                
                // 转换语言统计数据
                var languages = languagesResponse.ToDictionary(lang => lang.Name, lang => (long)lang.NumberOfBytes);

                var repositoryInfo = new RepositoryInfo
                {
                    Owner = repository.Owner.Login,
                    Name = repository.Name,
                    Url = repository.HtmlUrl,
                    Description = repository.Description,
                    Language = repository.Language,
                    StarCount = repository.StargazersCount,
                    ForkCount = repository.ForksCount,
                    Size = repository.Size,
                    CreatedAt = repository.CreatedAt.DateTime,
                    UpdatedAt = repository.UpdatedAt.DateTime,
                    DefaultBranch = repository.DefaultBranch,
                    IsPrivate = repository.Private,
                    HasIssues = repository.HasIssues,
                    HasWiki = repository.HasWiki,
                    OpenIssuesCount = repository.OpenIssuesCount,
                    WatchersCount = repository.SubscribersCount,
                    Languages = languages
                };

                _logger.LogInformation("成功获取仓库信息: {Owner}/{Name}", owner, name);
                return repositoryInfo;
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("仓库不存在: {RepositoryUrl}", repositoryUrl);
                throw new InvalidOperationException("仓库不存在或无法访问", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取仓库信息时发生错误: {RepositoryUrl}", repositoryUrl);
                throw;
            }
        }

        /// <summary>
        /// 获取文件树
        /// </summary>
        public async Task<FileTreeNode> GetFileTreeAsync(
            string owner,
            string repo,
            string? branch = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    throw new ArgumentException("仓库所有者和名称不能为空");
                }

                _logger.LogInformation("获取文件树: {Owner}/{Name}, 分支: {Branch}", owner, repo, branch ?? "default");

                // 如果没有指定分支，获取默认分支
                if (string.IsNullOrEmpty(branch))
                {
                    var repository = await _gitHubClient.Repository.Get(owner, repo);
                    branch = repository.DefaultBranch;
                }

                // 获取树结构（递归获取）
                var tree = await _gitHubClient.Git.Tree.GetRecursive(owner, repo, branch);
                
                // 构建文件树
                var rootNode = new FileTreeNode
                {
                    Name = repo,
                    Path = "",
                    Type = FileTreeNodeType.Directory,
                    Children = new List<FileTreeNode>()
                };

                // 按路径组织文件树
                foreach (var item in tree.Tree)
                {
                    AddToTree(rootNode, item.Path, item);
                }

                _logger.LogInformation("成功获取文件树: {Owner}/{Name}, 文件数: {Count}", owner, repo, tree.Tree.Count);
                return rootNode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件树时发生错误: {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        /// <summary>
        /// 获取README内容
        /// </summary>
        public async Task<string> GetReadmeContentAsync(
            string owner,
            string repo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    throw new ArgumentException("仓库所有者和名称不能为空");
                }

                _logger.LogInformation("获取README内容: {Owner}/{Name}", owner, repo);

                var readme = await _gitHubClient.Repository.Content.GetReadme(owner, repo);
                var content = readme.Content ?? string.Empty;

                _logger.LogInformation("成功获取README内容: {Owner}/{Name}, 长度: {Length}", owner, repo, content.Length);
                return content;
            }
            catch (NotFoundException)
            {
                _logger.LogInformation("README文件不存在: {Owner}/{Repo}", owner, repo);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取README内容时发生错误: {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        /// <summary>
        /// 获取文件内容
        /// </summary>
        public async Task<string> GetFileContentAsync(
            string owner,
            string repo,
            string path,
            string? branch = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    throw new ArgumentException("仓库所有者和名称不能为空");
                }

                _logger.LogInformation("获取文件内容: {Owner}/{Name}/{FilePath}", owner, repo, path);

                var fileContents = await _gitHubClient.Repository.Content.GetAllContents(owner, repo, path);
                var fileContent = fileContents.FirstOrDefault();

                if (fileContent?.Type == ContentType.File)
                {
                    _logger.LogInformation("成功获取文件内容: {Owner}/{Name}/{FilePath}, 长度: {Length}", 
                        owner, repo, path, fileContent.Content?.Length ?? 0);
                    return fileContent.Content ?? string.Empty;
                }

                return string.Empty;
            }
            catch (NotFoundException)
            {
                _logger.LogInformation("文件不存在: {Owner}/{Repo}/{FilePath}", owner, repo, path);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件内容时发生错误: {Owner}/{Repo}/{FilePath}", owner, repo, path);
                throw;
            }
        }

        /// <summary>
        /// 解析仓库URL
        /// </summary>
        public (string owner, string repo) ParseRepositoryUrl(string repositoryUrl)
        {
            if (string.IsNullOrWhiteSpace(repositoryUrl))
            {
                return (string.Empty, string.Empty);
            }

            var match = RepositoryUrlRegex.Match(repositoryUrl.Trim());
            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value);
            }

            return (string.Empty, string.Empty);
        }

        /// <summary>
        /// 验证仓库是否存在
        /// </summary>
        public async Task<bool> ValidateRepositoryAsync(
            string repositoryUrl,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (owner, name) = ParseRepositoryUrl(repositoryUrl);
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(name))
                {
                    return false;
                }

                _logger.LogInformation("验证仓库: {Owner}/{Name}", owner, name);

                await _gitHubClient.Repository.Get(owner, name);
                
                _logger.LogInformation("仓库验证成功: {Owner}/{Name}", owner, name);
                return true;
            }
            catch (NotFoundException)
            {
                _logger.LogWarning("仓库不存在: {RepositoryUrl}", repositoryUrl);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证仓库时发生错误: {RepositoryUrl}", repositoryUrl);
                return false;
            }
        }

        #region 私有方法

        /// <summary>
        /// 将文件项添加到树结构中
        /// </summary>
        private static void AddToTree(FileTreeNode root, string path, TreeItem item)
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            // 遍历路径的每一部分
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var isLastPart = i == parts.Length - 1;

                // 查找是否已存在该节点
                var existingNode = current.Children?.FirstOrDefault(c => c.Name == part);
                
                if (existingNode == null)
                {
                    // 创建新节点
                    var newNode = new FileTreeNode
                    {
                        Name = part,
                        Path = string.Join("/", parts.Take(i + 1)),
                        Parent = current
                    };

                    if (isLastPart)
                    {
                        // 最后一部分，设置为实际的文件或目录
                        newNode.Type = item.Type == TreeType.Tree ? FileTreeNodeType.Directory : FileTreeNodeType.File;
                        newNode.Size = item.Size;
                        newNode.Sha = item.Sha;
                        newNode.DownloadUrl = item.Url;
                    }
                    else
                    {
                        // 中间路径，设置为目录
                        newNode.Type = FileTreeNodeType.Directory;
                        newNode.Children = new List<FileTreeNode>();
                    }

                    current.Children ??= new List<FileTreeNode>();
                    current.Children.Add(newNode);
                    current = newNode;
                }
                else
                {
                    current = existingNode;
                }
            }
        }

        #endregion
    }
}