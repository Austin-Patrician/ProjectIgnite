using Microsoft.Extensions.DependencyInjection;
using ProjectIgnite.Data;
using ProjectIgnite.Repositories;
using ProjectIgnite.ViewModels;
// 移除不存在的 Interfaces 命名空间引用
using Microsoft.Extensions.Logging;
using System;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 简单的服务定位器，用于管理依赖注入
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// 配置服务
        /// </summary>
        public static void ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注册数据库上下文 - 修改为 Transient 以避免线程安全问题
            services.AddTransient<ProjectIgniteDbContext>();

            // 注册数据访问层 - 也改为 Transient 以匹配 DbContext
            services.AddTransient<IProjectRepository, ProjectRepository>();

            // 注册服务层
            services.AddSingleton<IGitService, GitService>();
            services.AddSingleton<ILinguistService, LinguistService>();
            
            // 注册图表相关服务
            services.AddSingleton<IDiagramService, DiagramService>();
            services.AddSingleton<IGitHubService, GitHubService>();
            services.AddSingleton<IAIService, AIService>();
            
            // 注册日志服务
            services.AddLogging(builder => builder.AddConsole());

            // 注册ViewModels
            services.AddTransient<ProjectSourceViewModel>();
            services.AddTransient<ProjectStructureViewModel>(provider => 
                new ProjectStructureViewModel(
                    provider.GetRequiredService<IDiagramService>(),
                    provider.GetRequiredService<IGitHubService>(),
                    provider.GetRequiredService<IAIService>()));
            services.AddTransient<AddProjectDialogViewModel>();
            services.AddTransient<CloneProgressViewModel>();

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T GetService<T>() where T : notnull
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("服务提供者未初始化，请先调用 ConfigureServices 方法");
            }

            var service = _serviceProvider.GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"无法获取服务 {typeof(T).Name}");
            }

            return service;
        }

        /// <summary>
        /// 尝试获取服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例，如果不存在则返回null</returns>
        public static T? TryGetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }
    }
}
