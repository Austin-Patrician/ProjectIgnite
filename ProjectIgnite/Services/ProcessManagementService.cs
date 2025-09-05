using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectIgnite.Models;
using ProjectIgnite.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjectIgnite.Services
{
    public class ProcessManagementService : IProcessManagementService
    {
        private readonly ILogger<ProcessManagementService> _logger;
        private readonly ProjectIgniteDbContext _dbContext;
        private readonly IProjectDetectionService _projectDetectionService;
        private readonly IPortManagementService _portManagementService;
        private readonly ConcurrentDictionary<int, Process> _processes;
        private readonly ConcurrentDictionary<int, List<string>> _processLogs;
        private readonly Timer _healthCheckTimer;

        public event EventHandler<ProcessOutputEventArgs>? ProcessOutput;
        public event EventHandler<ProcessStatusEventArgs>? ProcessStatusChanged;

        public ProcessManagementService(
            ILogger<ProcessManagementService> logger,
            ProjectIgniteDbContext dbContext,
            IProjectDetectionService projectDetectionService,
            IPortManagementService portManagementService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _projectDetectionService = projectDetectionService;
            _portManagementService = portManagementService;
            _processes = new ConcurrentDictionary<int, Process>();
            _processLogs = new ConcurrentDictionary<int, List<string>>();
            
            // 每30秒检查一次进程健康状态
            _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task<LaunchedProject> StartProjectAsync(ProjectSource projectSource, ProjectConfiguration configuration)
        {
            try
            {
                _logger.LogInformation($"Starting project: {projectSource.Name} with environment: {configuration.Environment}");

                // 分配端口
                var port = await _portManagementService.AllocatePortAsync(
                    projectSource.Name, 
                    configuration.DefaultPort ?? 0);

                if (port == null)
                {
                    throw new InvalidOperationException("Failed to allocate port for project");
                }

                // 创建启动的项目记录
                var launchedProject = new LaunchedProject
                {
                    ProjectName = projectSource.Name,
                    ProjectPath = projectSource.LocalPath,
                    CurrentEnvironment = configuration.Environment,
                    CurrentPort = port.Port,
                    Status = "Starting",
                    StartedAt = DateTime.Now,
                    ProcessId = 0 // 临时设置，启动后更新
                };

                // 保存到数据库
                _dbContext.LaunchedProjects.Add(launchedProject);
                await _dbContext.SaveChangesAsync();

                // 构建启动命令
                var startCommand = await BuildStartCommand(projectSource, configuration, port.Port);
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = startCommand.FileName,
                        Arguments = startCommand.Arguments,
                        WorkingDirectory = projectSource.LocalPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                // 设置环境变量
                foreach (var env in startCommand.EnvironmentVariables)
                {
                    process.StartInfo.Environment[env.Key] = env.Value;
                }

                // 初始化日志
                _processLogs.TryAdd(launchedProject.Id, new List<string>());

                // 订阅输出事件
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        AddLog(launchedProject.Id, e.Data, false);
                        ProcessOutput?.Invoke(this, new ProcessOutputEventArgs
                        {
                            LaunchedProjectId = launchedProject.Id,
                            Output = e.Data,
                            IsError = false,
                            Timestamp = DateTime.Now
                        });
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        AddLog(launchedProject.Id, e.Data, true);
                        ProcessOutput?.Invoke(this, new ProcessOutputEventArgs
                        {
                            LaunchedProjectId = launchedProject.Id,
                            Output = e.Data,
                            IsError = true,
                            Timestamp = DateTime.Now
                        });
                    }
                };

                process.Exited += async (sender, e) =>
                {
                    var proc = sender as Process;
                    if (proc != null)
                    {
                        await HandleProcessExit(launchedProject.Id, proc.ExitCode);
                    }
                };

                process.EnableRaisingEvents = true;

                if (!process.Start())
                {
                    throw new InvalidOperationException("Failed to start process");
                }

                // 开始异步读取输出
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 更新进程ID和状态
                launchedProject.ProcessId = process.Id;
                launchedProject.Status = "Running";
                _dbContext.LaunchedProjects.Update(launchedProject);
                await _dbContext.SaveChangesAsync();

                // 存储进程引用
                _processes.TryAdd(launchedProject.Id, process);

                // 触发状态变化事件
                ProcessStatusChanged?.Invoke(this, new ProcessStatusEventArgs
                {
                    LaunchedProjectId = launchedProject.Id,
                    OldStatus = "Starting",
                    NewStatus = "Running",
                    Timestamp = DateTime.Now
                });

                _logger.LogInformation($"Project started successfully. ID: {launchedProject.Id}, PID: {process.Id}");

                return launchedProject;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start project: {projectSource.Name}");
                throw;
            }
        }

        public async Task<bool> StopProjectAsync(int launchedProjectId)
        {
            try
            {
                var launchedProject = await _dbContext.LaunchedProjects
                    .FirstOrDefaultAsync(p => p.Id == launchedProjectId);

                if (launchedProject == null)
                {
                    _logger.LogWarning($"Launched project {launchedProjectId} not found");
                    return false;
                }

                if (!_processes.TryGetValue(launchedProjectId, out var process))
                {
                    _logger.LogWarning($"Process for launched project {launchedProjectId} not found");
                    // 更新数据库状态
                    launchedProject.Status = "Stopped";
                    launchedProject.StoppedAt = DateTime.Now;
                    _dbContext.LaunchedProjects.Update(launchedProject);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }

                _logger.LogInformation($"Stopping project {launchedProject.ProjectName} (ID: {launchedProjectId})");

                // 更新状态为停止中
                await UpdateProjectStatus(launchedProjectId, "Stopping");

                // 尝试优雅关闭
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    
                    // 等待5秒让进程优雅退出
                    if (!process.WaitForExit(5000))
                    {
                        // 强制杀死进程
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                }

                await HandleProcessExit(launchedProjectId, process.ExitCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to stop project {launchedProjectId}");
                return false;
            }
        }

        public async Task<bool> RestartProjectAsync(int launchedProjectId)
        {
            try
            {
                var launchedProject = await _dbContext.LaunchedProjects
                    .FirstOrDefaultAsync(p => p.Id == launchedProjectId);

                if (launchedProject == null)
                {
                    _logger.LogWarning($"Launched project {launchedProjectId} not found for restart");
                    return false;
                }

                _logger.LogInformation($"Restarting project {launchedProject.ProjectName} (ID: {launchedProjectId})");

                // 停止当前进程
                var stopResult = await StopProjectAsync(launchedProjectId);
                if (!stopResult)
                {
                    _logger.LogError($"Failed to stop project {launchedProjectId} for restart");
                    return false;
                }

                // 等待一秒确保资源清理
                await Task.Delay(1000);

                // 获取项目源和配置来重新启动
                var projectSource = await _dbContext.ProjectSources
                    .FirstOrDefaultAsync(p => p.Name == launchedProject.ProjectName);

                var configuration = await _dbContext.ProjectConfigurations
                    .FirstOrDefaultAsync(c => c.ProjectSourceId == projectSource!.Id && 
                                            c.Environment == launchedProject.CurrentEnvironment);

                if (projectSource == null || configuration == null)
                {
                    _logger.LogError($"Failed to find project source or configuration for restart");
                    return false;
                }

                // 重新启动
                var newLaunchedProject = await StartProjectAsync(projectSource, configuration);
                return newLaunchedProject != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to restart project {launchedProjectId}");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetProjectStatusAsync(int launchedProjectId)
        {
            try
            {
                var launchedProject = await _dbContext.LaunchedProjects
                    .FirstOrDefaultAsync(p => p.Id == launchedProjectId);

                if (launchedProject == null)
                {
                    return new Dictionary<string, object>
                    {
                        ["exists"] = false,
                        ["status"] = "Not Found"
                    };
                }

                var status = new Dictionary<string, object>
                {
                    ["exists"] = true,
                    ["id"] = launchedProject.Id,
                    ["projectName"] = launchedProject.ProjectName,
                    ["status"] = launchedProject.Status,
                    ["environment"] = launchedProject.CurrentEnvironment,
                    ["port"] = launchedProject.CurrentPort,
                    ["processId"] = launchedProject.ProcessId,
                    ["startedAt"] = launchedProject.StartedAt,
                    ["stoppedAt"] = launchedProject.StoppedAt
                };

                // 如果进程存在，添加运行时信息
                if (_processes.TryGetValue(launchedProjectId, out var process))
                {
                    try
                    {
                        status["isRunning"] = !process.HasExited;
                        if (!process.HasExited)
                        {
                            status["memoryUsage"] = process.WorkingSet64;
                            status["cpuTime"] = process.TotalProcessorTime.TotalMilliseconds;
                        }
                    }
                    catch
                    {
                        status["isRunning"] = false;
                    }
                }
                else
                {
                    status["isRunning"] = false;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get status for project {launchedProjectId}");
                return new Dictionary<string, object>
                {
                    ["exists"] = false,
                    ["status"] = "Error",
                    ["error"] = ex.Message
                };
            }
        }

        public async Task<List<LaunchedProject>> GetRunningProjectsAsync()
        {
            try
            {
                return await _dbContext.LaunchedProjects
                    .Where(p => p.Status == "Running" || p.Status == "Starting")
                    .OrderByDescending(p => p.StartedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get running projects");
                return new List<LaunchedProject>();
            }
        }

        public async Task<bool> HealthCheckProjectAsync(int launchedProjectId)
        {
            try
            {
                var status = await GetProjectStatusAsync(launchedProjectId);
                
                if (!(bool)status["exists"])
                {
                    return false;
                }

                var isRunning = status.ContainsKey("isRunning") && (bool)status["isRunning"];
                
                // 对于Web项目，可以添加HTTP健康检查
                if (isRunning && status.ContainsKey("port"))
                {
                    // TODO: 添加HTTP健康检查逻辑
                    // var port = (int)status["port"];
                    // return await CheckHttpHealthAsync(port);
                }

                return isRunning;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to perform health check for project {launchedProjectId}");
                return false;
            }
        }

        public async Task<List<string>> GetProjectLogsAsync(int launchedProjectId, int lines = 100)
        {
            try
            {
                if (_processLogs.TryGetValue(launchedProjectId, out var logs))
                {
                    return logs.TakeLast(lines).ToList();
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get logs for project {launchedProjectId}");
                return new List<string> { $"Error getting logs: {ex.Message}" };
            }
        }

        public async Task<int> CleanupZombieProcessesAsync()
        {
            try
            {
                var cleanedCount = 0;
                var runningProjects = await GetRunningProjectsAsync();

                foreach (var project in runningProjects)
                {
                    if (!_processes.TryGetValue(project.Id, out var process) || process.HasExited)
                    {
                        // 进程已经停止但数据库状态未更新
                        await UpdateProjectStatus(project.Id, "Stopped");
                        CleanupProcess(project.Id);
                        cleanedCount++;
                    }
                }

                _logger.LogInformation($"Cleaned up {cleanedCount} zombie processes");
                return cleanedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup zombie processes");
                return 0;
            }
        }

        private async Task<(string FileName, string Arguments, Dictionary<string, string> EnvironmentVariables)> BuildStartCommand(
            ProjectSource projectSource, ProjectConfiguration configuration, int port)
        {
            var projectType = await _projectDetectionService.DetectProjectTypeAsync(projectSource.LocalPath);
            var envVars = new Dictionary<string, string>();

            // 设置端口环境变量
            envVars["PORT"] = port.ToString();
            envVars["ASPNETCORE_URLS"] = $"http://localhost:{port}";

            // 解析配置中的环境变量
            if (!string.IsNullOrEmpty(configuration.EnvironmentVariables))
            {
                var configEnvVars = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(configuration.EnvironmentVariables);
                if (configEnvVars != null)
                {
                    foreach (var kvp in configEnvVars)
                    {
                        envVars[kvp.Key] = kvp.Value;
                    }
                }
            }

            // 根据项目类型构建启动命令
            return projectType switch
            {
                "DotNetWebApi" => ("dotnet", $"run --environment {configuration.Environment}", envVars),
                "NodeJs" => ("npm", "start", envVars),
                "Python" => ("python", configuration.StartCommand ?? "app.py", envVars),
                _ => ("dotnet", $"run --environment {configuration.Environment}", envVars)
            };
        }

        private async Task HandleProcessExit(int launchedProjectId, int exitCode)
        {
            try
            {
                var status = exitCode == 0 ? "Stopped" : "Failed";
                await UpdateProjectStatus(launchedProjectId, status);

                // 释放端口
                var launchedProject = await _dbContext.LaunchedProjects
                    .FirstOrDefaultAsync(p => p.Id == launchedProjectId);
                
                if (launchedProject != null && launchedProject.CurrentPort.HasValue)
                {
                    await _portManagementService.ReleasePortAsync(launchedProject.CurrentPort.Value);
                }

                CleanupProcess(launchedProjectId);

                _logger.LogInformation($"Process for project {launchedProjectId} exited with code {exitCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling process exit for project {launchedProjectId}");
            }
        }

        private async Task UpdateProjectStatus(int launchedProjectId, string newStatus)
        {
            try
            {
                var launchedProject = await _dbContext.LaunchedProjects
                    .FirstOrDefaultAsync(p => p.Id == launchedProjectId);

                if (launchedProject != null)
                {
                    var oldStatus = launchedProject.Status;
                    launchedProject.Status = newStatus;
                    
                    if (newStatus == "Stopped" || newStatus == "Failed")
                    {
                        launchedProject.StoppedAt = DateTime.Now;
                    }

                    _dbContext.LaunchedProjects.Update(launchedProject);
                    await _dbContext.SaveChangesAsync();

                    ProcessStatusChanged?.Invoke(this, new ProcessStatusEventArgs
                    {
                        LaunchedProjectId = launchedProjectId,
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update status for project {launchedProjectId}");
            }
        }

        private void AddLog(int launchedProjectId, string message, bool isError)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] {(isError ? "ERROR" : "INFO")}: {message}";
            
            _processLogs.AddOrUpdate(launchedProjectId, 
                new List<string> { logEntry },
                (key, existing) =>
                {
                    existing.Add(logEntry);
                    // 保持最多1000行日志
                    if (existing.Count > 1000)
                    {
                        existing.RemoveAt(0);
                    }
                    return existing;
                });
        }

        private void CleanupProcess(int launchedProjectId)
        {
            try
            {
                if (_processes.TryRemove(launchedProjectId, out var process))
                {
                    process.Dispose();
                }

                _processLogs.TryRemove(launchedProjectId, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cleaning up process for project {launchedProjectId}");
            }
        }

        private void PerformHealthCheck(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var runningProjects = await GetRunningProjectsAsync();
                    
                    foreach (var project in runningProjects)
                    {
                        var isHealthy = await HealthCheckProjectAsync(project.Id);
                        if (!isHealthy)
                        {
                            _logger.LogWarning($"Project {project.Id} ({project.ProjectName}) is unhealthy");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during health check");
                }
            });
        }

        public void Dispose()
        {
            try
            {
                _healthCheckTimer?.Dispose();

                // 停止所有运行中的进程
                var projectIds = _processes.Keys.ToList();
                foreach (var projectId in projectIds)
                {
                    _ = Task.Run(() => StopProjectAsync(projectId));
                }

                // 清理所有进程
                foreach (var process in _processes.Values)
                {
                    try
                    {
                        process?.Dispose();
                    }
                    catch
                    {
                        // 忽略清理时的异常
                    }
                }

                _processes.Clear();
                _processLogs.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ProcessManagementService disposal");
            }
        }
    }
}
