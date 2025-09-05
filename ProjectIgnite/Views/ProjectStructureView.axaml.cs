using Avalonia;
using Avalonia.Controls;
using ProjectIgnite.ViewModels;
using System;
using System.IO;
using Avalonia.VisualTree;
using ProjectIgnite.Services;

namespace ProjectIgnite.Views
{
    public partial class ProjectStructureView : UserControl
    {
        private ProjectStructureViewModel? viewModel;

        public ProjectStructureView()
        {
            InitializeComponent();
            DataContext = ServiceLocator.GetService<ProjectStructureViewModel>();
        }



        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is ProjectStructureViewModel vm)
            {
                // Unsubscribe from previous view model
                if (viewModel != null)
                {
                    viewModel.RequestBrowseProjectFolder -= OnRequestBrowseProjectFolder;
                }
                
                viewModel = vm;
                viewModel.RequestBrowseProjectFolder += OnRequestBrowseProjectFolder;
            }
        }



        private async void OnRequestBrowseProjectFolder()
        {
            if (viewModel == null)
                return;

            var owner = this.FindAncestorOfType<Window>();
            if (owner == null)
            {
                // Fallback: try TopLevel
                owner = TopLevel.GetTopLevel(this) as Window;
            }

            var dialog = new OpenFolderDialog
            {
                Title = "选择项目目录",
            };

            // 默认目录：文档目录
            string defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // 若已有路径，尽量预选其父目录
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedProjectPath))
            {
                try
                {
                    var path = viewModel.SelectedProjectPath;
                    var candidate = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate))
                    {
                        dialog.Directory = candidate;
                    }
                }
                catch
                {
                    // ignore invalid path
                }
            }

            if (string.IsNullOrEmpty(dialog.Directory))
            {
                dialog.Directory = defaultDir;
            }

            var result = owner != null
                ? await dialog.ShowAsync(owner)
                : await dialog.ShowAsync((Window)null);

            if (!string.IsNullOrEmpty(result))
            {
                viewModel.SelectedProjectPath = result;
            }
        }




    }


}
