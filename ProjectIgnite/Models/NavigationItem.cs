using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 导航菜单项模型
    /// </summary>
    public partial class NavigationItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _icon = string.Empty;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private string _targetView = string.Empty;

        public ICommand? NavigateCommand { get; set; }

        public NavigationItem(string title, string icon = "", string targetView = "")
        {
            Title = title;
            Icon = icon;
            TargetView = targetView;
        }
    }
}