using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.DTOs;
using System.Xml.Linq; // added for robust csproj parsing

namespace ProjectIgnite.Services;

/// <summary>
/// C# 语言分析器
/// </summary>
public class CSharpLanguageAnalyzer : ILanguageAnalyzer
{
    public string LanguageType => "csharp";

    private static readonly string[] KeyFilePatterns = 
    {
        "*.sln", "*.csproj", "*.vbproj", "*.fsproj",
        "global.json", "Directory.Build.props", "Directory.Build.targets"
    };

    private static readonly string[] ConfigFilePatterns =
    {
        "appsettings*.json", "launchSettings.json", "web.config", "app.config"
    };

    public async Task<LanguageDetectionResult> DetectAsync(string rootPath)
    {
        var result = new LanguageDetectionResult();
        var keyFiles = new List<string>();
        var confidence = 0.0;

        try
        {
            // 检查解决方案文件
            var slnFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.TopDirectoryOnly);
            if (slnFiles.Length > 0)
            {
                keyFiles.AddRange(slnFiles);
                confidence += 0.4;
            }

            // 检查项目文件
            var projFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(rootPath, "*.vbproj", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(rootPath, "*.fsproj", SearchOption.AllDirectories))
                .ToArray();

            if (projFiles.Length > 0)
            {
                keyFiles.AddRange(projFiles);
                confidence += 0.3;
            }

            // 检查global.json
            var globalJsonPath = Path.Combine(rootPath, "global.json");
            if (File.Exists(globalJsonPath))
            {
                keyFiles.Add(globalJsonPath);
                confidence += 0.1;
            }

            // 检查典型的C#文件
            var csFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .Take(10)
                .ToArray();

            if (csFiles.Length > 0)
            {
                keyFiles.AddRange(csFiles);
                confidence += Math.Min(csFiles.Length * 0.02, 0.2);
            }

            result.IsDetected = confidence > 0.1;
            result.Confidence = Math.Min(confidence, 1.0);
            result.KeyFiles = keyFiles;
            result.Reason = confidence > 0.1 ? 
                $"检测到 {slnFiles.Length} 个解决方案文件，{projFiles.Length} 个项目文件，{csFiles.Length} 个C#源文件" :
                "未检测到C#项目特征文件";
        }
        catch (Exception ex)
        {
            result.IsDetected = false;
            result.Confidence = 0;
            result.Reason = $"检测过程中发生错误: {ex.Message}";
        }

