using System;
using System.Collections.Generic;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 图表模型
    /// 表示一个完整的架构图表及其相关信息
    /// </summary>
    public class DiagramModel
    {
        /// <summary>
        /// 图表唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 关联的 GitHub 仓库 URL
        /// </summary>
        public string RepositoryUrl { get; set; } = string.Empty;

        /// <summary>
        /// Mermaid 图表代码
        /// </summary>
        public string MermaidCode { get; set; } = string.Empty;

        /// <summary>
        /// 架构说明文本
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 组件映射（JSON 字符串格式）
        /// </summary>
        public string ComponentMapping { get; set; } = string.Empty;

        /// <summary>
        /// 图表创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 图表最后更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 用户自定义指令
        /// </summary>
        public string? CustomInstructions { get; set; }

        /// <summary>
        /// 图表标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 图表描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 图表版本号
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 是否为收藏的图表
        /// </summary>
        public bool IsFavorite { get; set; } = false;

        /// <summary>
        /// 图表标签
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 更新图表信息
        /// </summary>
        /// <param name="mermaidCode">新的 Mermaid 代码</param>
        /// <param name="explanation">新的说明</param>
        /// <param name="componentMapping">新的组件映射</param>
        public void Update(string mermaidCode, string explanation, string? componentMapping = null)
        {
            MermaidCode = mermaidCode;
            Explanation = explanation;
            if (!string.IsNullOrEmpty(componentMapping))
            {
                ComponentMapping = componentMapping;
            }
            UpdatedAt = DateTime.UtcNow;
            Version++;
        }

        /// <summary>
        /// 获取仓库名称（从 URL 中提取）
        /// </summary>
        /// <returns>仓库名称</returns>
        public string GetRepositoryName()
        {
            if (string.IsNullOrEmpty(RepositoryUrl))
                return "Unknown";

            try
            {
                var uri = new Uri(RepositoryUrl);
                var segments = uri.AbsolutePath.Trim('/').Split('/');
                return segments.Length >= 2 ? $"{segments[0]}/{segments[1]}" : "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}