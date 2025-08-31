using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectIgnite.Models;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 图表服务实现
    /// </summary>
    public class DiagramService : IDiagramService
    {
        private readonly IGitHubService _gitHubService;
        private readonly IAIService _aiService;
        private readonly ILogger<DiagramService> _logger;

        public DiagramService(
            IGitHubService gitHubService,
            IAIService aiService,
            ILogger<DiagramService> logger)
        {
            _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 生成图表
        /// </summary>
        public async Task<DiagramResult> GenerateDiagramAsync(
            string repositoryUrl,
            string? customInstructions = null,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始生成图表，仓库URL: {RepositoryUrl}", repositoryUrl);

                // 1. 验证仓库URL
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.ValidatingRepository,
                    Percentage = 10,
                    Message = "验证仓库URL..."
                });

                if (!await _gitHubService.ValidateRepositoryAsync(repositoryUrl, cancellationToken))
                {
                    return DiagramResult.Failure("无效的仓库URL或仓库不存在");
                }

                // 2. 获取仓库信息
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.FetchingRepositoryInfo,
                    Percentage = 20,
                    Message = "获取仓库信息..."
                });

                var (owner, repo) = _gitHubService.ParseRepositoryUrl(repositoryUrl);
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    return DiagramResult.Failure("无效的仓库URL格式");
                }

                var repositoryInfo = await _gitHubService.GetRepositoryInfoAsync(repositoryUrl, cancellationToken);

                // 3. 获取文件树
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.FetchingFileTree,
                    Percentage = 40,
                    Message = "分析项目结构..."
                });

                var fileTree = await _gitHubService.GetFileTreeAsync(owner, repo, null, cancellationToken);

                // 4. 获取README内容（如果存在）
                var readmeContent = await _gitHubService.GetReadmeContentAsync(owner, repo, cancellationToken);

                // 5. 生成架构分析
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.GeneratingDiagram,
                    Percentage = 60,
                    Message = "分析项目架构..."
                });

                var architectureExplanation = await _aiService.GenerateArchitectureExplanationAsync(
                    fileTree.ToTreeString(),
                    readmeContent ?? string.Empty,
                    customInstructions,
                    cancellationToken);

                // 6. 生成组件映射
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.GeneratingDiagram,
                    Percentage = 80,
                    Message = "生成组件映射..."
                });

                var componentMapping = await _aiService.GenerateComponentMappingAsync(
                    architectureExplanation,
                    fileTree.ToTreeString(),
                    cancellationToken);

                // 7. 生成Mermaid图表代码
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.GeneratingDiagram,
                    Percentage = 90,
                    Message = "生成图表代码..."
                });

                var mermaidCode = await _aiService.GenerateMermaidDiagramAsync(
                    architectureExplanation,
                    componentMapping,
                    cancellationToken);

                // 8. 创建图表模型
                var diagram = new DiagramModel
                {
                    Id = Guid.NewGuid().ToString(),
                    RepositoryUrl = repositoryUrl,
                    MermaidCode = mermaidCode,
                    Explanation = architectureExplanation,
                    ComponentMapping = componentMapping,
                    CustomInstructions = customInstructions,
                    Title = $"{repositoryInfo.Name} 架构图",
                    Description = repositoryInfo.Description ?? "项目架构图表",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 1
                };

                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.Completed,
                    Percentage = 100,
                    Message = "图表生成完成"
                });

                _logger.LogInformation("图表生成成功，仓库: {RepositoryUrl}", repositoryUrl);
                // 解析组件映射字符串为字典
                Dictionary<string, string>? componentMappingDict = null;
                if (!string.IsNullOrEmpty(diagram.ComponentMapping))
                {
                    try
                    {
                        componentMappingDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(diagram.ComponentMapping);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析组件映射失败，使用空字典");
                    }
                }
                
                return DiagramResult.Success(diagram.MermaidCode, diagram.Explanation, componentMappingDict);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("图表生成被取消，仓库: {RepositoryUrl}", repositoryUrl);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成图表时发生错误，仓库: {RepositoryUrl}", repositoryUrl);
                return DiagramResult.Failure($"生成图表时发生错误: {ex.Message}");
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
                _logger.LogInformation("开始修改图表");

                // 调用 AI 服务修改图表
                var modifiedMermaidCode = await _aiService.ModifyDiagramAsync(currentDiagram, instructions, cancellationToken);
                
                _logger.LogInformation("图表修改成功");
                return modifiedMermaidCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改图表时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 导出图表
        /// </summary>
        public async Task<byte[]> ExportDiagramAsync(
            string diagramContent,
            ExportFormat format,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始导出图表，格式: {Format}", format);

                // TODO: 实现不同格式的导出功能
                switch (format)
                {
                    case ExportFormat.Mermaid:
                        // 返回 Mermaid 源码的字节数组
                        return System.Text.Encoding.UTF8.GetBytes(diagramContent);
                    case ExportFormat.Png:
                    case ExportFormat.Svg:
                        // TODO: 实现图片格式导出
                        _logger.LogWarning("图片格式导出功能尚未实现");
                        throw new NotImplementedException($"格式 {format} 的导出功能尚未实现");
                    default:
                        throw new ArgumentException($"不支持的导出格式: {format}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出图表时发生错误，格式: {Format}", format);
                throw;
            }
        }

        #region 私有方法

        /// <summary>
        /// 递增版本号
        /// </summary>
        private static string IncrementVersion(string currentVersion)
        {
            if (string.IsNullOrEmpty(currentVersion))
                return "1.0";

            var parts = currentVersion.Split('.');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var minor))
            {
                return $"{parts[0]}.{minor + 1}";
            }

            return currentVersion + ".1";
        }

        /// <summary>
        /// 导出为Mermaid格式
        /// </summary>
        private async Task ExportAsMermaidAsync(DiagramModel diagram, CancellationToken cancellationToken)
        {
            // TODO: 实现Mermaid格式导出
            await Task.Delay(100, cancellationToken); // 占位符
        }

        /// <summary>
        /// 导出为PNG格式
        /// </summary>
        private async Task ExportAsPngAsync(DiagramModel diagram, CancellationToken cancellationToken)
        {
            // TODO: 实现PNG格式导出
            await Task.Delay(100, cancellationToken); // 占位符
        }

        /// <summary>
        /// 导出为SVG格式
        /// </summary>
        private async Task ExportAsSvgAsync(DiagramModel diagram, CancellationToken cancellationToken)
        {
            // TODO: 实现SVG格式导出
            await Task.Delay(100, cancellationToken); // 占位符
        }

        /// <summary>
        /// 导出为PDF格式
        /// </summary>
        private async Task ExportAsPdfAsync(DiagramModel diagram, CancellationToken cancellationToken)
        {
            // TODO: 实现PDF格式导出
            await Task.Delay(100, cancellationToken); // 占位符
        }

        #endregion
    }
}