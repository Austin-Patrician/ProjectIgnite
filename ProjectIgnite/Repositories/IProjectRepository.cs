using ProjectIgnite.Models;
using ProjectIgnite.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectIgnite.Repositories
{
    /// <summary>
    /// 项目源数据访问接口
    /// </summary>
    public interface IProjectRepository
    {
        #region 项目源管理
        
        /// <summary>
        /// 获取所有项目源
        /// </summary>
        /// <returns>项目源列表</returns>
        Task<List<ProjectSourceInfo>> GetAllProjectsAsync();

        /// <summary>
        /// 根据ID获取项目源
        /// </summary>
        /// <param name="id">项目ID</param>
        /// <returns>项目源信息</returns>
        Task<ProjectSourceInfo?> GetProjectByIdAsync(int id);

        /// <summary>
        /// 根据Git URL获取项目源
        /// </summary>
        /// <param name="gitUrl">Git URL</param>
        /// <returns>项目源信息</returns>
        Task<ProjectSourceInfo?> GetProjectByGitUrlAsync(string gitUrl);

        /// <summary>
        /// 根据本地路径获取项目源
        /// </summary>
        /// <param name="localPath">本地路径</param>
        /// <returns>项目源信息</returns>
        Task<ProjectSourceInfo?> GetProjectByLocalPathAsync(string localPath);

        /// <summary>
        /// 创建新的项目源
        /// </summary>
        /// <param name="projectSource">项目源实体</param>
        /// <returns>创建的项目源ID</returns>
        Task<int> CreateProjectAsync(ProjectSource projectSource);

        /// <summary>
        /// 更新项目源
        /// </summary>
        /// <param name="projectSource">项目源实体</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateProjectAsync(ProjectSource projectSource);

        /// <summary>
        /// 删除项目源
        /// </summary>
        /// <param name="id">项目ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteProjectAsync(int id);

        /// <summary>
        /// 更新项目状态
        /// </summary>
        /// <param name="id">项目ID</param>
        /// <param name="status">新状态</param>
        /// <param name="errorMessage">错误信息（可选）</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateProjectStatusAsync(int id, string status, string? errorMessage = null);

        /// <summary>
        /// 更新项目进度
        /// </summary>
        /// <param name="id">项目ID</param>
        /// <param name="cloneProgress">克隆进度</param>
        /// <param name="analysisProgress">分析进度</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateProjectProgressAsync(int id, int? cloneProgress = null, int? analysisProgress = null);

        #endregion

        #region 语言分析管理

        /// <summary>
        /// 获取项目的语言分析结果
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>语言分析结果列表</returns>
        Task<List<LanguageAnalysis>> GetProjectLanguagesAsync(int projectId);

        /// <summary>
        /// 保存语言分析结果
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="languages">语言分析结果列表</param>
        /// <returns>是否保存成功</returns>
        Task<bool> SaveLanguageAnalysisAsync(int projectId, List<LanguageDetail> languages);

        /// <summary>
        /// 清除项目的语言分析结果
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>是否清除成功</returns>
        Task<bool> ClearLanguageAnalysisAsync(int projectId);

        #endregion

        #region 克隆历史管理

        /// <summary>
        /// 获取项目的克隆历史
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="limit">限制数量</param>
        /// <returns>克隆历史列表</returns>
        Task<List<CloneHistory>> GetCloneHistoryAsync(int projectId, int limit = 10);

        /// <summary>
        /// 创建克隆历史记录
        /// </summary>
        /// <param name="cloneHistory">克隆历史实体</param>
        /// <returns>创建的历史记录ID</returns>
        Task<int> CreateCloneHistoryAsync(CloneHistory cloneHistory);

        /// <summary>
        /// 更新克隆历史记录
        /// </summary>
        /// <param name="cloneHistory">克隆历史实体</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateCloneHistoryAsync(CloneHistory cloneHistory);

        /// <summary>
        /// 获取正在进行的克隆记录
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>正在进行的克隆记录</returns>
        Task<CloneHistory?> GetActiveCloneHistoryAsync(int projectId);

        #endregion

        #region 搜索和筛选

        /// <summary>
        /// 搜索项目源
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="status">状态筛选</param>
        /// <param name="language">语言筛选</param>
        /// <returns>匹配的项目源列表</returns>
        Task<List<ProjectSourceInfo>> SearchProjectsAsync(string? keyword = null, string? status = null, string? language = null);

        /// <summary>
        /// 获取所有使用的编程语言
        /// </summary>
        /// <returns>语言列表</returns>
        Task<List<string>> GetAllLanguagesAsync();

        /// <summary>
        /// 获取项目统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<ProjectStatistics> GetProjectStatisticsAsync();

        #endregion
    }

    /// <summary>
    /// 项目统计信息
    /// </summary>
    public class ProjectStatistics
    {
        /// <summary>
        /// 总项目数
        /// </summary>
        public int TotalProjects { get; set; }

        /// <summary>
        /// 已完成项目数
        /// </summary>
        public int CompletedProjects { get; set; }

        /// <summary>
        /// 正在处理的项目数
        /// </summary>
        public int ProcessingProjects { get; set; }

        /// <summary>
        /// 错误项目数
        /// </summary>
        public int ErrorProjects { get; set; }

        /// <summary>
        /// 总代码行数
        /// </summary>
        public long TotalLines { get; set; }

        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 使用的编程语言数量
        /// </summary>
        public int LanguageCount { get; set; }
    }
}