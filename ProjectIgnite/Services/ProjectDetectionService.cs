using ProjectIgnite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 项目检测服务实现
    /// </summary>
    public class ProjectDetectionService : IProjectDetectionService
    {
        /// <summary>
        /// 检测项目类型
        /// </summary>
        public async Task<string> DetectProjectTypeAsync(string projectPath)
        {
            if (!Directory.Exists(projectPath))
                return "Unknown";

            var files = Directory.GetFiles(projectPath, "*", SearchOption.TopDirectoryOnly);
            var directories = Directory.GetDirectories(projectPath);

            // 检测 .NET 项目
            if (files.Any(f => Path.GetExtension(f).Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
                              Path.GetExtension(f).Equals(".sln", StringComparison.OrdinalIgnoreCase)))
            {
                // 进一步检测是否为 WebAPI 项目
                if (await IsWebApiProjectAsync(projectPath))
                    return "DotNetWebApi";
                
                return "DotNet";
            }

            // 检测 Node.js 项目
            if (files.Any(f => Path.GetFileName(f).Equals("package.json", StringComparison.OrdinalIgnoreCase)))
            {
                var packageJsonPath = files.First(f => Path.GetFileName(f).Equals("package.json", StringComparison.OrdinalIgnoreCase));
                var projectSubType = await DetectNodeJsProjectTypeAsync(packageJsonPath);
                return $"NodeJs{projectSubType}";
            }

            // 检测 Python 项目
            if (files.Any(f => Path.GetFileName(f).Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
                              Path.GetFileName(f).Equals("setup.py", StringComparison.OrdinalIgnoreCase) ||
                              Path.GetFileName(f).Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase)))
            {
                var pythonSubType = await DetectPythonProjectTypeAsync(projectPath);
                return $"Python{pythonSubType}";
            }

            // 检测 Docker 项目
            if (files.Any(f => Path.GetFileName(f).Equals("Dockerfile", StringComparison.OrdinalIgnoreCase) ||
                              Path.GetFileName(f).Equals("docker-compose.yml", StringComparison.OrdinalIgnoreCase) ||
                              Path.GetFileName(f).Equals("docker-compose.yaml", StringComparison.OrdinalIgnoreCase)))
            {
                return "Docker";
            }

            // 检测 Java 项目
            if (files.Any(f => Path.GetFileName(f).Equals("pom.xml", StringComparison.OrdinalIgnoreCase)) ||
                directories.Any(d => Path.GetFileName(d).Equals("gradle", StringComparison.OrdinalIgnoreCase)))
            {
                return "Java";
            }

            // 检测前端项目
            if (files.Any(f => Path.GetFileName(f).Equals("index.html", StringComparison.OrdinalIgnoreCase)))
            {
                return "Frontend";
            }

            return "Unknown";
        }

        /// <summary>
        /// 生成项目配置
        /// </summary>
        public async Task<List<ProjectConfiguration>> GenerateProjectConfigurationsAsync(ProjectSource projectSource)
        {
            var configurations = new List<ProjectConfiguration>();
            var projectType = await DetectProjectTypeAsync(projectSource.LocalPath);
            var (startPort, endPort) = GetRecommendedPortRange(projectType);

            // 生成不同环境的配置
            var environments = new[] { "Development", "Staging", "Production" };
            
            foreach (var env in environments)
            {
                var config = new ProjectConfiguration
                {
                    ProjectSourceId = projectSource.Id,
                    Name = $"{env} Configuration",
                    Environment = env,
                    DefaultPort = startPort + Array.IndexOf(environments, env),
                    PortRangeStart = startPort,
                    PortRangeEnd = endPort,
                    IsDefault = env == "Development"
                };

                // 根据项目类型设置启动命令
                await SetStartCommandByProjectTypeAsync(config, projectSource.LocalPath, projectType);
                
                configurations.Add(config);
            }

            return configurations;
        }

        /// <summary>
        /// 检测项目的启动配置
        /// </summary>
        public async Task<Dictionary<string, object>> DetectLaunchSettingsAsync(string projectPath, string projectType)
        {
            var result = new Dictionary<string, object>();

            try
            {
                if (projectType.StartsWith("DotNet"))
                {
                    var launchSettingsPath = Path.Combine(projectPath, "Properties", "launchSettings.json");
                    if (File.Exists(launchSettingsPath))
                    {
                        var content = await File.ReadAllTextAsync(launchSettingsPath);
                        var launchSettings = JsonSerializer.Deserialize<JsonElement>(content);
                        result["launchSettings"] = launchSettings;
                    }

                    // 检测 appsettings 文件
                    var appSettingsFiles = Directory.GetFiles(projectPath, "appsettings*.json");
                    if (appSettingsFiles.Any())
                    {
                        result["appSettingsFiles"] = appSettingsFiles;
                    }
                }
                else if (projectType.StartsWith("NodeJs"))
                {
                    var packageJsonPath = Path.Combine(projectPath, "package.json");
                    if (File.Exists(packageJsonPath))
                    {
                        var content = await File.ReadAllTextAsync(packageJsonPath);
                        var packageJson = JsonSerializer.Deserialize<JsonElement>(content);
                        result["packageJson"] = packageJson;
                    }
                }
            }
            catch (Exception)
            {
                // 忽略解析错误
            }

            return result;
        }

        /// <summary>
        /// 获取推荐的端口范围
        /// </summary>
        public (int startPort, int endPort) GetRecommendedPortRange(string projectType)
        {
            return projectType.ToLower() switch
            {
                var type when type.Contains("dotnet") => (5000, 5999),
                var type when type.Contains("nodejs") => (3000, 3999),
                var type when type.Contains("python") => (8000, 8999),
                var type when type.Contains("java") => (8080, 8089),
                var type when type.Contains("frontend") => (4000, 4999),
                var type when type.Contains("docker") => (9000, 9999),
                _ => (7000, 7999)
            };
        }

        /// <summary>
        /// 检测环境配置文件
        /// </summary>
        public async Task<List<string>> DetectEnvironmentConfigFilesAsync(string projectPath, string projectType)
        {
            var configFiles = new List<string>();

            try
            {
                if (projectType.StartsWith("DotNet"))
                {
                    var appSettingsFiles = Directory.GetFiles(projectPath, "appsettings*.json", SearchOption.TopDirectoryOnly);
                    configFiles.AddRange(appSettingsFiles);
                }
                else if (projectType.StartsWith("NodeJs"))
                {
                    var envFiles = Directory.GetFiles(projectPath, ".env*", SearchOption.TopDirectoryOnly);
                    configFiles.AddRange(envFiles);
                    
                    var configDir = Path.Combine(projectPath, "config");
                    if (Directory.Exists(configDir))
                    {
                        var configDirFiles = Directory.GetFiles(configDir, "*.json", SearchOption.TopDirectoryOnly);
                        configFiles.AddRange(configDirFiles);
                    }
                }
                else if (projectType.StartsWith("Python"))
                {
                    var envFiles = Directory.GetFiles(projectPath, ".env*", SearchOption.TopDirectoryOnly);
                    configFiles.AddRange(envFiles);
                    
                    var settingsFiles = Directory.GetFiles(projectPath, "*settings*.py", SearchOption.TopDirectoryOnly);
                    configFiles.AddRange(settingsFiles);
                }
            }
            catch (Exception)
            {
                // 忽略文件系统错误
            }

            return configFiles;
        }

        /// <summary>
        /// 检测是否为 WebAPI 项目
        /// </summary>
        private async Task<bool> IsWebApiProjectAsync(string projectPath)
        {
            try
            {
                var programFile = Path.Combine(projectPath, "Program.cs");
                if (File.Exists(programFile))
                {
                    var content = await File.ReadAllTextAsync(programFile);
                    return content.Contains("WebApplication") || 
                           content.Contains("UseRouting") || 
                           content.Contains("MapControllers") ||
                           content.Contains("AddControllers");
                }

                var startupFile = Path.Combine(projectPath, "Startup.cs");
                if (File.Exists(startupFile))
                {
                    var content = await File.ReadAllTextAsync(startupFile);
                    return content.Contains("UseRouting") || 
                           content.Contains("UseEndpoints") ||
                           content.Contains("AddControllers");
                }
            }
            catch (Exception)
            {
                // 忽略文件读取错误
            }

            return false;
        }

        /// <summary>
        /// 检测 Node.js 项目子类型
        /// </summary>
        private async Task<string> DetectNodeJsProjectTypeAsync(string packageJsonPath)
        {
            try
            {
                var content = await File.ReadAllTextAsync(packageJsonPath);
                var packageJson = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (packageJson.TryGetProperty("dependencies", out var deps))
                {
                    var depsText = deps.ToString();
                    if (depsText.Contains("express"))
                        return "Express";
                    if (depsText.Contains("next"))
                        return "Next";
                    if (depsText.Contains("react"))
                        return "React";
                    if (depsText.Contains("vue"))
                        return "Vue";
                    if (depsText.Contains("angular"))
                        return "Angular";
                }
            }
            catch (Exception)
            {
                // 忽略解析错误
            }

            return "";
        }

        /// <summary>
        /// 检测 Python 项目子类型
        /// </summary>
        private async Task<string> DetectPythonProjectTypeAsync(string projectPath)
        {
            try
            {
                var files = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file);
                    if (content.Contains("from flask import") || content.Contains("import flask"))
                        return "Flask";
                    if (content.Contains("from django") || content.Contains("import django"))
                        return "Django";
                    if (content.Contains("from fastapi") || content.Contains("import fastapi"))
                        return "FastAPI";
                }
            }
            catch (Exception)
            {
                // 忽略文件读取错误
            }

            return "";
        }

        /// <summary>
        /// 根据项目类型设置启动命令
        /// </summary>
        private async Task SetStartCommandByProjectTypeAsync(ProjectConfiguration config, string projectPath, string projectType)
        {
            config.WorkingDirectory = projectPath;

            switch (projectType.ToLower())
            {
                case var type when type.Contains("dotnetwebapi"):
                    config.StartCommand = "dotnet run";
                    config.HealthCheckUrl = $"http://localhost:{config.DefaultPort}/health";
                    config.EnvironmentVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ASPNETCORE_ENVIRONMENT"] = config.Environment,
                        ["ASPNETCORE_URLS"] = $"http://localhost:{config.DefaultPort}"
                    });
                    break;

                case var type when type.Contains("dotnet"):
                    config.StartCommand = "dotnet run";
                    config.EnvironmentVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["DOTNET_ENVIRONMENT"] = config.Environment
                    });
                    break;

                case var type when type.Contains("nodejs"):
                    config.StartCommand = await GetNodeJsStartCommandAsync(projectPath);
                    config.EnvironmentVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["NODE_ENV"] = config.Environment.ToLower(),
                        ["PORT"] = config.DefaultPort.ToString()
                    });
                    break;

                case var type when type.Contains("python"):
                    config.StartCommand = await GetPythonStartCommandAsync(projectPath, type);
                    config.EnvironmentVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ENVIRONMENT"] = config.Environment.ToLower(),
                        ["PORT"] = config.DefaultPort.ToString()
                    });
                    break;

                case "docker":
                    config.StartCommand = "docker-compose up";
                    break;

                default:
                    config.StartCommand = "echo 'Unknown project type'";
                    break;
            }
        }

        /// <summary>
        /// 获取 Node.js 启动命令
        /// </summary>
        private async Task<string> GetNodeJsStartCommandAsync(string projectPath)
        {
            try
            {
                var packageJsonPath = Path.Combine(projectPath, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    var content = await File.ReadAllTextAsync(packageJsonPath);
                    var packageJson = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (packageJson.TryGetProperty("scripts", out var scripts) &&
                        scripts.TryGetProperty("start", out var startScript))
                    {
                        return $"npm run start";
                    }
                }
            }
            catch (Exception)
            {
                // 忽略解析错误
            }

            return "node index.js";
        }

        /// <summary>
        /// 获取 Python 启动命令
        /// </summary>
        private async Task<string> GetPythonStartCommandAsync(string projectPath, string projectType)
        {
            if (projectType.Contains("django"))
            {
                return "python manage.py runserver";
            }
            else if (projectType.Contains("flask"))
            {
                var appFiles = Directory.GetFiles(projectPath, "app.py");
                if (appFiles.Any())
                    return "python app.py";
            }
            else if (projectType.Contains("fastapi"))
            {
                return "uvicorn main:app --reload";
            }

            return "python main.py";
        }
    }
}
