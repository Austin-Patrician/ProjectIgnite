using System;
using System.Collections.Generic;

namespace ProjectIgnite.DTOs
{
    /// <summary>
    /// Git仓库信息
    /// </summary>
    public class GitRepositoryInfo
    {
        /// <summary>
        /// 仓库名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 仓库描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 默认分支
        /// </summary>
        public string DefaultBranch { get; set; } = "main";

        /// <summary>
        /// 所有分支列表
        /// </summary>
        public List<string> Branches { get; set; } = new();

        /// <summary>
        /// 仓库大小（KB）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 是否为私有仓库
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// 最后提交时间
        /// </summary>
        public DateTime? LastCommitDate { get; set; }

        /// <summary>
        /// 提供商类型
        /// </summary>
        public GitProviderType Provider { get; set; }

        /// <summary>
        /// 仓库URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 克隆URL
        /// </summary>
        public string CloneUrl { get; set; } = string.Empty;

        /// <summary>
        /// 主要编程语言
        /// </summary>
        public string PrimaryLanguage { get; set; } = string.Empty;

        /// <summary>
        /// 许可证
        /// </summary>
        public string License { get; set; } = string.Empty;

        /// <summary>
        /// 星标数
        /// </summary>
        public int Stars { get; set; }

        /// <summary>
        /// Fork数
        /// </summary>
        public int Forks { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string ValidationMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Git提供商类型
    /// </summary>
    public enum GitProviderType
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown,

        /// <summary>
        /// GitHub
        /// </summary>
        GitHub,

        /// <summary>
        /// GitLab
        /// </summary>
        GitLab,

        /// <summary>
        /// Bitbucket
        /// </summary>
        Bitbucket,

        /// <summary>
        /// Azure DevOps
        /// </summary>
        Azure,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }
}