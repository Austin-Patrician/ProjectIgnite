using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectIgnite.DTOs;
using ProjectIgnite.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;

namespace ProjectIgnite.ViewModels
{
    public class AddProjectDialogViewModel : INotifyPropertyChanged
    {
        private readonly IGitService _gitService;
        private string _gitUrl = string.Empty;
        private string _projectName = string.Empty;
        private string _localPath = string.Empty;
        private string _selectedBranch = string.Empty;
        private string _description = string.Empty;
        private bool _isShallowClone = true;
        private bool _autoAnalyze = true;
        private bool _overwriteExisting = false;
        private bool _isValidating = false;
        private bool _isValid = false;
        private string _validationMessage = string.Empty;
        private GitRepositoryInfo? _repositoryInfo;
        private bool _isLoadingBranches = false;

        public AddProjectDialogViewModel(IGitService gitService)
        {
            _gitService = gitService;
            AvailableBranches = new ObservableCollection<string>();
            
            // 初始化命令
            ValidateRepositoryCommand = new AsyncRelayCommand(ValidateGitUrlAsync, () => !string.IsNullOrWhiteSpace(GitUrl) && !IsValidating);
            BrowseFolderCommand = new RelayCommand(() => RequestBrowseFolder?.Invoke());
            AddProjectCommand = new AsyncRelayCommand(ConfirmAsync, () => CanConfirm);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
            
            // 设置默认本地路径基目录
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ProjectIgnite");
            LocalPath = baseDir;
        }

        #region Properties

