using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ProjectIgnite.ViewModels;

namespace ProjectIgnite.Views;

/// <summary>
/// 项目分析器视图
/// </summary>
public partial class ProjectAnalyzerView : UserControl
{
    public ProjectAnalyzerView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public ProjectAnalyzerView(ProjectAnalyzerViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}