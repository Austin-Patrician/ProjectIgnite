using System;
using System.Collections.Generic;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 项目分析结果
    /// </summary>
    public class ProjectAnalysisResult
    {
        /// <summary>
        /// 项目路径
        /// </summary>
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型
        /// </summary>
        public ProjectType ProjectType { get; set; }

        /// <summary>
        /// 主要编程语言
        /// </summary>
        public string PrimaryLanguage { get; set; } = string.Empty;

        /// <summary>
        /// 文件系统结构
        /// </summary>
        public FileSystemStructure FileStructure { get; set; } = new();

        /// <summary>
        /// 项目依赖信息
        /// </summary>
        public ProjectDependencies Dependencies { get; set; } = new();

        /// <summary>
        /// 配置文件信息
        /// </summary>
        public List<ConfigurationFile> ConfigurationFiles { get; set; } = new();

        /// <summary>
        /// 分析时间
        /// </summary>
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 自定义指令
        /// </summary>
        public string? CustomInstructions { get; set; }
    }

    /// <summary>
    /// 项目类型枚举
    /// </summary>
    public enum ProjectType
    {
        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown,

        /// <summary>
        /// .NET 项目
        /// </summary>
        DotNet,

        /// <summary>
        /// Node.js 项目
        /// </summary>
        NodeJs,

        /// <summary>
        /// Python 项目
        /// </summary>
        Python,

        /// <summary>
        /// Java 项目
        /// </summary>
        Java,

        /// <summary>
        /// React 项目
        /// </summary>
        React,

        /// <summary>
        /// Vue.js 项目
        /// </summary>
        Vue,

        /// <summary>
        /// Angular 项目
        /// </summary>
        Angular,

        /// <summary>
        /// Rust 项目
        /// </summary>
        Rust,

        /// <summary>
        /// Go 项目
        /// </summary>
        Go,

        /// <summary>
        /// PHP 项目
        /// </summary>
        Php,

        /// <summary>
        /// Ruby 项目
        /// </summary>
        Ruby
    }

    /// <summary>
    /// 文件系统结构
    /// </summary>
    public class FileSystemStructure
    {
        /// <summary>
        /// 根目录
        /// </summary>
        public DirectoryNode RootDirectory { get; set; } = new();

        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 总目录数
        /// </summary>
        public int TotalDirectories { get; set; }

        /// <summary>
        /// 文件类型统计
        /// </summary>
        public Dictionary<string, int> FileTypeCount { get; set; } = new();

        /// <summary>
        /// 转换为树形字符串表示
        /// </summary>
        public string ToTreeString()
        {
            return RootDirectory.ToTreeString();
        }
    }

    /// <summary>
    /// 目录节点
    /// </summary>
    public class DirectoryNode
    {
        /// <summary>
        /// 目录名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 相对路径
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// 子目录
        /// </summary>
        public List<DirectoryNode> SubDirectories { get; set; } = new();

        /// <summary>
        /// 文件列表
        /// </summary>
        public List<FileNode> Files { get; set; } = new();

        /// <summary>
        /// 转换为树形字符串
        /// </summary>
        public string ToTreeString(string indent = "")
        {
            var result = new System.Text.StringBuilder();
            
            if (!string.IsNullOrEmpty(Name))
            {
                result.AppendLine($"{indent}📁 {Name}/");
                indent += "  ";
            }

            // 添加子目录
            foreach (var dir in SubDirectories)
            {
                result.Append(dir.ToTreeString(indent));
            }

            // 添加文件
            foreach (var file in Files)
            {
                result.AppendLine($"{indent}📄 {file.Name}");
            }

            return result.ToString();
        }
    }

    /// <summary>
    /// 文件节点
    /// </summary>
    public class FileNode
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 相对路径
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 是否为重要文件（如配置文件、入口文件等）
        /// </summary>
        public bool IsImportant { get; set; }
    }

    /// <summary>
    /// 项目依赖信息
    /// </summary>
    public class ProjectDependencies
    {
        /// <summary>
        /// 包管理器类型
        /// </summary>
        public PackageManagerType PackageManager { get; set; }

        /// <summary>
        /// 依赖包列表
        /// </summary>
        public List<DependencyPackage> Packages { get; set; } = new();

        /// <summary>
        /// 开发依赖包列表
        /// </summary>
        public List<DependencyPackage> DevPackages { get; set; } = new();
    }

    /// <summary>
    /// 包管理器类型
    /// </summary>
    public enum PackageManagerType
    {
        None,
        NuGet,
        Npm,
        Pip,
        Maven,
        Gradle,
        Cargo,
        Go,
        Composer,
        Gem
    }

    /// <summary>
    /// 依赖包信息
    /// </summary>
    public class DependencyPackage
    {
        /// <summary>
        /// 包名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 配置文件信息
    /// </summary>
    public class ConfigurationFile
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 配置类型
        /// </summary>
        public ConfigurationType Type { get; set; }

        /// <summary>
        /// 内容摘要
        /// </summary>
        public string? ContentSummary { get; set; }
    }

    /// <summary>
    /// 配置文件类型
    /// </summary>
    public enum ConfigurationType
    {
        ProjectFile,      // 项目文件 (.csproj, package.json, etc.)
        BuildConfiguration, // 构建配置 (webpack.config.js, etc.)
        EnvironmentConfig,  // 环境配置 (.env, appsettings.json, etc.)
        DependencyConfig,   // 依赖配置 (requirements.txt, etc.)
        Other
    }
}
