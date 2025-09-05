using Microsoft.EntityFrameworkCore;
using ProjectIgnite.Data;
using ProjectIgnite.Models;
using ProjectIgnite.DTOs;
using ProjectIgnite.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ProjectIgnite.Repositories
{
    /// <summary>
    /// 项目源数据访问实现
    /// </summary>
    public class ProjectRepository : IProjectRepository
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<ProjectIgniteDbContext> _contextFactory;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<ProjectRepository>? _logger;

        public ProjectRepository(IServiceProvider serviceProvider, IDbContextFactory<ProjectIgniteDbContext> contextFactory, ILogger<ProjectRepository>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _contextFactory = contextFactory;
            _logger = logger;
        }

        #region 项目源管理

        public async Task<List<ProjectSourceInfo>> GetAllProjectsAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var projects = await context.ProjectSources
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var result = new List<ProjectSourceInfo>();
                foreach (var project in projects)
                {
                    try
                    {
                        var info = await MapToProjectSourceInfoAsync(project);
                        result.Add(info);
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但继续处理其他项目
                        Console.WriteLine($"Error mapping project {project.Id}: {ex.Message}");

                        // 添加一个错误状态的项目信息
                        result.Add(new ProjectSourceInfo
                        {
                            Id = project.Id,
                            Name = project.Name ?? $"Project {project.Id}",
                            Status = ProjectStatus.Error,
                            ErrorMessage = $"数据加载错误: {ex.Message}",
                            CreatedAt = project.CreatedAt,
                            UpdatedAt = project.UpdatedAt
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // 如果完全失败，返回空列表并记录错误
                Console.WriteLine($"Error getting all projects: {ex.Message}");
                return new List<ProjectSourceInfo>();
            }
        }

        public async Task<ProjectSourceInfo?> GetProjectByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return null;

                using var context = await _contextFactory.CreateDbContextAsync();
                var project = await context.ProjectSources
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (project == null)
                    return null;

                return await MapToProjectSourceInfoAsync(project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting project by ID {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<ProjectSourceInfo?> GetProjectByGitUrlAsync(string gitUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gitUrl))
                    return null;

                using var context = await _contextFactory.CreateDbContextAsync();
                var project = await context.ProjectSources
                    .FirstOrDefaultAsync(p => p.GitUrl == gitUrl);

                if (project == null)
                    return null;

                return await MapToProjectSourceInfoAsync(project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting project by Git URL {gitUrl}: {ex.Message}");
                return null;
            }
        }

        public async Task<ProjectSourceInfo?> GetProjectByLocalPathAsync(string localPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(localPath))
                    return null;

                using var context = await _contextFactory.CreateDbContextAsync();
                var project = await context.ProjectSources
                    .FirstOrDefaultAsync(p => p.LocalPath == localPath);

                if (project == null)
                    return null;

                return await MapToProjectSourceInfoAsync(project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting project by local path {localPath}: {ex.Message}");
                return null;
            }
        }

        public async Task<int> CreateProjectAsync(ProjectSource projectSource)
        {
            if (projectSource == null)
                throw new ArgumentNullException(nameof(projectSource));

            // 验证必需字段
            if (string.IsNullOrWhiteSpace(projectSource.Name))
                throw new ArgumentException("Project name is required", nameof(projectSource));

            if (string.IsNullOrWhiteSpace(projectSource.GitUrl))
                throw new ArgumentException("Git URL is required", nameof(projectSource));

            return await RetryHelper.ExecuteWithRetryAsync(async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                projectSource.CreatedAt = DateTime.Now;
                projectSource.UpdatedAt = DateTime.Now;

                // 设置默认状态
                if (string.IsNullOrWhiteSpace(projectSource.Status))
                    projectSource.Status = "pending";

                context.ProjectSources.Add(projectSource);
                await context.SaveChangesAsync();

                return projectSource.Id;
            }, maxRetries: 3, delayMs: 500, _logger, "CreateProject");
        }

        public async Task<bool> UpdateProjectAsync(ProjectSource projectSource)
        {
            if (projectSource == null)
                return false;

            try
            {
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    projectSource.UpdatedAt = DateTime.Now;
                    context.ProjectSources.Update(projectSource);
                    await context.SaveChangesAsync();
                }, maxRetries: 3, delayMs: 500, _logger, "UpdateProject");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update project {ProjectId}", projectSource.Id);
                return false;
            }
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            try
            {
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    var project = await context.ProjectSources.FindAsync(id);
                    if (project == null)
                        throw new InvalidOperationException($"Project with id {id} not found");

                    context.ProjectSources.Remove(project);
                    await context.SaveChangesAsync();
                }, maxRetries: 3, delayMs: 500, _logger, "DeleteProject");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete project {ProjectId}", id);
                return false;
            }
        }

        public async Task<bool> UpdateProjectStatusAsync(int id, string status, string? errorMessage = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    var project = await context.ProjectSources.FindAsync(id);
                    if (project == null)
                        throw new InvalidOperationException($"Project with id {id} not found");

                    project.Status = status;
                    project.ErrorMessage = errorMessage;
                    project.UpdatedAt = DateTime.Now;

                    await context.SaveChangesAsync();
                }, maxRetries: 3, delayMs: 500, _logger, "UpdateProjectStatus");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update project status for project {ProjectId}", id);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 更新项目进度（简化版本 - 仅在必要时使用）
        /// 注意：此方法已简化，建议使用UpdateProjectStatusAsync来更新状态
        /// </summary>
        public async Task<bool> UpdateProjectProgressAsync(int id, int? cloneProgress = null,
            int? analysisProgress = null)
        {
            // 如果没有提供任何进度值，直接返回成功
            if (!cloneProgress.HasValue && !analysisProgress.HasValue)
                return true;

            await _semaphore.WaitAsync();
            try
            {
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    var project = await context.ProjectSources.FindAsync(id);
                    if (project == null)
                        throw new InvalidOperationException($"Project with id {id} not found");

                    // 仅在提供值时更新进度
                    if (cloneProgress.HasValue)
                        project.CloneProgress = cloneProgress.Value;

                    if (analysisProgress.HasValue)
                        project.AnalysisProgress = analysisProgress.Value;

                    project.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                }, maxRetries: 3, delayMs: 500, _logger, "UpdateProjectProgress");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update project progress for project {ProjectId}", id);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        #endregion

        #region 语言分析管理

        public async Task<List<LanguageAnalysis>> GetProjectLanguagesAsync(int projectId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var results = await context.LanguageAnalyses
                .Where(la => la.ProjectId == projectId)
                .ToListAsync();

            return results
                .OrderByDescending(la => la.Percentage)
                .ToList();
        }

        public async Task<bool> SaveLanguageAnalysisAsync(int projectId, List<LanguageDetail> languages)
        {
            if (languages == null || !languages.Any())
                return false;

            try
            {
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    // 清除现有的语言分析结果
                    await ClearLanguageAnalysisAsync(projectId);

                    using var context = await _contextFactory.CreateDbContextAsync();
                    // 添加新的语言分析结果
                    var analysisResults = languages.Select(lang => new LanguageAnalysis
                    {
                        ProjectId = projectId,
                        Language = lang.Name,
                        LineCount = lang.LineCount,
                        FileCount = lang.FileCount,
                        Percentage = lang.Percentage,
                        ByteCount = lang.ByteCount,
                        AnalyzedAt = DateTime.Now
                    }).ToList();

                    context.LanguageAnalyses.AddRange(analysisResults);
                    await context.SaveChangesAsync();

                    // 更新项目的主要语言
                    //这要移除json , markdown 等非编程语言
                    string[] nonProgrammingLanguages = new[]
                        { "JSON", "Markdown", "Text", "XML", "YAML", "HTML", "CSS", "Shell", "Batchfile", "Makefile" };
                    var primaryLanguage = languages.Where(_ => !nonProgrammingLanguages.Contains(_.Name))
                        .MaxBy(l => l.Percentage);
                    if (primaryLanguage != null)
                    {
                        var project = await context.ProjectSources.FindAsync(projectId);
                        if (project != null)
                        {
                            project.PrimaryLanguage = primaryLanguage.Name;
                            project.LastAnalyzedAt = DateTime.Now;
                            project.UpdatedAt = DateTime.Now;
                            await context.SaveChangesAsync();
                        }
                    }
                }, maxRetries: 3, delayMs: 500, _logger, "SaveLanguageAnalysis");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save language analysis for project {ProjectId}", projectId);
                return false;
            }
        }

        public async Task<bool> ClearLanguageAnalysisAsync(int projectId)
        {
            try
            {
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    var existingAnalyses = await context.LanguageAnalyses
                        .Where(la => la.ProjectId == projectId)
                        .ToListAsync();

                    if (existingAnalyses.Any())
                    {
                        context.LanguageAnalyses.RemoveRange(existingAnalyses);
                        await context.SaveChangesAsync();
                    }
                }, maxRetries: 3, delayMs: 500, _logger, "ClearLanguageAnalysis");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear language analysis for project {ProjectId}", projectId);
                return false;
            }
        }

        #endregion

        #region 克隆历史管理

        public async Task<List<CloneHistory>> GetCloneHistoryAsync(int projectId, int limit = 10)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.CloneHistories
                .Where(ch => ch.ProjectId == projectId)
                .OrderByDescending(ch => ch.StartTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> CreateCloneHistoryAsync(CloneHistory cloneHistory)
        {
            if (cloneHistory == null)
                throw new ArgumentNullException(nameof(cloneHistory));

            try
            {
                int historyId = 0;
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    cloneHistory.StartTime = DateTime.Now;
                    context.CloneHistories.Add(cloneHistory);
                    await context.SaveChangesAsync();
                    historyId = cloneHistory.Id;
                }, maxRetries: 3, delayMs: 500, _logger, "CreateCloneHistory");
                
                return historyId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create clone history for project {ProjectId}", cloneHistory.ProjectId);
                throw;
            }
        }

        public async Task<bool> UpdateCloneHistoryAsync(CloneHistory cloneHistory)
        {
            if (cloneHistory == null)
                return false;

            try
            {
                await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    context.CloneHistories.Update(cloneHistory);
                    await context.SaveChangesAsync();
                }, maxRetries: 3, delayMs: 500, _logger, "UpdateCloneHistory");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update clone history {HistoryId}", cloneHistory.Id);
                return false;
            }
        }

        public async Task<CloneHistory?> GetActiveCloneHistoryAsync(int projectId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.CloneHistories
                .Where(ch => ch.ProjectId == projectId &&
                             (ch.Status == "started" || ch.Status == "progress"))
                .OrderByDescending(ch => ch.StartTime)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region 搜索和筛选

        public async Task<List<ProjectSourceInfo>> SearchProjectsAsync(string? keyword = null, string? status = null,
            string? language = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.ProjectSources.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) ||
                                         p.Description!.Contains(keyword) ||
                                         p.GitUrl.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                query = query.Where(p => p.PrimaryLanguage == language);
            }

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = new List<ProjectSourceInfo>();
            foreach (var project in projects)
            {
                var info = await MapToProjectSourceInfoAsync(project);
                result.Add(info);
            }

            return result;
        }

        public async Task<List<string>> GetAllLanguagesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LanguageAnalyses
                .Select(la => la.Language)
                .Distinct()
                .OrderBy(lang => lang)
                .ToListAsync();
        }

        public async Task<ProjectStatistics> GetProjectStatisticsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var totalProjects = await context.ProjectSources.CountAsync();
            var completedProjects = await context.ProjectSources.CountAsync(p => p.Status == "completed");
            var processingProjects =
                await context.ProjectSources.CountAsync(p => p.Status == "cloning" || p.Status == "analyzing");
            var errorProjects = await context.ProjectSources.CountAsync(p => p.Status == "error");

            // 定义非编程语言列表
            var nonProgrammingLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "JSON", "XML", "YAML", "TOML", "INI", "Configuration",
                "Markdown", "Text", "reStructuredText", "TeX", "LaTeX",
                "CSV", "TSV", "Database", "SQLite",
                "Ignore List", "Git Attributes", "EditorConfig", "Environment", "Log",
                "MSBuild", "Microsoft Visual Studio Solution",
                "Binary", "Image", "Audio", "Video", "Archive", "Font"
            };

            // 只计算编程语言的代码行数
            var totalLines = await context.LanguageAnalyses
                .Where(la => !nonProgrammingLanguages.Contains(la.Language))
                .SumAsync(la => la.LineCount);
            var totalFiles = await context.LanguageAnalyses.SumAsync(la => la.FileCount);
            var languageCount = await context.LanguageAnalyses.Select(la => la.Language).Distinct().CountAsync();

            return new ProjectStatistics
            {
                TotalProjects = totalProjects,
                CompletedProjects = completedProjects,
                ProcessingProjects = processingProjects,
                ErrorProjects = errorProjects,
                TotalLines = totalLines,
                TotalFiles = totalFiles,
                LanguageCount = languageCount
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将ProjectSource实体映射为ProjectSourceInfo DTO
        /// </summary>
        private async Task<ProjectSourceInfo> MapToProjectSourceInfoAsync(ProjectSource project)
        {
            try
            {
                var languages = await GetProjectLanguagesAsync(project.Id);
                var languageDetails = languages?.Select(la => new LanguageDetail
                {
                    Name = la.Language ?? string.Empty,
                    FileCount = la.FileCount,
                    LineCount = la.LineCount,
                    ByteCount = la.ByteCount,
                    Percentage = la.Percentage
                }).ToList() ?? new List<LanguageDetail>();

                // 定义非编程语言列表
                var nonProgrammingLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "JSON", "XML", "YAML", "TOML", "INI", "Configuration",
                    "Markdown", "Text", "reStructuredText", "TeX", "LaTeX",
                    "CSV", "TSV", "Database", "SQLite",
                    "Ignore List", "Git Attributes", "EditorConfig", "Environment", "Log",
                    "MSBuild", "Microsoft Visual Studio Solution",
                    "Binary", "Image", "Audio", "Video", "Archive", "Font"
                };

                // 计算统计数据，确保非负值
                var totalFiles = Math.Max(0, languages?.Sum(la => la.FileCount) ?? 0);
                // 只计算编程语言的代码行数
                var programmingLanguages =
                    languages?.Where(la => !nonProgrammingLanguages.Contains(la.Language ?? "")) ??
                    Enumerable.Empty<LanguageAnalysis>();
                var totalLines = Math.Max(0, (int)(programmingLanguages.Sum(la => la.LineCount)));
                var totalSize = Math.Max(0, languages?.Sum(la => la.ByteCount) ?? 0);

                // 计算项目文件夹实际大小
                var actualProjectSize = GetDirectorySize(project.LocalPath);

                // 构建语言百分比字典
                var languagePercentages = languages?.ToDictionary(
                    la => la.Language ?? "Unknown",
                    la => (double)la.Percentage
                ) ?? new Dictionary<string, double>();

                return new ProjectSourceInfo
                {
                    Id = project.Id,
                    Name = project.Name ?? string.Empty,
                    GitUrl = project.GitUrl ?? string.Empty,
                    LocalPath = project.LocalPath ?? string.Empty,
                    Description = project.Description ?? string.Empty,
                    PrimaryLanguage = project.PrimaryLanguage ?? string.Empty,
                    Status = Enum.TryParse<ProjectStatus>(project.Status, true, out var status)
                        ? status
                        : ProjectStatus.Pending,
                    CloneProgress = Math.Max(0, Math.Min(100, project.CloneProgress)),
                    AnalysisProgress = Math.Max(0, Math.Min(100, project.AnalysisProgress)),
                    ErrorMessage = project.ErrorMessage,
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt,
                    LastUpdated = project.UpdatedAt, // 设置LastUpdated属性
                    LastClonedAt = project.LastClonedAt,
                    LastAnalyzedAt = project.LastAnalyzedAt,
                    AutoAnalyze = true, // 默认值
                    Languages = languageDetails,
                    LanguagePercentages = languagePercentages,
                    TotalFiles = totalFiles,
                    TotalLines = totalLines,
                    TotalSize = totalSize,
                    LinesOfCode = totalLines, // 同时设置LinesOfCode属性
                    ProjectSize = actualProjectSize // 使用实际文件夹大小
                };
            }
            catch (Exception ex)
            {
                // 如果映射失败，返回基本信息
                return new ProjectSourceInfo
                {
                    Id = project.Id,
                    Name = project.Name ?? string.Empty,
                    GitUrl = project.GitUrl ?? string.Empty,
                    LocalPath = project.LocalPath ?? string.Empty,
                    Description = project.Description ?? string.Empty,
                    PrimaryLanguage = project.PrimaryLanguage ?? string.Empty,
                    Status = Enum.TryParse<ProjectStatus>(project.Status, true, out var status)
                        ? status
                        : ProjectStatus.Error,
                    CloneProgress = Math.Max(0, Math.Min(100, project.CloneProgress)),
                    AnalysisProgress = Math.Max(0, Math.Min(100, project.AnalysisProgress)),
                    ErrorMessage = project.ErrorMessage ?? $"数据映射错误: {ex.Message}",
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt,
                    LastUpdated = project.UpdatedAt,
                    LastClonedAt = project.LastClonedAt,
                    LastAnalyzedAt = project.LastAnalyzedAt,
                    AutoAnalyze = true,
                    Languages = new List<LanguageDetail>(),
                    LanguagePercentages = new Dictionary<string, double>(),
                    TotalFiles = 0,
                    TotalLines = 0,
                    TotalSize = 0,
                    LinesOfCode = 0,
                    ProjectSize = GetDirectorySize(project.LocalPath)
                };
            }
        }

        /// <summary>
        /// 计算目录的实际大小（字节）
        /// </summary>
        private static long GetDirectorySize(string? directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                return 0;

            try
            {
                var directoryInfo = new DirectoryInfo(directoryPath);
                return GetDirectorySizeRecursive(directoryInfo);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 递归计算目录大小
        /// </summary>
        private static long GetDirectorySizeRecursive(DirectoryInfo directoryInfo)
        {
            long size = 0;

            try
            {
                // 计算当前目录中所有文件的大小
                var files = directoryInfo.GetFiles();
                foreach (var file in files)
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch
                    {
                        // 忽略无法访问的文件
                    }
                }

                // 递归计算子目录的大小
                var subDirectories = directoryInfo.GetDirectories();
                foreach (var subDirectory in subDirectories)
                {
                    try
                    {
                        size += GetDirectorySizeRecursive(subDirectory);
                    }
                    catch
                    {
                        // 忽略无法访问的目录
                    }
                }
            }
            catch
            {
                // 忽略访问错误
            }

            return size;
        }

        #endregion
    }
}