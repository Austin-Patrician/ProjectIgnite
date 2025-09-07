using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ProjectIgnite.Data;
using ProjectIgnite.Repositories;
using ProjectIgnite.ViewModels;
// 移除不存在的 Interfaces 命名空间引用
using Microsoft.Extensions.Logging;
using System;
using System.ClientModel;
using OpenAI;

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
            
            // 注册DbContext为Scoped，确保在同一作用域内使用同一实例
            services.AddDbContext<ProjectIgniteDbContext>(options => {
                // DbContext配置将在OnConfiguring中处理
            }, ServiceLifetime.Scoped);
            
            // 注册DbContextFactory用于创建独立的DbContext实例
            services.AddDbContextFactory<ProjectIgniteDbContext>(options => {
                // DbContext配置将在OnConfiguring中处理
            });

            services.AddScoped<IProjectRepository, ProjectRepository>();

            // 注册服务层
            services.AddSingleton<IGitService, GitService>();
            services.AddSingleton<ILinguistService, LinguistService>();
            
            // 注册图表相关服务
            services.AddSingleton<IDiagramService, DiagramService>();
            services.AddSingleton<IGitHubService, GitHubService>();
            services.AddSingleton<IAIService, AIService>();
            services.AddSingleton<ILocalProjectAnalyzer, LocalProjectAnalyzer>();
            
            // 注册Project Launcher相关服务
            services.AddSingleton<IProjectDetectionService, ProjectDetectionService>();
            services.AddSingleton<IPortManagementService, PortManagementService>();
            services.AddSingleton<IProcessManagementService, ProcessManagementService>();
            
            // 注册AI驱动项目分析服务
            services.AddSingleton<ILanguageAnalyzer, CSharpLanguageAnalyzer>();
            services.AddSingleton<ILanguageAnalyzer, NodeLanguageAnalyzer>();
            services.AddSingleton<IContentSummarizer, ContentSummarizer>();
            services.AddSingleton<IAIInsightsService, AIInsightsService>();
            
            // 注册AI客户端（需要配置具体的AI服务提供商）
            // 这里使用一个占位符实现，实际使用时需要配置真实的AI服务
            services.AddSingleton<IChatClient>(provider =>
            {
                var chatClient = new OpenAI.Chat.ChatClient("openai/gpt-4.1",
                    new ApiKeyCredential("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"),
                    new OpenAIClientOptions()
                    {
                        Endpoint = new Uri("https://openrouter.ai/api/v1")
                    }).AsIChatClient();

                IChatClient client =
                    new ChatClientBuilder(chatClient)
                        .UseFunctionInvocation()
                        .Build();
                return client;
            });
            
            // 注册日志服务
            services.AddLogging(builder => builder.AddConsole());

            // 注册ViewModels
            services.AddTransient<ProjectSourceViewModel>();
            services.AddTransient<ProjectStructureViewModel>();
            services.AddTransient<AddProjectDialogViewModel>();
            services.AddTransient<CloneProgressViewModel>();
            services.AddTransient<ProjectLauncherViewModel>();
            services.AddSingleton<ProjectAnalyzerViewModel>();

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
