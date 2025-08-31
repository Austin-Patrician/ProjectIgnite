using Microsoft.EntityFrameworkCore;
using ProjectIgnite.Data;
using ProjectIgnite.Models;
using ProjectIgnite.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectIgnite.Repositories
{
    /// <summary>
    /// 项目源数据访问实现
    /// </summary>
    public class ProjectRepository : IProjectRepository
    {
        private readonly ProjectIgniteDbContext _context;

        public ProjectRepository(ProjectIgniteDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region 项目源管理

        public async Task<List<ProjectSourceInfo>> GetAllProjectsAsync()
        {
            try
            {
                var projects = await _context.ProjectSources
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

                var project = await _context.ProjectSources
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

                var project = await _context.ProjectSources
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

                var project = await _context.ProjectSources
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
            try
            {
                if (projectSource == null)
                    throw new ArgumentNullException(nameof(projectSource));

                // 验证必需字段
                if (string.IsNullOrWhiteSpace(projectSource.Name))
                    throw new ArgumentException("Project name is required", nameof(projectSource));

                if (string.IsNullOrWhiteSpace(projectSource.GitUrl))
                    throw new ArgumentException("Git URL is required", nameof(projectSource));

                projectSource.CreatedAt = DateTime.Now;
                projectSource.UpdatedAt = DateTime.Now;
                
                // 设置默认状态
                if (string.IsNullOrWhiteSpace(projectSource.Status))
                    projectSource.Status = "pending";

                _context.ProjectSources.Add(projectSource);
                await _context.SaveChangesAsync();

                return projectSource.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating project: {ex.Message}");
                throw; // 重新抛出异常，让调用者处理
            }
        }

        public async Task<bool> UpdateProjectAsync(ProjectSource projectSource)
        {
            try
            {
                projectSource.UpdatedAt = DateTime.Now;
                _context.ProjectSources.Update(projectSource);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            try
            {
                var project = await _context.ProjectSources.FindAsync(id);
                if (project == null)
                    return false;

                _context.ProjectSources.Remove(project);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateProjectStatusAsync(int id, string status, string? errorMessage = null)
        {
            try
            {
                var project = await _context.ProjectSources.FindAsync(id);
                if (project == null)
                    return false;

                project.Status = status;
                project.ErrorMessage = errorMessage;
                project.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateProjectProgressAsync(int id, int? cloneProgress = null, int? analysisProgress = null)
        {
            try
            {
                var project = await _context.ProjectSources.FindAsync(id);
                if (project == null)
                    return false;

                if (cloneProgress.HasValue)
                    project.CloneProgress = cloneProgress.Value;
                
                if (analysisProgress.HasValue)
                    project.AnalysisProgress = analysisProgress.Value;

                project.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 语言分析管理

        public async Task<List<LanguageAnalysis>> GetProjectLanguagesAsync(int projectId)
        {
            var results = await _context.LanguageAnalyses
                .Where(la => la.ProjectId == projectId)
                .ToListAsync();
            
            return results
                .OrderByDescending(la => la.Percentage)
                .ToList();
        }

        public async Task<bool> SaveLanguageAnalysisAsync(int projectId, List<LanguageDetail> languages)
        {
            try
            {
                // 清除现有的语言分析结果
                await ClearLanguageAnalysisAsync(projectId);

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

                _context.LanguageAnalyses.AddRange(analysisResults);
                await _context.SaveChangesAsync();

                // 更新项目的主要语言
                var primaryLanguage = languages.OrderByDescending(l => l.Percentage).FirstOrDefault();
                if (primaryLanguage != null)
                {
                    var project = await _context.ProjectSources.FindAsync(projectId);
                    if (project != null)
                    {
                        project.PrimaryLanguage = primaryLanguage.Name;
                        project.LastAnalyzedAt = DateTime.Now;
                        project.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ClearLanguageAnalysisAsync(int projectId)
        {
            try
            {
                var existingAnalyses = await _context.LanguageAnalyses
                    .Where(la => la.ProjectId == projectId)
                    .ToListAsync();

                if (existingAnalyses.Any())
                {
                    _context.LanguageAnalyses.RemoveRange(existingAnalyses);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 克隆历史管理

        public async Task<List<CloneHistory>> GetCloneHistoryAsync(int projectId, int limit = 10)
        {
            return await _context.CloneHistories
                .Where(ch => ch.ProjectId == projectId)
                .OrderByDescending(ch => ch.StartTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> CreateCloneHistoryAsync(CloneHistory cloneHistory)
        {
            cloneHistory.StartTime = DateTime.Now;
            _context.CloneHistories.Add(cloneHistory);
            await _context.SaveChangesAsync();
            return cloneHistory.Id;
        }

        public async Task<bool> UpdateCloneHistoryAsync(CloneHistory cloneHistory)
        {
            try
            {
                _context.CloneHistories.Update(cloneHistory);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CloneHistory?> GetActiveCloneHistoryAsync(int projectId)
        {
            return await _context.CloneHistories
                .Where(ch => ch.ProjectId == projectId && 
                           (ch.Status == "started" || ch.Status == "progress"))
                .OrderByDescending(ch => ch.StartTime)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region 搜索和筛选

        public async Task<List<ProjectSourceInfo>> SearchProjectsAsync(string? keyword = null, string? status = null, string? language = null)
        {
            var query = _context.ProjectSources.AsQueryable();

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
            return await _context.LanguageAnalyses
                .Select(la => la.Language)
                .Distinct()
                .OrderBy(lang => lang)
                .ToListAsync();
        }

        public async Task<ProjectStatistics> GetProjectStatisticsAsync()
        {
            var totalProjects = await _context.ProjectSources.CountAsync();
            var completedProjects = await _context.ProjectSources.CountAsync(p => p.Status == "completed");
            var processingProjects = await _context.ProjectSources.CountAsync(p => p.Status == "cloning" || p.Status == "analyzing");
            var errorProjects = await _context.ProjectSources.CountAsync(p => p.Status == "error");
            
            var totalLines = await _context.LanguageAnalyses.SumAsync(la => la.LineCount);
            var totalFiles = await _context.LanguageAnalyses.SumAsync(la => la.FileCount);
            var languageCount = await _context.LanguageAnalyses.Select(la => la.Language).Distinct().CountAsync();

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

                // 计算统计数据，确保非负值
                var totalFiles = Math.Max(0, languages?.Sum(la => la.FileCount) ?? 0);
                var totalLines = Math.Max(0, (int)(languages?.Sum(la => la.LineCount) ?? 0));
                var totalSize = Math.Max(0, languages?.Sum(la => la.ByteCount) ?? 0);

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
                    Status = Enum.TryParse<ProjectStatus>(project.Status, true, out var status) ? status : ProjectStatus.Pending,
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
                    ProjectSize = totalSize   // 同时设置ProjectSize属性
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
                    Status = Enum.TryParse<ProjectStatus>(project.Status, true, out var status) ? status : ProjectStatus.Error,
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
                    ProjectSize = 0
                };
            }
        }

        #endregion
    }
}
