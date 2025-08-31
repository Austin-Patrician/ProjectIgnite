using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectIgnite.Services;
using ProjectIgnite.DTOs;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.IO;

namespace ProjectIgnite.ViewModels
{
    public class CloneProgressViewModel : INotifyPropertyChanged
    {
        private readonly IGitService _gitService;
        private readonly ILinguistService _linguistService;
        private readonly CloneRequest _cloneRequest;
        private readonly ProjectSourceInfo _projectInfo;
        private CancellationTokenSource _cancellationTokenSource;
        
        private string _projectName = string.Empty;
        private string _gitUrl = string.Empty;
        private string _localPath = string.Empty;
        private string _branch = string.Empty;
        private double _overallProgress = 0;
        private string _currentStatus = "准备开始克隆...";
        private long _receivedObjects = 0;
        private long _totalObjects = 0;
        private long _receivedBytes = 0;
        private long _speed = 0;
        private TimeSpan _elapsedTime = TimeSpan.Zero;
        private TimeSpan? _estimatedTime;
        private bool _isCloning = false;
        private bool _isAnalyzing = false;
        private bool _isCompleted = false;
        private bool _isCancelled = false;
        private string _analysisStatus = string.Empty;
        private string _errorMessage = string.Empty;
        private DateTime _startTime;

        public CloneProgressViewModel(IGitService gitService, ILinguistService linguistService, 
            CloneRequest cloneRequest, ProjectSourceInfo projectInfo)
        {
            _gitService = gitService;
            _linguistService = linguistService;
            _cloneRequest = cloneRequest;
            _projectInfo = projectInfo;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 初始化属性
            ProjectName = projectInfo.Name;
            GitUrl = cloneRequest.GitUrl;
            LocalPath = cloneRequest.LocalPath;
            Branch = cloneRequest.Branch ?? "main";
            
            // 初始化命令
            CancelCommand = new RelayCommand(Cancel, () => CanCancel);
            RetryCommand = new RelayCommand(async () => await RetryAsync(), () => CanRetry);
            CompleteCommand = new RelayCommand(() => RequestClose?.Invoke(true), () => IsCompleted);
        }

        #region Properties

        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        public string GitUrl
        {
            get => _gitUrl;
            set => SetProperty(ref _gitUrl, value);
        }

        public string LocalPath
        {
            get => _localPath;
            set => SetProperty(ref _localPath, value);
        }

        public string Branch
        {
            get => _branch;
            set => SetProperty(ref _branch, value);
        }

        public double OverallProgress
        {
            get => _overallProgress;
            set => SetProperty(ref _overallProgress, value);
        }

        public string CurrentStatus
        {
            get => _currentStatus;
            set => SetProperty(ref _currentStatus, value);
        }

        public long ReceivedObjects
        {
            get => _receivedObjects;
            set => SetProperty(ref _receivedObjects, value);
        }

        public long TotalObjects
        {
            get => _totalObjects;
            set => SetProperty(ref _totalObjects, value);
        }

        public long ReceivedBytes
        {
            get => _receivedBytes;
            set => SetProperty(ref _receivedBytes, value);
        }

        public long Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public TimeSpan ElapsedTime
        {
            get => _elapsedTime;
            set => SetProperty(ref _elapsedTime, value);
        }

        public TimeSpan? EstimatedTime
        {
            get => _estimatedTime;
            set => SetProperty(ref _estimatedTime, value);
        }

        public bool IsCloning
        {
            get => _isCloning;
            set
            {
                if (SetProperty(ref _isCloning, value))
                {
                    OnPropertyChanged(nameof(CanCancel));
                }
            }
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (SetProperty(ref _isCompleted, value))
                {
                    OnPropertyChanged(nameof(CanRetry));
                }
            }
        }

        public bool IsCancelled
        {
            get => _isCancelled;
            set
            {
                if (SetProperty(ref _isCancelled, value))
                {
                    OnPropertyChanged(nameof(CanCancel));
                    OnPropertyChanged(nameof(CanRetry));
                }
            }
        }

