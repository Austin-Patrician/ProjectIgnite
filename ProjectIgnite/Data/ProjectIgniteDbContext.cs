using Microsoft.EntityFrameworkCore;
using ProjectIgnite.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectIgnite.Data
{
    /// <summary>
    /// ProjectIgnite数据库上下文
    /// </summary>
    public class ProjectIgniteDbContext : DbContext
    {
        /// <summary>
        /// 无参数构造函数
        /// </summary>
        public ProjectIgniteDbContext()
        {
        }

        /// <summary>
        /// 带选项的构造函数
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        public ProjectIgniteDbContext(DbContextOptions<ProjectIgniteDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 项目源表
        /// </summary>
        public DbSet<ProjectSource> ProjectSources { get; set; }

        /// <summary>
        /// 语言分析表
        /// </summary>
        public DbSet<LanguageAnalysis> LanguageAnalyses { get; set; }

        /// <summary>
        /// 克隆历史表
        /// </summary>
        public DbSet<CloneHistory> CloneHistories { get; set; }

        /// <summary>
        /// 启动项目表
        /// </summary>
        public DbSet<LaunchedProject> LaunchedProjects { get; set; }

        /// <summary>
        /// 项目配置表
        /// </summary>
        public DbSet<ProjectConfiguration> ProjectConfigurations { get; set; }

        /// <summary>
        /// 端口分配表
        /// </summary>
        public DbSet<PortAllocation> PortAllocations { get; set; }

        /// <summary>
        /// 配置数据库连接
        /// </summary>
        /// <param name="optionsBuilder">选项构建器</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // // 获取应用程序数据目录
                // var appDataPath = Path.Combine(
                //     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                //     "ProjectIgnite");
                //
                // // 确保目录存在
                // Directory.CreateDirectory(appDataPath);
                
                // 数据库文件路径
                var dbPath = "F:\\code\\Austin\\ProjectIgnite\\ProjectIgnite\\ProjectIgnite.db";
                
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        /// <summary>
        /// 配置实体模型
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置ProjectSource实体
            modelBuilder.Entity<ProjectSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.GitUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.LocalPath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.PrimaryLanguage).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.CloneProgress).HasDefaultValue(0);
                entity.Property(e => e.AnalysisProgress).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
                
                // 创建索引
                entity.HasIndex(e => e.GitUrl).IsUnique();
                entity.HasIndex(e => e.LocalPath).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });

            // 配置LanguageAnalysis实体
            modelBuilder.Entity<LanguageAnalysis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Percentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.AnalyzedAt).HasDefaultValueSql("datetime('now')");
                
                // 配置外键关系
                entity.HasOne(e => e.ProjectSource)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // 创建索引
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.Language);
                entity.HasIndex(e => new { e.ProjectId, e.Language }).IsUnique();
            });

            // 配置CloneHistory实体
            modelBuilder.Entity<CloneHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("started");
                entity.Property(e => e.Progress).HasDefaultValue(0);
                entity.Property(e => e.GitUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.TargetPath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.BranchName).HasMaxLength(100);
                entity.Property(e => e.StartTime).HasDefaultValueSql("datetime('now')");
                
                // 配置外键关系
                entity.HasOne(e => e.ProjectSource)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // 创建索引
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartTime);
            });

            // 配置LaunchedProject实体
            modelBuilder.Entity<LaunchedProject>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProjectName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ProjectPath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ProjectType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Stopped");
                entity.Property(e => e.CurrentEnvironment).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
                
                // 配置外键关系
                entity.HasOne(e => e.ProjectSource)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectSourceId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // 创建索引
                entity.HasIndex(e => e.ProjectSourceId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ProjectType);
                entity.HasIndex(e => e.ProcessId);
            });

            // 配置ProjectConfiguration实体
            modelBuilder.Entity<ProjectConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
                entity.Property(e => e.StartCommand).IsRequired().HasMaxLength(500);
                entity.Property(e => e.WorkingDirectory).HasMaxLength(500);
                entity.Property(e => e.Arguments).HasMaxLength(1000);
                entity.Property(e => e.ConfigFilePath).HasMaxLength(500);
                entity.Property(e => e.HealthCheckUrl).HasMaxLength(500);
                entity.Property(e => e.HealthCheckInterval).HasDefaultValue(30);
                entity.Property(e => e.AutoRestart).HasDefaultValue(false);
                entity.Property(e => e.MaxRestartCount).HasDefaultValue(3);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
                
                // 配置外键关系
                entity.HasOne(e => e.ProjectSource)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectSourceId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // 创建索引
                entity.HasIndex(e => e.ProjectSourceId);
                entity.HasIndex(e => e.Environment);
                entity.HasIndex(e => e.IsDefault);
                entity.HasIndex(e => new { e.ProjectSourceId, e.Name }).IsUnique();
            });

            // 配置PortAllocation实体
            modelBuilder.Entity<PortAllocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Available");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.UsageCount).HasDefaultValue(0);
                entity.Property(e => e.IsSystemReserved).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
                
                // 配置外键关系
                entity.HasOne(e => e.ProjectSource)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectSourceId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.LaunchedProject)
                      .WithMany()
                      .HasForeignKey(e => e.LaunchedProjectId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                // 创建索引
                entity.HasIndex(e => e.Port).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ProjectSourceId);
                entity.HasIndex(e => e.LaunchedProjectId);
                entity.HasIndex(e => e.IsSystemReserved);
            });
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                // 确保数据库已创建
                await Database.EnsureCreatedAsync();
                
                // 如果需要，可以在这里添加种子数据
                await SeedDataAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("数据库初始化失败", ex);
            }
        }

        /// <summary>
        /// 种子数据
        /// </summary>
        private async Task SeedDataAsync()
        {
            // 检查是否已有数据
            if (await ProjectSources.AnyAsync())
                return;

            // 可以在这里添加初始数据
            // 例如：默认的项目源配置等
            
            await SaveChangesAsync();
        }
    }
}
