using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// GitHub 仓库信息模型
    /// 包含仓库的基本信息和元数据
    /// </summary>
    public class RepositoryInfo
    {
        /// <summary>
        /// 仓库所有者
        /// </summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// 仓库名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 仓库完整名称（owner/name）
        /// </summary>
        public string FullName => $"{Owner}/{Name}";

        /// <summary>
        /// 仓库 URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 仓库描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 主要编程语言
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// 编程语言统计
        /// Key: 语言名称，Value: 字节数
        /// </summary>
        public Dictionary<string, long> Languages { get; set; } = new();

        /// <summary>
        /// 星标数
        /// </summary>
        public int StarCount { get; set; }

        /// <summary>
        /// Fork 数
        /// </summary>
        public int ForkCount { get; set; }

        /// <summary>
        /// 是否为私有仓库
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// 是否为 Fork 仓库
        /// </summary>
        public bool IsFork { get; set; }

        /// <summary>
        /// 默认分支名称
        /// </summary>
        public string DefaultBranch { get; set; } = "main";

        /// <summary>
        /// 仓库创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 仓库最后更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 仓库最后推送时间
        /// </summary>
        public DateTime? PushedAt { get; set; }

        /// <summary>
        /// 仓库大小（KB）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 开放问题数量
        /// </summary>
        public int OpenIssuesCount { get; set; }

        /// <summary>
        /// 是否有 Wiki
        /// </summary>
        public bool HasWiki { get; set; }

        /// <summary>
        /// 是否有 Issues
        /// </summary>
        public bool HasIssues { get; set; }

        /// <summary>
        /// 关注者数量
        /// </summary>
        public int WatchersCount { get; set; }

        /// <summary>
        /// 是否有 Projects
        /// </summary>
        public bool HasProjects { get; set; }

        /// <summary>
        /// 许可证信息
        /// </summary>
        public string? License { get; set; }

        /// <summary>
        /// 主题标签
        /// </summary>
        public List<string> Topics { get; set; } = new();

        /// <summary>
        /// README 内容
        /// </summary>
        public string ReadmeContent { get; set; } = string.Empty;

        /// <summary>
        /// 文件树结构
        /// </summary>
        public FileTreeNode? FileTree { get; set; }

        /// <summary>
        /// 获取主要语言百分比
        /// </summary>
        /// <returns>主要语言及其百分比</returns>
        public Dictionary<string, double> GetLanguagePercentages()
        {
            var result = new Dictionary<string, double>();
            if (!Languages.Any()) return result;

            var total = Languages.Values.Sum();
            if (total == 0) return result;

            foreach (var (language, bytes) in Languages)
            {
                result[language] = Math.Round((double)bytes / total * 100, 2);
            }

            return result.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// 获取仓库活跃度评分（0-100）
        /// </summary>
        /// <returns>活跃度评分</returns>
        public int GetActivityScore()
        {
            var score = 0;

            // 基于星标数（最多 30 分）
            score += Math.Min(StarCount / 10, 30);

            // 基于最近更新时间（最多 25 分）
            var daysSinceUpdate = (DateTime.UtcNow - UpdatedAt).Days;
            if (daysSinceUpdate <= 7) score += 25;
            else if (daysSinceUpdate <= 30) score += 20;
            else if (daysSinceUpdate <= 90) score += 15;
            else if (daysSinceUpdate <= 365) score += 10;

            // 基于 Fork 数（最多 20 分）
            score += Math.Min(ForkCount / 5, 20);

            // 基于开放问题数（最多 15 分）
            if (OpenIssuesCount > 0 && OpenIssuesCount <= 50) score += 15;
            else if (OpenIssuesCount > 50) score += 10;

            // 基于仓库特性（最多 10 分）
            if (HasWiki) score += 3;
            if (HasIssues) score += 3;
            if (HasProjects) score += 2;
            if (!string.IsNullOrEmpty(License)) score += 2;

            return Math.Min(score, 100);
        }
    }
}