using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 主题管理服务
    /// </summary>
    public partial class ThemeService : ObservableObject
    {
        [ObservableProperty]
        private ThemeVariant _currentTheme = ThemeVariant.Default;

        [ObservableProperty]
        private bool _isDarkMode;

        public event EventHandler<ThemeVariant>? ThemeChanged;

        public ThemeService()
        {
            // 初始化时检测系统主题
            UpdateThemeState();
        }

        /// <summary>
        /// 切换到指定主题
        /// </summary>
        /// <param name="theme">目标主题</param>
        public void SetTheme(ThemeVariant theme)
        {
            if (CurrentTheme != theme)
            {
                CurrentTheme = theme;
                ApplyTheme(theme);
                UpdateThemeState();
                ThemeChanged?.Invoke(this, theme);
            }
        }

        /// <summary>
        /// 切换主题（在Light和Dark之间切换）
        /// </summary>
        public void ToggleTheme()
        {
            var newTheme = IsDarkMode ? ThemeVariant.Light : ThemeVariant.Dark;
            SetTheme(newTheme);
        }

        /// <summary>
        /// 应用主题到应用程序
        /// </summary>
        /// <param name="theme">要应用的主题</param>
        private void ApplyTheme(ThemeVariant theme)
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = theme;
            }
        }

        /// <summary>
        /// 更新主题状态
        /// </summary>
        private void UpdateThemeState()
        {
            IsDarkMode = CurrentTheme == ThemeVariant.Dark ||
                        (CurrentTheme == ThemeVariant.Default && IsSystemDarkMode());
        }

        /// <summary>
        /// 检测系统是否为深色模式
        /// </summary>
        /// <returns>是否为深色模式</returns>
        private bool IsSystemDarkMode()
        {
            // 这里可以根据需要实现系统主题检测逻辑
            // 目前返回false作为默认值
            return false;
        }
    }
}