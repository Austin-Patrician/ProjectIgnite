using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectIgnite.DTOs;
using ProjectIgnite.Services;
using Avalonia.Threading;

namespace ProjectIgnite.ViewModels;

/// <summary>
/// 扫描计数器辅助类
/// </summary>
public class ScanCounters
{
    public int FileCount { get; set; }
    public int DirCount { get; set; }
}

/// <summary>
/// 项目分析器 ViewModel
/// </summary>
public partial class ProjectAnalyzerViewModel : ViewModelBase
{
    private readonly ILanguageAnalyzer[] _languageAnalyzers;
    private readonly IAIInsightsService _aiInsightsService;
    private readonly IContentSummarizer _contentSummarizer;
    private CancellationTokenSource? _cancellationTokenSource;

    public ProjectAnalyzerViewModel(
        IEnumerable<ILanguageAnalyzer> languageAnalyzers,
        IAIInsightsService aiInsightsService,
        IContentSummarizer contentSummarizer)
    {
        _languageAnalyzers = languageAnalyzers?.ToArray() ?? throw new ArgumentNullException(nameof(languageAnalyzers));
        _aiInsightsService = aiInsightsService ?? throw new ArgumentNullException(nameof(aiInsightsService));
        _contentSummarizer = contentSummarizer ?? throw new ArgumentNullException(nameof(contentSummarizer));

        // 初始化命令
        SelectFolderCommand = new AsyncRelayCommand(SelectFolderAsync);
        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => CanStartScan);
        CancelScanCommand = new RelayCommand(CancelScan, () => IsScanning);
        GenerateAIInsightsCommand = new AsyncRelayCommand(GenerateAIInsightsAsync, () => CanGenerateAI);
        ExportSvgCommand = new AsyncRelayCommand(ExportSvgAsync, () => HasResults);
        ExportJsonCommand = new AsyncRelayCommand(ExportJsonAsync, () => HasResults);
        PreviewAIContentCommand = new AsyncRelayCommand(PreviewAIContentAsync, () => HasResults);

