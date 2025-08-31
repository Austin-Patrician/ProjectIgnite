using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectIgnite.Models;
using ProjectIgnite.DTOs;
using ProjectIgnite.Repositories;
using ProjectIgnite.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProjectIgnite.ViewModels
{
    /// <summary>
    /// 项目源管理视图模型
    /// </summary>
    public partial class ProjectSourceViewModel : ObservableObject
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IGitService _gitService;
        private readonly ILinguistService _linguistService;
        private CancellationTokenSource? _currentOperationCancellation;

        public ProjectSourceViewModel(
            IProjectRepository projectRepository,
            IGitService gitService,
            ILinguistService linguistService)
        {
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            _linguistService = linguistService ?? throw new ArgumentNullException(nameof(linguistService));

            Projects = new ObservableCollection<ProjectSourceInfo>();
            FilteredProjects = new ObservableCollection<ProjectSourceInfo>();
            AvailableLanguages = new ObservableCollection<string>();
            AvailableStatuses = new ObservableCollection<string>
            {
                "全部", "已完成", "克隆中", "分析中", "错误", "待处理"
            };

            // 初始化命令
            LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);
            AddProjectCommand = new AsyncRelayCommand(ShowAddProjectDialogAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshProjectsAsync);
            DeleteProjectCommand = new AsyncRelayCommand<ProjectSourceInfo>(DeleteProjectAsync);
            CloneProjectCommand = new AsyncRelayCommand<ProjectSourceInfo>(CloneProjectAsync);
            AnalyzeProjectCommand = new AsyncRelayCommand<ProjectSourceInfo>(AnalyzeProjectAsync);
            OpenProjectFolderCommand = new RelayCommand<ProjectSourceInfo>(OpenProjectFolder);
            ViewProjectDetailsCommand = new RelayCommand<ProjectSourceInfo>(ViewProjectDetails);
            SearchCommand = new AsyncRelayCommand(SearchProjectsAsync);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            CancelOperationCommand = new RelayCommand(CancelCurrentOperation);

            // 设置默认值
            SelectedStatus = "全部";
            IsLoading = false;
        }

        #region 属性

        /// <summary>
        /// 项目列表
        /// </summary>
        public ObservableCollection<ProjectSourceInfo> Projects { get; }

        /// <summary>
        /// 筛选后的项目列表
        /// </summary>
        public ObservableCollection<ProjectSourceInfo> FilteredProjects { get; }

        /// <summary>
        /// 可用语言列表
        /// </summary>
        public ObservableCollection<string> AvailableLanguages { get; }

        /// <summary>
        /// 可用状态列表
        /// </summary>
        public ObservableCollection<string> AvailableStatuses { get; }

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedStatus = "全部";

        [ObservableProperty]
        private string _selectedLanguage = string.Empty;

        [ObservableProperty]
        private ProjectSourceInfo? _selectedProject;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private ProjectStatistics? _statistics;

        [ObservableProperty]
        private bool _isOperationInProgress;

        [ObservableProperty]
        private string _currentOperation = string.Empty;

        [ObservableProperty]
        private int _operationProgress;

        #endregion

        #region 命令

        public IAsyncRelayCommand LoadProjectsCommand { get; }
        public IAsyncRelayCommand AddProjectCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<ProjectSourceInfo> DeleteProjectCommand { get; }
        public IAsyncRelayCommand<ProjectSourceInfo> CloneProjectCommand { get; }
        public IAsyncRelayCommand<ProjectSourceInfo> AnalyzeProjectCommand { get; }
        public IRelayCommand<ProjectSourceInfo> OpenProjectFolderCommand { get; }
        public IRelayCommand<ProjectSourceInfo> ViewProjectDetailsCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearSearchCommand { get; }
        public IRelayCommand CancelOperationCommand { get; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await LoadProjectsAsync();
                await LoadStatisticsAsync();
                await LoadAvailableLanguagesAsync();
            }
            catch (Exception ex)
            {
                ShowError($"初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加新项目
        /// </summary>
        public async Task<bool> AddProjectAsync(CloneRequest request)
        {
            try
            {
                IsOperationInProgress = true;
                CurrentOperation = "正在添加项目...";

                // 检查项目是否已存在
                var existingProject = await _projectRepository.GetProjectByGitUrlAsync(request.GitUrl);
                if (existingProject != null)
                {
                    ShowError("该Git仓库已经存在");
                    return false;
                }

                // 创建项目记录
                var project = new ProjectSource
                {
                    Name = request.ProjectName,
                    GitUrl = request.GitUrl,
                    LocalPath = System.IO.Path.Combine(request.TargetPath, request.ProjectName),
                    Description = request.Description,
                    Status = "pending",
                    CloneProgress = 0,
                    AnalysisProgress = 0
                };

                var projectId = await _projectRepository.CreateProjectAsync(project);
                project.Id = projectId;

                // 开始克隆
                if (request.AutoAnalyze)
                {
                    _ = Task.Run(async () =>
                    {
                        await CloneAndAnalyzeProjectAsync(project, request);
                    });
                }
                else
                {
                    _ = Task.Run(async () =>
                    {
                        await CloneProjectInternalAsync(project, request);
                    });
                }

                await LoadProjectsAsync();
                ShowStatus("项目添加成功，正在后台克隆...");
                return true;
            }
            catch (Exception ex)
            {
                ShowError($"添加项目失败: {ex.Message}");
                return false;
            }
            finally
            {
                IsOperationInProgress = false;
                CurrentOperation = string.Empty;
            }
        }

        #endregion

        #region 命令实现

        private async Task LoadProjectsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载项目列表...";

                var projects = await _projectRepository.GetAllProjectsAsync();
                
                Projects.Clear();
                foreach (var project in projects)
                {
                    Projects.Add(project);
                }

                await ApplyFiltersAsync();
                ShowStatus($"已加载 {projects.Count} 个项目");
            }
            catch (Exception ex)
            {
                ShowError($"加载项目列表失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ShowAddProjectDialogAsync()
        {
            // 这里应该显示添加项目对话框
            // 由于这是ViewModel，实际的UI显示逻辑应该在View中处理
            // 可以通过事件或消息机制来通知View显示对话框
            OnAddProjectRequested?.Invoke();
            await Task.CompletedTask;
        }

        private async Task RefreshProjectsAsync()
        {
            await LoadProjectsAsync();
            await LoadStatisticsAsync();
        }

        private async Task DeleteProjectAsync(ProjectSourceInfo? project)
        {
            if (project == null) return;

            try
            {
                IsOperationInProgress = true;
                CurrentOperation = "正在删除项目...";

                var success = await _projectRepository.DeleteProjectAsync(project.Id);
                if (success)
                {
                    Projects.Remove(project);
                    await ApplyFiltersAsync();
                    await LoadStatisticsAsync();
                    ShowStatus("项目删除成功");
                }
                else
                {
                    ShowError("删除项目失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"删除项目失败: {ex.Message}");
            }
            finally
            {
                IsOperationInProgress = false;
                CurrentOperation = string.Empty;
            }
        }

        private async Task CloneProjectAsync(ProjectSourceInfo? project)
        {
            if (project == null) return;

            try
            {
                var cloneRequest = new CloneRequest
                {
                    GitUrl = project.GitUrl,
                    ProjectName = project.Name,
                    TargetPath = System.IO.Path.GetDirectoryName(project.LocalPath) ?? string.Empty,
                    Description = project.Description,
                    AutoAnalyze = true
                };

                var projectEntity = new ProjectSource
                {
                    Id = project.Id,
                    Name = project.Name,
                    GitUrl = project.GitUrl,
                    LocalPath = project.LocalPath,
                    Description = project.Description,
                    Status = project.Status.ToString().ToLower(),
                    CloneProgress = (int)project.CloneProgress,
                    AnalysisProgress = (int)project.AnalysisProgress
                };

                _ = Task.Run(async () =>
                {
                    await CloneAndAnalyzeProjectAsync(projectEntity, cloneRequest);
                });

                ShowStatus("开始重新克隆项目...");
            }
            catch (Exception ex)
            {
                ShowError($"克隆项目失败: {ex.Message}");
            }
        }

        private async Task AnalyzeProjectAsync(ProjectSourceInfo? project)
        {
            if (project == null) return;

            try
            {
                _ = Task.Run(async () =>
                {
                    await AnalyzeProjectInternalAsync(project.Id, project.LocalPath);
                });

                ShowStatus("开始分析项目语言...");
            }
            catch (Exception ex)
            {
                ShowError($"分析项目失败: {ex.Message}");
            }
        }

        private void OpenProjectFolder(ProjectSourceInfo? project)
        {
            if (project == null || string.IsNullOrEmpty(project.LocalPath)) return;

            try
            {
                if (System.IO.Directory.Exists(project.LocalPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = project.LocalPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    ShowError("项目文件夹不存在");
                }
            }
            catch (Exception ex)
            {
                ShowError($"打开文件夹失败: {ex.Message}");
            }
        }

        private void ViewProjectDetails(ProjectSourceInfo? project)
        {
            if (project == null) return;

            SelectedProject = project;
            OnProjectDetailsRequested?.Invoke(project);
        }

        private async Task SearchProjectsAsync()
        {
            await ApplyFiltersAsync();
        }

        private void ClearSearch()
        {
            SearchKeyword = string.Empty;
            SelectedStatus = "全部";
            SelectedLanguage = string.Empty;
            _ = Task.Run(ApplyFiltersAsync);
        }

        private void CancelCurrentOperation()
        {
            _currentOperationCancellation?.Cancel();
            IsOperationInProgress = false;
            CurrentOperation = string.Empty;
            ShowStatus("操作已取消");
        }

        #endregion

        #region 私有方法

        private async Task ApplyFiltersAsync()
        {
            try
            {
                var keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim();
                var status = SelectedStatus == "全部" ? null : MapStatusToInternal(SelectedStatus);
                var language = string.IsNullOrWhiteSpace(SelectedLanguage) ? null : SelectedLanguage;

                var filteredProjects = await _projectRepository.SearchProjectsAsync(keyword, status, language);
                
                FilteredProjects.Clear();
                foreach (var project in filteredProjects)
                {
                    FilteredProjects.Add(project);
                }
            }
            catch (Exception ex)
            {
                ShowError($"筛选项目失败: {ex.Message}");
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                Statistics = await _projectRepository.GetProjectStatisticsAsync();
            }
            catch (Exception ex)
            {
                ShowError($"加载统计信息失败: {ex.Message}");
            }
        }

        private async Task LoadAvailableLanguagesAsync()
        {
            try
            {
                var languages = await _projectRepository.GetAllLanguagesAsync();
                
                AvailableLanguages.Clear();
                AvailableLanguages.Add("全部");
                foreach (var language in languages)
                {
                    AvailableLanguages.Add(language);
                }
            }
            catch (Exception ex)
            {
                ShowError($"加载语言列表失败: {ex.Message}");
            }
        }

        private async Task CloneAndAnalyzeProjectAsync(ProjectSource project, CloneRequest request)
        {
            _currentOperationCancellation = new CancellationTokenSource();
            
            try
            {
                // 更新状态为克隆中
                await _projectRepository.UpdateProjectStatusAsync(project.Id, "cloning");
                await RefreshProjectInList(project.Id);

                // 克隆项目
                var cloneResult = await _gitService.CloneRepositoryAsync(request, 
                    new Progress<CloneProgress>(async progress =>
                    {
                        await _projectRepository.UpdateProjectProgressAsync(project.Id, (int)progress.Progress);
                        await RefreshProjectInList(project.Id);
                    }), _currentOperationCancellation.Token);

                if (cloneResult.Success)
                {
                    // 克隆成功，开始分析
                    await _projectRepository.UpdateProjectStatusAsync(project.Id, "analyzing");
                    await _projectRepository.UpdateProjectProgressAsync(project.Id, 100, 0);
                    project.LastClonedAt = DateTime.Now;
                    await _projectRepository.UpdateProjectAsync(project);
                    await RefreshProjectInList(project.Id);

                    // 分析语言
                    await AnalyzeProjectInternalAsync(project.Id, cloneResult.LocalPath!);
                }
                else
                {
                    // 克隆失败
                    await _projectRepository.UpdateProjectStatusAsync(project.Id, "error", cloneResult.ErrorMessage);
                    await RefreshProjectInList(project.Id);
                }
            }
            catch (OperationCanceledException)
            {
                await _projectRepository.UpdateProjectStatusAsync(project.Id, "cancelled", "操作已取消");
                await RefreshProjectInList(project.Id);
            }
            catch (Exception ex)
            {
                await _projectRepository.UpdateProjectStatusAsync(project.Id, "error", ex.Message);
                await RefreshProjectInList(project.Id);
            }
        }

        private async Task CloneProjectInternalAsync(ProjectSource project, CloneRequest request)
        {
            _currentOperationCancellation = new CancellationTokenSource();
            
            try
            {
                await _projectRepository.UpdateProjectStatusAsync(project.Id, "cloning");
                await RefreshProjectInList(project.Id);

                var cloneResult = await _gitService.CloneRepositoryAsync(request, 
                    new Progress<CloneProgress>(async progress =>
                    {
                        await _projectRepository.UpdateProjectProgressAsync(project.Id, (int)progress.Progress);
                        await RefreshProjectInList(project.Id);
                    }), _currentOperationCancellation.Token);

                if (cloneResult.Success)
                {
                    await _projectRepository.UpdateProjectStatusAsync(project.Id, "completed");
                    await _projectRepository.UpdateProjectProgressAsync(project.Id, 100);
                    project.LastClonedAt = DateTime.Now;
                    await _projectRepository.UpdateProjectAsync(project);
                }
                else
                {
                    await _projectRepository.UpdateProjectStatusAsync(project.Id, "error", cloneResult.ErrorMessage);
                }
                
                await RefreshProjectInList(project.Id);
            }
            catch (OperationCanceledException)
            {
                await _projectRepository.UpdateProjectStatusAsync(project.Id, "cancelled", "操作已取消");
                await RefreshProjectInList(project.Id);
            }
            catch (Exception ex)
            {
                await _projectRepository.UpdateProjectStatusAsync(project.Id, "error", ex.Message);
                await RefreshProjectInList(project.Id);
            }
        }

        private async Task AnalyzeProjectInternalAsync(int projectId, string localPath)
        {
            try
            {
                await _projectRepository.UpdateProjectStatusAsync(projectId, "analyzing");
                await _projectRepository.UpdateProjectProgressAsync(projectId, null, 0);
                await RefreshProjectInList(projectId);

                var analysisResult = await _linguistService.AnalyzeProjectAsync(localPath, 
                    new Progress<LinguistAnalysisResult>(async progress =>
                    {
                        await _projectRepository.UpdateProjectProgressAsync(projectId, null, progress.Progress);
                        await RefreshProjectInList(projectId);
                    }));

                if (analysisResult.Success)
                {
                    // 转换语言统计数据为LanguageDetail列表
                    var languageDetails = analysisResult.Languages.Select(kvp => new LanguageDetail
                    {
                        Name = kvp.Value.Language,
                        FileCount = kvp.Value.FileCount,
                        LineCount = kvp.Value.LineCount,
                        ByteCount = kvp.Value.ByteCount,
                        Percentage = (decimal)kvp.Value.Percentage,
                        Color = kvp.Value.Color
                    }).ToList();

                    // 保存分析结果
                    await _projectRepository.SaveLanguageAnalysisAsync(projectId, languageDetails);
                    await _projectRepository.UpdateProjectStatusAsync(projectId, "completed");
                    await _projectRepository.UpdateProjectProgressAsync(projectId, null, 100);
                }
                else
                {
                    await _projectRepository.UpdateProjectStatusAsync(projectId, "error", analysisResult.ErrorMessage);
                }
                
                await RefreshProjectInList(projectId);
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                await _projectRepository.UpdateProjectStatusAsync(projectId, "error", ex.Message);
                await RefreshProjectInList(projectId);
            }
        }

        private async Task RefreshProjectInList(int projectId)
        {
            try
            {
                var updatedProject = await _projectRepository.GetProjectByIdAsync(projectId);
                if (updatedProject != null)
                {
                    var existingProject = Projects.FirstOrDefault(p => p.Id == projectId);
                    if (existingProject != null)
                    {
                        var index = Projects.IndexOf(existingProject);
                        Projects[index] = updatedProject;
                    }

                    var existingFilteredProject = FilteredProjects.FirstOrDefault(p => p.Id == projectId);
                    if (existingFilteredProject != null)
                    {
                        var index = FilteredProjects.IndexOf(existingFilteredProject);
                        FilteredProjects[index] = updatedProject;
                    }
                }
            }
            catch
            {
                // 忽略刷新错误
            }
        }

        private string? MapStatusToInternal(string displayStatus)
        {
            return displayStatus switch
            {
                "已完成" => "completed",
                "克隆中" => "cloning",
                "分析中" => "analyzing",
                "错误" => "error",
                "待处理" => "pending",
                _ => null
            };
        }

        private void ShowStatus(string message)
        {
            StatusMessage = message;
            HasError = false;
            ErrorMessage = string.Empty;
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            StatusMessage = string.Empty;
        }

        #endregion

        #region 事件

        /// <summary>
        /// 请求添加项目事件
        /// </summary>
        public event Action? OnAddProjectRequested;

        /// <summary>
        /// 请求查看项目详情事件
        /// </summary>
        public event Action<ProjectSourceInfo>? OnProjectDetailsRequested;

        #endregion
    }


}