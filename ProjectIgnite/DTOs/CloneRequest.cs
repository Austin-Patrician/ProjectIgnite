using System;

namespace ProjectIgnite.DTOs
{
    /// <summary>
    /// Git克隆请求
    /// </summary>
    public class CloneRequest
    {
        /// <summary>
        /// Git仓库URL
        /// </summary>
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>
        /// 本地路径
        /// </summary>
        public string LocalPath { get; set; } = string.Empty;

        /// <summary>
        /// 分支名称
        /// </summary>
        public string? Branch { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// 目标路径
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// 项目描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否浅克隆
        /// </summary>
        public bool IsShallowClone { get; set; } = true;

        /// <summary>
        /// 是否覆盖现有内容
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// 是否自动分析
        /// </summary>
        public bool AutoAnalyze { get; set; } = true;
    }
}