        return result;
    }

    public async Task<LanguageSpecificResult> ScanAsync(ProjectScanRequest request, CancellationToken cancellationToken = default)
    {
        var result = new LanguageSpecificResult
        {
            LanguageType = LanguageType
        };

        try
        {
            // 并行执行各种分析任务
            var tasks = new List<Task>
            {
                Task.Run(async () => result.Environment = await ExtractEnvironmentInfoAsync(request.RootPath, cancellationToken), cancellationToken),
                Task.Run(async () => result.Dependencies = await ExtractDependenciesAsync(request.RootPath, cancellationToken), cancellationToken),
                Task.Run(async () => result.RunInfo = await ExtractRunInfoAsync(request.RootPath, cancellationToken), cancellationToken),
                Task.Run(async () => 
                {
                    var (nodes, edges) = await ExtractStructureAsync(request.RootPath, cancellationToken);
                    result.StructureNodes = nodes;
                    result.StructureEdges = edges;
                }, cancellationToken),
                Task.Run(async () => result.KeySnippets = await ExtractKeySnippetsAsync(request.RootPath, cancellationToken), cancellationToken)
            };

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // 记录错误但不中断整个流程
            Console.WriteLine($"C# 扫描过程中发生错误: {ex.Message}");
        }

        return result;
    }

    public async Task<RunInfo> ExtractRunInfoAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var runInfo = new RunInfo();

        try
        {
            // 从 launchSettings.json 提取
            await ExtractFromLaunchSettingsAsync(rootPath, runInfo, cancellationToken);

            // 从 appsettings.json 提取
            await ExtractFromAppSettingsAsync(rootPath, runInfo, cancellationToken);

            // 从项目文件推断
            await ExtractFromProjectFilesAsync(rootPath, runInfo, cancellationToken);

            // 从代码文件推断
            await ExtractFromCodeFilesAsync(rootPath, runInfo, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取运行信息时发生错误: {ex.Message}");
        }

        return runInfo;
    }

    public async Task<DependenciesResult> ExtractDependenciesAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var result = new DependenciesResult();

        try
        {
            var projFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

            foreach (var projFile in projFiles)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var packages = await ExtractPackageReferencesAsync(projFile, cancellationToken);
                result.CSharp.AddRange(packages);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取依赖信息时发生错误: {ex.Message}");
        }

        return result;
    }

    public async Task<ProjectSummary> SummarizeAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var summary = new ProjectSummary
        {
            ProjectName = Path.GetFileName(rootPath),
            ProjectType = "C# Project"
        };

        try
        {
            // 分析项目结构
            var slnFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.TopDirectoryOnly);
            var projFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

            if (slnFiles.Length > 0)
            {
                summary.ProjectName = Path.GetFileNameWithoutExtension(slnFiles[0]);
                summary.ProjectType = "C# Solution";
            }
            else if (projFiles.Length > 0)
            {
                summary.ProjectName = Path.GetFileNameWithoutExtension(projFiles[0]);
            }

            // 分析框架类型
            summary.Framework = await DetectFrameworkAsync(rootPath, cancellationToken);

            // 分析主要模块
            summary.MainModules = await DetectMainModulesAsync(rootPath, cancellationToken);

            // 分析关键文件
            summary.KeyFiles = await DetectKeyFilesAsync(rootPath, cancellationToken);

            // 分析顶级依赖
            var deps = await ExtractDependenciesAsync(rootPath, cancellationToken);
            summary.TopDependencies = deps.CSharp
                .Where(p => !p.DevDependency)
                .OrderByDescending(p => p.RiskScore)
                .Take(10)
                .Select(p => $"{p.Name} ({p.Version})")
                .ToList();

            // 计算复杂度指标
            summary.ComplexityMetrics = await CalculateComplexityMetricsAsync(rootPath, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成项目摘要时发生错误: {ex.Message}");
        }

        return summary;
    }

    #region 私有辅助方法

    private async Task<EnvironmentInfo> ExtractEnvironmentInfoAsync(string rootPath, CancellationToken cancellationToken)
    {
        var envInfo = new EnvironmentInfo();

        try
        {
            var projFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);
            
            foreach (var projFile in projFiles.Take(5)) // 限制分析数量
            {
                if (cancellationToken.IsCancellationRequested) break;

                var content = await File.ReadAllTextAsync(projFile, cancellationToken);
                
                // 提取 SDK 类型
                var sdkMatch = Regex.Match(content, @"<Project\s+Sdk\s*=\s*[""']([^""']+)[""']");
                if (sdkMatch.Success && string.IsNullOrEmpty(envInfo.DotNetSdk))
                {
                    envInfo.DotNetSdk = sdkMatch.Groups[1].Value;
                }

                // 提取目标框架
                var tfmMatches = Regex.Matches(content, @"<TargetFramework[s]?>([^<]+)</TargetFramework[s]?>");
                foreach (Match match in tfmMatches)
                {
                    var frameworks = match.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var fw in frameworks)
                    {
                        if (!envInfo.TargetFrameworks.Contains(fw.Trim()))
                        {
                            envInfo.TargetFrameworks.Add(fw.Trim());
                        }
                    }
                }

                // 提取语言版本
                var langVersionMatch = Regex.Match(content, @"<LangVersion>([^<]+)</LangVersion>");
                if (langVersionMatch.Success && string.IsNullOrEmpty(envInfo.LanguageVersion))
                {
                    envInfo.LanguageVersion = langVersionMatch.Groups[1].Value;
                }

                // 提取 Nullable 设置
                var nullableMatch = Regex.Match(content, @"<Nullable>([^<]+)</Nullable>");
                if (nullableMatch.Success && !envInfo.NullableEnabled.HasValue)
                {
                    envInfo.NullableEnabled = nullableMatch.Groups[1].Value.Equals("enable", StringComparison.OrdinalIgnoreCase);
                }

                // 提取 ImplicitUsings 设置
                var implicitMatch = Regex.Match(content, @"<ImplicitUsings>([^<]+)</ImplicitUsings>");
                if (implicitMatch.Success && !envInfo.ImplicitUsings.HasValue)
                {
                    envInfo.ImplicitUsings = implicitMatch.Groups[1].Value.Equals("enable", StringComparison.OrdinalIgnoreCase);
                }
            }

            // 检查 global.json
            var globalJsonPath = Path.Combine(rootPath, "global.json");
            if (File.Exists(globalJsonPath))
            {
                var globalContent = await File.ReadAllTextAsync(globalJsonPath, cancellationToken);
                try
                {
                    var globalJson = JsonSerializer.Deserialize<JsonElement>(globalContent);
                    if (globalJson.TryGetProperty("sdk", out var sdkElement) && 
                        sdkElement.TryGetProperty("version", out var versionElement))
                    {
                        envInfo.RuntimeVersions.Add($"SDK: {versionElement.GetString()}");
                    }
                }
                catch
                {
                    // 忽略 JSON 解析错误
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取环境信息时发生错误: {ex.Message}");
        }

        return envInfo;
    }

    private async Task ExtractFromLaunchSettingsAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        var launchSettingsPath = Path.Combine(rootPath, "Properties", "launchSettings.json");
        if (!File.Exists(launchSettingsPath))
        {
            // 尝试在子目录中查找
            var launchFiles = Directory.GetFiles(rootPath, "launchSettings.json", SearchOption.AllDirectories);
            if (launchFiles.Length > 0)
            {
                launchSettingsPath = launchFiles[0];
            }
            else
            {
                return;
            }
        }

        try
        {
            var content = await File.ReadAllTextAsync(launchSettingsPath, cancellationToken);
            var launchSettings = JsonSerializer.Deserialize<JsonElement>(content);

            if (launchSettings.TryGetProperty("profiles", out var profiles))
            {
                foreach (var profile in profiles.EnumerateObject())
                {
                    if (profile.Value.TryGetProperty("applicationUrl", out var urlElement))
                    {
                        var urls = urlElement.GetString()?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                        foreach (var url in urls)
                        {
                            runInfo.Urls.Add(new UrlCandidate
                            {
                                Value = url.Trim(),
                                Confidence = 0.9,
                                Source = RunInfoSource.LaunchSettings,
                                Notes = $"来自 profile: {profile.Name}"
                            });

                            // 提取端口
                            if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Port > 0)
                            {
                                runInfo.Ports.Add(new PortCandidate
                                {
                                    Value = uri.Port,
                                    Confidence = 0.9,
                                    Source = RunInfoSource.LaunchSettings,
                                    Notes = $"来自 applicationUrl: {url.Trim()}"
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析 launchSettings.json 时发生错误: {ex.Message}");
        }
    }

    private async Task ExtractFromAppSettingsAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        var appSettingsFiles = Directory.GetFiles(rootPath, "appsettings*.json", SearchOption.AllDirectories);

        foreach (var file in appSettingsFiles.Take(5))
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var appSettings = JsonSerializer.Deserialize<JsonElement>(content);

                // 检查 Kestrel 配置
                if (appSettings.TryGetProperty("Kestrel", out var kestrel))
                {
                    if (kestrel.TryGetProperty("Endpoints", out var endpoints))
                    {
                        foreach (var endpoint in endpoints.EnumerateObject())
                        {
                            if (endpoint.Value.TryGetProperty("Url", out var urlElement))
                            {
                                var url = urlElement.GetString();
                                if (!string.IsNullOrEmpty(url))
                                {
                                    runInfo.Urls.Add(new UrlCandidate
                                    {
                                        Value = url,
                                        Confidence = 0.8,
                                        Source = RunInfoSource.AppSettings,
                                        Notes = $"来自 Kestrel.Endpoints.{endpoint.Name}"
                                    });

                                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Port > 0)
                                    {
                                        runInfo.Ports.Add(new PortCandidate
                                        {
                                            Value = uri.Port,
                                            Confidence = 0.8,
                                            Source = RunInfoSource.AppSettings,
                                            Notes = $"来自 Kestrel 端点配置"
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                // 检查 Urls 配置
                if (appSettings.TryGetProperty("Urls", out var urlsElement))
                {
                    var urls = urlsElement.GetString()?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    foreach (var url in urls)
                    {
                        runInfo.Urls.Add(new UrlCandidate
                        {
                            Value = url.Trim(),
                            Confidence = 0.8,
                            Source = RunInfoSource.AppSettings,
                            Notes = "来自 Urls 配置"
                        });

                        if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Port > 0)
                        {
                            runInfo.Ports.Add(new PortCandidate
                            {
                                Value = uri.Port,
                                Confidence = 0.8,
                                Source = RunInfoSource.AppSettings,
                                Notes = "来自 Urls 配置"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析 {Path.GetFileName(file)} 时发生错误: {ex.Message}");
            }
        }
    }

    private async Task ExtractFromProjectFilesAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        var projFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

        foreach (var projFile in projFiles.Take(5))
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var content = await File.ReadAllTextAsync(projFile, cancellationToken);
                
                // 检查是否为 Web 项目
                if (content.Contains("Microsoft.NET.Sdk.Web"))
                {
                    // Web 项目默认端口
                    runInfo.Ports.Add(new PortCandidate
                    {
                        Value = 5000,
                        Confidence = 0.3,
                        Source = RunInfoSource.Default,
                        Notes = "ASP.NET Core 默认 HTTP 端口"
                    });

                    runInfo.Ports.Add(new PortCandidate
                    {
                        Value = 5001,
                        Confidence = 0.3,
                        Source = RunInfoSource.Default,
                        Notes = "ASP.NET Core 默认 HTTPS 端口"
                    });

                    runInfo.Urls.Add(new UrlCandidate
                    {
                        Value = "http://localhost:5000",
                        Confidence = 0.3,
                        Source = RunInfoSource.Default,
                        Notes = "ASP.NET Core 默认 URL"
                    });

                    runInfo.Urls.Add(new UrlCandidate
                    {
                        Value = "https://localhost:5001",
                        Confidence = 0.3,
                        Source = RunInfoSource.Default,
                        Notes = "ASP.NET Core 默认 HTTPS URL"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析项目文件 {Path.GetFileName(projFile)} 时发生错误: {ex.Message}");
            }
        }
    }

    private async Task ExtractFromCodeFilesAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        var csFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .Take(20)
            .ToArray();

        foreach (var csFile in csFiles)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var content = await File.ReadAllTextAsync(csFile, cancellationToken);
                
                // 查找 UseUrls 调用
                var urlMatches = Regex.Matches(content, @"UseUrls\s*\(\s*[""']([^""']+)[""']\s*\)");
                foreach (Match match in urlMatches)
                {
                    var url = match.Groups[1].Value;
                    runInfo.Urls.Add(new UrlCandidate
                    {
                        Value = url,
                        Confidence = 0.7,
                        Source = RunInfoSource.CodeInference,
                        Notes = $"来自代码文件: {Path.GetFileName(csFile)}"
                    });

                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Port > 0)
                    {
                        runInfo.Ports.Add(new PortCandidate
                        {
                            Value = uri.Port,
                            Confidence = 0.7,
                            Source = RunInfoSource.CodeInference,
                            Notes = $"来自 UseUrls 调用"
                        });
                    }
                }

                // 查找端口配置
                var portMatches = Regex.Matches(content, @"(?:port|Port)\s*[=:]\s*(\d+)");
                foreach (Match match in portMatches)
                {
                    if (int.TryParse(match.Groups[1].Value, out var port) && port > 1000 && port < 65536)
                    {
                        runInfo.Ports.Add(new PortCandidate
                        {
                            Value = port,
                            Confidence = 0.5,
                            Source = RunInfoSource.CodeInference,
                            Notes = $"来自代码推断: {Path.GetFileName(csFile)}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析代码文件 {Path.GetFileName(csFile)} 时发生错误: {ex.Message}");
            }
        }
    }

    private async Task<List<PackageEntry>> ExtractPackageReferencesAsync(string projFile, CancellationToken cancellationToken)
    {
        var packages = new List<PackageEntry>();

        try
        {
            // 使用 XML 解析以兼容多种写法（属性版/子元素版/属性顺序任意/自闭合标签等）
            var xdoc = XDocument.Load(projFile);
            var ns = xdoc.Root?.Name.Namespace ?? XNamespace.None;

            var packageRefs = xdoc
                .Descendants(ns + "PackageReference")
                .ToList();

            foreach (var pr in packageRefs)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var name = (string?)pr.Attribute("Include") ?? (string?)pr.Attribute("Update");
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 版本可能出现在属性或子元素中，也可能走 Central Package Management（版本不在项目中）
                var version = (string?)pr.Attribute("Version")
                              ?? (string?)pr.Element(ns + "Version")
                              ?? string.Empty; // 保留空字符串，至少展示包名

                // Dev 依赖启发式：PrivateAssets=all/build 或者测试相关包
                var privateAssets = ((string?)pr.Attribute("PrivateAssets")
                                   ?? (string?)pr.Element(ns + "PrivateAssets")
                                   ?? string.Empty).ToLowerInvariant();
                var isDev = privateAssets.Contains("all") || privateAssets.Contains("build");
                var lowerName = name.ToLowerInvariant();
                if (!isDev && (lowerName.Contains("xunit") || lowerName.Contains("nunit") || lowerName.Contains("mstest") || lowerName.Contains("coverlet")))
                {
                    isDev = true;
                }

                packages.Add(new PackageEntry
                {
                    Name = name,
                    Version = version,
                    DevDependency = isDev,
                    Source = PackageSource.CsProj,
                    RiskScore = CalculatePackageRiskScore(name, version)
                });
            }

            // 如果 XML 解析失败或未找到，回退简单正则以尽量覆盖更多情况（不要求 Version 必填）
            if (packages.Count == 0)
            {
                var content = await File.ReadAllTextAsync(projFile, cancellationToken);
                // 捕获 Include 值；Version 属性可选
                var regex = new Regex("<PackageReference\\s+[^>]*Include\\s*=\\s*\"([^\"]+)\"[^>]*>(.*?)</PackageReference>|<PackageReference\\s+[^>]*Include\\s*=\\s*\"([^\"]+)\"[^>]*/>", RegexOptions.Singleline);
                var matches = regex.Matches(content);
                foreach (Match m in matches)
                {
                    var name = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[3].Value;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var inner = m.Groups[2].Success ? m.Groups[2].Value : string.Empty;
                    var versionAttrMatch = Regex.Match(m.Value, "Version\\s*=\\s*\"([^\"]+)\"");
                    var versionElemMatch = Regex.Match(inner, "<Version>\\s*([^<]+)\\s*</Version>");
                    var version = versionAttrMatch.Success ? versionAttrMatch.Groups[1].Value
                                  : (versionElemMatch.Success ? versionElemMatch.Groups[1].Value : string.Empty);

                    packages.Add(new PackageEntry
                    {
                        Name = name,
                        Version = version,
                        DevDependency = false,
                        Source = PackageSource.CsProj,
                        RiskScore = CalculatePackageRiskScore(name, version)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取包引用时发生错误: {ex.Message}");
        }

        return packages;
    }

    private async Task<(List<GraphNode>, List<GraphEdge>)> ExtractStructureAsync(string rootPath, CancellationToken cancellationToken)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        try
        {
            // 分析解决方案结构
            var slnFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.TopDirectoryOnly);
            if (slnFiles.Length > 0)
            {
                var slnNode = new GraphNode
                {
                    Id = "solution",
                    Type = GraphNodeType.Solution,
                    Name = Path.GetFileNameWithoutExtension(slnFiles[0]),
                    FileRefs = new List<string> { slnFiles[0] }
                };
                nodes.Add(slnNode);
            }

            // 分析项目结构
            var projFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);
            foreach (var projFile in projFiles.Take(10))
            {
                if (cancellationToken.IsCancellationRequested) break;

                var projName = Path.GetFileNameWithoutExtension(projFile);
                var projNode = new GraphNode
                {
                    Id = $"project_{projName}",
                    Type = GraphNodeType.Project,
                    Name = projName,
                    FileRefs = new List<string> { projFile }
                };
                nodes.Add(projNode);

                // 如果有解决方案，添加边
                if (slnFiles.Length > 0)
                {
                    edges.Add(new GraphEdge
                    {
                        SourceId = "solution",
                        TargetId = projNode.Id,
                        RelationType = GraphRelationType.Contains
                    });
                }

                // 分析项目内部结构
                await AnalyzeProjectStructureAsync(projFile, projNode.Id, nodes, edges, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取结构信息时发生错误: {ex.Message}");
        }

        return (nodes, edges);
    }

    private async Task AnalyzeProjectStructureAsync(string projFile, string projectId, List<GraphNode> nodes, List<GraphEdge> edges, CancellationToken cancellationToken)
    {
        try
        {
            var projectDir = Path.GetDirectoryName(projFile);
            if (string.IsNullOrEmpty(projectDir)) return;

            // 分析控制器
            var controllerFiles = Directory.GetFiles(projectDir, "*Controller.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .Take(10)
                .ToArray();

            foreach (var controllerFile in controllerFiles)
            {
                var controllerName = Path.GetFileNameWithoutExtension(controllerFile);
                var controllerNode = new GraphNode
                {
                    Id = $"controller_{controllerName}",
                    Type = GraphNodeType.Controller,
                    Name = controllerName,
                    FileRefs = new List<string> { controllerFile }
                };
                nodes.Add(controllerNode);

                edges.Add(new GraphEdge
                {
                    SourceId = projectId,
                    TargetId = controllerNode.Id,
                    RelationType = GraphRelationType.Contains
                });
            }

            // 分析服务
            var serviceFiles = Directory.GetFiles(projectDir, "*Service.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .Take(10)
                .ToArray();

            foreach (var serviceFile in serviceFiles)
            {
                var serviceName = Path.GetFileNameWithoutExtension(serviceFile);
                var serviceNode = new GraphNode
                {
                    Id = $"service_{serviceName}",
                    Type = GraphNodeType.Service,
                    Name = serviceName,
                    FileRefs = new List<string> { serviceFile }
                };
                nodes.Add(serviceNode);

                edges.Add(new GraphEdge
                {
                    SourceId = projectId,
                    TargetId = serviceNode.Id,
                    RelationType = GraphRelationType.Contains
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"分析项目结构时发生错误: {ex.Message}");
        }
    }

    private async Task<List<CodeSnippet>> ExtractKeySnippetsAsync(string rootPath, CancellationToken cancellationToken)
    {
        var snippets = new List<CodeSnippet>();

        try
        {
            // 提取关键配置文件片段
            var configFiles = new[] { "Program.cs", "Startup.cs" };
            
            foreach (var configFile in configFiles)
            {
                var files = Directory.GetFiles(rootPath, configFile, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                    .Take(3)
                    .ToArray();

                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var content = await File.ReadAllTextAsync(file, cancellationToken);
                    var lines = content.Split('\n');

                    // 提取关键方法
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (line.Contains("ConfigureServices") || 
                            line.Contains("Configure") || 
                            line.Contains("CreateBuilder") ||
                            line.Contains("UseUrls"))
                        {
                            var snippet = ExtractMethodSnippet(lines, i, file);
                            if (snippet != null)
                            {
                                snippets.Add(snippet);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取关键代码片段时发生错误: {ex.Message}");
        }

        return snippets.Take(20).ToList(); // 限制数量
    }

    private CodeSnippet? ExtractMethodSnippet(string[] lines, int startIndex, string filePath)
    {
        try
        {
            var startLine = Math.Max(0, startIndex - 2);
            var endLine = Math.Min(lines.Length - 1, startIndex + 10);

            var content = string.Join("\n", lines[startLine..endLine]);
            
            return new CodeSnippet
            {
                FilePath = filePath,
                StartLine = startLine + 1,
                EndLine = endLine + 1,
                Content = content,
                Type = "Configuration",
                Importance = 0.8,
                Context = "关键配置方法"
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> DetectFrameworkAsync(string rootPath, CancellationToken cancellationToken)
    {
        try
        {
            var projFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);
            
            foreach (var projFile in projFiles.Take(3))
            {
                var content = await File.ReadAllTextAsync(projFile, cancellationToken);
                
                if (content.Contains("Microsoft.NET.Sdk.Web"))
                {
                    return "ASP.NET Core";
                }
                else if (content.Contains("Microsoft.NET.Sdk"))
                {
                    return ".NET";
                }
                else if (content.Contains("Microsoft.NET.Sdk.WindowsDesktop"))
                {
                    return "WPF/WinForms";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检测框架类型时发生错误: {ex.Message}");
        }

        return "Unknown";
    }

    private async Task<List<string>> DetectMainModulesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var modules = new List<string>();

        try
        {
            var directories = Directory.GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly)
                .Where(d => !Path.GetFileName(d).StartsWith(".") && 
                           !Path.GetFileName(d).Equals("bin", StringComparison.OrdinalIgnoreCase) &&
                           !Path.GetFileName(d).Equals("obj", StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToArray();

            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                var csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Length;
                
                if (csFiles > 0)
                {
                    modules.Add($"{dirName} ({csFiles} files)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检测主要模块时发生错误: {ex.Message}");
        }

        return modules;
    }

    private async Task<List<string>> DetectKeyFilesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var keyFiles = new List<string>();

        try
        {
            // 添加解决方案和项目文件
            keyFiles.AddRange(Directory.GetFiles(rootPath, "*.sln", SearchOption.TopDirectoryOnly));
            keyFiles.AddRange(Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories).Take(5));
            
            // 添加关键配置文件
            var configFiles = new[] { "Program.cs", "Startup.cs", "appsettings.json", "launchSettings.json" };
            foreach (var configFile in configFiles)
            {
                var files = Directory.GetFiles(rootPath, configFile, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                    .Take(2);
                keyFiles.AddRange(files);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检测关键文件时发生错误: {ex.Message}");
        }

        return keyFiles.Take(20).ToList();
    }

    private async Task<Dictionary<string, object>> CalculateComplexityMetricsAsync(string rootPath, CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            var csFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToArray();

            metrics["TotalFiles"] = csFiles.Length;
            metrics["TotalProjects"] = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories).Length;
            
            var totalLines = 0;
            var totalMethods = 0;
            
            foreach (var file in csFiles.Take(50)) // 限制分析数量
            {
                if (cancellationToken.IsCancellationRequested) break;

                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l.Trim())).Count();
                var methods = Regex.Matches(content, @"\b(public|private|protected|internal)\s+[^\s]+\s+\w+\s*\(").Count;
                
                totalLines += lines;
                totalMethods += methods;
            }

            metrics["TotalLinesOfCode"] = totalLines;
            metrics["TotalMethods"] = totalMethods;
            metrics["AverageMethodsPerFile"] = csFiles.Length > 0 ? (double)totalMethods / csFiles.Length : 0;
            metrics["AverageLinesPerFile"] = csFiles.Length > 0 ? (double)totalLines / csFiles.Length : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"计算复杂度指标时发生错误: {ex.Message}");
            metrics["Error"] = ex.Message;
        }

        return metrics;
    }

    private double CalculatePackageRiskScore(string packageName, string version)
    {
        // 简单的风险评分逻辑
        var riskScore = 0.0;

        // 检查是否为预览版本
        if (version.Contains("preview") || version.Contains("alpha") || version.Contains("beta"))
        {
            riskScore += 30;
        }

        // 检查版本是否过旧（简单启发式）
        if (version.StartsWith("1.") || version.StartsWith("2."))
        {
            riskScore += 20;
        }

        // 检查是否为已知的高风险包（示例）
        var highRiskPackages = new[] { "Newtonsoft.Json" }; // 示例
        if (highRiskPackages.Contains(packageName, StringComparer.OrdinalIgnoreCase))
        {
            riskScore += 10;
        }

        return Math.Min(riskScore, 100);
    }

    #endregion
}