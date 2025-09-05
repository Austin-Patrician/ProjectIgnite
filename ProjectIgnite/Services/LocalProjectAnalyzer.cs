using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ProjectIgnite.Models;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// 本地项目分析器实现
    /// </summary>
    public class LocalProjectAnalyzer : ILocalProjectAnalyzer
    {
        private readonly ILogger<LocalProjectAnalyzer> _logger;

        // 忽略的目录
        private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", ".vscode", "node_modules", "bin", "obj", "target", 
            "__pycache__", ".idea", "dist", "build", ".next", "coverage",
            ".nuget", "packages", "vendor"
        };

        // 忽略的文件扩展名
        private static readonly HashSet<string> IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".pdb", ".cache", ".tmp", ".temp", ".log",
            ".suo", ".user", ".lock", ".min.js", ".min.css"
        };

        // 重要文件模式
        private static readonly HashSet<string> ImportantFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "README.md", "README.txt", "package.json", "requirements.txt",
            "Cargo.toml", "go.mod", "composer.json", "Gemfile", "pom.xml",
            "build.gradle", "webpack.config.js", "tsconfig.json", ".env",
            "appsettings.json", "web.config", "app.config"
        };

        public LocalProjectAnalyzer(ILogger<LocalProjectAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
            string projectPath,
            string projectName,
            string? customInstructions = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始分析项目: {ProjectPath}", projectPath);

                if (!Directory.Exists(projectPath))
                {
                    throw new DirectoryNotFoundException($"项目目录不存在: {projectPath}");
                }

                var projectType = DetectProjectType(projectPath);
                _logger.LogInformation("检测到项目类型: {ProjectType}", projectType);

                var fileStructure = await ScanFileSystemAsync(projectPath, cancellationToken: cancellationToken);
                var dependencies = await AnalyzeDependenciesAsync(projectPath, projectType, cancellationToken);
                var configFiles = AnalyzeConfigurationFiles(projectPath);

                var result = new ProjectAnalysisResult
                {
                    ProjectPath = projectPath,
                    ProjectName = projectName,
                    ProjectType = projectType,
                    PrimaryLanguage = DeterminePrimaryLanguage(fileStructure, projectType),
                    FileStructure = fileStructure,
                    Dependencies = dependencies,
                    ConfigurationFiles = configFiles,
                    CustomInstructions = customInstructions,
                    AnalyzedAt = DateTime.UtcNow
                };

                _logger.LogInformation("项目分析完成: {ProjectName}, 文件数: {FileCount}", 
                    projectName, fileStructure.TotalFiles);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析项目时发生错误: {ProjectPath}", projectPath);
                throw;
            }
        }

        public ProjectType DetectProjectType(string projectPath)
        {
            try
            {
                var files = Directory.GetFiles(projectPath, "*", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // .NET 项目
                if (files.Any(f => f?.EndsWith(".csproj") == true || f?.EndsWith(".sln") == true))
                    return ProjectType.DotNet;

                // Node.js 项目
                if (files.Contains("package.json"))
                {
                    // 尝试读取 package.json 来确定更具体的类型
                    var packageJsonPath = Path.Combine(projectPath, "package.json");
                    if (File.Exists(packageJsonPath))
                    {
                        try
                        {
                            var packageJson = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(packageJsonPath));
                            if (packageJson.TryGetProperty("dependencies", out var deps) ||
                                packageJson.TryGetProperty("devDependencies", out deps))
                            {
                                var depsStr = deps.ToString();
                                if (depsStr.Contains("react")) return ProjectType.React;
                                if (depsStr.Contains("vue")) return ProjectType.Vue;
                                if (depsStr.Contains("@angular")) return ProjectType.Angular;
                            }
                        }
                        catch
                        {
                            // 如果解析失败，继续使用 NodeJs 类型
                        }
                    }
                    return ProjectType.NodeJs;
                }

                // Python 项目
                if (files.Contains("requirements.txt") || files.Contains("setup.py") || files.Contains("pyproject.toml"))
                    return ProjectType.Python;

                // Java 项目
                if (files.Contains("pom.xml") || files.Contains("build.gradle"))
                    return ProjectType.Java;

                // Rust 项目
                if (files.Contains("Cargo.toml"))
                    return ProjectType.Rust;

                // Go 项目
                if (files.Contains("go.mod") || files.Contains("go.sum"))
                    return ProjectType.Go;

                // PHP 项目
                if (files.Contains("composer.json"))
                    return ProjectType.Php;

                // Ruby 项目
                if (files.Contains("Gemfile"))
                    return ProjectType.Ruby;

                return ProjectType.Unknown;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检测项目类型时发生错误: {ProjectPath}", projectPath);
                return ProjectType.Unknown;
            }
        }

        public async Task<FileSystemStructure> ScanFileSystemAsync(
            string projectPath,
            int maxDepth = 10,
            CancellationToken cancellationToken = default)
        {
            var structure = new FileSystemStructure();
            var fileTypeCount = new Dictionary<string, int>();
            var counters = new Dictionary<string, int> { ["files"] = 0, ["directories"] = 0 };

            structure.RootDirectory = await ScanDirectoryAsync(
                new DirectoryInfo(projectPath),
                "",
                maxDepth,
                fileTypeCount,
                counters,
                cancellationToken);

            structure.TotalFiles = counters["files"];
            structure.TotalDirectories = counters["directories"];
            structure.FileTypeCount = fileTypeCount;

            return structure;
        }

        public async Task<ProjectDependencies> AnalyzeDependenciesAsync(
            string projectPath,
            ProjectType projectType,
            CancellationToken cancellationToken = default)
        {
            var dependencies = new ProjectDependencies();

            try
            {
                switch (projectType)
                {
                    case ProjectType.DotNet:
                        dependencies = await AnalyzeDotNetDependenciesAsync(projectPath, cancellationToken);
                        break;
                    case ProjectType.NodeJs:
                    case ProjectType.React:
                    case ProjectType.Vue:
                    case ProjectType.Angular:
                        dependencies = await AnalyzeNodeJsDependenciesAsync(projectPath, cancellationToken);
                        break;
                    case ProjectType.Python:
                        dependencies = await AnalyzePythonDependenciesAsync(projectPath, cancellationToken);
                        break;
                    case ProjectType.Rust:
                        dependencies = await AnalyzeRustDependenciesAsync(projectPath, cancellationToken);
                        break;
                    default:
                        dependencies.PackageManager = PackageManagerType.None;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "分析依赖时发生错误: {ProjectPath}", projectPath);
            }

            return dependencies;
        }

        #region Private Methods

        private async Task<DirectoryNode> ScanDirectoryAsync(
            DirectoryInfo directory,
            string relativePath,
            int remainingDepth,
            Dictionary<string, int> fileTypeCount,
            Dictionary<string, int> counters,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = new DirectoryNode
            {
                Name = directory.Name,
                RelativePath = relativePath
            };

            if (remainingDepth <= 0) return node;

            try
            {
                // 扫描文件
                foreach (var file in directory.GetFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ShouldIgnoreFile(file.Name)) continue;

                    var fileNode = new FileNode
                    {
                        Name = file.Name,
                        RelativePath = Path.Combine(relativePath, file.Name),
                        Extension = file.Extension,
                        Size = file.Length,
                        IsImportant = ImportantFiles.Contains(file.Name)
                    };

                    node.Files.Add(fileNode);
                    counters["files"]++;

                    // 统计文件类型
                    var ext = file.Extension.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(ext))
                    {
                        fileTypeCount[ext] = fileTypeCount.GetValueOrDefault(ext, 0) + 1;
                    }
                }

                // 扫描子目录
                foreach (var subDir in directory.GetDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ShouldIgnoreDirectory(subDir.Name)) continue;

                    var subNode = await ScanDirectoryAsync(
                        subDir,
                        Path.Combine(relativePath, subDir.Name),
                        remainingDepth - 1,
                        fileTypeCount,
                        counters,
                        cancellationToken);

                    node.SubDirectories.Add(subNode);
                    counters["directories"]++;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("无权限访问目录: {Directory} - {Error}", directory.FullName, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "扫描目录时发生错误: {Directory}", directory.FullName);
            }

            return node;
        }

        private bool ShouldIgnoreDirectory(string directoryName)
        {
            return IgnoredDirectories.Contains(directoryName);
        }

        private bool ShouldIgnoreFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return IgnoredExtensions.Contains(extension);
        }

        private string DeterminePrimaryLanguage(FileSystemStructure structure, ProjectType projectType)
        {
            // 基于项目类型推断主要语言
            return projectType switch
            {
                ProjectType.DotNet => "C#",
                ProjectType.NodeJs or ProjectType.React or ProjectType.Vue or ProjectType.Angular => "JavaScript/TypeScript",
                ProjectType.Python => "Python",
                ProjectType.Java => "Java",
                ProjectType.Rust => "Rust",
                ProjectType.Go => "Go",
                ProjectType.Php => "PHP",
                ProjectType.Ruby => "Ruby",
                _ => DetermineLanguageFromFiles(structure.FileTypeCount)
            };
        }

        private string DetermineLanguageFromFiles(Dictionary<string, int> fileTypeCount)
        {
            var languageMapping = new Dictionary<string, string>
            {
                [".cs"] = "C#",
                [".js"] = "JavaScript",
                [".ts"] = "TypeScript",
                [".py"] = "Python",
                [".java"] = "Java",
                [".rs"] = "Rust",
                [".go"] = "Go",
                [".php"] = "PHP",
                [".rb"] = "Ruby",
                [".cpp"] = "C++",
                [".c"] = "C",
                [".h"] = "C/C++",
                [".kt"] = "Kotlin",
                [".swift"] = "Swift"
            };

            var mostCommonExt = fileTypeCount
                .Where(kvp => languageMapping.ContainsKey(kvp.Key))
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();

            return mostCommonExt.Key != null ? languageMapping[mostCommonExt.Key] : "Unknown";
        }

        private List<ConfigurationFile> AnalyzeConfigurationFiles(string projectPath)
        {
            var configFiles = new List<ConfigurationFile>();

            try
            {
                var files = Directory.GetFiles(projectPath, "*", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var configType = DetermineConfigurationType(fileName);

                    if (configType != ConfigurationType.Other || ImportantFiles.Contains(fileName))
                    {
                        configFiles.Add(new ConfigurationFile
                        {
                            FileName = fileName,
                            FilePath = file,
                            Type = configType,
                            ContentSummary = GetFileSummary(file)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "分析配置文件时发生错误: {ProjectPath}", projectPath);
            }

            return configFiles;
        }

        private ConfigurationType DetermineConfigurationType(string fileName)
        {
            return fileName.ToLowerInvariant() switch
            {
                var name when name.EndsWith(".csproj") || name.EndsWith(".sln") || name == "package.json" 
                    || name == "pom.xml" || name == "build.gradle" || name == "cargo.toml" => ConfigurationType.ProjectFile,
                var name when name.StartsWith("webpack") || name.StartsWith("vite") || name == "tsconfig.json" 
                    || name == "babel.config.js" => ConfigurationType.BuildConfiguration,
                var name when name.StartsWith(".env") || name.StartsWith("appsettings") || name == "web.config" 
                    || name == "app.config" => ConfigurationType.EnvironmentConfig,
                "requirements.txt" or "gemfile" or "composer.json" => ConfigurationType.DependencyConfig,
                _ => ConfigurationType.Other
            };
        }

        private string? GetFileSummary(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > 1024 * 100) // 大于100KB的文件不读取内容
                {
                    return $"文件大小: {fileInfo.Length / 1024}KB";
                }

                return fileName.ToLowerInvariant() switch
                {
                    "package.json" => GetPackageJsonSummary(filePath),
                    var name when name.EndsWith(".csproj") => GetCsprojSummary(filePath),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private string? GetPackageJsonSummary(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var json = JsonSerializer.Deserialize<JsonElement>(content);

                var name = json.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "";
                var version = json.TryGetProperty("version", out var versionEl) ? versionEl.GetString() : "";
                var description = json.TryGetProperty("description", out var descEl) ? descEl.GetString() : "";

                return $"名称: {name}, 版本: {version}, 描述: {description}";
            }
            catch
            {
                return null;
            }
        }

        private string? GetCsprojSummary(string filePath)
        {
            try
            {
                var doc = XDocument.Load(filePath);
                var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value;
                var outputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;

                return $"目标框架: {targetFramework}, 输出类型: {outputType}";
            }
            catch
            {
                return null;
            }
        }

        private async Task<ProjectDependencies> AnalyzeDotNetDependenciesAsync(string projectPath, CancellationToken cancellationToken)
        {
            var dependencies = new ProjectDependencies { PackageManager = PackageManagerType.NuGet };

            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            
            foreach (var csprojFile in csprojFiles)
            {
                try
                {
                    var doc = XDocument.Load(csprojFile);
                    var packageRefs = doc.Descendants("PackageReference");

                    foreach (var packageRef in packageRefs)
                    {
                        var name = packageRef.Attribute("Include")?.Value ?? "";
                        var version = packageRef.Attribute("Version")?.Value ?? "";

                        if (!string.IsNullOrEmpty(name))
                        {
                            dependencies.Packages.Add(new DependencyPackage
                            {
                                Name = name,
                                Version = version
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析.csproj文件失败: {File}", csprojFile);
                }
            }

            return dependencies;
        }

        private async Task<ProjectDependencies> AnalyzeNodeJsDependenciesAsync(string projectPath, CancellationToken cancellationToken)
        {
            var dependencies = new ProjectDependencies { PackageManager = PackageManagerType.Npm };
            var packageJsonPath = Path.Combine(projectPath, "package.json");

            if (!File.Exists(packageJsonPath))
                return dependencies;

            try
            {
                var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
                var json = JsonSerializer.Deserialize<JsonElement>(content);

                // 分析生产依赖
                if (json.TryGetProperty("dependencies", out var deps))
                {
                    foreach (var dep in deps.EnumerateObject())
                    {
                        dependencies.Packages.Add(new DependencyPackage
                        {
                            Name = dep.Name,
                            Version = dep.Value.GetString() ?? ""
                        });
                    }
                }

                // 分析开发依赖
                if (json.TryGetProperty("devDependencies", out var devDeps))
                {
                    foreach (var dep in devDeps.EnumerateObject())
                    {
                        dependencies.DevPackages.Add(new DependencyPackage
                        {
                            Name = dep.Name,
                            Version = dep.Value.GetString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析package.json失败: {File}", packageJsonPath);
            }

            return dependencies;
        }

        private async Task<ProjectDependencies> AnalyzePythonDependenciesAsync(string projectPath, CancellationToken cancellationToken)
        {
            var dependencies = new ProjectDependencies { PackageManager = PackageManagerType.Pip };
            var requirementsPath = Path.Combine(projectPath, "requirements.txt");

            if (!File.Exists(requirementsPath))
                return dependencies;

            try
            {
                var lines = await File.ReadAllLinesAsync(requirementsPath, cancellationToken);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    var parts = trimmedLine.Split(new[] { "==", ">=", "<=", ">" }, StringSplitOptions.RemoveEmptyEntries);
                    var name = parts[0].Trim();
                    var version = parts.Length > 1 ? parts[1].Trim() : "";

                    dependencies.Packages.Add(new DependencyPackage
                    {
                        Name = name,
                        Version = version
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析requirements.txt失败: {File}", requirementsPath);
            }

            return dependencies;
        }

        private async Task<ProjectDependencies> AnalyzeRustDependenciesAsync(string projectPath, CancellationToken cancellationToken)
        {
            var dependencies = new ProjectDependencies { PackageManager = PackageManagerType.Cargo };
            var cargoPath = Path.Combine(projectPath, "Cargo.toml");

            if (!File.Exists(cargoPath))
                return dependencies;

            try
            {
                var content = await File.ReadAllTextAsync(cargoPath, cancellationToken);
                var lines = content.Split('\n');
                bool inDependencies = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    if (trimmedLine == "[dependencies]")
                    {
                        inDependencies = true;
                        continue;
                    }
                    
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]") && trimmedLine != "[dependencies]")
                    {
                        inDependencies = false;
                        continue;
                    }

                    if (inDependencies && trimmedLine.Contains("="))
                    {
                        var parts = trimmedLine.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var name = parts[0].Trim();
                            var version = parts[1].Trim().Trim('"');

                            dependencies.Packages.Add(new DependencyPackage
                            {
                                Name = name,
                                Version = version
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析Cargo.toml失败: {File}", cargoPath);
            }

            return dependencies;
        }

        #endregion
    }
}
