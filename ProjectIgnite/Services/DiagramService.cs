using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ProjectIgnite.Models;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 图表服务实现 - 基于本地项目分析
    /// </summary>
    public class DiagramService : IDiagramService
    {
        private readonly ILocalProjectAnalyzer _projectAnalyzer;
        private readonly IAIService _aiService;
        private readonly ILogger<DiagramService> _logger;

        // 本地存储基路径
        private readonly string _diagramStoragePath;

        // 复用 HttpClient
        private static readonly HttpClient _httpClient = new HttpClient();

        public DiagramService(
            ILocalProjectAnalyzer projectAnalyzer,
            IAIService aiService,
            ILogger<DiagramService> logger)
        {
            _projectAnalyzer = projectAnalyzer ?? throw new ArgumentNullException(nameof(projectAnalyzer));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 设置存储路径为用户文档文件夹下的 ProjectIgnite/Diagrams
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _diagramStoragePath = Path.Combine(documentsPath, "ProjectIgnite", "Diagrams");
            
            // 确保目录存在
            Directory.CreateDirectory(_diagramStoragePath);
        }

        public async Task<DiagramResult> AnalyzeLocalProjectAsync(
            string projectPath,
            string projectName,
            string? customInstructions = null,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始分析本地项目: {ProjectPath}", projectPath);

                // 1. 验证项目路径
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.ValidatingRepository,
                    Percentage = 5,
                    Message = "验证项目路径..."
                });

                if (!Directory.Exists(projectPath))
                {
                    return DiagramResult.Failure($"项目目录不存在: {projectPath}");
                }

                // 2. 分析项目结构
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.FetchingFileTree,
                    Percentage = 20,
                    Message = "分析项目结构..."
                });

                var analysisResult = await _projectAnalyzer.AnalyzeProjectAsync(
                    projectPath, projectName, customInstructions, cancellationToken);

                // 3. 生成架构说明
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.GeneratingDiagram,
                    Percentage = 40,
                    Message = "生成架构说明..."
                });

                var architectureExplanation = await GenerateArchitectureExplanationAsync(
                    analysisResult, cancellationToken);

                // 4. 生成组件映射
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.GeneratingDiagram,
                    Percentage = 60,
                    Message = "生成组件映射..."
                });

                var componentMapping = await GenerateComponentMappingAsync(
                    architectureExplanation, analysisResult, cancellationToken);

                // 5. 生成Mermaid图表代码
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.GeneratingDiagram,
                    Percentage = 80,
                    Message = "生成图表代码..."
                });

                var mermaidCode = await GenerateMermaidDiagramAsync(
                    architectureExplanation, componentMapping, analysisResult, cancellationToken);

                // 6. 保存结果
                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.GeneratingDiagram,
                    Percentage = 95,
                    Message = "保存分析结果..."
                });

                var result = DiagramResult.Success(mermaidCode, architectureExplanation, componentMapping);
                await SaveAnalysisAsync(projectPath, result, cancellationToken);

                progress?.Report(new GenerationProgress
                {
                    State = GenerationState.Completed,
                    Percentage = 100,
                    Message = "分析完成"
                });

                _logger.LogInformation("本地项目分析完成: {ProjectName}", projectName);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("项目分析被取消: {ProjectPath}", projectPath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析本地项目时发生错误: {ProjectPath}", projectPath);
                return DiagramResult.Failure($"分析项目时发生错误: {ex.Message}");
            }
        }

        public async Task<DiagramResult> RegenerateAnalysisAsync(
            string projectPath,
            string projectName,
            string? customInstructions = null,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("重新生成项目分析: {ProjectPath}", projectPath);
            
            // 重新分析与初次分析流程相同
            return await AnalyzeLocalProjectAsync(projectPath, projectName, customInstructions, progress, cancellationToken);
        }

        public async Task<string> ModifyDiagramAsync(
            string currentDiagram,
            string instructions,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始修改图表");

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

        public async Task<byte[]> ExportDiagramAsync(
            string diagramContent,
            ExportFormat format,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始导出图表，格式: {Format}", format);

                byte[] exportData;

                switch (format)
                {
                    case ExportFormat.Mermaid:
                        exportData = System.Text.Encoding.UTF8.GetBytes(diagramContent);
                        break;
                    case ExportFormat.Png:
                        exportData = await RenderWithKrokiAsync("mermaid", "png", diagramContent, cancellationToken);
                        break;
                    case ExportFormat.Svg:
                        exportData = await RenderWithKrokiAsync("mermaid", "svg", diagramContent, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException($"不支持的导出格式: {format}");
                }

                // 保存到指定路径
                if (!string.IsNullOrEmpty(outputPath))
                {
                    var directory = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    await File.WriteAllBytesAsync(outputPath, exportData, cancellationToken);
                }

                _logger.LogInformation("图表导出成功: {OutputPath}", outputPath);
                return exportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出图表时发生错误，格式: {Format}", format);
                throw;
            }
        }

        private static async Task<byte[]> RenderWithKrokiAsync(string engine, string format, string code, CancellationToken ct)
        {
            // 参考：https://kroki.io/ 文档，直接以纯文本 POST 代码获取渲染结果
            var url = $"https://kroki.io/{engine}/{format}";
            using var content = new StringContent(code, Encoding.UTF8, "text/plain");
            using var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        public async Task<DiagramResult?> GetSavedAnalysisAsync(string projectPath)
        {
            try
            {
                var projectHash = GetProjectHash(projectPath);
                var analysisFilePath = Path.Combine(_diagramStoragePath, projectHash, "analysis.json");

                if (!File.Exists(analysisFilePath))
                {
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(analysisFilePath);
                var savedData = JsonSerializer.Deserialize<SavedAnalysisData>(jsonContent);

                if (savedData == null)
                {
                    return null;
                }

                var componentMapping = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(savedData.ComponentMappingJson))
                {
                    try
                    {
                        componentMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(savedData.ComponentMappingJson) 
                            ?? new Dictionary<string, string>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析组件映射失败");
                    }
                }

                return DiagramResult.Success(
                    savedData.MermaidCode ?? "",
                    savedData.Explanation ?? "",
                    componentMapping);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取保存的分析结果失败: {ProjectPath}", projectPath);
                return null;
            }
        }

        public async Task SaveAnalysisAsync(string projectPath, DiagramResult result, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!result.IsSuccess)
                {
                    return;
                }

                var projectHash = GetProjectHash(projectPath);
                var projectStoragePath = Path.Combine(_diagramStoragePath, projectHash);
                Directory.CreateDirectory(projectStoragePath);

                var savedData = new SavedAnalysisData
                {
                    ProjectPath = projectPath,
                    MermaidCode = result.MermaidCode,
                    Explanation = result.Explanation,
                    ComponentMappingJson = result.ComponentMapping != null 
                        ? JsonSerializer.Serialize(result.ComponentMapping)
                        : null,
                    SavedAt = DateTime.UtcNow
                };

                var jsonContent = JsonSerializer.Serialize(savedData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                var analysisFilePath = Path.Combine(projectStoragePath, "analysis.json");
                await File.WriteAllTextAsync(analysisFilePath, jsonContent, cancellationToken);

                // 同时保存纯Mermaid代码文件
                if (!string.IsNullOrEmpty(result.MermaidCode))
                {
                    var mermaidFilePath = Path.Combine(projectStoragePath, "diagram.mermaid");
                    await File.WriteAllTextAsync(mermaidFilePath, result.MermaidCode, cancellationToken);
                }

                _logger.LogInformation("分析结果已保存: {ProjectPath}", projectPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存分析结果失败: {ProjectPath}", projectPath);
                throw;
            }
        }

        #region Private Methods

        private async Task<string> GenerateArchitectureExplanationAsync(
            ProjectAnalysisResult analysisResult,
            CancellationToken cancellationToken)
        {
            var prompt = BuildArchitectureAnalysisPrompt(analysisResult);
            
            return await _aiService.GenerateArchitectureExplanationAsync(
                analysisResult.FileStructure.ToTreeString(),
                prompt,
                analysisResult.CustomInstructions,
                cancellationToken);
        }

        private async Task<Dictionary<string, string>> GenerateComponentMappingAsync(
            string architectureExplanation,
            ProjectAnalysisResult analysisResult,
            CancellationToken cancellationToken)
        {
            var mappingText = await _aiService.GenerateComponentMappingAsync(
                architectureExplanation,
                analysisResult.FileStructure.ToTreeString(),
                cancellationToken);

            return ParseComponentMapping(mappingText);
        }

        private async Task<string> GenerateMermaidDiagramAsync(
            string architectureExplanation,
            Dictionary<string, string> componentMapping,
            ProjectAnalysisResult analysisResult,
            CancellationToken cancellationToken)
        {
            var componentMappingJson = JsonSerializer.Serialize(componentMapping);
            
            return await _aiService.GenerateMermaidDiagramAsync(
                architectureExplanation,
                componentMappingJson,
                cancellationToken);
        }

        private string BuildArchitectureAnalysisPrompt(ProjectAnalysisResult analysisResult)
        {
            var prompt = $@"
项目分析信息：
- 项目名称: {analysisResult.ProjectName}
- 项目类型: {analysisResult.ProjectType}
- 主要语言: {analysisResult.PrimaryLanguage}
- 文件总数: {analysisResult.FileStructure.TotalFiles}
- 目录总数: {analysisResult.FileStructure.TotalDirectories}

依赖信息：
- 包管理器: {analysisResult.Dependencies.PackageManager}
- 依赖包数量: {analysisResult.Dependencies.Packages.Count}
- 开发依赖数量: {analysisResult.Dependencies.DevPackages.Count}

配置文件数量: {analysisResult.ConfigurationFiles.Count}

请基于以上信息分析项目架构。
";

            return prompt;
        }

        private Dictionary<string, string> ParseComponentMapping(string mappingText)
        {
            var mapping = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(mappingText))
                return mapping;

            try
            {
                // 尝试解析为JSON
                var jsonMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingText);
                if (jsonMapping != null)
                {
                    return jsonMapping;
                }
            }
            catch
            {
                // 如果JSON解析失败，尝试文本解析
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
            }

            return mapping;
        }

        private string GetProjectHash(string projectPath)
        {
            // 使用项目路径的哈希值作为存储目录名
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(projectPath.ToLowerInvariant())))
                .ToLowerInvariant()[..16]; // 取前16个字符
        }

        #endregion

        #region Private Classes

        private class SavedAnalysisData
        {
            public string ProjectPath { get; set; } = string.Empty;
            public string? MermaidCode { get; set; }
            public string? Explanation { get; set; }
            public string? ComponentMappingJson { get; set; }
            public DateTime SavedAt { get; set; }
        }

        #endregion
    }
}
