using Avalonia.Controls;
using Avalonia.Threading;
using ProjectIgnite.Services;
using ProjectIgnite.ViewModels;
using ProjectIgnite.Models;
using System;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Threading.Tasks;


namespace ProjectIgnite.Views
{
    /// <summary>
    /// 项目结构视图
    /// 显示项目的文件结构和组织架构
    /// </summary>
    public partial class ProjectStructureView : UserControl
    {
        private ProjectStructureViewModel? _viewModel;

        
        public ProjectStructureView()
        {
            InitializeComponent();
            
            // 设置数据上下文
            _viewModel = ServiceLocator.GetService<ProjectStructureViewModel>();
            DataContext = _viewModel;
            
            
            // 订阅属性变化事件
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }
        

        
        
        /// <summary>
        /// 处理 ViewModel 属性变化
        /// </summary>
        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectStructureViewModel.CurrentDiagram))
            {
                await UpdateDiagramDisplay();
            }
            else if (e.PropertyName == nameof(ProjectStructureViewModel.MermaidCode))
            {
                await UpdateMermaidDiagram();
            }
        }
        
        /// <summary>
        /// 更新图表显示
        /// </summary>
        private async Task UpdateDiagramDisplay()
        {
            if (_viewModel?.CurrentDiagram == null)
            {
                return;
            }
            
            // 图表功能暂时不可用
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// 更新 Mermaid 图表
        /// </summary>
        private async Task UpdateMermaidDiagram()
        {
            if (string.IsNullOrEmpty(_viewModel?.MermaidCode))
            {
                return;
            }
            
            // 图表功能暂时不可用
            await Task.CompletedTask;
        }


        
        /// <summary>
        /// 将 HTML 内容转换为 data URI
        /// </summary>
        private string ConvertHtmlToDataUri(string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            var base64 = Convert.ToBase64String(bytes);
            return $"data:text/html;base64,{base64}";
        }
        
        /// <summary>
        /// 生成 Mermaid HTML 内容
        /// </summary>
        private string GenerateMermaidHtml(string mermaidCode)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='utf-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1'>");
            html.AppendLine("    <title>Architecture Diagram</title>");
            html.AppendLine("    <script src='https://cdn.jsdelivr.net/npm/mermaid@10.6.1/dist/mermaid.min.js'></script>");
            html.AppendLine("    <style>");
            html.AppendLine("        body {");
            html.AppendLine("            margin: 0;");
            html.AppendLine("            padding: 20px;");
            html.AppendLine("            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;");
            html.AppendLine("            background-color: #ffffff;");
            html.AppendLine("            display: flex;");
            html.AppendLine("            justify-content: center;");
            html.AppendLine("            align-items: center;");
            html.AppendLine("            min-height: calc(100vh - 40px);");
            html.AppendLine("        }");
            html.AppendLine("        .mermaid {");
            html.AppendLine("            max-width: 100%;");
            html.AppendLine("            height: auto;");
            html.AppendLine("        }");
            html.AppendLine("        .error {");
            html.AppendLine("            color: #d32f2f;");
            html.AppendLine("            text-align: center;");
            html.AppendLine("            padding: 20px;");
            html.AppendLine("        }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class='mermaid'>");
            html.AppendLine(mermaidCode);
            html.AppendLine("    </div>");
            html.AppendLine("    <script>");
            html.AppendLine("        mermaid.initialize({");
            html.AppendLine("            startOnLoad: true,");
            html.AppendLine("            theme: 'default',");
            html.AppendLine("            securityLevel: 'loose',");
            html.AppendLine("            flowchart: {");
            html.AppendLine("                useMaxWidth: true,");
            html.AppendLine("                htmlLabels: true");
            html.AppendLine("            }");
            html.AppendLine("        });");
            html.AppendLine("        ");
            html.AppendLine("        // 错误处理");
            html.AppendLine("        window.addEventListener('error', function(e) {");
            html.AppendLine("            document.body.innerHTML = '<div class=\"error\"><h3>图表渲染失败</h3><p>' + e.message + '</p></div>';");
            html.AppendLine("        });");
            html.AppendLine("    </script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            
            // 取消订阅事件
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            
        }
    }
}