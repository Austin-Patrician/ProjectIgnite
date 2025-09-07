using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.Services;

/// <summary>
/// 内容摘要器实现
/// </summary>
public class ContentSummarizer : IContentSummarizer
{
    private const int AVERAGE_TOKENS_PER_CHAR = 4; // 粗略估算：1个token约等于4个字符
    private const int MAX_SNIPPET_LENGTH = 2000; // 单个代码片段最大字符数
    private const int MAX_SUMMARY_LENGTH = 1500; // 单个摘要部分最大字符数

    public async Task<ProjectContentSummary> BuildSummaryAsync(
        ProjectScanResult scanResult, 
        int tokenBudget, 
        CancellationToken cancellationToken = default)
    {
        var summary = new ProjectContentSummary();

        try
        {
            // 1. 构建基本信息
            summary.BasicInfo = BuildBasicInfo(scanResult);

            // 2. 构建各部分摘要
            summary.FileTreeSummary = BuildFileTreeSummary(scanResult.FileTreeSummary);
            summary.DependenciesSummary = BuildDependenciesSummary(scanResult.Dependencies);
            summary.EnvironmentSummary = BuildEnvironmentSummary(scanResult.Environments);
            summary.RunInfoSummary = BuildRunInfoSummary(scanResult.RunInfo);
            summary.StructureSummary = BuildStructureSummary(scanResult.StructureGraph);

            // 3. 提取关键代码片段
            summary.KeySnippets = await ExtractKeySnippetsAsync(scanResult, 10, 50, cancellationToken);

            // 4. 根据 Token 预算调整内容
            summary = OptimizeForTokenBudget(summary, tokenBudget);

            // 5. 计算最终 Token 估算
            summary.EstimatedTokens = CalculateTotalTokens(summary);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"构建项目摘要时发生错误: {ex.Message}");
        }