        public string AnalysisStatus
        {
            get => _analysisStatus;
            set => SetProperty(ref _analysisStatus, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(CanRetry));
                }
            }
        }

        public bool CanCancel => IsCloning && !IsCancelled;
        public bool CanRetry => !IsCloning && !IsCompleted && (!string.IsNullOrEmpty(ErrorMessage) || IsCancelled);

        #endregion

        #region Commands

        public ICommand CancelCommand { get; }
        public ICommand RetryCommand { get; }
        public ICommand CompleteCommand { get; }

        #endregion

        #region Events

        public event Action<bool?>? RequestClose;
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Public Methods

        public async Task StartCloneAsync()
        {
            try
            {
                await ResetStateAsync();
                await ExecuteCloneAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"克隆过程中发生错误：{ex.Message}";
                IsCloning = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task ResetStateAsync()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            
            OverallProgress = 0;
            CurrentStatus = "准备开始克隆...";
            ReceivedObjects = 0;
            TotalObjects = 0;
            ReceivedBytes = 0;
            Speed = 0;
            ElapsedTime = TimeSpan.Zero;
            EstimatedTime = null;
            IsCloning = false;
            IsAnalyzing = false;
            IsCompleted = false;
            IsCancelled = false;
            AnalysisStatus = string.Empty;
            ErrorMessage = string.Empty;
            
            await Task.Delay(100); // 给UI一点时间更新
        }

        private async Task ExecuteCloneAsync()
        {
            IsCloning = true;
            _startTime = DateTime.Now;
            
            try
            {
                // 创建进度报告器
                var progressReporter = new Progress<CloneProgress>(OnCloneProgressChanged);
                
                // 开始克隆
                CurrentStatus = "正在克隆仓库...";
                var cloneResult = await _gitService.CloneRepositoryAsync(
                    _cloneRequest, 
                    progressReporter, 
                    _cancellationTokenSource.Token);
                
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    IsCancelled = true;
                    CurrentStatus = "克隆已取消";
                    return;
                }
                
                if (!cloneResult.Success)
                {
                    ErrorMessage = cloneResult.ErrorMessage ?? "克隆失败";
                    return;
                }
                
                // 克隆完成，开始语言分析（如果需要）
                if (_projectInfo.AutoAnalyze)
                {
                    await PerformLanguageAnalysisAsync();
                }
                
                // 完成
                IsCompleted = true;
                OverallProgress = 100;
                CurrentStatus = "克隆完成";
            }
            catch (OperationCanceledException)
            {
                IsCancelled = true;
                CurrentStatus = "克隆已取消";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"克隆过程中发生错误：{ex.Message}";
            }
            finally
            {
                IsCloning = false;
            }
        }

        private async Task PerformLanguageAnalysisAsync()
        {
            try
            {
                IsAnalyzing = true;
                AnalysisStatus = "正在分析项目语言组成...";
                
                // 检查目录是否存在
                if (!Directory.Exists(LocalPath))
                {
                    AnalysisStatus = "分析跳过：目录不存在";
                    return;
                }
                
                // 执行语言分析
                var analysisResult = await _linguistService.AnalyzeProjectAsync(
                    LocalPath, 
                    new Progress<LinguistAnalysisResult>());
                
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    AnalysisStatus = "分析已取消";
                    return;
                }
                
                if (analysisResult.Status == LinguistAnalysisStatus.Completed)
                {
                    var mainLanguage = analysisResult.Languages?.FirstOrDefault();
                    AnalysisStatus = mainLanguage != null 
                        ? $"分析完成，主要语言：{mainLanguage.Value.Value} ({mainLanguage.Value.Key:F1}%)"
                        : "分析完成";
                }
                else
                {
                    AnalysisStatus = $"分析失败：{analysisResult.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                AnalysisStatus = $"分析过程中发生错误：{ex.Message}";
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private void OnCloneProgressChanged(CloneProgress progress)
        {
            // 更新进度信息
            ReceivedObjects = progress.ReceivedObjects;
            TotalObjects = progress.TotalObjects;
            ReceivedBytes = (long)progress.ReceivedBytes;
            Speed = (long)progress.Speed;
            
            // 计算总体进度
            if (TotalObjects > 0)
            {
                var cloneProgress = (double)ReceivedObjects / TotalObjects * 80; // 克隆占80%
                OverallProgress = Math.Min(cloneProgress, 80);
            }
            
            // 更新状态
            CurrentStatus = progress.Stage switch
            {
                CloneStage.Initializing => "正在初始化...",
                CloneStage.Connecting => "正在连接远程仓库...",
                CloneStage.ReceivingObjects => $"正在接收对象 ({ReceivedObjects}/{TotalObjects})...",
                CloneStage.ResolvingDeltas => "正在解析增量...",
                CloneStage.CheckingOut => "正在检出文件...",
                CloneStage.Completed => "克隆完成",
                CloneStage.Error => "克隆出错",
                _ => "正在克隆..."
            };
            
            // 计算时间
            ElapsedTime = DateTime.Now - _startTime;
            
            // 估算剩余时间
            if (Speed > 0 && TotalObjects > 0 && ReceivedObjects > 0)
            {
                var remainingObjects = TotalObjects - ReceivedObjects;
                var estimatedSeconds = remainingObjects / (double)ReceivedObjects * ElapsedTime.TotalSeconds;
                EstimatedTime = TimeSpan.FromSeconds(estimatedSeconds);
            }
        }

        private void Cancel()
        {
            if (CanCancel)
            {
                _cancellationTokenSource?.Cancel();
                CurrentStatus = "正在取消...";
            }
        }

        private async Task RetryAsync()
        {
            if (CanRetry)
            {
                await StartCloneAsync();
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

        #region IDisposable

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        #endregion
    }
}
