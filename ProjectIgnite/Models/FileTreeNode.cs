using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 文件树节点类型
    /// </summary>
    public enum FileTreeNodeType
    {
        /// <summary>
        /// 文件
        /// </summary>
        File,

        /// <summary>
        /// 目录
        /// </summary>
        Directory
    }

    /// <summary>
    /// 文件树节点模型
    /// 表示 GitHub 仓库中的文件或目录结构
    /// </summary>
    public class FileTreeNode
    {
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点完整路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 节点类型
        /// </summary>
        public FileTreeNodeType Type { get; set; }

        /// <summary>
        /// 文件大小（字节，仅对文件有效）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// SHA 哈希值
        /// </summary>
        public string Sha { get; set; } = string.Empty;

        /// <summary>
        /// 下载 URL（仅对文件有效）
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<FileTreeNode> Children { get; set; } = new();

        /// <summary>
        /// 父节点引用
        /// </summary>
        [JsonIgnore]
        public FileTreeNode? Parent { get; set; }

        /// <summary>
        /// 节点深度（根节点为 0）
        /// </summary>
        [JsonIgnore]
        public int Depth => Parent?.Depth + 1 ?? 0;

        /// <summary>
        /// 是否为文件
        /// </summary>
        [JsonIgnore]
        public bool IsFile => Type == FileTreeNodeType.File;

        /// <summary>
        /// 是否为目录
        /// </summary>
        [JsonIgnore]
        public bool IsDirectory => Type == FileTreeNodeType.Directory;

        /// <summary>
        /// 是否为根节点
        /// </summary>
        [JsonIgnore]
        public bool IsRoot => Parent == null;

        /// <summary>
        /// 是否为叶子节点
        /// </summary>
        [JsonIgnore]
        public bool IsLeaf => !Children.Any();

        /// <summary>
        /// 文件扩展名（仅对文件有效）
        /// </summary>
        [JsonIgnore]
        public string Extension
        {
            get
            {
                if (!IsFile) return string.Empty;
                var lastDot = Name.LastIndexOf('.');
                return lastDot >= 0 ? Name.Substring(lastDot) : string.Empty;
            }
        }

        /// <summary>
        /// 获取所有子文件数量
        /// </summary>
        /// <returns>子文件数量</returns>
        public int GetFileCount()
        {
            var count = IsFile ? 1 : 0;
            return count + Children.Sum(child => child.GetFileCount());
        }

        /// <summary>
        /// 获取所有子目录数量
        /// </summary>
        /// <returns>子目录数量</returns>
        public int GetDirectoryCount()
        {
            var count = IsDirectory ? 1 : 0;
            return count + Children.Sum(child => child.GetDirectoryCount());
        }

        /// <summary>
        /// 获取总大小（包括所有子节点）
        /// </summary>
        /// <returns>总大小（字节）</returns>
        public long GetTotalSize()
        {
            return Size + Children.Sum(child => child.GetTotalSize());
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="child">子节点</param>
        public void AddChild(FileTreeNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        /// <param name="child">要移除的子节点</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveChild(FileTreeNode child)
        {
            if (Children.Remove(child))
            {
                child.Parent = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 根据路径查找节点
        /// </summary>
        /// <param name="path">节点路径</param>
        /// <returns>找到的节点，如果不存在则返回 null</returns>
        public FileTreeNode? FindByPath(string path)
        {
            if (Path == path) return this;

            foreach (var child in Children)
            {
                var result = child.FindByPath(path);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// 获取所有文件节点
        /// </summary>
        /// <param name="extension">文件扩展名过滤（可选）</param>
        /// <returns>文件节点列表</returns>
        public List<FileTreeNode> GetAllFiles(string? extension = null)
        {
            var files = new List<FileTreeNode>();

            if (IsFile)
            {
                if (extension == null || Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(this);
                }
            }

            foreach (var child in Children)
            {
                files.AddRange(child.GetAllFiles(extension));
            }

            return files;
        }

        /// <summary>
        /// 获取所有目录节点
        /// </summary>
        /// <returns>目录节点列表</returns>
        public List<FileTreeNode> GetAllDirectories()
        {
            var directories = new List<FileTreeNode>();

            if (IsDirectory)
            {
                directories.Add(this);
            }

            foreach (var child in Children)
            {
                directories.AddRange(child.GetAllDirectories());
            }

            return directories;
        }

        /// <summary>
        /// 转换为树形字符串表示
        /// </summary>
        /// <param name="indent">缩进字符串</param>
        /// <param name="isLast">是否为最后一个节点</param>
        /// <returns>树形字符串</returns>
        public string ToTreeString(string indent = "", bool isLast = true)
        {
            var result = indent;
            result += isLast ? "└── " : "├── ";
            result += Name;
            
            if (IsFile && Size > 0)
            {
                result += $" ({FormatFileSize(Size)})";
            }
            
            result += Environment.NewLine;

            var childIndent = indent + (isLast ? "    " : "│   ");
            for (int i = 0; i < Children.Count; i++)
            {
                result += Children[i].ToTreeString(childIndent, i == Children.Count - 1);
            }

            return result;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的文件大小</returns>
        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1}{suffixes[counter]}";
        }

        /// <summary>
        /// 重写 ToString 方法
        /// </summary>
        /// <returns>节点的字符串表示</returns>
        public override string ToString()
        {
            return $"{(IsDirectory ? "📁" : "📄")} {Name} ({Path})";
        }
    }
}