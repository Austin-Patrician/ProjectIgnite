using Avalonia.Controls;
using Avalonia.Interactivity;
using ProjectIgnite.ViewModels;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectIgnite.Views
{
    /// <summary>
    /// AddProjectDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AddProjectDialog : Window
    {
        public AddProjectDialogViewModel ViewModel { get; }
        private CancellationTokenSource? _validationCts;

        public AddProjectDialog(AddProjectDialogViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
            
            // 订阅ViewModel事件
            viewModel.RequestClose += OnRequestClose;
            viewModel.RequestBrowseFolder += OnRequestBrowseFolder;
        }

        private void OnRequestClose(bool? dialogResult)
        {
            Close(dialogResult);
        }

        private async void OnRequestBrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择项目存储目录",
                Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (!string.IsNullOrEmpty(ViewModel.LocalPath) && Directory.Exists(Path.GetDirectoryName(ViewModel.LocalPath)))
            {
                dialog.Directory = Path.GetDirectoryName(ViewModel.LocalPath);
            }

            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                ViewModel.LocalPath = result;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择项目存储目录",
                Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (!string.IsNullOrEmpty(ViewModel.LocalPath) && Directory.Exists(Path.GetDirectoryName(ViewModel.LocalPath)))
            {
                dialog.Directory = Path.GetDirectoryName(ViewModel.LocalPath);
            }

            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                ViewModel.LocalPath = result;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void GitUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 取消之前的验证任务
            _validationCts?.Cancel();
            _validationCts = new CancellationTokenSource();

            var textBox = sender as TextBox;
            if (textBox == null) return;

            var url = textBox.Text?.Trim();
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            try
            {
                // 延迟验证，避免频繁调用
                await Task.Delay(800, _validationCts.Token);
                
                // 如果URL看起来有效，应用错误样式类
                if (IsValidUrlFormat(url))
                {
                    textBox.Classes.Remove("HasError");
                }
                else
                {
                    textBox.Classes.Add("HasError");
                }
            }
            catch (OperationCanceledException)
            {
                // 验证被取消，忽略
            }
        }

        private bool IsValidUrlFormat(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            // 基本的URL格式检查
            return url.StartsWith("http://") || 
                   url.StartsWith("https://") || 
                   url.StartsWith("git@") ||
                   url.Contains("github.com") ||
                   url.Contains("gitlab.com") ||
                   url.Contains("bitbucket.org");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后，聚焦到第一个输入框
            var gitUrlTextBox = this.FindControl<TextBox>("GitUrlTextBox");
            if (gitUrlTextBox != null)
            {
                gitUrlTextBox.Focus();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件，避免内存泄漏
            ViewModel.RequestClose -= OnRequestClose;
            ViewModel.RequestBrowseFolder -= OnRequestBrowseFolder;
            base.OnClosed(e);
        }
    }
}
