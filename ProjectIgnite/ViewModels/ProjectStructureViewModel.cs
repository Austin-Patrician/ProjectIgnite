using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ProjectIgnite.Models;
using ProjectIgnite.Services;
using ProjectIgnite.Repositories;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.ViewModels
{
    /// <summary>
    /// 项目结构视图模型
    /// 负责管理项目结构的显示和交互逻辑，支持图表生成功能
    /// </summary>
    public partial class ProjectStructureViewModel : ViewModelBase
    {
        private readonly IDiagramService _diagramService;
        private readonly ILogger<ProjectStructureViewModel> _logger;
        private readonly IProjectRepository _projectRepository;
        private CancellationTokenSource? _cancellationTokenSource;
        
        // 新增：渲染Markdown中图像的取消令牌
        private CancellationTokenSource? _renderCts;

        [ObservableProperty]
        private string _selectedProjectPath = string.Empty;

        [ObservableProperty]
        private string _selectedProjectName = string.Empty;

        [ObservableProperty]
        private string _currentMermaidCode = string.Empty;

        [ObservableProperty]
        private string _architectureExplanation = string.Empty;

        [ObservableProperty]
        private bool _isAnalyzing = false;

        [ObservableProperty]
        private bool _hasAnalysisResult = false;

        [ObservableProperty]
        private double _analysisProgress = 0;

        [ObservableProperty]
        private string _analysisStatus = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _customInstructions = string.Empty;

        [ObservableProperty]
        private bool _canExport = false;

        [ObservableProperty]
        private bool _canRegenerate = false;

        [ObservableProperty]
        private bool _canAnalyze = false;

        [ObservableProperty]
        private bool _showMermaidCode = false;

        [ObservableProperty]
        private string _markdownContent = string.Empty;

        // 新增：下拉框选中项目的 Id
        [ObservableProperty]
        private int? _selectedProjectId;

        // 新增：下拉框选中的项目对象（用于 Avalonia SelectedItem 绑定）
        [ObservableProperty]
        private ProjectSourceInfo? _selectedProject;

        public ObservableCollection<ProjectItem> AvailableProjects { get; }
        // 新增：从仓库加载的“最近/所有”项目列表
        public ObservableCollection<ProjectSourceInfo> RecentProjects { get; }
        public ObservableCollection<string> RecentProjectPaths { get; }

        // Computed properties for UI state
        public bool ShowEmptyState => !IsAnalyzing && !HasAnalysisResult && !HasError;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage) && !IsAnalyzing;
        // 更新：根据 RecentProjects 是否有数据控制可见性
        public bool HasRecentProjects => RecentProjects.Count > 0;
        public string StatusText => IsAnalyzing ? "Analyzing..." : HasAnalysisResult ? "Ready" : "Idle";
        public string ProgressText => AnalysisStatus;
        public double ProgressPercentage => AnalysisProgress;
        public DateTime? LastAnalysisTime => HasAnalysisResult ? DateTime.Now : null; // This should be stored properly



        public ICommand BrowseProjectCommand { get; }
        public ICommand AnalyzeProjectCommand { get; }
        public ICommand RegenerateAnalysisCommand { get; }
        public ICommand ExportMermaidCommand { get; }
        public ICommand ExportPngCommand { get; }
        public ICommand LoadSavedAnalysisCommand { get; }
        public ICommand CancelAnalysisCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand ToggleMermaidCodeCommand { get; }

        public ProjectStructureViewModel(IDiagramService diagramService, ILogger<ProjectStructureViewModel> logger, IProjectRepository projectRepository)
        {
            _diagramService = diagramService ?? throw new ArgumentNullException(nameof(diagramService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));

            AvailableProjects = new ObservableCollection<ProjectItem>();
            RecentProjects = new ObservableCollection<ProjectSourceInfo>();
            RecentProjectPaths = new ObservableCollection<string>();

            // 初始化命令
            BrowseProjectCommand = new AsyncRelayCommand(BrowseProjectAsync);
            AnalyzeProjectCommand = new AsyncRelayCommand(AnalyzeProjectAsync, () => CanAnalyze);
            RegenerateAnalysisCommand = new AsyncRelayCommand(RegenerateAnalysisAsync, () => CanRegenerate);
            ExportMermaidCommand = new AsyncRelayCommand(ExportMermaidAsync, () => CanExport);
            ExportPngCommand = new AsyncRelayCommand(ExportPngAsync, () => CanExport);
            LoadSavedAnalysisCommand = new AsyncRelayCommand(LoadSavedAnalysisAsync);
            CancelAnalysisCommand = new RelayCommand(CancelAnalysis, () => IsAnalyzing);
            ClearResultsCommand = new RelayCommand(ClearResults, () => HasAnalysisResult);
            ToggleMermaidCodeCommand = new RelayCommand(() => ShowMermaidCode = !ShowMermaidCode);

            // 初始化时加载可用项目（本地扫描逻辑保留）
            _ = Task.Run(LoadAvailableProjectsAsync);
            // 新增：从仓库加载项目列表用于下拉框
            _ = Task.Run(LoadRecentProjectsAsync);
        }

        partial void OnSelectedProjectPathChanged(string value)
        {
            UpdateCommandStates();
            if (!string.IsNullOrEmpty(value))
            {
                SelectedProjectName = Path.GetFileName(value);
                _ = Task.Run(() => LoadSavedAnalysisAsync());
            }
        }

        // 新增：当下拉框选中项目 Id 变化时，同步路径和名称
        partial void OnSelectedProjectIdChanged(int? value)
        {
            try
            {
                if (value.HasValue)
                {
                    var proj = RecentProjects.FirstOrDefault(p => p.Id == value.Value);
                    if (proj != null)
                    {
                        // 同步选中对象，触发 OnSelectedProjectChanged 派生逻辑
                        SelectedProject = proj;
                        SelectedProjectPath = proj.LocalPath ?? string.Empty;
                        SelectedProjectName = proj.Name ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "处理选中项目时发生错误");
            }
        }

        partial void OnSelectedProjectChanged(ProjectSourceInfo? value)
        {
            try
            {
                if (value != null)
                {
                    // 同步派生字段
                    SelectedProjectId = value.Id;
                    SelectedProjectPath = value.LocalPath ?? string.Empty;
                    SelectedProjectName = value.Name ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "处理 SelectedProject 变更时发生错误");
            }
        }

        partial void OnIsAnalyzingChanged(bool value)
        {
            UpdateCommandStates();
        }

        partial void OnHasAnalysisResultChanged(bool value)
        {
            UpdateCommandStates();
        }

        private void UpdateCommandStates()
        {
            CanAnalyze = !string.IsNullOrEmpty(SelectedProjectPath) && !IsAnalyzing && Directory.Exists(SelectedProjectPath);
            CanRegenerate = CanAnalyze && HasAnalysisResult;
            CanExport = HasAnalysisResult && !string.IsNullOrEmpty(CurrentMermaidCode);

            // 通知命令状态更新
            ((AsyncRelayCommand)AnalyzeProjectCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RegenerateAnalysisCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ExportMermaidCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ExportPngCommand).NotifyCanExecuteChanged();
            ((RelayCommand)CancelAnalysisCommand).NotifyCanExecuteChanged();
            ((RelayCommand)ClearResultsCommand).NotifyCanExecuteChanged();
        }

        // 向视图发出请求：打开文件夹选择对话框
        public event Action? RequestBrowseProjectFolder;

        private async Task BrowseProjectAsync()
        {
            try
            {
                // 通过事件让 View 弹出文件夹选择对话框，并在 View 侧设置 SelectedProjectPath
                _logger.LogInformation("请求浏览项目文件夹");
                RequestBrowseProjectFolder?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "浏览项目文件夹时发生错误");
                ErrorMessage = $"浏览文件夹失败: {ex.Message}";
            }
        }

        private async Task AnalyzeProjectAsync()
        {
            if (!CanAnalyze) return;

            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                IsAnalyzing = true;
                HasAnalysisResult = false;
                ErrorMessage = string.Empty;
                AnalysisProgress = 0;
                AnalysisStatus = "开始分析...";

                _logger.LogInformation("开始分析项目: {ProjectPath}", SelectedProjectPath);

                // 创建进度报告器
                var progress = new Progress<Services.GenerationProgress>(OnAnalysisProgressChanged);

                // 执行分析
                var result = await _diagramService.AnalyzeLocalProjectAsync(
                    SelectedProjectPath,
                    SelectedProjectName,
                    string.IsNullOrWhiteSpace(CustomInstructions) ? null : CustomInstructions,
                    progress,
                    _cancellationTokenSource.Token);

                if (result.IsSuccess)
                {
                    CurrentMermaidCode = result.MermaidCode ?? string.Empty;
                    ArchitectureExplanation = result.Explanation ?? string.Empty;
                    HasAnalysisResult = true;
                    AnalysisStatus = "分析完成";

                    // 添加到最近项目列表（保留原有路径历史逻辑）
                    AddToRecentProjects(SelectedProjectPath);

                    _logger.LogInformation("项目分析成功: {ProjectName}", SelectedProjectName);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "分析失败";
                    AnalysisStatus = "分析失败";
                    _logger.LogWarning("项目分析失败: {ProjectPath}, 错误: {Error}", SelectedProjectPath, result.ErrorMessage);
                }
            }
            catch (OperationCanceledException)
            {
                AnalysisStatus = "分析已取消";
                _logger.LogInformation("项目分析被取消: {ProjectPath}", SelectedProjectPath);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"分析过程中发生错误: {ex.Message}";
                AnalysisStatus = "分析错误";
                _logger.LogError(ex, "项目分析时发生错误: {ProjectPath}", SelectedProjectPath);
            }
            finally
            {
                IsAnalyzing = false;
                AnalysisProgress = 100;
            }
        }

        private async Task RegenerateAnalysisAsync()
        {
            if (!CanRegenerate) return;

            try
            {
                _logger.LogInformation("重新生成分析: {ProjectPath}", SelectedProjectPath);

                // 清除当前结果
                ClearResults();

                // 重新分析
                await AnalyzeProjectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新生成分析时发生错误: {ProjectPath}", SelectedProjectPath);
                ErrorMessage = $"重新分析失败: {ex.Message}";
            }
        }

        private async Task ExportMermaidAsync()
        {
            if (!CanExport) return;

            try
            {
                _logger.LogInformation("导出Mermaid文件: {ProjectName}", SelectedProjectName);

                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var fileName = $"{SelectedProjectName}_diagram.mermaid";
                var outputPath = Path.Combine(documentsPath, fileName);

                await _diagramService.ExportDiagramAsync(
                    CurrentMermaidCode,
                    ExportFormat.Mermaid,
                    outputPath,
                    CancellationToken.None);

                AnalysisStatus = $"Mermaid文件已导出到: {outputPath}";
                _logger.LogInformation("Mermaid文件导出成功: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Mermaid文件时发生错误");
                ErrorMessage = $"导出失败: {ex.Message}";
            }
        }

        private async Task ExportPngAsync()
        {
            if (!CanExport) return;

            try
            {
                _logger.LogInformation("导出PNG文件: {ProjectName}", SelectedProjectName);

                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var fileName = $"{SelectedProjectName}_diagram.png";
                var outputPath = Path.Combine(documentsPath, fileName);

                await _diagramService.ExportDiagramAsync(
                    CurrentMermaidCode,
                    ExportFormat.Png,
                    outputPath,
                    CancellationToken.None);

                AnalysisStatus = $"PNG文件已导出到: {outputPath}";
                _logger.LogInformation("PNG文件导出成功: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出PNG文件时发生错误");
                ErrorMessage = $"导出失败: {ex.Message}";
            }
        }

        private async Task LoadSavedAnalysisAsync()
        {
            if (string.IsNullOrEmpty(SelectedProjectPath)) return;

            try
            {
                _logger.LogInformation("加载已保存的分析结果: {ProjectPath}", SelectedProjectPath);

                var savedResult = await _diagramService.GetSavedAnalysisAsync(SelectedProjectPath);
                if (savedResult != null && savedResult.IsSuccess)
                {
                    CurrentMermaidCode = savedResult.MermaidCode ?? string.Empty;
                    ArchitectureExplanation = savedResult.Explanation ?? string.Empty;
                    HasAnalysisResult = true;
                    AnalysisStatus = "已加载保存的分析结果";

                    _logger.LogInformation("成功加载保存的分析结果: {ProjectPath}", SelectedProjectPath);
                }
                else
                {
                    // 清除之前的结果
                    ClearResults();
                    AnalysisStatus = "未找到保存的分析结果";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载保存的分析结果时发生错误: {ProjectPath}", SelectedProjectPath);
                ClearResults();
            }
        }

        private void CancelAnalysis()
        {
            if (IsAnalyzing)
            {
                _cancellationTokenSource?.Cancel();
                AnalysisStatus = "正在取消分析...";
                _logger.LogInformation("用户取消分析: {ProjectPath}", SelectedProjectPath);
            }
        }

        private void ClearResults()
        {
            CurrentMermaidCode = string.Empty;
            ArchitectureExplanation = string.Empty;
            HasAnalysisResult = false;
            ErrorMessage = string.Empty;
            AnalysisProgress = 0;
            AnalysisStatus = string.Empty;
        }

        private void OnAnalysisProgressChanged(ProjectIgnite.Services.GenerationProgress progress)
        {
            AnalysisProgress = progress.Percentage;
            AnalysisStatus = progress.Message;

            if (!string.IsNullOrEmpty(progress.ErrorMessage))
            {
                ErrorMessage = progress.ErrorMessage;
            }
        }

        private async Task LoadAvailableProjectsAsync()
        {
            try
            {
                await Task.Delay(100); // 模拟异步操作

                // 加载Documents下的ProjectIgnite项目
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var projectIgnitePath = Path.Combine(documentsPath, "ProjectIgnite");

                if (Directory.Exists(projectIgnitePath))
                {
                    var projectDirs = Directory.GetDirectories(projectIgnitePath)
                        .Where(dir => !Path.GetFileName(dir).Equals("Diagrams", StringComparison.OrdinalIgnoreCase))
                        .Take(10); // 限制数量

                    foreach (var projectDir in projectDirs)
                    {
                        var projectName = Path.GetFileName(projectDir);
                        AvailableProjects.Add(new ProjectItem
                        {
                            Name = projectName,
                            Path = projectDir,
                            LastModified = Directory.GetLastWriteTime(projectDir)
                        });
                    }
                }

                _logger.LogInformation("加载了 {Count} 个可用项目", AvailableProjects.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载可用项目时发生错误");
            }
        }

        // 新增：从仓库加载“最近/全部”项目列表
        private async Task LoadRecentProjectsAsync()
        {
            try
            {
                var projects = await _projectRepository.GetAllProjectsAsync();
                RecentProjects.Clear();
                foreach (var p in projects)
                {
                    RecentProjects.Add(p);
                }
                // 通知 UI 更新 HasRecentProjects 相关依赖
                OnPropertyChanged(nameof(HasRecentProjects));
                _logger.LogInformation("从仓库加载了 {Count} 个项目", RecentProjects.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从仓库加载项目时发生错误");
            }
        }

        private void AddToRecentProjects(string projectPath)
        {
            try
            {
                // 移除重复项
                while (RecentProjectPaths.Contains(projectPath))
                {
                    RecentProjectPaths.Remove(projectPath);
                }

                // 添加到最前面
                RecentProjectPaths.Insert(0, projectPath);

                // 限制数量
                while (RecentProjectPaths.Count > 10)
                {
                    RecentProjectPaths.RemoveAt(RecentProjectPaths.Count - 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "添加到最近项目列表时发生错误");
            }
        }



        partial void OnCurrentMermaidCodeChanged(string value)
        {
            UpdateMarkdownContent();
        }

        partial void OnArchitectureExplanationChanged(string value)
        {
            UpdateMarkdownContent();
        }

        private void UpdateMarkdownContent()
        {
            // CurrentMermaidCode 现在包含完整的 Markdown 内容（架构说明 + Mermaid 图表）
            if (!string.IsNullOrEmpty(CurrentMermaidCode))
            {
                MarkdownContent = CurrentMermaidCode;
            }
            else if (!string.IsNullOrEmpty(ArchitectureExplanation))
            {
                // 兼容旧版本：如果只有架构说明，则显示架构说明
                MarkdownContent = $"## 项目架构说明\n\n{ArchitectureExplanation}";
            }
            else
            {
                MarkdownContent = string.Empty;
            }
        
            // 异步渲染 mermaid -> SVG 并以 data URI 嵌入，渲染完成后再更新 Markdown
            StartRebuildMarkdownWithImageAsync();
        }
        
        private void StartRebuildMarkdownWithImageAsync()
        {
            if (string.IsNullOrEmpty(CurrentMermaidCode)) return;
        
            // 取消上一次渲染
            _renderCts?.Cancel();
            _renderCts?.Dispose();
            _renderCts = new CancellationTokenSource();
            var token = _renderCts.Token;
        
            _ = Task.Run(async () =>
            {
                try
                {
                    // 从 Markdown 内容中提取 Mermaid 代码块
                    var mermaidCode = ExtractMermaidCodeFromMarkdown(CurrentMermaidCode);
                    if (string.IsNullOrEmpty(mermaidCode))
                    {
                        // 如果没有找到 Mermaid 代码块，直接返回
                        return;
                    }
        
                    // 调用图表服务渲染为 SVG
                    var bytes = await _diagramService.ExportDiagramAsync(
                        mermaidCode,
                        ExportFormat.Svg,
                        outputPath: null!,
                        cancellationToken: token);
        
                    if (token.IsCancellationRequested) return;
        
                    var base64 = Convert.ToBase64String(bytes);
                    
                    // 替换 Markdown 中的 Mermaid 代码块为图像
                    var updatedMarkdown = ReplaceMermaidCodeWithImage(CurrentMermaidCode, base64);
        
                    // 更新到 UI
                    MarkdownContent = updatedMarkdown;
                }
                catch (OperationCanceledException)
                {
                    // 忽略取消
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "内联渲染 Mermaid 图像失败，保留代码围栏显示");
                    // 失败时保留已有的围栏版本，不抛出错误
                }
            }, token);
        }
        
        private string ExtractMermaidCodeFromMarkdown(string markdown)
        {
            var startPattern = "```mermaid";
            var endPattern = "```";
            
            var startIndex = markdown.IndexOf(startPattern, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return string.Empty;
            
            startIndex += startPattern.Length;
            var endIndex = markdown.IndexOf(endPattern, startIndex);
            if (endIndex == -1) return string.Empty;
            
            return markdown.Substring(startIndex, endIndex - startIndex).Trim();
        }
        
        private string ReplaceMermaidCodeWithImage(string markdown, string base64Image)
        {
            var startPattern = "```mermaid";
            var endPattern = "```";
            
            var startIndex = markdown.IndexOf(startPattern, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return markdown;
            
            var endIndex = markdown.IndexOf(endPattern, startIndex + startPattern.Length);
            if (endIndex == -1) return markdown;
            
            endIndex += endPattern.Length;
            
            var beforeCode = markdown.Substring(0, startIndex);
            var afterCode = markdown.Substring(endIndex);
            var imageMarkdown = $"![项目结构图](data:image/svg+xml;base64,{base64Image})";
            
            return beforeCode + imageMarkdown + afterCode;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
        }
    }

    /// <summary>
    /// 项目项
    /// </summary>
    public class ProjectItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string DisplayText => $"{Name} ({LastModified:yyyy-MM-dd})";
    }
}
