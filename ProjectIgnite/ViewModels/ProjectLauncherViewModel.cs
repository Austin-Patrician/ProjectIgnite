using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectIgnite.DTOs;
using ProjectIgnite.Models;
using ProjectIgnite.Services;
using ProjectIgnite.Repositories;
using System;
using System.Collections.Generic;

namespace ProjectIgnite.ViewModels
{
    /// <summary>
    /// Project Launcher 页面视图模型
    /// </summary>
    public partial class ProjectLauncherViewModel : ViewModelBase
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectDetectionService _projectDetectionService;
        private readonly IPortManagementService _portManagementService;
        private readonly IProcessManagementService _processManagementService;

        [ObservableProperty]
        private ObservableCollection<ProjectLauncherProjectSourceViewModel> _projectSources = new();

        [ObservableProperty]
        private ObservableCollection<LaunchedProjectViewModel> _runningProjects = new();

        [ObservableProperty]
        private ObservableCollection<PortAllocationViewModel> _portAllocations = new();

        [ObservableProperty]
        private ObservableCollection<string> _projectLogs = new();

        [ObservableProperty]
        private ProjectLauncherProjectSourceViewModel? _selectedProject;

        [ObservableProperty]
        private LaunchedProjectViewModel? _selectedRunningProject;

        [ObservableProperty]
        private string _selectedEnvironment = "Development";

        [ObservableProperty]
        private int? _customPort;

        [ObservableProperty]
        private string _filterText = string.Empty;

        [ObservableProperty]
        private string _selectedProjectType = "All";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isProjectsLoading;

        [ObservableProperty]
        private bool _isRunningProjectsLoading;

        [ObservableProperty]
        private bool _isPortsLoading;

        [ObservableProperty]
        private string _statusMessage = "就绪";

        public ObservableCollection<string> Environments { get; } = new() { "Development", "Staging", "Production" };
        public ObservableCollection<string> ProjectTypes { get; } = new() { "All", "DotNet", "NodeJs", "Python", "Docker", "Java" };

        public ICommand LoadProjectsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand StartProjectCommand { get; }
        public ICommand StopProjectCommand { get; }
        public ICommand RestartProjectCommand { get; }
        public ICommand ConfigureProjectCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand CleanupPortsCommand { get; }

        public ProjectLauncherViewModel(
            IProjectRepository projectRepository,
            IProjectDetectionService projectDetectionService,
            IPortManagementService portManagementService,
            IProcessManagementService processManagementService)
        {
            _projectRepository = projectRepository;
            _projectDetectionService = projectDetectionService;
            _portManagementService = portManagementService;
            _processManagementService = processManagementService;

            // 初始化命令
            LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAllDataAsync);
            StartProjectCommand = new AsyncRelayCommand(StartSelectedProjectAsync, () => SelectedProject != null);
            StopProjectCommand = new AsyncRelayCommand<LaunchedProjectViewModel>(StopProjectAsync, p => p != null);
            RestartProjectCommand = new AsyncRelayCommand<LaunchedProjectViewModel>(RestartProjectAsync, p => p != null);
            ConfigureProjectCommand = new RelayCommand<ProjectLauncherProjectSourceViewModel>(ConfigureProject, p => p != null);
            ViewLogsCommand = new AsyncRelayCommand<LaunchedProjectViewModel>(ViewProjectLogsAsync, p => p != null);
            ClearLogsCommand = new RelayCommand(ClearLogs);
            CleanupPortsCommand = new AsyncRelayCommand(CleanupUnusedPortsAsync);

            // 订阅进程事件
            _processManagementService.ProcessOutput += OnProcessOutput;
            _processManagementService.ProcessStatusChanged += OnProcessStatusChanged;

            // 页面初始化时立即显示，不阻塞UI
            StatusMessage = "欢迎使用 Project Launcher";
            
