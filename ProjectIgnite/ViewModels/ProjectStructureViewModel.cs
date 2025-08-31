using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectIgnite.Models;
using ProjectIgnite.Services;

namespace ProjectIgnite.ViewModels
{
    /// <summary>
    /// 项目结构视图模型
    /// 负责管理项目结构的显示和交互逻辑，支持图表生成功能
    /// </summary>
    public partial class ProjectStructureViewModel : ViewModelBase
    {
        private readonly IDiagramService _diagramService;
        private readonly IGitHubService _gitHubService;
        private readonly IAIService _aiService;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private string _title = "项目结构";

        [ObservableProperty]
        private string _description = "这里将显示项目的文件结构和组织架构";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _emptyStateMessage = "暂无项目结构数据";

        // 图表功能相关属性
        [ObservableProperty]
        private bool _isDiagramMode = false;

        [ObservableProperty]
        private string _repositoryUrl = string.Empty;

        [ObservableProperty]
        private string _customInstructions = string.Empty;

        [ObservableProperty]
        private DiagramModel? _currentDiagram;

        [ObservableProperty]
        private ProjectIgnite.Services.GenerationProgress _generationProgress = new();

        [ObservableProperty]
        private bool _isGenerating = false;

        [ObservableProperty]
        private string _mermaidCode = string.Empty;

        [ObservableProperty]
        private string _diagramExplanation = string.Empty;

        [ObservableProperty]
        private RepositoryInfo? _repositoryInfo;

        [ObservableProperty]
        private bool _showDiagramControls = false;

        [ObservableProperty]
        private string _modificationInstructions = string.Empty;

