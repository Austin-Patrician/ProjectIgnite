using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ProjectIgnite.Models;
using OpenAI.Chat;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// AI 服务接口
    /// 基于 Microsoft.Extensions.AI 提供统一的 AI 服务抽象
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// 生成流式聊天完成
        /// </summary>
        /// <param name="systemPrompt">系统提示词</param>
        /// <param name="userMessage">用户消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>流式响应</returns>
        IAsyncEnumerable<StreamingChatCompletionUpdate> GenerateStreamingAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成聊天完成（非流式）
        /// </summary>
        /// <param name="systemPrompt">系统提示词</param>
        /// <param name="userMessage">用户消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完整响应</returns>
        Task<ChatCompletion> GenerateAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成架构说明
        /// </summary>
        /// <param name="fileTree">文件树结构</param>
        /// <param name="readmeContent">README 内容</param>
        /// <param name="customInstructions">自定义指令</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>架构说明</returns>
        Task<string> GenerateArchitectureExplanationAsync(
            string fileTree,
            string readmeContent,
            string? customInstructions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成组件映射
        /// </summary>
        /// <param name="architectureExplanation">架构说明</param>
        /// <param name="fileTree">文件树结构</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>组件映射（JSON 格式）</returns>
        Task<string> GenerateComponentMappingAsync(
            string architectureExplanation,
            string fileTree,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成 Mermaid 图表代码
        /// </summary>
        /// <param name="architectureExplanation">架构说明</param>
        /// <param name="componentMapping">组件映射</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Mermaid 图表代码</returns>
        Task<string> GenerateMermaidDiagramAsync(
            string architectureExplanation,
            string componentMapping,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 修改现有图表
        /// </summary>
        /// <param name="currentDiagram">当前图表代码</param>
        /// <param name="modificationInstructions">修改指令</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>修改后的图表代码</returns>
        Task<string> ModifyDiagramAsync(
            string currentDiagram,
            string modificationInstructions,
            CancellationToken cancellationToken = default);
    }
}