using Avalonia.Controls;
using ProjectIgnite.Services;
using ProjectIgnite.ViewModels;

namespace ProjectIgnite.Views
{
    public partial class ProjectLauncherView : UserControl
    {
        public ProjectLauncherView()
        {
            InitializeComponent();
            
            var viewModel = ServiceLocator.GetService<ProjectLauncherViewModel>();
            DataContext = viewModel;
        }
    }
}
