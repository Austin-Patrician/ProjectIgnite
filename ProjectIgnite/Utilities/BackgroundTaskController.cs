using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ProjectIgnite.Utilities;

public class BackgroundTaskController
{
    private readonly Dictionary<string, BackgroundTaskInfo> _tasks = new();
    private readonly ObservableCollection<BackgroundTaskInfo> _taskList = new();

    // 事件
    public event Action<BackgroundTaskInfo> TaskStarted;
    public event Action<BackgroundTaskInfo> TaskCompleted;
    public event Action<BackgroundTaskInfo> TaskFailed;
    public event Action<BackgroundTaskInfo> TaskCancelled;
    public event Action<TaskProgress> ProgressUpdated;

    // 只读任务列表
    public ReadOnlyObservableCollection<BackgroundTaskInfo> Tasks { get; }

    public BackgroundTaskController()
    {
        Tasks = new ReadOnlyObservableCollection<BackgroundTaskInfo>(_taskList);
    }

    /// <summary>
    /// 运行后台任务
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="taskFunc">任务函数</param>
    /// <param name="name">任务名称</param>
    /// <param name="description">任务描述</param>
    /// <param name="progress">进度报告接口</param>
    /// <returns>任务信息</returns>
    public BackgroundTaskInfo RunTask<T>(
        Func<CancellationToken, IProgress<TaskProgress>, Task<T>> taskFunc,
        string name,
        string description = null,
        Action<T> onCompleted = null)
    {
        var taskInfo = new BackgroundTaskInfo
        {
            Name = name,
            Description = description ?? name
        };

        // 创建进度报告器
        var progress = new Progress<TaskProgress>(p =>
        {
            if (p.TaskId == taskInfo.Id)
            {
                taskInfo.Progress = p.Percentage;
                Dispatcher.UIThread.Post(() => ProgressUpdated?.Invoke(p));
            }
        });

        // 创建并启动任务
        taskInfo.Task = Task.Run(async () =>
        {
            try
            {
                // 更新状态为运行中
                UpdateTaskStatus(taskInfo, TaskStatus.Running);

                // 执行任务
                var result = await taskFunc(taskInfo.CancellationTokenSource.Token, progress);

                // 任务完成
                UpdateTaskStatus(taskInfo, TaskStatus.Completed);

                // 在UI线程执行完成回调
                if (onCompleted != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => onCompleted(result));
                }
            }
            catch (OperationCanceledException)
            {
                UpdateTaskStatus(taskInfo, TaskStatus.Cancelled);
            }
            catch (Exception ex)
            {
                taskInfo.ErrorMessage = ex.Message;
                UpdateTaskStatus(taskInfo, TaskStatus.Failed);
            }
        }, taskInfo.CancellationTokenSource.Token);

        // 添加到管理列表
        AddTask(taskInfo);

        return taskInfo;
    }

    /// <summary>
    /// 运行无返回值的后台任务
    /// </summary>
    public BackgroundTaskInfo RunTask(
        Func<CancellationToken, IProgress<TaskProgress>, Task> taskFunc,
        string name,
        string description = null)
    {
        return RunTask<object>(async (token, progress) =>
        {
            await taskFunc(token, progress);
            return null;
        }, name, description);
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    public void CancelTask(string taskId)
    {
        if (_tasks.TryGetValue(taskId, out var taskInfo))
        {
            taskInfo.CancellationTokenSource.Cancel();
        }
    }

    /// <summary>
    /// 取消所有任务
    /// </summary>
    public void CancelAllTasks()
    {
        foreach (var task in _tasks.Values)
        {
            if (task.Status == TaskStatus.Running || task.Status == TaskStatus.Pending)
            {
                task.CancellationTokenSource.Cancel();
            }
        }
    }

    /// <summary>
    /// 清理已完成的任务
    /// </summary>
    public void ClearCompletedTasks()
    {
        var completedTasks = _taskList.Where(t =>
            t.Status == TaskStatus.Completed ||
            t.Status == TaskStatus.Failed ||
            t.Status == TaskStatus.Cancelled).ToList();

        foreach (var task in completedTasks)
        {
            RemoveTask(task.Id);
        }
    }

    /// <summary>
    /// 获取任务信息
    /// </summary>
    public BackgroundTaskInfo GetTask(string taskId)
    {
        return _tasks.TryGetValue(taskId, out var task) ? task : null;
    }

    /// <summary>
    /// 等待所有任务完成
    /// </summary>
    public async Task WaitAllTasksAsync()
    {
        var runningTasks = _tasks.Values
            .Where(t => t.Status == TaskStatus.Running || t.Status == TaskStatus.Pending)
            .Select(t => t.Task)
            .ToArray();

        if (runningTasks.Any())
        {
            await Task.WhenAll(runningTasks);
        }
    }

    private void AddTask(BackgroundTaskInfo taskInfo)
    {
        _tasks[taskInfo.Id] = taskInfo;
        Dispatcher.UIThread.Post(() => _taskList.Add(taskInfo));
    }

    private void RemoveTask(string taskId)
    {
        if (_tasks.TryGetValue(taskId, out var taskInfo))
        {
            _tasks.Remove(taskId);
            Dispatcher.UIThread.Post(() => _taskList.Remove(taskInfo));
        }
    }

    private void UpdateTaskStatus(BackgroundTaskInfo taskInfo, TaskStatus status)
    {
        taskInfo.Status = status;

        switch (status)
        {
            case TaskStatus.Running:
                taskInfo.StartedAt = DateTime.Now;
                Dispatcher.UIThread.Post(() => TaskStarted?.Invoke(taskInfo));
                break;
            case TaskStatus.Completed:
                taskInfo.CompletedAt = DateTime.Now;
                taskInfo.Progress = 100;
                Dispatcher.UIThread.Post(() => TaskCompleted?.Invoke(taskInfo));
                break;
            case TaskStatus.Failed:
                taskInfo.CompletedAt = DateTime.Now;
                Dispatcher.UIThread.Post(() => TaskFailed?.Invoke(taskInfo));
                break;
            case TaskStatus.Cancelled:
                taskInfo.CompletedAt = DateTime.Now;
                Dispatcher.UIThread.Post(() => TaskCancelled?.Invoke(taskInfo));
                break;
        }
    }
}

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

// 后台任务信息
public class BackgroundTaskInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double Progress { get; set; } = 0;
    public string ErrorMessage { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public Task Task { get; set; }
}

// 任务进度报告
public class TaskProgress
{
    public string TaskId { get; set; }
    public double Percentage { get; set; }
    public string Message { get; set; }
}

public static class BackgroundTaskControllerExtensions
{
    /// <summary>
    /// 快速运行简单任务
    /// </summary>
    public static BackgroundTaskInfo RunSimpleTask(
        this BackgroundTaskController controller,
        Func<Task> taskFunc,
        string name)
    {
        return controller.RunTask(async (token, progress) => { await taskFunc(); }, name);
    }

    /// <summary>
    /// 运行带进度的任务
    /// </summary>
    public static BackgroundTaskInfo RunTaskWithProgress(
        this BackgroundTaskController controller,
        Func<IProgress<double>, Task> taskFunc,
        string name,
        string description = null)
    {
        return controller.RunTask(async (token, progress) =>
        {
            var simpleProgress = new Progress<double>(p =>
            {
                progress.Report(new TaskProgress
                {
                    TaskId = "",
                    Percentage = p,
                    Message = $"进度: {p:F1}%"
                });
            });

            await taskFunc(simpleProgress);
        }, name, description);
    }
}