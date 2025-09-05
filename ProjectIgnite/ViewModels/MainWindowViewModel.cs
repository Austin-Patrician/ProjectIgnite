using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectIgnite.Models;
using ProjectIgnite.Services;
using ProjectIgnite.Views;

namespace ProjectIgnite.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ThemeService _themeService;

        [ObservableProperty]
        private string _greeting = "Welcome to ProjectIgnite!";

        [ObservableProperty]
        private NavigationItem? _selectedNavigationItem;

        [ObservableProperty]
        private string _currentPageTitle = "首页";

        [ObservableProperty]
        private bool _isDarkMode;

        [ObservableProperty]
        private bool _isLogPanelVisible = true;

        [ObservableProperty]
    private string _currentContentType = "Home";

    [ObservableProperty]
    private object? _currentContent;

        public ObservableCollection<NavigationItem> NavigationItems { get; }
        public ObservableCollection<LogMessage> LogMessages { get; }

        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleLogPanelCommand { get; }
        public ICommand ClearLogsCommand { get; }

        public MainWindowViewModel()
        {
            _themeService = new ThemeService();
            NavigationItems = new ObservableCollection<NavigationItem>();
            LogMessages = new ObservableCollection<LogMessage>();

            // 初始化命令
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            ToggleLogPanelCommand = new RelayCommand(ToggleLogPanel);
            ClearLogsCommand = new RelayCommand(ClearLogs);

            // 初始化导航菜单
            InitializeNavigationItems();
            InitializeDefaultContent();

            // 订阅主题变化事件
            _themeService.ThemeChanged += OnThemeChanged;
            IsDarkMode = _themeService.IsDarkMode;

            // 添加示例日志
            AddSampleLogs();
        }

        private void InitializeNavigationItems()
        {
            var navigateCommand = new RelayCommand<NavigationItem>(OnNavigate);
            
            NavigationItems.Add(new NavigationItem("首页", "🏠", "Home") { NavigateCommand = navigateCommand });
            NavigationItems.Add(new NavigationItem("项目源", "📁", "ProjectSource") { NavigateCommand = navigateCommand });
            NavigationItems.Add(new NavigationItem("项目结构", "🏗️", "ProjectStructure") { NavigateCommand = navigateCommand });
            NavigationItems.Add(new NavigationItem("Project Launcher", "🚀", "ProjectLauncher") { NavigateCommand = navigateCommand });
            NavigationItems.Add(new NavigationItem("设置", "⚙️", "Settings") { NavigateCommand = navigateCommand });
            NavigationItems.Add(new NavigationItem("日志", "📋", "Logs") { NavigateCommand = navigateCommand });
            NavigationItems.Add(new NavigationItem("关于", "ℹ️", "About") { NavigateCommand = navigateCommand });

            // 默认选中第一项
            if (NavigationItems.Count > 0)
            {
                SelectedNavigationItem = NavigationItems[0];
                SelectedNavigationItem.IsSelected = true;
            }
        }

        private void OnNavigate(NavigationItem? item)
        {
            if (item == null) return;

            // 取消之前选中的项
            if (SelectedNavigationItem != null)
            {
                SelectedNavigationItem.IsSelected = false;
            }

            // 选中新项
            SelectedNavigationItem = item;
            item.IsSelected = true;
            CurrentPageTitle = item.Title;
            CurrentContentType = item.TargetView;

            // 根据目标视图设置内容
            switch (item.TargetView)
            {
                case "ProjectSource":
                    CurrentContent = new ProjectSourceView();
                    break;
                case "ProjectStructure":
                    CurrentContent = new ProjectStructureView();
                    break;
                case "ProjectLauncher":
                    CurrentContent = new ProjectLauncherView();
                    break;
                case "Home":
                    InitializeDefaultContent();
                    break;
                default:
                    // 其他页面保持原有逻辑
                    break;
            }

            // 添加导航日志
            AddLog(LogLevel.Info, $"导航到 {item.Title} 页面", "Navigation");
        }

        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
        }

        private void OnThemeChanged(object? sender, Avalonia.Styling.ThemeVariant theme)
        {
            IsDarkMode = _themeService.IsDarkMode;
            AddLog(LogLevel.Info, $"主题已切换到 {(IsDarkMode ? "深色" : "浅色")} 模式", "Theme");
        }

        private void ToggleLogPanel()
        {
            IsLogPanelVisible = !IsLogPanelVisible;
            AddLog(LogLevel.Info, $"日志面板已{(IsLogPanelVisible ? "显示" : "隐藏")}", "UI");
        }

        private void ClearLogs()
        {
            LogMessages.Clear();
            AddLog(LogLevel.Info, "日志已清空", "System");
        }

        public void AddLog(LogLevel level, string message, string source = "")
        {
            var logMessage = new LogMessage(level, message, source);
            LogMessages.Add(logMessage);

            // 限制日志数量，避免内存占用过多
            while (LogMessages.Count > 1000)
            {
                LogMessages.RemoveAt(0);
            }
        }

        private void AddSampleLogs()
        {
            AddLog(LogLevel.Info, "应用程序启动完成", "System");
            AddLog(LogLevel.Info, "MVVM架构初始化完成", "Framework");
            AddLog(LogLevel.Debug, "导航菜单加载完成", "Navigation");
        }

        private void InitializeDefaultContent()
        {
            // 创建默认主页内容
            var defaultContent = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(24),
                Child = new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = Greeting,
                                FontSize = 24,
                                FontWeight = FontWeight.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Margin = new Thickness(0, 0, 0, 24)
                            },
                            new Border
                            {
                                Background = new SolidColorBrush(Color.FromArgb(255, 248, 249, 250)),
                                CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(24),
                                MinHeight = 300,
                                Child = new StackPanel
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = "📄",
                                            FontSize = 48,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Margin = new Thickness(0, 0, 0, 16)
                                        },
                                        new TextBlock
                                        {
                                            Text = "主内容区域",
                                            FontSize = 18,
                                            FontWeight = FontWeight.SemiBold,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Margin = new Thickness(0, 0, 0, 8)
                                        },
                                        new TextBlock
                                        {
                                            Text = "这里将显示不同页面的具体内容",
                                            FontSize = 14,
                                            Foreground = new SolidColorBrush(Color.FromArgb(255, 108, 117, 125)),
                                            HorizontalAlignment = HorizontalAlignment.Center
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            
            CurrentContent = defaultContent;
        }
    }
}
