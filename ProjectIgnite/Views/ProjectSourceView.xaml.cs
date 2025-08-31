using Avalonia.Controls;
using ProjectIgnite.ViewModels;
using ProjectIgnite.Services;
using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace ProjectIgnite.Views
{
    public partial class ProjectSourceView : UserControl
    {
        public ProjectSourceView()
        {
            InitializeComponent();
            var viewModel = ServiceLocator.GetService<ProjectSourceViewModel>();
            DataContext = viewModel;
            
            // 订阅添加项目请求事件
            if (viewModel != null)
            {
                viewModel.OnAddProjectRequested += OnAddProjectRequested;
            }
        }

        private async void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ProjectSourceViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private async void OnAddProjectRequested()
        {
            try
            {
                // 创建AddProjectDialog对话框
                var dialogViewModel = ServiceLocator.GetService<AddProjectDialogViewModel>();
                var dialog = new AddProjectDialog(dialogViewModel);

                // 显示对话框并获取结果
                var topLevel = TopLevel.GetTopLevel(this);
                var result = await dialog.ShowDialog<bool?>(topLevel as Window);

                // 如果用户确认添加项目
                if (result == true && DataContext is ProjectSourceViewModel viewModel)
                {
                    var cloneRequest = dialogViewModel.CreateCloneRequest();
                    await viewModel.AddProjectAsync(cloneRequest);
                }
            }
            catch (Exception ex)
            {
                // 处理异常，可以添加日志记录
                System.Diagnostics.Debug.WriteLine($"添加项目时发生错误: {ex.Message}");
            }
        }
    }
}