        /// <summary>
        /// 项目结构树节点集合
        /// </summary>
        public ObservableCollection<object> StructureItems { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public IRelayCommand RefreshCommand { get; }

        /// <summary>
        /// 展开所有节点命令
        /// </summary>
        public IRelayCommand ExpandAllCommand { get; }

        /// <summary>
        /// 折叠所有节点命令
        /// </summary>
        public IRelayCommand CollapseAllCommand { get; }

        // 图表功能命令
        /// <summary>
        /// 切换模式命令（结构视图/图表视图）
        /// </summary>
        public IRelayCommand ToggleModeCommand { get; }

        /// <summary>
        /// 生成图表命令
        /// </summary>
        public IRelayCommand GenerateDiagramCommand { get; }

        /// <summary>
        /// 取消生成命令
        /// </summary>
        public IRelayCommand CancelGenerationCommand { get; }

        /// <summary>
        /// 修改图表命令
        /// </summary>
        public IRelayCommand ModifyDiagramCommand { get; }

        /// <summary>
        /// 导出图表命令
        /// </summary>
        public IRelayCommand<string> ExportDiagramCommand { get; }

        /// <summary>
        /// 复制图表代码命令
        /// </summary>
        public IRelayCommand CopyDiagramCodeCommand { get; }

        /// <summary>
        /// 清除图表命令
        /// </summary>
        public IRelayCommand ClearDiagramCommand { get; }

        public ProjectStructureViewModel(
            IDiagramService diagramService,
            IGitHubService gitHubService,
            IAIService aiService)
        {
            _diagramService = diagramService;
            _gitHubService = gitHubService;
            _aiService = aiService;
            
            StructureItems = new ObservableCollection<object>();
            
            // 初始化命令
            RefreshCommand = new RelayCommand(RefreshStructure);
            ExpandAllCommand = new RelayCommand(ExpandAll);
            CollapseAllCommand = new RelayCommand(CollapseAll);
            
            // 初始化图表功能命令
            ToggleModeCommand = new RelayCommand(ToggleMode);
            GenerateDiagramCommand = new RelayCommand(GenerateDiagram, CanGenerateDiagram);
            CancelGenerationCommand = new RelayCommand(CancelGeneration, () => IsGenerating);
            ModifyDiagramCommand = new RelayCommand(ModifyDiagram, CanModifyDiagram);
            ExportDiagramCommand = new RelayCommand<string>(ExportDiagram, CanExportDiagram);
            CopyDiagramCodeCommand = new RelayCommand(CopyDiagramCode, CanCopyDiagramCode);
            ClearDiagramCommand = new RelayCommand(ClearDiagram, CanClearDiagram);

            // 设置初始状态为非加载状态，确保空状态可见
            IsLoading = false;
            EmptyStateMessage = "点击刷新按钮加载项目结构";
            
            // 监听属性变化以更新命令状态
            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        private async void InitializeAsync()
        {
            await LoadStructureDataAsync();
        }

        /// <summary>
        /// 加载项目结构数据
        /// </summary>
        private async Task LoadStructureDataAsync()
        {
            IsLoading = true;
            
            try
            {
                // TODO: 实现项目结构数据加载逻辑
                // 这里暂时留空，等待后续实现
                await Task.Delay(100); // 模拟异步操作
                
                // 暂时清空集合，显示空状态
                StructureItems.Clear();
            }
            catch (Exception ex)
            {
                // TODO: 添加错误处理和日志记录
                EmptyStateMessage = $"加载项目结构时出错: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 刷新项目结构
        /// </summary>
        private async void RefreshStructure()
        {
            await LoadStructureDataAsync();
        }

        /// <summary>
        /// 展开所有节点
        /// </summary>
        private void ExpandAll()
        {
            // TODO: 实现展开所有节点的逻辑
        }

        /// <summary>
        /// 折叠所有节点
        /// </summary>
        private void CollapseAll()
        {
            // TODO: 实现折叠所有节点的逻辑
        }

        #region 图表功能方法

        /// <summary>
        /// 属性变化事件处理
        /// </summary>
        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 更新命令的可执行状态
            switch (e.PropertyName)
            {
                case nameof(RepositoryUrl):
                case nameof(IsGenerating):
                    GenerateDiagramCommand.NotifyCanExecuteChanged();
                    CancelGenerationCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(CurrentDiagram):
                case nameof(MermaidCode):
                    ModifyDiagramCommand.NotifyCanExecuteChanged();
                    ExportDiagramCommand.NotifyCanExecuteChanged();
                    CopyDiagramCodeCommand.NotifyCanExecuteChanged();
                    ClearDiagramCommand.NotifyCanExecuteChanged();
                    break;
            }
        }

        /// <summary>
        /// 切换模式（结构视图/图表视图）
        /// </summary>
        private void ToggleMode()
        {
            IsDiagramMode = !IsDiagramMode;
            Title = IsDiagramMode ? "架构图表" : "项目结构";
            Description = IsDiagramMode ? "生成和查看项目架构图表" : "这里将显示项目的文件结构和组织架构";
        }

        /// <summary>
        /// 生成图表
        /// </summary>
        private async void GenerateDiagram()
        {
            if (IsGenerating) return;

            try
            {
                IsGenerating = true;
                _cancellationTokenSource = new CancellationTokenSource();
                GenerationProgress.Reset();
                ShowDiagramControls = false;

                var progressReporter = new Progress<ProjectIgnite.Services.GenerationProgress>(progress => 
                {
                    GenerationProgress = progress;
                });

                var result = await _diagramService.GenerateDiagramAsync(
                    RepositoryUrl,
                    CustomInstructions,
                    progressReporter,
                    _cancellationTokenSource.Token);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.MermaidCode))
                {
                    // 创建 DiagramModel 对象
                    CurrentDiagram = new DiagramModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        RepositoryUrl = RepositoryUrl,
                        MermaidCode = result.MermaidCode,
                        Explanation = result.Explanation ?? string.Empty,
                        ComponentMapping = result.ComponentMapping != null ? 
                            System.Text.Json.JsonSerializer.Serialize(result.ComponentMapping) : 
                            System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>()),
                        Title = $"架构图表",
                        Description = "项目架构图表",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = 1
                    };
                    MermaidCode = result.MermaidCode;
                    DiagramExplanation = result.Explanation ?? string.Empty;
                    RepositoryInfo = await _gitHubService.GetRepositoryInfoAsync(RepositoryUrl, _cancellationTokenSource.Token);
                    ShowDiagramControls = true;
                }
                else
                {
                    GenerationProgress.SetError(result.ErrorMessage ?? "生成图表失败");
                }
            }
            catch (OperationCanceledException)
            {
                GenerationProgress.SetCancelled();
            }
            catch (Exception ex)
            {
                GenerationProgress.SetError($"生成图表时发生错误: {ex.Message}");
            }
            finally
            {
                IsGenerating = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 取消生成
        /// </summary>
        private void CancelGeneration()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// 修改图表
        /// </summary>
        private async void ModifyDiagram()
        {
            if (CurrentDiagram == null || string.IsNullOrWhiteSpace(ModificationInstructions)) return;

            try
            {
                IsGenerating = true;
                _cancellationTokenSource = new CancellationTokenSource();
                GenerationProgress.Reset();
                GenerationProgress.Update(GenerationState.GeneratingDiagram, 50, "正在修改图表...");

                var modifiedCode = await _diagramService.ModifyDiagramAsync(
                    CurrentDiagram.MermaidCode,
                    ModificationInstructions,
                    _cancellationTokenSource.Token);

                if (!string.IsNullOrEmpty(modifiedCode))
                {
                    CurrentDiagram.MermaidCode = modifiedCode;
                    MermaidCode = modifiedCode;
                    ModificationInstructions = string.Empty; // 清空修改指令
                    GenerationProgress.Update(GenerationState.Completed, 100, "图表修改完成");
                }
                else
                {
                    GenerationProgress.SetError("修改图表失败");
                }
            }
            catch (OperationCanceledException)
            {
                GenerationProgress.SetCancelled();
            }
            catch (Exception ex)
            {
                GenerationProgress.SetError($"修改图表时发生错误: {ex.Message}");
            }
            finally
            {
                IsGenerating = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 导出图表
        /// </summary>
        private async void ExportDiagram(string? format)
        {
            if (CurrentDiagram == null || string.IsNullOrEmpty(format)) return;

            try
            {
                if (Enum.TryParse<ExportFormat>(format, true, out var exportFormat))
                {
                    await _diagramService.ExportDiagramAsync(CurrentDiagram.MermaidCode, exportFormat);
                }
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息给用户
                System.Diagnostics.Debug.WriteLine($"导出图表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 复制图表代码
        /// </summary>
        private async void CopyDiagramCode()
        {
            if (string.IsNullOrEmpty(MermaidCode)) return;

            try
            {
                // TODO: 实现复制到剪贴板功能
                // await Clipboard.SetTextAsync(MermaidCode);
                System.Diagnostics.Debug.WriteLine("图表代码已复制到剪贴板");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"复制失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除图表
        /// </summary>
        private void ClearDiagram()
        {
            CurrentDiagram = null;
            MermaidCode = string.Empty;
            DiagramExplanation = string.Empty;
            RepositoryInfo = null;
            ShowDiagramControls = false;
            GenerationProgress.Reset();
            ModificationInstructions = string.Empty;
        }

        #endregion

        #region 命令可执行状态判断

        /// <summary>
        /// 判断是否可以生成图表
        /// </summary>
        private bool CanGenerateDiagram()
        {
            return !IsGenerating && !string.IsNullOrWhiteSpace(RepositoryUrl);
        }

        /// <summary>
        /// 判断是否可以修改图表
        /// </summary>
        private bool CanModifyDiagram()
        {
            return !IsGenerating && CurrentDiagram != null && !string.IsNullOrWhiteSpace(ModificationInstructions);
        }

        /// <summary>
        /// 判断是否可以导出图表
        /// </summary>
        private bool CanExportDiagram(string? format)
        {
            return CurrentDiagram != null && !string.IsNullOrEmpty(format);
        }

        /// <summary>
        /// 判断是否可以复制图表代码
        /// </summary>
        private bool CanCopyDiagramCode()
        {
            return !string.IsNullOrEmpty(MermaidCode);
        }

        /// <summary>
        /// 判断是否可以清除图表
        /// </summary>
        private bool CanClearDiagram()
        {
            return CurrentDiagram != null;
        }

        #endregion

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}