        public string GitUrl
        {
            get => _gitUrl;
            set
            {
                if (SetProperty(ref _gitUrl, value))
                {
                    OnGitUrlChanged();
                    // 通知验证命令状态更新
                    ((AsyncRelayCommand)ValidateRepositoryCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)AddProjectCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (SetProperty(ref _projectName, value))
                {
                    // 通知添加项目命令状态更新
                    ((AsyncRelayCommand)AddProjectCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string LocalPath
        {
            get => _localPath;
            set
            {
                if (SetProperty(ref _localPath, value))
                {
                    UpdateProjectNameFromPath();
                    // 通知添加项目命令状态更新
                    ((AsyncRelayCommand)AddProjectCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string SelectedBranch
        {
            get => _selectedBranch;
            set => SetProperty(ref _selectedBranch, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsShallowClone
        {
            get => _isShallowClone;
            set => SetProperty(ref _isShallowClone, value);
        }

        public bool AutoAnalyze
        {
            get => _autoAnalyze;
            set => SetProperty(ref _autoAnalyze, value);
        }

        public bool OverwriteExisting
        {
            get => _overwriteExisting;
            set => SetProperty(ref _overwriteExisting, value);
        }

        public bool IsValidating
        {
            get => _isValidating;
            set
            {
                if (SetProperty(ref _isValidating, value))
                {
                    OnPropertyChanged(nameof(CanValidate));
                    // 通知验证命令状态更新
                    ((AsyncRelayCommand)ValidateRepositoryCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (SetProperty(ref _isValid, value))
                {
                    OnPropertyChanged(nameof(CanConfirm));
                    // 通知添加项目命令状态更新
                    ((AsyncRelayCommand)AddProjectCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                if (SetProperty(ref _validationMessage, value))
                {
                    OnPropertyChanged(nameof(HasValidationMessage));
                }
            }
        }

        public GitRepositoryInfo? RepositoryInfo
        {
            get => _repositoryInfo;
            set
            {
                if (SetProperty(ref _repositoryInfo, value))
                {
                    OnRepositoryInfoChanged();
                    OnPropertyChanged(nameof(HasRepositoryInfo));
                    OnPropertyChanged(nameof(HasRepositoryDescription));
                    OnPropertyChanged(nameof(HasRepositoryDefaultBranch));
                    OnPropertyChanged(nameof(HasRepositoryLanguage));
                    OnPropertyChanged(nameof(RepositoryDefaultBranchText));
                    OnPropertyChanged(nameof(RepositoryLanguageText));
                }
            }
        }

        public ObservableCollection<string> AvailableBranches { get; }

        public bool IsLoadingBranches
        {
            get => _isLoadingBranches;
            set
            {
                if (SetProperty(ref _isLoadingBranches, value))
                {
                    OnPropertyChanged(nameof(BranchStatusText));
                }
            }
        }

        public string BranchStatusText => IsLoadingBranches ? "正在获取分支..." : "选择分支";

        public bool CanValidate => !string.IsNullOrWhiteSpace(GitUrl) && !IsValidating;
        public bool CanConfirm => IsValid && !string.IsNullOrWhiteSpace(ProjectName) && !string.IsNullOrWhiteSpace(LocalPath);
        
        // 新增的属性用于支持 Avalonia 绑定
        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
        public bool HasRepositoryInfo => RepositoryInfo != null;
        public bool HasRepositoryDescription => RepositoryInfo != null && !string.IsNullOrWhiteSpace(RepositoryInfo.Description);
        public bool HasRepositoryDefaultBranch => RepositoryInfo != null && !string.IsNullOrWhiteSpace(RepositoryInfo.DefaultBranch);
        public bool HasRepositoryLanguage => RepositoryInfo != null && !string.IsNullOrWhiteSpace(RepositoryInfo.PrimaryLanguage);
        public string RepositoryDefaultBranchText => RepositoryInfo != null && !string.IsNullOrWhiteSpace(RepositoryInfo.DefaultBranch) ? $"默认分支: {RepositoryInfo.DefaultBranch}" : string.Empty;
        public string RepositoryLanguageText => RepositoryInfo != null && !string.IsNullOrWhiteSpace(RepositoryInfo.PrimaryLanguage) ? $"主要语言: {RepositoryInfo.PrimaryLanguage}" : string.Empty;

        #endregion

        #region Commands

        public ICommand ValidateRepositoryCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand AddProjectCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Events

        public event Action<bool?>? RequestClose;
        public event Action? RequestBrowseFolder;
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Public Methods

        public CloneRequest CreateCloneRequest()
        {
            return new CloneRequest
            {
                GitUrl = GitUrl,
                LocalPath = LocalPath,
                ProjectName = ProjectName,
                TargetPath = Path.GetDirectoryName(LocalPath) ?? string.Empty,
                Description = Description,
                Branch = string.IsNullOrEmpty(SelectedBranch) ? null : SelectedBranch,
                IsShallowClone = IsShallowClone,
                OverwriteExisting = OverwriteExisting,
                AutoAnalyze = AutoAnalyze
            };
        }

        public ProjectSourceInfo CreateProjectSourceInfo()
        {
            return new ProjectSourceInfo
            {
                Name = ProjectName,
                GitUrl = GitUrl,
                LocalPath = LocalPath,
                Branch = string.IsNullOrEmpty(SelectedBranch) ? RepositoryInfo?.DefaultBranch ?? "main" : SelectedBranch,
                Description = Description,
                Status = ProjectStatus.Pending,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        #endregion

        #region Private Methods

        private async Task ValidateGitUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(GitUrl))
                return;

            IsValidating = true;
            IsValid = false;
            ValidationMessage = "正在验证Git URL...";

            try
            {
                var validationResult = await _gitService.ValidateGitUrlAsync(GitUrl);
                
                if (validationResult.IsValid)
                {
                    IsValid = true;
                    ValidationMessage = "Git URL验证成功";
                    
                    // 获取仓库信息
                    var repoInfo = await _gitService.GetRepositoryInfoAsync(GitUrl);
                    RepositoryInfo = repoInfo;
                    
                    // 获取分支列表
                    var branches = await _gitService.GetBranchesAsync(GitUrl);
                    AvailableBranches.Clear();
                    foreach (var branch in branches)
                    {
                        AvailableBranches.Add(branch);
                    }
                    
                    // 设置默认分支
                    if (repoInfo != null && !string.IsNullOrEmpty(repoInfo.DefaultBranch) && AvailableBranches.Contains(repoInfo.DefaultBranch))
                    {
                        SelectedBranch = repoInfo.DefaultBranch;
                    }
                    else if (AvailableBranches.Count > 0)
                    {
                        SelectedBranch = AvailableBranches.First();
                    }
                }
                else
                {
                    IsValid = false;
                    ValidationMessage = validationResult.ErrorMessage ?? "Git URL验证失败";
                    RepositoryInfo = null;
                    AvailableBranches.Clear();
                    SelectedBranch = string.Empty;
                }
            }
            catch (Exception ex)
            {
                IsValid = false;
                ValidationMessage = $"验证过程中发生错误：{ex.Message}";
                RepositoryInfo = null;
                AvailableBranches.Clear();
                SelectedBranch = string.Empty;
            }
            finally
            {
                IsValidating = false;
            }
        }

        private async Task ConfirmAsync()
        {
            if (!CanConfirm)
                return;

            // 检查本地路径
            if (Directory.Exists(LocalPath) && Directory.GetFileSystemEntries(LocalPath).Length > 0 && !OverwriteExisting)
            {
                ValidationMessage = "目标目录不为空，请选择覆盖现有内容或选择其他目录";
                return;
            }

            RequestClose?.Invoke(true);
        }

        private async void OnGitUrlChanged()
        {
            IsValid = false;
            ValidationMessage = string.Empty;
            RepositoryInfo = null;
            AvailableBranches.Clear();
            SelectedBranch = string.Empty;
            
            // 从Git URL推断项目名称并自动获取分支
            if (!string.IsNullOrWhiteSpace(GitUrl))
            {
                // 增强的项目名称解析
                var projectName = ExtractProjectNameFromGitUrl(GitUrl);
                if (!string.IsNullOrEmpty(projectName))
                {
                    ProjectName = projectName;
                    UpdateLocalPathFromProjectName();
                }
                
                // 自动获取分支列表
                _ = Task.Run(async () => await LoadBranchesAsync(GitUrl));
            }
        }

        private string ExtractProjectNameFromGitUrl(string gitUrl)
        {
            try
            {
                // 处理各种Git URL格式
                var url = gitUrl.Trim();
                
                // 移除 .git 后缀
                if (url.EndsWith(".git"))
                    url = url.Substring(0, url.Length - 4);
                
                // 处理SSH格式: git@github.com:user/repo
                if (url.StartsWith("git@"))
                {
                    var parts = url.Split(':');
                    if (parts.Length >= 2)
                    {
                        var pathPart = parts[1];
                        var segments = pathPart.Split('/');
                        return segments.Length > 0 ? segments.Last() : string.Empty;
                    }
                }
                
                // 处理HTTP/HTTPS格式
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    var segments = uri.AbsolutePath.Trim('/').Split('/');
                    return segments.Length > 0 ? segments.Last() : string.Empty;
                }
                
                // 作为后备，尝试从路径中提取
                var lastSlash = url.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < url.Length - 1)
                {
                    return url.Substring(lastSlash + 1);
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task LoadBranchesAsync(string gitUrl)
        {
            try
            {
                IsLoadingBranches = true;
                
                // 先进行基本的URL验证
                var validationResult = await _gitService.ValidateGitUrlAsync(gitUrl);
                
                if (validationResult.IsValid)
                {
                    // 获取分支列表
                    var branches = await _gitService.GetBranchesAsync(gitUrl);
                    
                    // 在UI线程上更新分支列表
                    AvailableBranches.Clear();
                    foreach (var branch in branches)
                    {
                        AvailableBranches.Add(branch);
                    }
                    
                    // 获取仓库信息以确定默认分支
                    var repoInfo = await _gitService.GetRepositoryInfoAsync(gitUrl);
                    if (repoInfo != null)
                    {
                        RepositoryInfo = repoInfo;
                        
                        // 设置默认分支
                        if (!string.IsNullOrEmpty(repoInfo.DefaultBranch) && AvailableBranches.Contains(repoInfo.DefaultBranch))
                        {
                            SelectedBranch = repoInfo.DefaultBranch;
                        }
                        else if (AvailableBranches.Count > 0)
                        {
                            SelectedBranch = AvailableBranches.First();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 静默处理分支加载错误，不影响主要功能
                System.Diagnostics.Debug.WriteLine($"Failed to load branches: {ex.Message}");
            }
            finally
            {
                IsLoadingBranches = false;
            }
        }

        private void OnRepositoryInfoChanged()
        {
            if (RepositoryInfo != null)
            {
                // 如果项目名称为空，使用仓库名称
                if (string.IsNullOrEmpty(ProjectName) && !string.IsNullOrEmpty(RepositoryInfo.Name))
                {
                    ProjectName = RepositoryInfo.Name;
                    UpdateLocalPathFromProjectName();
                }
                
                // 如果描述为空，使用仓库描述
                if (string.IsNullOrEmpty(Description) && !string.IsNullOrEmpty(RepositoryInfo.Description))
                {
                    Description = RepositoryInfo.Description;
                }
            }
        }

        private void UpdateProjectNameFromPath()
        {
            if (!string.IsNullOrEmpty(LocalPath) && string.IsNullOrEmpty(ProjectName))
            {
                ProjectName = Path.GetFileName(LocalPath.TrimEnd(Path.DirectorySeparatorChar));
            }
        }

        private void UpdateLocalPathFromProjectName()
        {
            if (!string.IsNullOrEmpty(ProjectName))
            {
                // 获取基目录，如果当前路径已经包含项目名称，则使用其父目录
                var baseDir = Path.GetDirectoryName(LocalPath);
                if (string.IsNullOrEmpty(baseDir) || baseDir == LocalPath)
                {
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ProjectIgnite");
                }
                
                // 确保路径总是包含项目名称
                var newPath = Path.Combine(baseDir, ProjectName);
                if (newPath != LocalPath)
                {
                    LocalPath = newPath;
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }


}
