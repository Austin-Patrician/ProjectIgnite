using ProjectIgnite.DTOs;
using ProjectIgnite.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// Git服务实现
    /// </summary>
    public class GitService : IGitService
    {
        private readonly string _gitExecutable;

        public GitService()
        {
            _gitExecutable = FindGitExecutable();
        }

        public async Task<CloneResult> CloneRepositoryAsync(CloneRequest request, IProgress<CloneProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            var result = new CloneResult();

            try
            {
                // 验证Git URL
                var validation = await ValidateGitUrlAsync(request.GitUrl, cancellationToken);
                if (!validation.IsValid)
                {
                    result.ErrorMessage = validation.ErrorMessage;
                    return result;
                }

                // 准备目标路径
                var targetPath = Path.Combine(request.TargetPath, request.ProjectName);
                if (Directory.Exists(targetPath))
                {
                    if (!request.OverwriteExisting)
                    {
                        result.ErrorMessage = $"目标路径已存在: {targetPath}";
                        return result;
                    }
                    Directory.Delete(targetPath, true);
                }

                Directory.CreateDirectory(request.TargetPath);

                // 构建Git克隆命令
                var args = BuildCloneArguments(request, targetPath);

                // 报告开始克隆
                progress?.Report(new CloneProgress
                {
                    ProjectId = 0,
                    ProjectName = request.ProjectName,
                    GitUrl = request.GitUrl,
                    TargetPath = targetPath,
                    Status = "started",
                    Progress = 0,
                    CurrentOperation = "开始克隆仓库...",
                    StartTime = startTime,
                    CanCancel = true
                });

                // 执行Git克隆
                var processResult = await ExecuteGitCommandAsync(args, progress, request.ProjectName, request.GitUrl, targetPath, startTime, cancellationToken);

                if (processResult.Success)
                {
                    // 获取克隆结果信息
                    var cloneInfo = await GetCloneInfoAsync(targetPath);
                    result.Success = true;
                    result.LocalPath = targetPath;
                    result.Duration = DateTime.Now - startTime;
                    result.TotalBytes = cloneInfo.TotalBytes;
                    result.TotalFiles = cloneInfo.TotalFiles;

                    progress?.Report(new CloneProgress
                    {
                        ProjectId = 0,
                        ProjectName = request.ProjectName,
                        GitUrl = request.GitUrl,
                        TargetPath = targetPath,
                        Status = "completed",
                        Progress = 100,
                        CurrentOperation = "克隆完成",
                        StartTime = startTime,
                        TotalObjects = cloneInfo.TotalFiles,
                        TotalBytes = cloneInfo.TotalBytes,
                        CanCancel = false
                    });
                }
                else
                {
                    result.ErrorMessage = processResult.ErrorMessage;
                    progress?.Report(new CloneProgress
                    {
                        ProjectId = 0,
                        ProjectName = request.ProjectName,
                        GitUrl = request.GitUrl,
                        TargetPath = targetPath,
                        Status = "error",
                        Progress = 0,
                        CurrentOperation = "克隆失败",
                        ErrorMessage = processResult.ErrorMessage,
                        StartTime = startTime,
                        CanCancel = false
                    });
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "克隆操作已取消";
                progress?.Report(new CloneProgress
                {
                    ProjectId = 0,
                    ProjectName = request.ProjectName,
                    GitUrl = request.GitUrl,
                    TargetPath = request.TargetPath,
                    Status = "cancelled",
                    Progress = 0,
                    CurrentOperation = "操作已取消",
                    ErrorMessage = "克隆操作已取消",
                    StartTime = startTime,
                    CanCancel = false
                });
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"克隆过程中发生错误: {ex.Message}";
            }

            return result;
        }

        public async Task<GitValidationResult> ValidateGitUrlAsync(string gitUrl, CancellationToken cancellationToken = default)
        {
            var result = new GitValidationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(gitUrl))
                {
                    result.ErrorMessage = "Git URL不能为空";
                    return result;
                }

                // 标准化URL
                var normalizedUrl = NormalizeGitUrl(gitUrl);
                result.NormalizedUrl = normalizedUrl;
                result.Provider = DetectGitProvider(normalizedUrl);

                // 使用git ls-remote验证URL
                var args = $"ls-remote --heads {normalizedUrl}";
                var processResult = await ExecuteGitCommandSimpleAsync(args, cancellationToken);

                if (processResult.Success)
                {
                    result.IsValid = true;
                }
                else
                {
                    result.ErrorMessage = $"无法访问Git仓库: {processResult.ErrorMessage}";
                    // 检查是否需要认证
                    if (processResult.ErrorMessage?.Contains("Authentication") == true ||
                        processResult.ErrorMessage?.Contains("Permission") == true)
                    {
                        result.RequiresAuthentication = true;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"验证Git URL时发生错误: {ex.Message}";
            }

            return result;
        }

        public async Task<GitRepositoryInfo?> GetRepositoryInfoAsync(string gitUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var validation = await ValidateGitUrlAsync(gitUrl, cancellationToken);
                if (!validation.IsValid)
                    return null;

                var info = new GitRepositoryInfo
                {
                    CloneUrl = validation.NormalizedUrl ?? gitUrl,
                    Name = ExtractRepositoryName(gitUrl)
                };

                // 尝试获取默认分支
                var defaultBranch = await GetDefaultBranchAsync(gitUrl, cancellationToken);
                if (!string.IsNullOrEmpty(defaultBranch))
                {
                    info.DefaultBranch = defaultBranch;
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string[]> GetBranchesAsync(string gitUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                // 获取所有远程分支和标签
                var args = $"ls-remote --heads --tags {gitUrl}";
                var result = await ExecuteGitCommandSimpleAsync(args, cancellationToken);

                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    var branches = new List<string>();
                    var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 2)
                        {
                            var refName = parts[1].Trim();
                            
                            // 处理分支引用
                            if (refName.StartsWith("refs/heads/"))
                            {
                                var branchName = refName.Replace("refs/heads/", "");
                                if (!string.IsNullOrEmpty(branchName))
                                {
                                    branches.Add(branchName);
                                }
                            }
                            // 可选：也包含标签（如果需要的话）
                            else if (refName.StartsWith("refs/tags/") && !refName.EndsWith("^{}"))
                            {
                                var tagName = refName.Replace("refs/tags/", "");
                                if (!string.IsNullOrEmpty(tagName))
                                {
                                    // 标签前缀区分
                                    branches.Add($"tag/{tagName}");
                                }
                            }
                        }
                    }

                    // 如果没有找到分支，尝试只获取分支
                    if (!branches.Any())
                    {
                        args = $"ls-remote --heads {gitUrl}";
                        result = await ExecuteGitCommandSimpleAsync(args, cancellationToken);
                        
                        if (result.Success && !string.IsNullOrEmpty(result.Output))
                        {
                            branches = result.Output
                                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                .Select(line => {
                                    var parts = line.Split('\t');
                                    return parts.Length >= 2 ? parts[1].Replace("refs/heads/", "").Trim() : null;
                                })
                                .Where(branch => !string.IsNullOrEmpty(branch))
                                .Cast<string>()
                                .ToList();
                        }
                    }

                    // 移除标签，只返回分支
                    var branchesOnly = branches.Where(b => !b.StartsWith("tag/")).ToArray();
                    
                    // 如果有分支，返回分支；否则返回所有引用
                    return branchesOnly.Any() ? branchesOnly : branches.ToArray();
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常
                System.Diagnostics.Debug.WriteLine($"获取分支失败: {ex.Message}");
            }

            return Array.Empty<string>();
        }

        public bool IsGitRepository(string localPath)
        {
            try
            {
                if (!Directory.Exists(localPath))
                    return false;

                var gitDir = Path.Combine(localPath, ".git");
                return Directory.Exists(gitDir) || File.Exists(gitDir);
            }
            catch
            {
                return false;
            }
        }

        public async Task<LocalGitInfo?> GetLocalRepositoryInfoAsync(string localPath)
        {
            try
            {
                if (!IsGitRepository(localPath))
                    return null;

                var info = new LocalGitInfo();

                // 获取远程URL
                var remoteResult = await ExecuteGitCommandInDirectoryAsync("remote get-url origin", localPath);
                if (remoteResult.Success)
                {
                    info.RemoteUrl = remoteResult.Output?.Trim() ?? string.Empty;
                }

                // 获取当前分支
                var branchResult = await ExecuteGitCommandInDirectoryAsync("branch --show-current", localPath);
                if (branchResult.Success)
                {
                    info.CurrentBranch = branchResult.Output?.Trim() ?? string.Empty;
                }

                // 获取所有分支
                var branchesResult = await ExecuteGitCommandInDirectoryAsync("branch -a", localPath);
                if (branchesResult.Success && !string.IsNullOrEmpty(branchesResult.Output))
                {
                    info.Branches = branchesResult.Output
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(b => b.Trim().TrimStart('*').Trim())
                        .Where(b => !string.IsNullOrEmpty(b))
                        .ToArray();
                }

                // 检查未提交的更改
                var statusResult = await ExecuteGitCommandInDirectoryAsync("status --porcelain", localPath);
                info.HasUncommittedChanges = !string.IsNullOrWhiteSpace(statusResult.Output);

                // 获取最后提交信息
                var commitResult = await ExecuteGitCommandInDirectoryAsync("log -1 --format=\"%H|%ci|%s\"", localPath);
                if (commitResult.Success && !string.IsNullOrEmpty(commitResult.Output))
                {
                    var parts = commitResult.Output.Trim().Trim('"').Split('|');
                    if (parts.Length >= 3)
                    {
                        info.LastCommitHash = parts[0];
                        if (DateTime.TryParse(parts[1], out var commitDate))
                        {
                            info.LastCommitDate = commitDate;
                        }
                        info.LastCommitMessage = parts[2];
                    }
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        public async Task<PullResult> PullRepositoryAsync(string localPath, IProgress<CloneProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var result = new PullResult();

            try
            {
                if (!IsGitRepository(localPath))
                {
                    result.ErrorMessage = "指定路径不是Git仓库";
                    return result;
                }

                progress?.Report(new CloneProgress
                {
                    Status = "started",
                    CurrentOperation = "正在拉取最新代码...",
                    Progress = 0
                });

                var pullResult = await ExecuteGitCommandInDirectoryAsync("pull", localPath, cancellationToken);

                if (pullResult.Success)
                {
                    result.Success = true;
                    progress?.Report(new CloneProgress
                    {
                        Status = "completed",
                        CurrentOperation = "拉取完成",
                        Progress = 100
                    });
                }
                else
                {
                    result.ErrorMessage = pullResult.ErrorMessage;
                    progress?.Report(new CloneProgress
                    {
                        Status = "error",
                        CurrentOperation = "拉取失败",
                        ErrorMessage = pullResult.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"拉取过程中发生错误: {ex.Message}";
            }

            return result;
        }

        #region 私有方法

        private string FindGitExecutable()
        {
            // 在Windows上查找git.exe
            var paths = new[]
            {
                "git",
                @"C:\Program Files\Git\bin\git.exe",
                @"C:\Program Files (x86)\Git\bin\git.exe",
                @"C:\Git\bin\git.exe"
            };

            foreach (var path in paths)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    process.WaitForExit(5000);

                    if (process.ExitCode == 0)
                    {
                        return path;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return "git"; // 默认使用PATH中的git
        }

        private string BuildCloneArguments(CloneRequest request, string targetPath)
        {
            var args = "clone";

            if (request.IsShallowClone)
            {
                args += " --depth 1";
            }

            if (!string.IsNullOrEmpty(request.Branch))
            {
                args += $" --branch {request.Branch}";
            }

            args += " --progress";
            args += $" \"{request.GitUrl}\" \"{targetPath}\"";

            return args;
        }

        private async Task<(bool Success, string? ErrorMessage)> ExecuteGitCommandAsync(
            string arguments,
            IProgress<CloneProgress>? progress,
            string projectName,
            string gitUrl,
            string targetPath,
            DateTime startTime,
            CancellationToken cancellationToken)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _gitExecutable,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                var errorOutput = string.Empty;

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorOutput += e.Data + "\n";
                        
                        // 解析进度信息
                        var progressInfo = ParseGitProgress(e.Data);
                        if (progressInfo != null)
                        {
                            progress?.Report(new CloneProgress
                            {
                                ProjectName = projectName,
                                GitUrl = gitUrl,
                                TargetPath = targetPath,
                                Status = "progress",
                                Progress = progressInfo.Value.Progress,
                                CurrentOperation = progressInfo.Value.Operation,
                                ProcessedObjects = progressInfo.Value.ProcessedObjects,
                                TotalObjects = progressInfo.Value.TotalObjects,
                                ReceivedBytes = progressInfo.Value.ReceivedBytes,
                                TotalBytes = progressInfo.Value.TotalBytes,
                                TransferSpeed = progressInfo.Value.TransferSpeed,
                                StartTime = startTime,
                                CanCancel = true
                            });
                        }
                    }
                };

                process.Start();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    return (true, null);
                }
                else
                {
                    return (false, errorOutput.Trim());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string? Output, string? ErrorMessage)> ExecuteGitCommandSimpleAsync(
            string arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _gitExecutable,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private async Task<(bool Success, string? Output, string? ErrorMessage)> ExecuteGitCommandInDirectoryAsync(
            string arguments, string workingDirectory, CancellationToken cancellationToken = default)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _gitExecutable,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private (int Progress, string Operation, int ProcessedObjects, int TotalObjects, long ReceivedBytes, long TotalBytes, long TransferSpeed)? ParseGitProgress(string line)
        {
            try
            {
                // 解析Git进度输出
                // 示例: "Receiving objects:  50% (500/1000), 1.2 MiB | 500 KiB/s"
                var receivingMatch = Regex.Match(line, @"Receiving objects:\s+(\d+)%\s+\((\d+)/(\d+)\)(?:,\s+([\d.]+)\s+(\w+))?(?:\s+\|\s+([\d.]+)\s+(\w+)/s)?");
                if (receivingMatch.Success)
                {
                    var progress = int.Parse(receivingMatch.Groups[1].Value);
                    var processed = int.Parse(receivingMatch.Groups[2].Value);
                    var total = int.Parse(receivingMatch.Groups[3].Value);
                    
                    long receivedBytes = 0;
                    if (receivingMatch.Groups[4].Success)
                    {
                        receivedBytes = ParseDataSize(receivingMatch.Groups[4].Value, receivingMatch.Groups[5].Value);
                    }

                    long transferSpeed = 0;
                    if (receivingMatch.Groups[6].Success)
                    {
                        transferSpeed = ParseDataSize(receivingMatch.Groups[6].Value, receivingMatch.Groups[7].Value);
                    }

                    return (progress, "接收对象", processed, total, receivedBytes, total * (receivedBytes / Math.Max(processed, 1)), transferSpeed);
                }

                // 解析其他进度信息
                var resolvingMatch = Regex.Match(line, @"Resolving deltas:\s+(\d+)%\s+\((\d+)/(\d+)\)");
                if (resolvingMatch.Success)
                {
                    var progress = int.Parse(resolvingMatch.Groups[1].Value);
                    var processed = int.Parse(resolvingMatch.Groups[2].Value);
                    var total = int.Parse(resolvingMatch.Groups[3].Value);
                    return (progress, "解析增量", processed, total, 0, 0, 0);
                }
            }
            catch
            {
                // 忽略解析错误
            }

            return null;
        }

        private long ParseDataSize(string value, string unit)
        {
            if (!double.TryParse(value, out var size))
                return 0;

            return unit.ToLower() switch
            {
                "b" => (long)size,
                "kb" or "kib" => (long)(size * 1024),
                "mb" or "mib" => (long)(size * 1024 * 1024),
                "gb" or "gib" => (long)(size * 1024 * 1024 * 1024),
                _ => (long)size
            };
        }

        private string NormalizeGitUrl(string gitUrl)
        {
            // 标准化Git URL格式
            gitUrl = gitUrl.Trim();

            // 处理GitHub简写格式
            if (Regex.IsMatch(gitUrl, @"^[\w-]+/[\w-]+$"))
            {
                return $"https://github.com/{gitUrl}.git";
            }

            // 确保HTTPS URL以.git结尾
            if (gitUrl.StartsWith("https://") && !gitUrl.EndsWith(".git"))
            {
                return gitUrl + ".git";
            }

            return gitUrl;
        }

        private GitProviderType DetectGitProvider(string gitUrl)
        {
            if (gitUrl.Contains("github.com"))
                return GitProviderType.GitHub;
            if (gitUrl.Contains("gitlab.com"))
                return GitProviderType.GitLab;
            if (gitUrl.Contains("bitbucket.org"))
                return GitProviderType.Bitbucket;
            if (gitUrl.Contains("dev.azure.com") || gitUrl.Contains("visualstudio.com"))
                return GitProviderType.Azure;

            return GitProviderType.Other;
        }

        private string ExtractRepositoryName(string gitUrl)
        {
            try
            {
                var uri = new Uri(gitUrl.Replace("git@", "https://").Replace(":", "/"));
                var segments = uri.Segments;
                var lastSegment = segments.LastOrDefault()?.TrimEnd('/');
                return lastSegment?.Replace(".git", "") ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<string?> GetDefaultBranchAsync(string gitUrl, CancellationToken cancellationToken)
        {
            try
            {
                var args = $"ls-remote --symref {gitUrl} HEAD";
                var result = await ExecuteGitCommandSimpleAsync(args, cancellationToken);

                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    var match = Regex.Match(result.Output, @"ref: refs/heads/([^\s]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return null;
        }

        private async Task<(long TotalBytes, int TotalFiles)> GetCloneInfoAsync(string localPath)
        {
            try
            {
                if (!Directory.Exists(localPath))
                    return (0, 0);

                var files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\.git\\"))
                    .ToArray();

                var totalFiles = files.Length;
                var totalBytes = files.Sum(f =>
                {
                    try
                    {
                        return new FileInfo(f).Length;
                    }
                    catch
                    {
                        return 0;
                    }
                });

                return (totalBytes, totalFiles);
            }
            catch
            {
                return (0, 0);
            }
        }

        #endregion
    }
}