        return summary;
    }

    public async Task<List<CodeSnippet>> ExtractKeySnippetsAsync(
        ProjectScanResult scanResult,
        int maxSnippets = 10,
        int maxLinesPerSnippet = 50,
        CancellationToken cancellationToken = default)
    {
        var snippets = new List<CodeSnippet>();

        try
        {
            // 从语言特定结果中提取片段
            foreach (var language in scanResult.LanguagesDetected)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // 这里应该调用对应的语言分析器获取片段
                // 为了简化，我们从文件树中提取关键文件的片段
                var keyFiles = scanResult.FileTreeSummary
                    .Where(f => f.IsKey && f.Type == FileNodeType.File)
                    .Take(maxSnippets / 2)
                    .ToList();

                foreach (var keyFile in keyFiles)
                {
                    if (snippets.Count >= maxSnippets) break;
                    if (cancellationToken.IsCancellationRequested) break;

                    var snippet = await ExtractFileSnippetAsync(keyFile.Path, maxLinesPerSnippet, cancellationToken);
                    if (snippet != null)
                    {
                        snippets.Add(snippet);
                    }
                }
            }

            // 按重要性排序
            snippets = snippets
                .OrderByDescending(s => s.Importance)
                .Take(maxSnippets)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取关键代码片段时发生错误: {ex.Message}");
        }

        return snippets;
    }

    public int EstimateTokenCount(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        // 简单的 Token 估算：基于字符数和单词数
        var charCount = content.Length;
        var wordCount = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        // 使用更精确的估算公式
        var estimatedTokens = Math.Max(charCount / AVERAGE_TOKENS_PER_CHAR, wordCount * 1.3);
        
        return (int)Math.Ceiling(estimatedTokens);
    }

    public (string truncatedContent, string truncationNote) TruncateContent(string content, int maxTokens)
    {
        if (string.IsNullOrEmpty(content))
            return (content, "");

        var estimatedTokens = EstimateTokenCount(content);
        if (estimatedTokens <= maxTokens)
            return (content, "");

        // 计算需要保留的字符数
        var maxChars = maxTokens * AVERAGE_TOKENS_PER_CHAR;
        
        if (content.Length <= maxChars)
            return (content, "");

        // 尝试在合适的位置截断（句号、换行符等）
        var truncateAt = maxChars - 100; // 留一些缓冲
        var lines = content.Split('\n');
        var truncatedLines = new List<string>();
        var currentLength = 0;

        foreach (var line in lines)
        {
            if (currentLength + line.Length > truncateAt)
                break;
            
            truncatedLines.Add(line);
            currentLength += line.Length + 1; // +1 for newline
        }

        var truncatedContent = string.Join("\n", truncatedLines);
        if (truncatedContent.Length < content.Length)
        {
            truncatedContent += "\n\n[内容已截断...]";
        }

        var note = $"内容从 {content.Length} 字符截断到 {truncatedContent.Length} 字符";
        return (truncatedContent, note);
    }

    #region 私有方法

    private ProjectBasicInfo BuildBasicInfo(ProjectScanResult scanResult)
    {
        var basicInfo = new ProjectBasicInfo
        {
            Languages = scanResult.LanguagesDetected.ToList(),
            FileStats = new FileStatistics
            {
                TotalFiles = scanResult.TotalFilesScanned,
                TotalDirectories = scanResult.TotalDirectoriesScanned,
                KeyFilesCount = scanResult.FileTreeSummary.Count(f => f.IsKey)
            }
        };

        // 推断项目名称和类型
        var rootPath = scanResult.FileTreeSummary.FirstOrDefault()?.Path;
        if (!string.IsNullOrEmpty(rootPath))
        {
            basicInfo.Name = Path.GetFileName(Path.GetDirectoryName(rootPath) ?? "Unknown");
        }

        // 根据语言推断项目类型
        if (scanResult.LanguagesDetected.Contains("csharp"))
        {
            basicInfo.Type = "C# Project";
            basicInfo.Framework = InferCSharpFramework(scanResult);
        }
        else if (scanResult.LanguagesDetected.Contains("node"))
        {
            basicInfo.Type = "Node.js Project";
            basicInfo.Framework = InferNodeFramework(scanResult);
        }
        else
        {
            basicInfo.Type = "Multi-language Project";
            basicInfo.Framework = "Mixed";
        }

        // 按语言统计文件数
        foreach (var lang in scanResult.LanguagesDetected)
        {
            var count = scanResult.FileTreeSummary.Count(f => f.Language == lang);
            if (count > 0)
            {
                basicInfo.FileStats.FilesByLanguage[lang] = count;
            }
        }

        return basicInfo;
    }

    private string InferCSharpFramework(ProjectScanResult scanResult)
    {
        // 从环境信息推断框架
        if (!string.IsNullOrEmpty(scanResult.Environments.DotNetSdk))
        {
            if (scanResult.Environments.DotNetSdk.Contains("Web"))
                return "ASP.NET Core";
            if (scanResult.Environments.DotNetSdk.Contains("WindowsDesktop"))
                return "WPF/WinForms";
            return ".NET";
        }

        // 从依赖推断
        var webPackages = new[] { "Microsoft.AspNetCore", "Microsoft.AspNetCore.App" };
        if (scanResult.Dependencies.CSharp.Any(p => webPackages.Any(wp => p.Name.Contains(wp))))
        {
            return "ASP.NET Core";
        }

        return ".NET";
    }

    private string InferNodeFramework(ProjectScanResult scanResult)
    {
        var frameworks = new Dictionary<string, string[]>
        {
            { "Next.js", new[] { "next" } },
            { "React", new[] { "react" } },
            { "Vue.js", new[] { "vue" } },
            { "Express", new[] { "express" } },
            { "NestJS", new[] { "@nestjs/core" } }
        };

        foreach (var framework in frameworks)
        {
            if (scanResult.Dependencies.Node.Any(p => framework.Value.Any(dep => p.Name.Contains(dep))))
            {
                return framework.Key;
            }
        }

        return "Node.js";
    }

    private string BuildFileTreeSummary(List<FileNodeSummary> fileTree)
    {
        if (!fileTree.Any())
            return "文件树信息不可用";

        var summary = new StringBuilder();
        summary.AppendLine("项目文件结构：");

        // 只显示关键目录和文件
        var keyItems = fileTree
            .Where(f => f.IsKey || f.Type == FileNodeType.Directory)
            .Take(20)
            .ToList();

        foreach (var item in keyItems)
        {
            var indent = GetIndentLevel(item.Path) * 2;
            var prefix = new string(' ', indent);
            var name = Path.GetFileName(item.Path);
            var typeIndicator = item.Type == FileNodeType.Directory ? "/" : "";
            var keyIndicator = item.IsKey ? " [关键]" : "";
            
            summary.AppendLine($"{prefix}- {name}{typeIndicator}{keyIndicator}");
        }

        var result = summary.ToString();
        if (result.Length > MAX_SUMMARY_LENGTH)
        {
            result = result.Substring(0, MAX_SUMMARY_LENGTH) + "\n[文件树已截断...]";
        }

        return result;
    }

    private int GetIndentLevel(string path)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length - 1;
    }

    private string BuildDependenciesSummary(DependenciesResult dependencies)
    {
        var summary = new StringBuilder();
        
        if (dependencies.CSharp.Any())
        {
            summary.AppendLine($"C# 依赖 ({dependencies.CSharp.Count} 个):");
            var topCSharpDeps = dependencies.CSharp
                .OrderByDescending(p => p.RiskScore)
                .Take(10)
                .ToList();
            
            foreach (var dep in topCSharpDeps)
            {
                var devIndicator = dep.DevDependency ? " [开发]" : "";
                var riskIndicator = dep.RiskScore > 50 ? " [高风险]" : "";
                summary.AppendLine($"  - {dep.Name} ({dep.Version}){devIndicator}{riskIndicator}");
            }
        }

        if (dependencies.Node.Any())
        {
            summary.AppendLine($"\nNode.js 依赖 ({dependencies.Node.Count} 个):");
            var topNodeDeps = dependencies.Node
                .OrderByDescending(p => p.RiskScore)
                .Take(10)
                .ToList();
            
            foreach (var dep in topNodeDeps)
            {
                var devIndicator = dep.DevDependency ? " [开发]" : "";
                var riskIndicator = dep.RiskScore > 50 ? " [高风险]" : "";
                summary.AppendLine($"  - {dep.Name} ({dep.Version}){devIndicator}{riskIndicator}");
            }
        }

        if (!dependencies.CSharp.Any() && !dependencies.Node.Any())
        {
            summary.AppendLine("未检测到依赖信息");
        }

        var result = summary.ToString();
        if (result.Length > MAX_SUMMARY_LENGTH)
        {
            result = result.Substring(0, MAX_SUMMARY_LENGTH) + "\n[依赖列表已截断...]";
        }

        return result;
    }

    private string BuildEnvironmentSummary(EnvironmentInfo environment)
    {
        var summary = new StringBuilder();
        summary.AppendLine("环境配置：");

        if (!string.IsNullOrEmpty(environment.DotNetSdk))
        {
            summary.AppendLine($"  - .NET SDK: {environment.DotNetSdk}");
        }

        if (environment.TargetFrameworks.Any())
        {
            summary.AppendLine($"  - 目标框架: {string.Join(", ", environment.TargetFrameworks)}");
        }

        if (!string.IsNullOrEmpty(environment.NodeVersion))
        {
            summary.AppendLine($"  - Node.js 版本: {environment.NodeVersion}");
        }

        if (environment.PackageManager.HasValue)
        {
            summary.AppendLine($"  - 包管理器: {environment.PackageManager}");
        }

        if (!string.IsNullOrEmpty(environment.LanguageVersion))
        {
            summary.AppendLine($"  - 语言版本: {environment.LanguageVersion}");
        }

        if (environment.NullableEnabled.HasValue)
        {
            summary.AppendLine($"  - 可空引用类型: {(environment.NullableEnabled.Value ? "启用" : "禁用")}");
        }

        var result = summary.ToString();
        if (result.Trim() == "环境配置：")
        {
            result = "环境配置信息不可用";
        }

        return result;
    }

    private string BuildRunInfoSummary(RunInfo runInfo)
    {
        var summary = new StringBuilder();
        summary.AppendLine("运行配置：");

        if (runInfo.Ports.Any())
        {
            summary.AppendLine("  端口候选：");
            var topPorts = runInfo.Ports
                .OrderByDescending(p => p.Confidence)
                .Take(5)
                .ToList();
            
            foreach (var port in topPorts)
            {
                var confidencePercent = (int)(port.Confidence * 100);
                summary.AppendLine($"    - {port.Value} (置信度: {confidencePercent}%, 来源: {port.Source})");
            }
        }

        if (runInfo.Urls.Any())
        {
            summary.AppendLine("  URL 候选：");
            var topUrls = runInfo.Urls
                .OrderByDescending(u => u.Confidence)
                .Take(5)
                .ToList();
            
            foreach (var url in topUrls)
            {
                var confidencePercent = (int)(url.Confidence * 100);
                summary.AppendLine($"    - {url.Value} (置信度: {confidencePercent}%)");
            }
        }

        if (runInfo.StartCommands.Any())
        {
            summary.AppendLine($"  启动命令 ({runInfo.StartCommands.Count} 个):");
            foreach (var cmd in runInfo.StartCommands.Take(3))
            {
                summary.AppendLine($"    - {cmd}");
            }
        }

        if (runInfo.EnvironmentFiles.Any())
        {
            summary.AppendLine($"  环境变量文件: {runInfo.EnvironmentFiles.Count} 个");
        }

        var result = summary.ToString();
        if (result.Trim() == "运行配置：")
        {
            result = "运行配置信息不可用";
        }

        return result;
    }

    private string BuildStructureSummary(StructureGraph structure)
    {
        var summary = new StringBuilder();
        summary.AppendLine("项目结构：");

        if (structure.Nodes.Any())
        {
            var nodesByType = structure.Nodes
                .GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var nodeType in nodesByType)
            {
                summary.AppendLine($"  - {nodeType.Key}: {nodeType.Value} 个");
            }

            summary.AppendLine($"  - 总连接数: {structure.Edges.Count}");

            // 显示主要节点
            var importantNodes = structure.Nodes
                .Where(n => n.RiskScore.HasValue && n.RiskScore > 30)
                .OrderByDescending(n => n.RiskScore)
                .Take(5)
                .ToList();

            if (importantNodes.Any())
            {
                summary.AppendLine("  重要节点：");
                foreach (var node in importantNodes)
                {
                    summary.AppendLine($"    - {node.Name} ({node.Type}, 风险: {node.RiskScore:F0})");
                }
            }
        }
        else
        {
            summary.AppendLine("  结构图信息不可用");
        }

        return summary.ToString();
    }

    private async Task<CodeSnippet?> ExtractFileSnippetAsync(string filePath, int maxLines, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var lines = content.Split('\n');
            
            // 限制行数
            var snippetLines = lines.Take(maxLines).ToArray();
            var snippetContent = string.Join("\n", snippetLines);
            
            // 限制字符数
            if (snippetContent.Length > MAX_SNIPPET_LENGTH)
            {
                snippetContent = snippetContent.Substring(0, MAX_SNIPPET_LENGTH) + "\n[代码已截断...]";
            }

            return new CodeSnippet
            {
                FilePath = filePath,
                StartLine = 1,
                EndLine = Math.Min(lines.Length, maxLines),
                Content = snippetContent,
                Type = DetermineSnippetType(filePath),
                Importance = CalculateSnippetImportance(filePath, content),
                Context = $"文件: {Path.GetFileName(filePath)}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取文件片段时发生错误 {filePath}: {ex.Message}");
            return null;
        }
    }

    private string DetermineSnippetType(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLower();
        var extension = Path.GetExtension(filePath).ToLower();

        if (fileName.Contains("config") || extension == ".json" || extension == ".xml")
            return "Configuration";
        if (fileName.Contains("test") || fileName.Contains("spec"))
            return "Test";
        if (extension == ".cs" || extension == ".js" || extension == ".ts")
            return "Source Code";
        if (fileName == "readme.md" || fileName == "package.json" || fileName.EndsWith(".csproj"))
            return "Project File";
        
        return "Other";
    }

    private double CalculateSnippetImportance(string filePath, string content)
    {
        var importance = 0.5; // 基础重要性
        var fileName = Path.GetFileName(filePath).ToLower();

        // 根据文件名调整重要性
        if (fileName.Contains("main") || fileName.Contains("index") || fileName.Contains("app"))
            importance += 0.3;
        if (fileName.Contains("config") || fileName.Contains("setting"))
            importance += 0.2;
        if (fileName.EndsWith(".csproj") || fileName == "package.json")
            importance += 0.4;
        if (fileName.Contains("startup") || fileName.Contains("program"))
            importance += 0.3;

        // 根据内容调整重要性
        if (content.Contains("class") || content.Contains("function") || content.Contains("export"))
            importance += 0.1;
        if (content.Contains("async") || content.Contains("await"))
            importance += 0.1;

        return Math.Min(importance, 1.0);
    }

    private ProjectContentSummary OptimizeForTokenBudget(ProjectContentSummary summary, int tokenBudget)
    {
        var currentTokens = CalculateTotalTokens(summary);
        
        if (currentTokens <= tokenBudget)
            return summary;

        // 按优先级截断内容
        var budgetPerSection = tokenBudget / 6; // 6个主要部分

        // 1. 截断文件树摘要
        if (EstimateTokenCount(summary.FileTreeSummary) > budgetPerSection)
        {
            var (truncated, _) = TruncateContent(summary.FileTreeSummary, budgetPerSection);
            summary.FileTreeSummary = truncated;
        }

        // 2. 截断依赖摘要
        if (EstimateTokenCount(summary.DependenciesSummary) > budgetPerSection)
        {
            var (truncated, _) = TruncateContent(summary.DependenciesSummary, budgetPerSection);
            summary.DependenciesSummary = truncated;
        }

        // 3. 减少代码片段数量
        var snippetBudget = budgetPerSection * 2; // 给代码片段更多预算
        var snippetTokens = summary.KeySnippets.Sum(s => EstimateTokenCount(s.Content));
        
        if (snippetTokens > snippetBudget)
        {
            var targetSnippets = (int)Math.Ceiling((double)snippetBudget / (snippetTokens / summary.KeySnippets.Count));
            summary.KeySnippets = summary.KeySnippets
                .OrderByDescending(s => s.Importance)
                .Take(Math.Max(targetSnippets, 3)) // 至少保留3个
                .ToList();
        }

        return summary;
    }

    private int CalculateTotalTokens(ProjectContentSummary summary)
    {
        var total = 0;
        total += EstimateTokenCount(summary.FileTreeSummary);
        total += EstimateTokenCount(summary.DependenciesSummary);
        total += EstimateTokenCount(summary.EnvironmentSummary);
        total += EstimateTokenCount(summary.RunInfoSummary);
        total += EstimateTokenCount(summary.StructureSummary);
        total += summary.KeySnippets.Sum(s => EstimateTokenCount(s.Content));
        
        return total;
    }

    #endregion
}