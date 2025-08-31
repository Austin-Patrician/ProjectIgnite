using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ProjectIgnite.Models;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// AI服务实现
    /// </summary>
    public class AIService : IAIService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<AIService> _logger;

        public AIService(
            IChatClient chatClient,
            ILogger<AIService> logger)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 生成流式聊天完成
        /// </summary>
        public async IAsyncEnumerable<StreamingChatCompletionUpdate> GenerateStreamingAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userMessage)
            };

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
            {
                // TODO: 需要将 Microsoft.Extensions.AI 的响应转换为 OpenAI.Chat.StreamingChatCompletionUpdate
                // 这里需要适配器模式来转换类型
                yield break; // 临时实现
            }
        }

        /// <summary>
        /// 生成聊天完成（非流式）
        /// </summary>
        public async Task<ChatCompletion> GenerateAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new(ChatRole.System, systemPrompt),
                    new(ChatRole.User, userMessage)
                };

                var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
                
                // TODO: 需要将 Microsoft.Extensions.AI 的响应转换为 OpenAI.Chat.ChatCompletion
                // 这里需要适配器模式来转换类型
                throw new NotImplementedException("需要实现类型转换");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成聊天完成时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 内部方法：生成基本聊天完成
        /// </summary>
        private async Task<string> GenerateChatCompletionInternalAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new(ChatRole.System, systemPrompt),
                    new(ChatRole.User, userMessage)
                };

                var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
                return response.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成聊天完成时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 生成架构说明
        /// </summary>
        public async Task<string> GenerateArchitectureExplanationAsync(
            string fileTree,
            string readmeContent,
            string? customInstructions = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("生成架构解释");

                var systemPrompt = "你是一个专业的软件架构分析师。请分析给定的项目结构并生成详细的架构说明。";
                var userMessage = $"项目文件结构：\n{fileTree}\n\nREADME内容：\n{readmeContent}";
                
                if (!string.IsNullOrEmpty(customInstructions))
                {
                    userMessage += $"\n\n自定义指令：\n{customInstructions}";
                }

                var explanation = await GenerateChatCompletionInternalAsync(systemPrompt, userMessage, cancellationToken);

                _logger.LogInformation("架构解释生成完成, 长度: {Length}", explanation.Length);
                
                return explanation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成架构解释时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 生成组件映射
        /// </summary>
        public async Task<string> GenerateComponentMappingAsync(
            string architectureExplanation,
            string fileTree,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("生成组件映射");

                var systemPrompt = "你是一个专业的软件架构分析师。请根据架构说明和文件结构生成组件映射，返回JSON格式。";
                var userMessage = $"架构说明：\n{architectureExplanation}\n\n文件结构：\n{fileTree}";
                
                var mappingText = await GenerateChatCompletionInternalAsync(systemPrompt, userMessage, cancellationToken);

                _logger.LogInformation("组件映射生成完成, 长度: {Length}", mappingText.Length);
                
                return mappingText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成组件映射时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 生成 Mermaid 图表代码
        /// </summary>
        public async Task<string> GenerateMermaidDiagramAsync(
            string architectureExplanation,
            string componentMapping,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("生成 Mermaid 图表");

                var systemPrompt = "你是一个专业的图表生成专家。请根据架构说明和组件映射生成 Mermaid 图表代码。";
                var userMessage = $"架构说明：\n{architectureExplanation}\n\n组件映射：\n{componentMapping}";
                
                var mermaidCode = await GenerateChatCompletionInternalAsync(systemPrompt, userMessage, cancellationToken);

                _logger.LogInformation("Mermaid 图表生成完成, 长度: {Length}", mermaidCode.Length);
                
                return mermaidCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成 Mermaid 图表时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 修改现有图表
        /// </summary>
        public async Task<string> ModifyDiagramAsync(
            string currentDiagram,
            string instructions,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("修改图表，指令长度: {Length}", instructions.Length);

                var systemPrompt = "你是一个专业的图表修改专家。请根据修改指令更新 Mermaid 图表代码。";
                var userMessage = $"当前图表：\n{currentDiagram}\n\n修改指令：\n{instructions}";
                
                var modifiedCode = await GenerateChatCompletionInternalAsync(systemPrompt, userMessage, cancellationToken);

                _logger.LogInformation("图表修改完成，代码长度: {Length}", modifiedCode.Length);
                
                return modifiedCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改图表时发生错误");
                throw;
            }
        }

        #region 私有方法

        /// <summary>
        /// 构建架构分析提示词
        /// </summary>
        private static string BuildArchitectureAnalysisPrompt(
            RepositoryInfo repositoryInfo,
            FileTreeNode fileTree,
            string? readmeContent,
            string? customInstructions)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("请分析以下GitHub仓库的架构和设计模式：");
            prompt.AppendLine();
            prompt.AppendLine($"**仓库信息：**");
            prompt.AppendLine($"- 名称: {repositoryInfo.Name}");
            prompt.AppendLine($"- 描述: {repositoryInfo.Description ?? "无描述"}");
            prompt.AppendLine($"- 主要语言: {repositoryInfo.Language ?? "未知"}");
            prompt.AppendLine($"- 星标数: {repositoryInfo.StarCount}");
            prompt.AppendLine();

            if (!string.IsNullOrEmpty(readmeContent))
            {
                prompt.AppendLine("**README内容：**");
                prompt.AppendLine(readmeContent.Length > 2000 ? readmeContent[..2000] + "..." : readmeContent);
                prompt.AppendLine();
            }

            prompt.AppendLine("**项目结构：**");
            prompt.AppendLine("```");
            prompt.AppendLine(fileTree.ToTreeString());
            prompt.AppendLine("```");
            prompt.AppendLine();

            if (!string.IsNullOrEmpty(customInstructions))
            {
                prompt.AppendLine("**特殊要求：**");
                prompt.AppendLine(customInstructions);
                prompt.AppendLine();
            }

            prompt.AppendLine("请提供详细的架构分析，包括：");
            prompt.AppendLine("1. 项目的整体架构模式");
            prompt.AppendLine("2. 主要组件和模块");
            prompt.AppendLine("3. 数据流和控制流");
            prompt.AppendLine("4. 技术栈和依赖关系");
            prompt.AppendLine("5. 设计模式的使用");

            return prompt.ToString();
        }

        /// <summary>
        /// 构建组件映射提示词
        /// </summary>
        private static string BuildComponentMappingPrompt(
            RepositoryInfo repositoryInfo,
            FileTreeNode fileTree,
            string architectureExplanation)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("基于以下架构分析，请创建组件到文件/目录的映射关系：");
            prompt.AppendLine();
            prompt.AppendLine("**架构分析：**");
            prompt.AppendLine(architectureExplanation);
            prompt.AppendLine();
            prompt.AppendLine("**项目结构：**");
            prompt.AppendLine("```");
            prompt.AppendLine(fileTree.ToTreeString());
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("请以以下格式提供组件映射（每行一个映射）：");
            prompt.AppendLine("组件名称 -> 文件/目录路径");
            prompt.AppendLine();
            prompt.AppendLine("例如：");
            prompt.AppendLine("用户认证模块 -> src/auth/");
            prompt.AppendLine("数据库层 -> src/data/");
            prompt.AppendLine("API控制器 -> src/controllers/");

            return prompt.ToString();
        }

        /// <summary>
        /// 构建Mermaid图表提示词
        /// </summary>
        private static string BuildMermaidDiagramPrompt(
            RepositoryInfo repositoryInfo,
            FileTreeNode fileTree,
            string architectureExplanation,
            Dictionary<string, string> componentMapping,
            string? customInstructions)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("请基于以下信息生成Mermaid架构图：");
            prompt.AppendLine();
            prompt.AppendLine("**项目信息：**");
            prompt.AppendLine($"- 名称: {repositoryInfo.Name}");
            prompt.AppendLine($"- 主要语言: {repositoryInfo.Language ?? "未知"}");
            prompt.AppendLine();
            prompt.AppendLine("**架构分析：**");
            prompt.AppendLine(architectureExplanation);
            prompt.AppendLine();
            
            if (componentMapping.Any())
            {
                prompt.AppendLine("**组件映射：**");
                foreach (var mapping in componentMapping)
                {
                    prompt.AppendLine($"- {mapping.Key} -> {mapping.Value}");
                }
                prompt.AppendLine();
            }

            if (!string.IsNullOrEmpty(customInstructions))
            {
                prompt.AppendLine("**特殊要求：**");
                prompt.AppendLine(customInstructions);
                prompt.AppendLine();
            }

            prompt.AppendLine("请生成一个清晰的Mermaid图表，要求：");
            prompt.AppendLine("1. 使用适当的图表类型（flowchart、graph、C4Context等）");
            prompt.AppendLine("2. 显示主要组件和它们之间的关系");
            prompt.AppendLine("3. 包含数据流向和依赖关系");
            prompt.AppendLine("4. 使用清晰的标签和描述");
            prompt.AppendLine("5. 只返回Mermaid代码，不要包含其他解释文本");
            prompt.AppendLine();
            prompt.AppendLine("示例格式：");
            prompt.AppendLine("```mermaid");
            prompt.AppendLine("graph TD");
            prompt.AppendLine("    A[组件A] --> B[组件B]");
            prompt.AppendLine("    B --> C[组件C]");
            prompt.AppendLine("```");

            return prompt.ToString();
        }

        /// <summary>
        /// 构建修改提示词
        /// </summary>
        private static string BuildModificationPrompt(
            string currentMermaidCode,
            string currentExplanation,
            string modificationInstructions)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("请根据以下修改指令更新Mermaid图表：");
            prompt.AppendLine();
            prompt.AppendLine("**当前图表代码：**");
            prompt.AppendLine("```mermaid");
            prompt.AppendLine(currentMermaidCode);
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("**当前架构说明：**");
            prompt.AppendLine(currentExplanation);
            prompt.AppendLine();
            prompt.AppendLine("**修改指令：**");
            prompt.AppendLine(modificationInstructions);
            prompt.AppendLine();
            prompt.AppendLine("请提供修改后的Mermaid代码，要求：");
            prompt.AppendLine("1. 保持图表的整体结构和风格");
            prompt.AppendLine("2. 根据指令进行精确修改");
            prompt.AppendLine("3. 确保修改后的图表仍然清晰易读");
            prompt.AppendLine("4. 只返回修改后的Mermaid代码，不要包含其他文本");

            return prompt.ToString();
        }

        /// <summary>
        /// 解析组件映射
        /// </summary>
        private static Dictionary<string, string> ParseComponentMapping(string mappingText)
        {
            var mapping = new Dictionary<string, string>();
            
            if (string.IsNullOrWhiteSpace(mappingText))
                return mapping;

            var lines = mappingText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                    continue;

                var arrowIndex = trimmedLine.IndexOf(" -> ", StringComparison.OrdinalIgnoreCase);
                if (arrowIndex > 0 && arrowIndex < trimmedLine.Length - 4)
                {
                    var component = trimmedLine[..arrowIndex].Trim();
                    var path = trimmedLine[(arrowIndex + 4)..].Trim();
                    
                    if (!string.IsNullOrEmpty(component) && !string.IsNullOrEmpty(path))
                    {
                        mapping[component] = path;
                    }
                }
            }

            return mapping;
        }

        /// <summary>
        /// 清理Mermaid代码
        /// </summary>
        private static string CleanMermaidCode(string mermaidCode)
        {
            if (string.IsNullOrWhiteSpace(mermaidCode))
                return string.Empty;

            var lines = mermaidCode.Split('\n');
            var cleanedLines = new List<string>();
            bool inCodeBlock = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 检测代码块标记
                if (trimmedLine.StartsWith("```mermaid", StringComparison.OrdinalIgnoreCase))
                {
                    inCodeBlock = true;
                    continue;
                }
                
                if (trimmedLine.StartsWith("```") && inCodeBlock)
                {
                    inCodeBlock = false;
                    continue;
                }

                // 如果在代码块中或者行看起来像Mermaid语法，则保留
                if (inCodeBlock || IsMermaidSyntax(trimmedLine))
                {
                    cleanedLines.Add(line);
                }
            }

            return string.Join('\n', cleanedLines).Trim();
        }

        /// <summary>
        /// 判断是否为Mermaid语法
        /// </summary>
        private static bool IsMermaidSyntax(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true; // 保留空行

            var trimmed = line.Trim();
            
            // Mermaid图表类型
            var mermaidKeywords = new[] 
            {
                "graph", "flowchart", "sequenceDiagram", "classDiagram", "stateDiagram",
                "erDiagram", "journey", "gantt", "pie", "gitgraph", "C4Context",
                "TD", "TB", "BT", "RL", "LR", "-->", "---", "==>", "-.->"  
            };

            return mermaidKeywords.Any(keyword => 
                trimmed.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                trimmed.Contains("[") || trimmed.Contains("]") ||
                trimmed.Contains("(") || trimmed.Contains(")") ||
                trimmed.Contains("{") || trimmed.Contains("}");
        }

        #endregion
    }
}