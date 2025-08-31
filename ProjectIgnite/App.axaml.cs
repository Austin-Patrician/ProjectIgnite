using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using ProjectIgnite.ViewModels;
using ProjectIgnite.Views;
using ProjectIgnite.Services;
using ProjectIgnite.Data;

namespace ProjectIgnite
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            
            // 初始化依赖注入服务
            ServiceLocator.ConfigureServices();

            // 初始化数据库
            Task.Run(async () => await InitializeDatabaseAsync());

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var dbContext = ServiceLocator.GetService<ProjectIgniteDbContext>();
                await dbContext.InitializeDatabaseAsync();
            }
            catch (System.Exception ex)
            {
                // 记录错误日志，但不阻止应用程序启动
                System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}");
            }
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}