            // 添加延迟后自动加载数据，让页面先显示
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // 让UI先渲染
                await InitializeDataAsync();
            });
        }

        /// <summary>
        /// 初始化数据 - 并行加载各个模块
        /// </summary>
        private async Task InitializeDataAsync()
        {
            try
            {
                StatusMessage = "正在加载数据...";

                // 并行加载各个模块的数据，不阻塞UI
                var loadProjectsTask = LoadProjectsInternalAsync();
                var loadRunningProjectsTask = LoadRunningProjectsInternalAsync();
                var loadPortAllocationsTask = LoadPortAllocationsInternalAsync();

                // 等待所有加载任务完成
                await Task.WhenAll(loadProjectsTask, loadRunningProjectsTask, loadPortAllocationsTask);

                StatusMessage = $"数据加载完成 - 项目: {ProjectSources.Count}, 运行中: {RunningProjects.Count}, 端口: {PortAllocations.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"数据加载失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 加载项目列表 - 对外接口
        /// </summary>
        private async Task LoadProjectsAsync()
        {
            await LoadProjectsInternalAsync();
        }

        /// <summary>
        /// 内部加载项目列表方法
        /// </summary>
        private async Task LoadProjectsInternalAsync()
        {
            try
            {
                IsProjectsLoading = true;

                // 使用 IProjectRepository 获取已完成的项目
                var projects = await _projectRepository.SearchProjectsAsync(status: "Completed");

                var projectViewModels = new List<ProjectLauncherProjectSourceViewModel>();

                foreach (var project in projects)
                {
                    var projectType = await _projectDetectionService.DetectProjectTypeAsync(project.LocalPath);
                    
                    // TODO: 需要添加获取项目配置的方法到 IProjectRepository
                    // 暂时创建空配置列表，后续需要完善
                    var configurations = new List<ProjectConfiguration>();

                    var viewModel = new ProjectLauncherProjectSourceViewModel
                    {
                        Id = project.Id,
                        Name = project.Name,
                        LocalPath = project.LocalPath,
                        ProjectType = projectType,
                        PrimaryLanguage = project.PrimaryLanguage ?? "Unknown",
                        Configurations = new ObservableCollection<ProjectConfiguration>(configurations),
                        Status = "Ready"
                    };
                    
                    projectViewModels.Add(viewModel);
                }

                ProjectSources = new ObservableCollection<ProjectLauncherProjectSourceViewModel>(projectViewModels);
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载项目失败: {ex.Message}";
            }
            finally
            {
                IsProjectsLoading = false;
            }
        }

        /// <summary>
        /// 加载运行中的项目 - 对外接口
        /// </summary>
        private async Task LoadRunningProjectsAsync()
        {
            await LoadRunningProjectsInternalAsync();
        }

        /// <summary>
        /// 内部加载运行中项目方法
        /// </summary>
        private async Task LoadRunningProjectsInternalAsync()
        {
            try
            {
                IsRunningProjectsLoading = true;

                var runningProjects = await _processManagementService.GetRunningProjectsAsync();
                var viewModels = runningProjects.Select(p => new LaunchedProjectViewModel
                {
                    Id = p.Id,
                    ProjectName = p.ProjectName,
                    ProjectType = p.ProjectType,
                    Status = p.Status,
                    CurrentPort = p.CurrentPort,
                    CurrentEnvironment = p.CurrentEnvironment ?? "Unknown",
                    StartedAt = p.StartedAt,
                    ProcessId = p.ProcessId
                }).ToList();

                RunningProjects = new ObservableCollection<LaunchedProjectViewModel>(viewModels);
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载运行中项目失败: {ex.Message}";
            }
            finally
            {
                IsRunningProjectsLoading = false;
            }
        }

        /// <summary>
        /// 加载端口分配信息 - 对外接口
        /// </summary>
        private async Task LoadPortAllocationsAsync()
        {
            await LoadPortAllocationsInternalAsync();
        }

        /// <summary>
        /// 内部加载端口分配信息方法
        /// </summary>
        private async Task LoadPortAllocationsInternalAsync()
        {
            try
            {
                IsPortsLoading = true;

                var allocations = await _portManagementService.GetActivePortAllocationsAsync();

                var viewModels = allocations.Select(a => new PortAllocationViewModel
                {
                    Port = a.Port,
                    Status = a.Status,
                    ProjectName = a.ProjectSource?.Name ?? "Unknown",
                    Description = a.Description ?? "",
                    LastUsedAt = a.LastUsedAt
                }).ToList();

                PortAllocations = new ObservableCollection<PortAllocationViewModel>(viewModels);
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载端口信息失败: {ex.Message}";
            }
            finally
            {
                IsPortsLoading = false;
            }
        }

        /// <summary>
        /// 刷新所有数据
        /// </summary>
        private async Task RefreshAllDataAsync()
        {
            StatusMessage = "正在刷新数据...";
            await InitializeDataAsync();
        }

        /// <summary>
        /// 启动选中的项目
        /// </summary>
        private async Task StartSelectedProjectAsync()
        {
            if (SelectedProject == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"正在启动项目 {SelectedProject.Name}...";

                // 获取配置
                var config = SelectedProject.Configurations.FirstOrDefault(c => c.Environment == SelectedEnvironment);
                if (config == null)
                {
                    StatusMessage = $"未找到环境 {SelectedEnvironment} 的配置";
                    return;
                }

                // 设置自定义端口
                if (CustomPort.HasValue)
                {
                    config.DefaultPort = CustomPort.Value;
                }

                // 获取项目源
                var projectSourceInfo = await _projectRepository.GetProjectByIdAsync(SelectedProject.Id);
                if (projectSourceInfo == null)
                {
                    StatusMessage = "项目源不存在";
                    return;
                }

                // 转换为 ProjectSource 模型
                var projectSource = new ProjectSource
                {
                    Id = projectSourceInfo.Id,
                    Name = projectSourceInfo.Name,
                    LocalPath = projectSourceInfo.LocalPath,
                    GitUrl = projectSourceInfo.GitUrl,
                    PrimaryLanguage = projectSourceInfo.PrimaryLanguage,
                    Description = projectSourceInfo.Description,
                    Status = projectSourceInfo.Status.ToString().ToLower(),
                    LastClonedAt = projectSourceInfo.LastClonedAt
                };

                // 启动项目
                var launchedProject = await _processManagementService.StartProjectAsync(projectSource, config);
                
                // 更新UI
                var viewModel = new LaunchedProjectViewModel
                {
                    Id = launchedProject.Id,
                    ProjectName = launchedProject.ProjectName,
                    ProjectType = launchedProject.ProjectType,
                    Status = launchedProject.Status,
                    CurrentPort = launchedProject.CurrentPort,
                    CurrentEnvironment = launchedProject.CurrentEnvironment ?? "Unknown",
                    StartedAt = launchedProject.StartedAt,
                    ProcessId = launchedProject.ProcessId
                };

                RunningProjects.Add(viewModel);
                SelectedProject.Status = "Running";

                StatusMessage = $"项目 {SelectedProject.Name} 启动成功，端口: {launchedProject.CurrentPort}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"启动项目失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 停止项目
        /// </summary>
        private async Task StopProjectAsync(LaunchedProjectViewModel? project)
        {
            if (project == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"正在停止项目 {project.ProjectName}...";

                var success = await _processManagementService.StopProjectAsync(project.Id);
                
                if (success)
                {
                    RunningProjects.Remove(project);
                    
                    // 更新对应的项目源状态
                    var sourceProject = ProjectSources.FirstOrDefault(p => p.Name == project.ProjectName);
                    if (sourceProject != null)
                    {
                        sourceProject.Status = "Ready";
                    }

                    StatusMessage = $"项目 {project.ProjectName} 已停止";
                }
                else
                {
                    StatusMessage = $"停止项目 {project.ProjectName} 失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"停止项目失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 重启项目
        /// </summary>
        private async Task RestartProjectAsync(LaunchedProjectViewModel? project)
        {
            if (project == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"正在重启项目 {project.ProjectName}...";

                var success = await _processManagementService.RestartProjectAsync(project.Id);
                
                if (success)
                {
                    project.Status = "Running";
                    StatusMessage = $"项目 {project.ProjectName} 重启成功";
                }
                else
                {
                    StatusMessage = $"重启项目 {project.ProjectName} 失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"重启项目失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 配置项目
        /// </summary>
        private void ConfigureProject(ProjectLauncherProjectSourceViewModel? project)
        {
            if (project == null) return;
            
            // TODO: 打开配置对话框
            StatusMessage = $"配置项目 {project.Name}";
        }

        /// <summary>
        /// 查看项目日志
        /// </summary>
        private async Task ViewProjectLogsAsync(LaunchedProjectViewModel? project)
        {
            if (project == null) return;

            try
            {
                var logs = await _processManagementService.GetProjectLogsAsync(project.Id, 50);
                ProjectLogs = new ObservableCollection<string>(logs);
                SelectedRunningProject = project;
                StatusMessage = $"已加载项目 {project.ProjectName} 的日志";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载日志失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        private void ClearLogs()
        {
            ProjectLogs.Clear();
            StatusMessage = "日志已清空";
        }

        /// <summary>
        /// 清理未使用的端口
        /// </summary>
        private async Task CleanupUnusedPortsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在清理未使用的端口...";

                var cleanedCount = await _portManagementService.CleanupUnusedPortAllocationsAsync();
                await LoadPortAllocationsAsync();

                StatusMessage = $"已清理 {cleanedCount} 个未使用的端口分配";
            }
            catch (Exception ex)
            {
                StatusMessage = $"清理端口失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 处理进程输出事件
        /// </summary>
        private void OnProcessOutput(object? sender, ProcessOutputEventArgs e)
        {
            if (SelectedRunningProject?.Id == e.LaunchedProjectId)
            {
                ProjectLogs.Add($"[{e.Timestamp:HH:mm:ss}] {e.Output}");
                
                // 限制日志数量
                while (ProjectLogs.Count > 1000)
                {
                    ProjectLogs.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 处理进程状态变化事件
        /// </summary>
        private void OnProcessStatusChanged(object? sender, ProcessStatusEventArgs e)
        {
            var project = RunningProjects.FirstOrDefault(p => p.Id == e.LaunchedProjectId);
            if (project != null)
            {
                project.Status = e.NewStatus;
                
                if (!string.IsNullOrEmpty(e.ErrorMessage))
                {
                    StatusMessage = $"项目 {project.ProjectName}: {e.ErrorMessage}";
                }
            }
        }
    }

    /// <summary>
    /// Project Launcher 专用的项目源视图模型
    /// </summary>
    public partial class ProjectLauncherProjectSourceViewModel : ObservableObject
    {
        [ObservableProperty] private int _id;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _localPath = string.Empty;
        [ObservableProperty] private string _projectType = string.Empty;
        [ObservableProperty] private string _primaryLanguage = string.Empty;
        [ObservableProperty] private string _status = string.Empty;
        [ObservableProperty] private ObservableCollection<ProjectConfiguration> _configurations = new();
    }

    /// <summary>
    /// 启动项目视图模型
    /// </summary>
    public partial class LaunchedProjectViewModel : ObservableObject
    {
        [ObservableProperty] private int _id;
        [ObservableProperty] private string _projectName = string.Empty;
        [ObservableProperty] private string _projectType = string.Empty;
        [ObservableProperty] private string _status = string.Empty;
        [ObservableProperty] private int? _currentPort;
        [ObservableProperty] private string _currentEnvironment = string.Empty;
        [ObservableProperty] private DateTime? _startedAt;
        [ObservableProperty] private int? _processId;
    }

    /// <summary>
    /// 端口分配视图模型
    /// </summary>
    public partial class PortAllocationViewModel : ObservableObject
    {
        [ObservableProperty] private int _port;
        [ObservableProperty] private string _status = string.Empty;
        [ObservableProperty] private string _projectName = string.Empty;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private DateTime? _lastUsedAt;
    }
}