        // 监听属性变化以更新命令状态
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedPath) || 
                e.PropertyName == nameof(IsScanning) ||
                e.PropertyName == nameof(HasResults) ||
                e.PropertyName == nameof(IsGeneratingAI))
            {
                UpdateCommandStates();
            }
        };
    }

    #region 属性

    [ObservableProperty]
    private string _selectedPath = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isGeneratingAI;

    [ObservableProperty]
    private double _scanProgress;

    [ObservableProperty]
    private string _statusMessage = "请选择要分析的项目文件夹";

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private ProjectScanResult? _scanResult;

    [ObservableProperty]
    private AIInsights? _aiInsights;

    [ObservableProperty]
    private AIContentPreview? _aiContentPreview;

    [ObservableProperty]
    private AIServiceStatus? _aiServiceStatus;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _enableAI = true;

    [ObservableProperty]
    private int _tokenBudget = 15000;

    // 集合属性
    public ObservableCollection<FileNodeSummary> FileTree { get; } = new();
    public ObservableCollection<PackageEntry> CSharpDependencies { get; } = new();
    public ObservableCollection<PackageEntry> NodeDependencies { get; } = new();
    public ObservableCollection<PortCandidate> Ports { get; } = new();
    public ObservableCollection<UrlCandidate> Urls { get; } = new();
    public ObservableCollection<LogItem> Logs { get; } = new();
    public ObservableCollection<WarningItem> Warnings { get; } = new();
    public ObservableCollection<GraphNode> StructureNodes { get; } = new();
    public ObservableCollection<GraphEdge> StructureEdges { get; } = new();
    public ObservableCollection<ModuleInsight> AIModules { get; } = new();
    public ObservableCollection<RiskItem> AIRisks { get; } = new();
    public ObservableCollection<DependencyAdviceItem> AIAdvice { get; } = new();

    // 计算属性
    public bool CanStartScan => !string.IsNullOrEmpty(SelectedPath) && !IsScanning && (Directory.Exists(SelectedPath) || File.Exists(SelectedPath));
    public bool CanGenerateAI => HasResults && !IsGeneratingAI && EnableAI;
    public string ScanButtonText => IsScanning ? "扫描中..." : "开始扫描";
    public string AIButtonText => IsGeneratingAI ? "AI 分析中..." : "生成 AI 洞察";

    // 衍生属性通知（确保绑定到 CanStartScan/ScanButtonText/AIButtonText 的 UI 能及时刷新）
    partial void OnSelectedPathChanged(string value)
    {
        OnPropertyChanged(nameof(CanStartScan));
        ((AsyncRelayCommand)StartScanCommand).NotifyCanExecuteChanged();
    }

    partial void OnIsScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStartScan));
        OnPropertyChanged(nameof(ScanButtonText));
        ((AsyncRelayCommand)StartScanCommand).NotifyCanExecuteChanged();
        ((RelayCommand)CancelScanCommand).NotifyCanExecuteChanged();
    }

    partial void OnIsGeneratingAIChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGenerateAI));
        OnPropertyChanged(nameof(AIButtonText));
        ((AsyncRelayCommand)GenerateAIInsightsCommand).NotifyCanExecuteChanged();
    }

    partial void OnHasResultsChanged(bool value)
    {
        // HasResults 影响 CanGenerateAI，需要通知绑定更新
        OnPropertyChanged(nameof(CanGenerateAI));
        ((AsyncRelayCommand)ExportSvgCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)ExportJsonCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)PreviewAIContentCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)GenerateAIInsightsCommand).NotifyCanExecuteChanged();
    }

    // EnableAI 变化同样影响 CanGenerateAI，需要显式通知
    partial void OnEnableAIChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGenerateAI));
        ((AsyncRelayCommand)GenerateAIInsightsCommand).NotifyCanExecuteChanged();
    }

    #endregion

    #region 命令

    public ICommand SelectFolderCommand { get; }
    public ICommand StartScanCommand { get; }
    public ICommand CancelScanCommand { get; }
    public ICommand GenerateAIInsightsCommand { get; }
    public ICommand ExportSvgCommand { get; }
    public ICommand ExportJsonCommand { get; }
    public ICommand PreviewAIContentCommand { get; }

    #endregion

    #region 命令实现

    private async Task SelectFolderAsync()
    {
        try
        {
            // 获取主窗口实例
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow == null)
            {
                StatusMessage = "无法获取主窗口实例";
                return;
            }

            // 使用 Avalonia 的 StorageProvider API 选择文件夹
            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
            {
                StatusMessage = "存储提供程序不可用";
                return;
            }

            var options = new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "选择要分析的项目文件夹",
                AllowMultiple = false
            };

            var result = await storageProvider.OpenFolderPickerAsync(options);
            
            if (result != null && result.Count > 0)
            {
                var selectedFolder = result[0];
                SelectedPath = selectedFolder.Path.LocalPath;
                StatusMessage = $"已选择文件夹: {Path.GetFileName(SelectedPath)}";
                
                // 清除之前的结果
                ClearResults();
                
                AddLog(LogLevel.Info, $"用户选择了项目文件夹: {SelectedPath}");
            }
            else
            {
                StatusMessage = "未选择文件夹";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择文件夹时发生错误: {ex.Message}";
            AddLog(LogLevel.Error, $"选择文件夹失败: {ex.Message}");
        }
    }

    private async Task StartScanAsync()
    {
        if (!CanStartScan) return;

        try
        {
            IsScanning = true;
            ScanProgress = 0;
            StatusMessage = "正在扫描项目...";
            _cancellationTokenSource = new CancellationTokenSource();

            // 如果 SelectedPath 是文件，则取其所在目录作为扫描根目录
            var rootForScan = Directory.Exists(SelectedPath)
                ? SelectedPath
                : (File.Exists(SelectedPath) ? Path.GetDirectoryName(SelectedPath)! : SelectedPath);

            // 创建扫描请求
            var request = new ProjectScanRequest
            {
                RootPath = rootForScan,
                EnableAI = EnableAI,
                TokenBudget = TokenBudget
            };

            AddLog(LogLevel.Info, $"开始扫描项目: {rootForScan}");

            // 执行扫描
            var result = await PerformScanAsync(request, _cancellationTokenSource.Token);

            if (result != null)
            {
                ScanResult = result;
                HasResults = true;
                StatusMessage = $"扫描完成（根目录：{rootForScan}），发现 {result.LanguagesDetected.Count} 种语言，{result.TotalFilesScanned} 个文件";

                // 更新UI集合
                UpdateUICollections(result);

                AddLog(LogLevel.Info, "项目扫描完成");

                // 检查AI服务状态
                if (EnableAI)
                {
                    await CheckAIServiceStatusAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "扫描已取消";
            AddLog(LogLevel.Warning, "用户取消了扫描操作");
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描失败: {ex.Message}";
            AddLog(LogLevel.Error, $"扫描过程中发生错误: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
            ScanProgress = 100;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void CancelScan()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "正在取消扫描...";
    }

    private async Task GenerateAIInsightsAsync()
    {
        if (!CanGenerateAI || ScanResult == null) return;

        try
        {
            IsGeneratingAI = true;
            StatusMessage = "正在生成 AI 洞察...";
            
            AddLog(LogLevel.Info, "开始生成 AI 洞察");

            var insights = await _aiInsightsService.GenerateInsightsAsync(
                ScanResult, 
                TokenBudget, 
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            if (insights != null)
            {
                AiInsights = insights;
                StatusMessage = $"AI 洞察生成完成，置信度: {insights.ConfidenceScores.Overall:P0}";
                
                // 更新AI相关集合
                UpdateAICollections(insights);
                
                AddLog(LogLevel.Info, "AI 洞察生成完成");
                
                // 自动切换到AI洞察标签页
                SelectedTabIndex = 6; // 假设AI洞察是第7个标签页
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"AI 洞察生成失败: {ex.Message}";
            AddLog(LogLevel.Error, $"AI 洞察生成过程中发生错误: {ex.Message}");
        }
        finally
        {
            IsGeneratingAI = false;
        }
    }

    private async Task PreviewAIContentAsync()
    {
        if (ScanResult == null) return;

        try
        {
            StatusMessage = "正在生成 AI 内容预览...";
            
            var preview = await _aiInsightsService.DryRunPreviewAsync(ScanResult, TokenBudget);
            AiContentPreview = preview;
            
            StatusMessage = $"预览生成完成，预估使用 {preview.EstimatedTokens} tokens ({preview.BudgetUsageRatio:P0})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"预览生成失败: {ex.Message}";
            AddLog(LogLevel.Error, $"预览生成过程中发生错误: {ex.Message}");
        }
    }

    private async Task ExportSvgAsync()
    {
        if (ScanResult?.StructureGraph == null) return;

        try
        {
            // 这里应该实现SVG导出逻辑
            StatusMessage = "SVG 导出功能待实现";
            AddLog(LogLevel.Info, "用户请求导出 SVG");
        }
        catch (Exception ex)
        {
            StatusMessage = $"SVG 导出失败: {ex.Message}";
            AddLog(LogLevel.Error, $"SVG 导出过程中发生错误: {ex.Message}");
        }
    }

    private async Task ExportJsonAsync()
    {
        if (ScanResult == null) return;

        try
        {
            // 这里应该实现JSON导出逻辑
            StatusMessage = "JSON 导出功能待实现";
            AddLog(LogLevel.Info, "用户请求导出 JSON");
        }
        catch (Exception ex)
        {
            StatusMessage = $"JSON 导出失败: {ex.Message}";
            AddLog(LogLevel.Error, $"JSON 导出过程中发生错误: {ex.Message}");
        }
    }

    #endregion

    #region 私有方法

    private async Task<ProjectScanResult?> PerformScanAsync(ProjectScanRequest request, CancellationToken cancellationToken)
    {
        var result = new ProjectScanResult
        {
            GeneratedAt = DateTimeOffset.Now
        };

        var startTime = DateTime.Now;

        try
        {
            // 1. 语言检测
            ScanProgress = 10;
            StatusMessage = "检测项目语言...";
            await DetectLanguagesAsync(request.RootPath, result, cancellationToken);

            // 2. 文件树扫描
            ScanProgress = 30;
            StatusMessage = "扫描文件结构...";
            await ScanFileTreeAsync(request.RootPath, result, cancellationToken);

            // 3. 语言特定分析
            ScanProgress = 60;
            StatusMessage = "分析项目结构...";
            await PerformLanguageSpecificAnalysisAsync(request, result, cancellationToken);

            // 4. 合并结果
            ScanProgress = 90;
            StatusMessage = "整理分析结果...";
            await FinalizeResultsAsync(result, cancellationToken);

            result.IsCompleted = true;
            result.ScanDurationMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
            
            return result;
        }
        catch (Exception ex)
        {
            result.IsCompleted = false;
            result.ErrorMessage = ex.Message;
            result.ScanDurationMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
            
            throw;
        }
    }

    private async Task DetectLanguagesAsync(string rootPath, ProjectScanResult result, CancellationToken cancellationToken)
    {
        var detectedLanguages = new List<(string language, double confidence)>();

        foreach (var analyzer in _languageAnalyzers)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var detection = await analyzer.DetectAsync(rootPath);
                if (detection.IsDetected)
                {
                    detectedLanguages.Add((analyzer.LanguageType, detection.Confidence));
                    AddLog(LogLevel.Info, $"检测到 {analyzer.LanguageType} 项目，置信度: {detection.Confidence:P0}");
                }
            }
            catch (Exception ex)
            {
                AddLog(LogLevel.Warning, $"语言检测失败 ({analyzer.LanguageType}): {ex.Message}");
            }
        }

        // 按置信度排序
        result.LanguagesDetected = detectedLanguages
            .OrderByDescending(l => l.confidence)
            .Select(l => l.language)
            .ToList();
    }

    private async Task ScanFileTreeAsync(string rootPath, ProjectScanResult result, CancellationToken cancellationToken)
    {
        try
        {
            var fileTree = new List<FileNodeSummary>();
            var fileCount = 0;
            var dirCount = 0;

            var counters = new ScanCounters { FileCount = 0, DirCount = 0 };
            await ScanDirectoryRecursiveAsync(rootPath, fileTree, counters, 0, 6, cancellationToken);
            
            fileCount = counters.FileCount;
            dirCount = counters.DirCount;

            result.FileTreeSummary = fileTree;
            result.TotalFilesScanned = fileCount;
            result.TotalDirectoriesScanned = dirCount;
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Error, $"文件树扫描失败: {ex.Message}");
            throw;
        }
    }

    private async Task ScanDirectoryRecursiveAsync(
        string dirPath, 
        List<FileNodeSummary> fileTree, 
        ScanCounters counters, 
        int currentDepth, 
        int maxDepth, 
        CancellationToken cancellationToken)
    {
        if (currentDepth > maxDepth || cancellationToken.IsCancellationRequested)
            return;

        try
        {
            var dirInfo = new DirectoryInfo(dirPath);
            var dirNode = new FileNodeSummary
            {
                Path = dirPath,
                Type = FileNodeType.Directory,
                LastModified = dirInfo.LastWriteTime
            };

            counters.DirCount++;

            // 扫描文件
            foreach (var file in dirInfo.GetFiles())
            {
                if (cancellationToken.IsCancellationRequested) break;

                var fileNode = new FileNodeSummary
                {
                    Path = file.FullName,
                    Type = FileNodeType.File,
                    Size = file.Length,
                    LastModified = file.LastWriteTime,
                    Extension = file.Extension,
                    IsKey = IsKeyFile(file.Name)
                };

                dirNode.Children.Add(fileNode);
                counters.FileCount++;
            }

            // 递归扫描子目录
            foreach (var subDir in dirInfo.GetDirectories())
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (ShouldSkipDirectory(subDir.Name)) continue;

                await ScanDirectoryRecursiveAsync(
                    subDir.FullName, 
                    dirNode.Children, 
                    counters, 
                    currentDepth + 1, 
                    maxDepth, 
                    cancellationToken);
            }

            fileTree.Add(dirNode);
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Warning, $"扫描目录失败 {dirPath}: {ex.Message}");
        }
    }

    private bool IsKeyFile(string fileName)
    {
        var keyFiles = new[]
        {
            "package.json", "package-lock.json", "yarn.lock", "pnpm-lock.yaml",
            "*.csproj", "*.sln", "global.json",
            "appsettings.json", "launchSettings.json",
            "Program.cs", "Startup.cs", "index.js", "index.ts",
            "webpack.config.js", "vite.config.js", "next.config.js",
            "tsconfig.json", "README.md", ".env"
        };

        return keyFiles.Any(pattern => 
            pattern.Contains("*") ? 
                fileName.EndsWith(pattern.Substring(1)) : 
                fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldSkipDirectory(string dirName)
    {
        var skipDirs = new[]
        {
            "node_modules", "bin", "obj", "dist", "build", ".git", ".vs", ".idea", 
            "packages", "target", ".cache", "coverage", "__pycache__"
        };

        return skipDirs.Contains(dirName, StringComparer.OrdinalIgnoreCase);
    }

    private async Task PerformLanguageSpecificAnalysisAsync(ProjectScanRequest request, ProjectScanResult result, CancellationToken cancellationToken)
    {
        foreach (var language in result.LanguagesDetected)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var analyzer = _languageAnalyzers.FirstOrDefault(a => a.LanguageType == language);
            if (analyzer == null) continue;

            try
            {
                StatusMessage = $"分析 {language} 项目...";
                var languageResult = await analyzer.ScanAsync(request, cancellationToken);

                // 合并结果
                MergeLanguageResults(result, languageResult);
                
                AddLog(LogLevel.Info, $"{language} 项目分析完成");
            }
            catch (Exception ex)
            {
                AddLog(LogLevel.Error, $"{language} 项目分析失败: {ex.Message}");
            }
        }
    }

    private void MergeLanguageResults(ProjectScanResult result, LanguageSpecificResult languageResult)
    {
        // 合并环境信息
        if (string.IsNullOrEmpty(result.Environments.DotNetSdk) && !string.IsNullOrEmpty(languageResult.Environment.DotNetSdk))
        {
            result.Environments.DotNetSdk = languageResult.Environment.DotNetSdk;
        }

        if (!result.Environments.TargetFrameworks.Any())
        {
            result.Environments.TargetFrameworks.AddRange(languageResult.Environment.TargetFrameworks);
        }

        if (string.IsNullOrEmpty(result.Environments.NodeVersion) && !string.IsNullOrEmpty(languageResult.Environment.NodeVersion))
        {
            result.Environments.NodeVersion = languageResult.Environment.NodeVersion;
        }

        if (!result.Environments.PackageManager.HasValue && languageResult.Environment.PackageManager.HasValue)
        {
            result.Environments.PackageManager = languageResult.Environment.PackageManager;
        }

        // 合并依赖
        result.Dependencies.CSharp.AddRange(languageResult.Dependencies.CSharp);
        result.Dependencies.Node.AddRange(languageResult.Dependencies.Node);

        // 合并运行信息
        result.RunInfo.Ports.AddRange(languageResult.RunInfo.Ports);
        result.RunInfo.Urls.AddRange(languageResult.RunInfo.Urls);
        result.RunInfo.StartCommands.AddRange(languageResult.RunInfo.StartCommands);
        result.RunInfo.EnvironmentFiles.AddRange(languageResult.RunInfo.EnvironmentFiles);

        // 合并结构图
        result.StructureGraph.Nodes.AddRange(languageResult.StructureNodes);
        result.StructureGraph.Edges.AddRange(languageResult.StructureEdges);
    }

    private async Task FinalizeResultsAsync(ProjectScanResult result, CancellationToken cancellationToken)
    {
        // 去重和排序
        result.RunInfo.Ports = result.RunInfo.Ports
            .GroupBy(p => p.Value)
            .Select(g => g.OrderByDescending(p => p.Confidence).First())
            .OrderByDescending(p => p.Confidence)
            .ToList();

        result.RunInfo.Urls = result.RunInfo.Urls
            .GroupBy(u => u.Value)
            .Select(g => g.OrderByDescending(u => u.Confidence).First())
            .OrderByDescending(u => u.Confidence)
            .ToList();

        // 添加扫描完成日志
        result.Logs.Add(new LogItem
        {
            Level = LogLevel.Info,
            Message = "项目扫描完成",
            Timestamp = DateTimeOffset.Now
        });
    }

    private async Task CheckAIServiceStatusAsync()
    {
        try
        {
            AiServiceStatus = await _aiInsightsService.CheckServiceStatusAsync();
            
            if (AiServiceStatus?.IsAvailable == true)
            {
                AddLog(LogLevel.Info, "AI 服务可用");
            }
            else
            {
                AddLog(LogLevel.Warning, $"AI 服务不可用: {AiServiceStatus?.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Error, $"检查 AI 服务状态失败: {ex.Message}");
        }
    }

    private void UpdateUICollections(ProjectScanResult result)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // 更新文件树
            FileTree.Clear();
            foreach (var item in result.FileTreeSummary.Take(100)) // 限制显示数量
            {
                FileTree.Add(item);
            }

            // 更新依赖
            CSharpDependencies.Clear();
            foreach (var dep in result.Dependencies.CSharp)
            {
                CSharpDependencies.Add(dep);
            }

            NodeDependencies.Clear();
            foreach (var dep in result.Dependencies.Node)
            {
                NodeDependencies.Add(dep);
            }

            // 更新运行信息
            Ports.Clear();
            foreach (var port in result.RunInfo.Ports)
            {
                Ports.Add(port);
            }

            Urls.Clear();
            foreach (var url in result.RunInfo.Urls)
            {
                Urls.Add(url);
            }

            // 更新结构图
            StructureNodes.Clear();
            {
                var nodes = result.StructureGraph?.Nodes ?? new List<GraphNode>();
                var count = nodes.Count;
                if (count > 0)
                {
                    // 当未提供布局坐标时，按网格进行简单布局，避免所有节点重叠在 (0,0)
                    int columns = (int)Math.Ceiling(Math.Sqrt(count));
                    double cellW = 180;
                    double cellH = 120;
                    double margin = 20;

                    for (int i = 0; i < count; i++)
                    {
                        var node = nodes[i];
                        if (node.Position == null)
                        {
                            node.Position = new NodePosition
                            {
                                X = margin + (i % columns) * cellW,
                                Y = margin + (i / columns) * cellH
                            };
                        }

                        StructureNodes.Add(node);
                    }
                }
            }

            StructureEdges.Clear();
            foreach (var edge in result.StructureGraph.Edges)
            {
                StructureEdges.Add(edge);
            }

            // 更新日志和警告
            Logs.Clear();
            foreach (var log in result.Logs)
            {
                Logs.Add(log);
            }

            Warnings.Clear();
            foreach (var warning in result.Warnings)
            {
                Warnings.Add(warning);
            }
        });
    }

    private void UpdateAICollections(AIInsights insights)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // 更新AI模块
            AIModules.Clear();
            foreach (var module in insights.Modules)
            {
                AIModules.Add(module);
            }

            // 更新AI风险
            AIRisks.Clear();
            foreach (var risk in insights.PotentialRisks)
            {
                AIRisks.Add(risk);
            }

            // 更新AI建议
            AIAdvice.Clear();
            foreach (var advice in insights.DependencyAdvice)
            {
                AIAdvice.Add(advice);
            }
        });
    }

    private void ClearResults()
    {
        HasResults = false;
        ScanResult = null;
        AiInsights = null;
        AiContentPreview = null;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            FileTree.Clear();
            CSharpDependencies.Clear();
            NodeDependencies.Clear();
            Ports.Clear();
            Urls.Clear();
            Logs.Clear();
            Warnings.Clear();
            StructureNodes.Clear();
            StructureEdges.Clear();
            AIModules.Clear();
            AIRisks.Clear();
            AIAdvice.Clear();
        });
    }

    private void UpdateCommandStates()
    {
        ((AsyncRelayCommand)StartScanCommand).NotifyCanExecuteChanged();
        ((RelayCommand)CancelScanCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)GenerateAIInsightsCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)ExportSvgCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)ExportJsonCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)PreviewAIContentCommand).NotifyCanExecuteChanged();
    }

    private void AddLog(LogLevel level, string message)
    {
        var logItem = new LogItem
        {
            Level = level,
            Message = message,
            Timestamp = DateTimeOffset.Now,
            Source = "ProjectAnalyzer"
        };

        // 在UI线程上添加日志
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            Logs.Insert(0, logItem); // 最新的在前面
            
            // 限制日志数量
            while (Logs.Count > 1000)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }
        });
    }

    #endregion
}