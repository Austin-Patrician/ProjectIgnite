using Avalonia.Controls;
using ProjectIgnite.ViewModels;
using System;
using System.ComponentModel;

namespace ProjectIgnite.Views
{
    /// <summary>
    /// CloneProgressView.xaml 的交互逻辑
    /// </summary>
    public partial class CloneProgressView : Window
    {
        public CloneProgressViewModel ViewModel { get; }

        public CloneProgressView(CloneProgressViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
            
            // 订阅ViewModel事件
            viewModel.RequestClose += OnRequestClose;
            
            // 处理窗口关闭事件
            Closing += OnWindowClosing;
        }

        private void OnRequestClose(bool? dialogResult)
        {
            Close();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // 如果克隆正在进行中，直接取消操作
            if (ViewModel.IsCloning && !ViewModel.IsCancelled)
            {
                // 执行取消操作
                ViewModel.CancelCommand?.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件，避免内存泄漏
            ViewModel.RequestClose -= OnRequestClose;
            Closing -= OnWindowClosing;
            
            // 确保取消克隆操作
            if (ViewModel.IsCloning && !ViewModel.IsCancelled)
            {
                ViewModel.CancelCommand?.Execute(null);
            }
            
            base.OnClosed(e);
        }
    }
}