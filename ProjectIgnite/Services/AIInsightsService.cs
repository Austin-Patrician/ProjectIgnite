using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.Services;

/// <summary>
/// AI 洞察服务实现
/// </summary>
public class AIInsightsService : IAIInsightsService
{
    private readonly IChatClient _chatClient;
    private readonly IContentSummarizer _contentSummarizer;
    private readonly string _defaultModel;

    public AIInsightsService(
        IChatClient chatClient,
        IContentSummarizer contentSummarizer,
        string defaultModel = "gpt-4")
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _contentSummarizer = contentSummarizer ?? throw new ArgumentNullException(nameof(contentSummarizer));
        _defaultModel = defaultModel;
    }

    public async Task<AIInsights> GenerateInsightsAsync(
        ProjectScanResult scanResult, 
        int tokenBudget = 15000, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 构建项目摘要
            var summary = await _contentSummarizer.BuildSummaryAsync(scanResult, tokenBudget, cancellationToken);

            // 2. 构建 AI 提示词
            var prompt = BuildAnalysisPrompt(summary);

            // 3. 调用 AI 服务
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetSystemPrompt()),
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = Math.Min(tokenBudget / 2, 4000),
                    Temperature = 0.3f,
                    ResponseFormat = ChatResponseFormat.Json
                },
                cancellationToken);

            // 4. 解析响应
            var insights = ParseAIResponse(response.Text ?? "{}");
            
            // 5. 设置元数据
            insights.GeneratedAt = DateTimeOffset.Now;
            insights.ConfidenceScores.DataCompleteness = CalculateDataCompleteness(scanResult);
            insights.ConfidenceScores.AnalysisDepth = CalculateAnalysisDepth(summary);

            return insights;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成 AI 洞察时发生错误: {ex.Message}");
            return CreateFallbackInsights(scanResult, ex.Message);
        }
    }

    public async Task<AIContentPreview> DryRunPreviewAsync(
        ProjectScanResult scanResult, 
        int tokenBudget = 15000)
    {
        try
        {
            var summary = await _contentSummarizer.BuildSummaryAsync(scanResult, tokenBudget);
            var prompt = BuildAnalysisPrompt(summary);
            
            var estimatedTokens = _contentSummarizer.EstimateTokenCount(GetSystemPrompt() + prompt);
            
            return new AIContentPreview
            {
                SummaryContent = BuildPreviewContent(summary),
                KeySnippets = summary.KeySnippets,
                EstimatedTokens = estimatedTokens,
                BudgetUsageRatio = (double)estimatedTokens / tokenBudget,
                TruncationNotes = GetTruncationNotes(summary),
                DataCompletenessScore = CalculateDataCompleteness(scanResult)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成预览时发生错误: {ex.Message}");
            return new AIContentPreview
            {
                SummaryContent = $"预览生成失败: {ex.Message}",
                EstimatedTokens = 0,
                BudgetUsageRatio = 0,
                DataCompletenessScore = 0
            };
        }
    }

    public async Task<AIServiceStatus> CheckServiceStatusAsync()
    {
        try
        {
            // 发送简单的测试请求
            var testMessages = new List<ChatMessage>
            {
                new(ChatRole.User, "Hello, please respond with 'OK' if you can understand this message.")
            };
            
            var response = await _chatClient.GetResponseAsync(
                testMessages,
                new ChatOptions
                {
                    MaxOutputTokens = 10,
                    Temperature = 0
                });

            var isWorking = !string.IsNullOrEmpty(response.Text);

            return new AIServiceStatus
            {
                IsAvailable = isWorking,
                CurrentModel = _defaultModel,
                StatusMessage = isWorking ? "服务正常" : "服务响应异常",
                LastChecked = DateTimeOffset.Now
            };
        }
        catch (Exception ex)
        {
            return new AIServiceStatus
            {
                IsAvailable = false,
                CurrentModel = _defaultModel,
                StatusMessage = "服务不可用",
                ErrorMessage = ex.Message,
                LastChecked = DateTimeOffset.Now
            };
        }
    }

    public async Task<string[]> GetAvailableModelsAsync()
    {
        try
        {
            // 这里返回常见的模型列表，实际实现可能需要调用具体的 API
            return new[] 
            {
                "gpt-4",
                "gpt-4-turbo",
                "gpt-3.5-turbo",
                "claude-3-sonnet",
                "claude-3-haiku"
            };
        }
        catch
        {
            return new[] { _defaultModel };
        }
    }

    #region 私有方法

    private string GetSystemPrompt()
    {
        return @"你是一位资深的软件架构师和全栈工程师。请分析提供的项目结构化摘要和关键代码片段，生成详细的项目洞察。

分析要求：
1. 仅基于提供的数据进行推断，不要虚构不存在的信息
2. 对每个建议都要标注置信度和来源
3. 重点关注架构模式、潜在风险和改进建议
4. 输出必须是有效的 JSON 格式，严格遵循指定的 schema

输出 JSON Schema:
{
  ""highLevelArchitecture"": ""string (最多1000字符)"",
  ""modules"": [
    {
      ""name"": ""string"",
      ""role"": ""string"",
      ""keyFiles"": [""string""],
      ""suggestions"": [""string""],
      ""complexityScore"": ""number (0-100)""
    }
  ],
  ""runRecommendations"": {
    ""entrypoints"": [""string""],
    ""portsToUse"": [""number""],
    ""urlsToTry"": [""string""],
    ""notes"": [""string""],
    ""environmentVariables"": [""string""]
  },
  ""dependencyAdvice"": [
    {
      ""package"": ""string"",
      ""current"": ""string"",
      ""suggested"": ""string"",
      ""reason"": ""string"",
      ""type"": ""Upgrade|Security|Performance|Compatibility|Remove""
    }
  ],
  ""potentialRisks"": [
    {
      ""title"": ""string"",
      ""description"": ""string"",
      ""severity"": ""Low|Medium|High|Critical"",
      ""confidence"": ""number (0-1)"",
      ""affectedItems"": [""string""],
      ""recommendations"": [""string""]
    }
  ],
  ""confidenceScores"": {
    ""overall"": ""number (0-1)"",
    ""reasoning"": ""string"",
    ""dataCompleteness"": ""number (0-1)"",
    ""analysisDepth"": ""number (0-1)""
  }
}

约束条件：
- modules 数组最多 10 个元素
- dependencyAdvice 数组最多 15 个元素
- potentialRisks 数组最多 10 个元素
- 所有字符串字段都有长度限制，请保持简洁
- 必须返回完整的 JSON，不能缺少任何必需字段";
    }

    private string BuildAnalysisPrompt(ProjectContentSummary summary)
    {
        var prompt = $@"
## 项目分析请求

### 基本信息
- 项目名称: {summary.BasicInfo.Name}
- 项目类型: {summary.BasicInfo.Type}
- 主要语言: {string.Join(", ", summary.BasicInfo.Languages)}
- 框架: {summary.BasicInfo.Framework}
- 文件统计: {summary.BasicInfo.FileStats.TotalFiles} 个文件，{summary.BasicInfo.FileStats.TotalDirectories} 个目录

### 文件结构
{summary.FileTreeSummary}

### 依赖分析
{summary.DependenciesSummary}

### 环境配置
{summary.EnvironmentSummary}

### 运行信息
{summary.RunInfoSummary}

### 架构结构
{summary.StructureSummary}
";

        // 添加关键代码片段
        if (summary.KeySnippets.Any())
        {
            prompt += "\n### 关键代码片段\n";
            foreach (var snippet in summary.KeySnippets.Take(8))
            {
                prompt += $@"
#### {snippet.Type}: {System.IO.Path.GetFileName(snippet.FilePath)} (行 {snippet.StartLine}-{snippet.EndLine})
```
{snippet.Content}
```
";
            }
        }

        prompt += "\n\n请基于以上信息生成详细的项目洞察分析，输出格式必须是有效的 JSON。";

        return prompt;
    }

    private AIInsights ParseAIResponse(string responseText)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            // 允许使用字符串表示的枚举值，例如 "Compatibility"、"High" 等
            options.Converters.Add(new JsonStringEnumConverter());

            var insights = JsonSerializer.Deserialize<AIInsights>(responseText, options);
            return insights ?? new AIInsights();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"解析 AI 响应 JSON 时发生错误: {ex.Message}");
            
            // 尝试提取部分信息
            return new AIInsights
            {
                HighLevelArchitecture = "AI 响应解析失败，请检查服务配置",
                ConfidenceScores = new ConfidenceScore
                {
                    Overall = 0.1,
                    Reasoning = $"JSON 解析错误: {ex.Message}"
                }
            };
        }
    }

    private AIInsights CreateFallbackInsights(ProjectScanResult scanResult, string errorMessage)
    {
        var insights = new AIInsights
        {
            HighLevelArchitecture = $"项目包含 {scanResult.LanguagesDetected.Count} 种语言：{string.Join(", ", scanResult.LanguagesDetected)}。" +
                                  $"共扫描 {scanResult.TotalFilesScanned} 个文件。由于 AI 服务不可用，无法提供详细分析。",
            
            RunRecommendations = new RunRecommendation(),
            
            ConfidenceScores = new ConfidenceScore
            {
                Overall = 0.2,
                Reasoning = $"AI 服务错误，使用基础分析: {errorMessage}",
                DataCompleteness = CalculateDataCompleteness(scanResult),
                AnalysisDepth = 0.1
            }
        };

        // 添加基本的运行信息
        if (scanResult.RunInfo.Ports.Any())
        {
            insights.RunRecommendations.PortsToUse = scanResult.RunInfo.Ports
                .OrderByDescending(p => p.Confidence)
                .Take(3)
                .Select(p => p.Value)
                .ToList();
        }

        if (scanResult.RunInfo.Urls.Any())
        {
            insights.RunRecommendations.UrlsToTry = scanResult.RunInfo.Urls
                .OrderByDescending(u => u.Confidence)
                .Take(3)
                .Select(u => u.Value)
                .ToList();
        }

        // 添加基本风险提示
        insights.PotentialRisks.Add(new RiskItem
        {
            Title = "AI 分析服务不可用",
            Description = "无法获取详细的架构分析和风险评估，建议检查网络连接和 API 配置",
            Severity = RiskSeverity.Medium,
            Confidence = 1.0,
            Recommendations = new List<string> { "检查 AI 服务配置", "验证网络连接", "查看服务日志" }
        });

        return insights;
    }

    private string BuildPreviewContent(ProjectContentSummary summary)
    {
        return $@"项目摘要预览：

基本信息：
- 名称: {summary.BasicInfo.Name}
- 类型: {summary.BasicInfo.Type}
- 语言: {string.Join(", ", summary.BasicInfo.Languages)}
- 框架: {summary.BasicInfo.Framework}

文件统计：
- 总文件数: {summary.BasicInfo.FileStats.TotalFiles}
- 总目录数: {summary.BasicInfo.FileStats.TotalDirectories}
- 关键文件数: {summary.BasicInfo.FileStats.KeyFilesCount}

将发送给 AI 的内容包括：
- 文件结构摘要 ({summary.FileTreeSummary.Length} 字符)
- 依赖信息摘要 ({summary.DependenciesSummary.Length} 字符)
- 环境配置摘要 ({summary.EnvironmentSummary.Length} 字符)
- 运行信息摘要 ({summary.RunInfoSummary.Length} 字符)
- 架构结构摘要 ({summary.StructureSummary.Length} 字符)
- {summary.KeySnippets.Count} 个关键代码片段

预估 Token 使用量: {summary.EstimatedTokens}";
    }

    private List<string> GetTruncationNotes(ProjectContentSummary summary)
    {
        var notes = new List<string>();

        if (summary.KeySnippets.Count > 8)
        {
            notes.Add($"代码片段已从 {summary.KeySnippets.Count} 个截断到 8 个");
        }

        if (summary.FileTreeSummary.Length > 2000)
        {
            notes.Add("文件树摘要已截断以适应 Token 预算");
        }

        if (summary.DependenciesSummary.Length > 1500)
        {
            notes.Add("依赖信息已截断以适应 Token 预算");
        }

        return notes;
    }

    private double CalculateDataCompleteness(ProjectScanResult scanResult)
    {
        var completeness = 0.0;
        var maxScore = 5.0;

        // 检查各个数据维度的完整性
        if (scanResult.LanguagesDetected.Any()) completeness += 1.0;
        if (scanResult.Dependencies.TotalCount > 0) completeness += 1.0;
        if (scanResult.RunInfo.Ports.Any() || scanResult.RunInfo.Urls.Any()) completeness += 1.0;
        if (scanResult.StructureGraph.Nodes.Any()) completeness += 1.0;
        if (scanResult.FileTreeSummary.Any()) completeness += 1.0;

        return completeness / maxScore;
    }

    private double CalculateAnalysisDepth(ProjectContentSummary summary)
    {
        var depth = 0.0;
        var maxScore = 4.0;

        // 检查分析深度
        if (summary.KeySnippets.Any()) depth += 1.0;
        if (summary.BasicInfo.ComplexityMetrics.Any()) depth += 1.0;
        if (!string.IsNullOrEmpty(summary.StructureSummary)) depth += 1.0;
        if (summary.EstimatedTokens > 5000) depth += 1.0; // 内容丰富度

        return depth / maxScore;
    }

    #endregion
}