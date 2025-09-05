using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProjectIgnite.Models;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 图表生成服务接口
    /// 负责处理本地项目到架构图表的转换
    /// </summary>
    public interface IDiagramService
    {
        /// <summary>
        /// 分析本地项目并生成架构图表
        /// </summary>
        /// <param name="projectPath">本地项目路径</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="customInstructions">自定义指令（可选）</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图表生成结果</returns>
        Task<DiagramResult> AnalyzeLocalProjectAsync(
            string projectPath,
            string projectName,
            string? customInstructions = null,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 重新分析现有项目
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="customInstructions">自定义指令</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图表生成结果</returns>
        Task<DiagramResult> RegenerateAnalysisAsync(
            string projectPath,
            string projectName,
            string? customInstructions = null,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 修改现有图表
        /// </summary>
        /// <param name="currentDiagram">当前图表的 Mermaid 代码</param>
        /// <param name="instructions">修改指令</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>修改后的图表代码</returns>
        Task<string> ModifyDiagramAsync(
            string currentDiagram,
            string instructions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 导出图表
        /// </summary>
        /// <param name="diagramContent">图表内容</param>
        /// <param name="format">导出格式</param>
        /// <param name="outputPath">输出路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>导出的文件数据</returns>
        Task<byte[]> ExportDiagramAsync(
            string diagramContent,
            ExportFormat format,
            string outputPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取已保存的项目分析结果
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>分析结果，如果不存在则返回null</returns>
        Task<DiagramResult?> GetSavedAnalysisAsync(string projectPath);

        /// <summary>
        /// 保存分析结果到本地
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="result">分析结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SaveAnalysisAsync(string projectPath, DiagramResult result, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 图表生成结果
    /// </summary>
    public class DiagramResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Mermaid 图表代码
        /// </summary>
        public string? MermaidCode { get; set; }

        /// <summary>
        /// 架构说明
        /// </summary>
        public string? Explanation { get; set; }

        /// <summary>
        /// 组件映射（组件名 -> 文件路径）
        /// </summary>
        public Dictionary<string, string>? ComponentMapping { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static DiagramResult Success(string mermaidCode, string explanation, Dictionary<string, string>? componentMapping = null)
        {
            return new DiagramResult
            {
                IsSuccess = true,
                MermaidCode = mermaidCode,
                Explanation = explanation,
                ComponentMapping = componentMapping
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static DiagramResult Failure(string errorMessage)
        {
            return new DiagramResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// 生成进度信息
    /// </summary>
    public class GenerationProgress
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        public GenerationState State { get; set; }

        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// 当前消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 详细信息
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="state">新状态</param>
        /// <param name="percentage">进度百分比</param>
        /// <param name="message">消息</param>
        /// <param name="details">详细信息</param>
        public void Update(GenerationState state, double percentage, string message, string? details = null)
        {
            State = state;
            Percentage = Math.Clamp(percentage, 0, 100);
            Message = message;
            Details = details;
            ErrorMessage = null; // 清除之前的错误信息
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="details">错误详细信息</param>
        public void SetError(string errorMessage, string? details = null)
        {
            State = GenerationState.Error;
            Message = "生成失败";
            ErrorMessage = errorMessage;
            Details = details;
        }

        /// <summary>
        /// 设置取消状态
        /// </summary>
        public void SetCancelled()
        {
            State = GenerationState.Cancelled;
            Message = "已取消";
            ErrorMessage = null;
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public void Reset()
        {
            State = GenerationState.Idle;
            Percentage = 0;
            Message = string.Empty;
            Details = null;
            ErrorMessage = null;
        }
    }

    /// <summary>
    /// 导出格式
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// PNG 图片
        /// </summary>
        Png,

        /// <summary>
        /// SVG 矢量图
        /// </summary>
        Svg,

        /// <summary>
        /// Mermaid 源码
        /// </summary>
        Mermaid
    }
}
