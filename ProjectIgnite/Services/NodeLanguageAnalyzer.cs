using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProjectIgnite.DTOs;

namespace ProjectIgnite.Services;

/// <summary>
/// Node.js 语言分析器
/// </summary>
public class NodeLanguageAnalyzer : ILanguageAnalyzer
{
    public string LanguageType => "node";

    private static readonly string[] KeyFilePatterns = 
    {
        "package.json", "package-lock.json", "yarn.lock", "pnpm-lock.yaml",
        "tsconfig.json", "webpack.config.js", "vite.config.js", "next.config.js"
    };

    private static readonly Dictionary<string, int> FrameworkDefaultPorts = new()
    {
        { "express", 3000 },
        { "koa", 3000 },
        { "fastify", 3000 },
        { "next", 3000 },
        { "nuxt", 3000 },
        { "vite", 5173 },
        { "webpack-dev-server", 8080 },
        { "create-react-app", 3000 },
        { "vue-cli-service", 8080 },
        { "angular", 4200 },
        { "nest", 3000 }
    };

    public async Task<LanguageDetectionResult> DetectAsync(string rootPath)
    {
        var result = new LanguageDetectionResult();
        var keyFiles = new List<string>();
        var confidence = 0.0;

        try
        {
            // 检查 package.json
            var packageJsonPath = Path.Combine(rootPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                keyFiles.Add(packageJsonPath);
                confidence += 0.6;

                // 分析 package.json 内容
                var packageContent = await File.ReadAllTextAsync(packageJsonPath);
                try
                {
                    var packageJson = JsonSerializer.Deserialize<JsonElement>(packageContent);
                    
                    // 检查是否有 Node.js 特定的字段
                    if (packageJson.TryGetProperty("engines", out var engines) &&
                        engines.TryGetProperty("node", out _))
                    {
                        confidence += 0.2;
                    }

                    if (packageJson.TryGetProperty("scripts", out _))
                    {
                        confidence += 0.1;
                    }

                    if (packageJson.TryGetProperty("dependencies", out _) ||
                        packageJson.TryGetProperty("devDependencies", out _))
                    {
                        confidence += 0.1;
                    }
                }
                catch
                {
                    // JSON 解析失败，但文件存在仍然有一定置信度
                }
            }

            // 检查锁文件
            var lockFiles = new[] { "package-lock.json", "yarn.lock", "pnpm-lock.yaml" };
            foreach (var lockFile in lockFiles)
            {
                var lockPath = Path.Combine(rootPath, lockFile);
                if (File.Exists(lockPath))
                {
                    keyFiles.Add(lockPath);
                    confidence += 0.1;
                    break; // 只需要一个锁文件
                }
            }

            // 检查 node_modules 目录
            var nodeModulesPath = Path.Combine(rootPath, "node_modules");
            if (Directory.Exists(nodeModulesPath))
            {
                confidence += 0.1;
            }

            // 检查 TypeScript 配置
            var tsConfigPath = Path.Combine(rootPath, "tsconfig.json");
            if (File.Exists(tsConfigPath))
            {
                keyFiles.Add(tsConfigPath);
                confidence += 0.1;
            }

            // 检查 JavaScript/TypeScript 文件
            var jsFiles = Directory.GetFiles(rootPath, "*.js", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(rootPath, "*.ts", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(rootPath, "*.jsx", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(rootPath, "*.tsx", SearchOption.AllDirectories))
                .Where(f => !f.Contains("node_modules") && !f.Contains("dist") && !f.Contains("build"))
                .Take(10)
                .ToArray();

            if (jsFiles.Length > 0)
            {
                keyFiles.AddRange(jsFiles);
                confidence += Math.Min(jsFiles.Length * 0.01, 0.1);
            }

            result.IsDetected = confidence > 0.2;
            result.Confidence = Math.Min(confidence, 1.0);
            result.KeyFiles = keyFiles;
            result.Reason = confidence > 0.2 ?
                $"检测到 package.json: {File.Exists(packageJsonPath)}，{jsFiles.Length} 个 JS/TS 文件" :
                "未检测到 Node.js 项目特征文件";
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
            Console.WriteLine($"Node.js 扫描过程中发生错误: {ex.Message}");
        }

        return result;
    }

    public async Task<RunInfo> ExtractRunInfoAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var runInfo = new RunInfo();

        try
        {
            // 从 package.json 提取
            await ExtractFromPackageJsonAsync(rootPath, runInfo, cancellationToken);

            // 从环境变量文件提取
            await ExtractFromEnvFilesAsync(rootPath, runInfo, cancellationToken);

            // 从代码文件推断
            await ExtractFromCodeFilesAsync(rootPath, runInfo, cancellationToken);

            // 从框架配置推断
            await ExtractFromFrameworkConfigAsync(rootPath, runInfo, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取 Node.js 运行信息时发生错误: {ex.Message}");
        }

        return runInfo;
    }

    public async Task<DependenciesResult> ExtractDependenciesAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var result = new DependenciesResult();

        try
        {
            var packageJsonPath = Path.Combine(rootPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
                var packageJson = JsonSerializer.Deserialize<JsonElement>(content);

                // 提取生产依赖
                if (packageJson.TryGetProperty("dependencies", out var dependencies))
                {
                    foreach (var dep in dependencies.EnumerateObject())
                    {
                        result.Node.Add(new PackageEntry
                        {
                            Name = dep.Name,
                            Version = dep.Value.GetString() ?? "unknown",
                            DevDependency = false,
                            Source = PackageSource.PackageJson,
                            RiskScore = CalculatePackageRiskScore(dep.Name, dep.Value.GetString() ?? "")
                        });
                    }
                }

                // 提取开发依赖
                if (packageJson.TryGetProperty("devDependencies", out var devDependencies))
                {
                    foreach (var dep in devDependencies.EnumerateObject())
                    {
                        result.Node.Add(new PackageEntry
                        {
                            Name = dep.Name,
                            Version = dep.Value.GetString() ?? "unknown",
                            DevDependency = true,
                            Source = PackageSource.PackageJson,
                            RiskScore = CalculatePackageRiskScore(dep.Name, dep.Value.GetString() ?? "")
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取 Node.js 依赖信息时发生错误: {ex.Message}");
        }

        return result;
    }

    public async Task<ProjectSummary> SummarizeAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var summary = new ProjectSummary
        {
            ProjectName = Path.GetFileName(rootPath),
            ProjectType = "Node.js Project"
        };

        try
        {
            var packageJsonPath = Path.Combine(rootPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
                var packageJson = JsonSerializer.Deserialize<JsonElement>(content);

                // 获取项目名称
                if (packageJson.TryGetProperty("name", out var nameElement))
                {
                    summary.ProjectName = nameElement.GetString() ?? summary.ProjectName;
                }

                // 检测框架类型
                summary.Framework = await DetectFrameworkAsync(packageJson, rootPath, cancellationToken);
                summary.ProjectType = $"Node.js {summary.Framework} Project";

                // 分析主要模块
                summary.MainModules = await DetectMainModulesAsync(rootPath, cancellationToken);

                // 分析关键文件
                summary.KeyFiles = await DetectKeyFilesAsync(rootPath, cancellationToken);

                // 分析顶级依赖
                var deps = await ExtractDependenciesAsync(rootPath, cancellationToken);
                summary.TopDependencies = deps.Node
                    .Where(p => !p.DevDependency)
                    .OrderByDescending(p => p.RiskScore)
                    .Take(10)
                    .Select(p => $"{p.Name} ({p.Version})")
                    .ToList();

                // 计算复杂度指标
                summary.ComplexityMetrics = await CalculateComplexityMetricsAsync(rootPath, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成 Node.js 项目摘要时发生错误: {ex.Message}");
        }

        return summary;
    }

    #region 私有辅助方法

    private async Task<EnvironmentInfo> ExtractEnvironmentInfoAsync(string rootPath, CancellationToken cancellationToken)
    {
        var envInfo = new EnvironmentInfo();

        try
        {
            var packageJsonPath = Path.Combine(rootPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
                var packageJson = JsonSerializer.Deserialize<JsonElement>(content);

                // 提取 Node.js 版本要求
                if (packageJson.TryGetProperty("engines", out var engines))
                {
                    if (engines.TryGetProperty("node", out var nodeVersion))
                    {
                        envInfo.NodeVersion = nodeVersion.GetString();
                    }
                }

                // 检测包管理器
                envInfo.PackageManager = DetectPackageManager(rootPath);
            }

            // 检查 TypeScript 配置
            var tsConfigPath = Path.Combine(rootPath, "tsconfig.json");
            if (File.Exists(tsConfigPath))
            {
                envInfo.LanguageVersion = "TypeScript";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取 Node.js 环境信息时发生错误: {ex.Message}");
        }

        return envInfo;
    }

    private PackageManagerType DetectPackageManager(string rootPath)
    {
        if (File.Exists(Path.Combine(rootPath, "pnpm-lock.yaml")))
            return PackageManagerType.Pnpm;
        if (File.Exists(Path.Combine(rootPath, "yarn.lock")))
            return PackageManagerType.Yarn;
        if (File.Exists(Path.Combine(rootPath, "package-lock.json")))
            return PackageManagerType.Npm;
        
        return PackageManagerType.Npm; // 默认
    }

    private async Task ExtractFromPackageJsonAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        var packageJsonPath = Path.Combine(rootPath, "package.json");
        if (!File.Exists(packageJsonPath)) return;

        try
        {
            var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
            var packageJson = JsonSerializer.Deserialize<JsonElement>(content);

            if (packageJson.TryGetProperty("scripts", out var scripts))
            {
                foreach (var script in scripts.EnumerateObject())
                {
                    var scriptValue = script.Value.GetString() ?? "";
                    runInfo.StartCommands.Add($"npm run {script.Name}");

                    // 从脚本中提取端口信息
                    ExtractPortsFromScript(scriptValue, runInfo, script.Name);
                }
            }

            // 检测框架并添加默认端口
            var framework = await DetectFrameworkAsync(packageJson, rootPath, cancellationToken);
            if (FrameworkDefaultPorts.TryGetValue(framework.ToLower(), out var defaultPort))
            {
                runInfo.Ports.Add(new PortCandidate
                {
                    Value = defaultPort,
                    Confidence = 0.4,
                    Source = RunInfoSource.Default,
                    Notes = $"{framework} 框架默认端口"
                });

                runInfo.Urls.Add(new UrlCandidate
                {
                    Value = $"http://localhost:{defaultPort}",
                    Confidence = 0.4,
                    Source = RunInfoSource.Default,
                    Notes = $"{framework} 框架默认 URL"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析 package.json 时发生错误: {ex.Message}");
        }
    }

    private void ExtractPortsFromScript(string script, RunInfo runInfo, string scriptName)
    {
        // 匹配 --port 参数
        var portMatches = Regex.Matches(script, @"--port[\s=](\d+)");
        foreach (Match match in portMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out var port))
            {
                runInfo.Ports.Add(new PortCandidate
                {
                    Value = port,
                    Confidence = 0.8,
                    Source = RunInfoSource.Scripts,
                    Notes = $"来自 {scriptName} 脚本的 --port 参数"
                });
            }
        }

        // 匹配 PORT= 环境变量
        var envPortMatches = Regex.Matches(script, @"PORT=(\d+)");
        foreach (Match match in envPortMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out var port))
            {
                runInfo.Ports.Add(new PortCandidate
                {
                    Value = port,
                    Confidence = 0.7,
                    Source = RunInfoSource.Scripts,
                    Notes = $"来自 {scriptName} 脚本的 PORT 环境变量"
                });
            }
        }

        // 匹配 -p 参数
        var pMatches = Regex.Matches(script, @"\s-p\s+(\d+)");
        foreach (Match match in pMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out var port))
            {
                runInfo.Ports.Add(new PortCandidate
                {
                    Value = port,
                    Confidence = 0.7,
                    Source = RunInfoSource.Scripts,
                    Notes = $"来自 {scriptName} 脚本的 -p 参数"
                });
            }
        }
    }

    private async Task ExtractFromEnvFilesAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        var envFiles = new[] { ".env", ".env.local", ".env.development", ".env.production" };

        foreach (var envFile in envFiles)
        {
            var envPath = Path.Combine(rootPath, envFile);
            if (File.Exists(envPath))
            {
                runInfo.EnvironmentFiles.Add(envPath);
                
                try
                {
                    // 注意：这里只记录文件存在，不读取内容以保护隐私
                    // 但可以尝试提取端口相关的非敏感信息
                    var lines = await File.ReadAllLinesAsync(envPath, cancellationToken);
                    foreach (var line in lines.Take(50)) // 限制读取行数
                    {
                        if (line.StartsWith("PORT=") && !line.Contains("PASSWORD") && !line.Contains("SECRET"))
                        {
                            var portValue = line.Substring(5).Trim();
                            if (int.TryParse(portValue, out var port))
                            {
                                runInfo.Ports.Add(new PortCandidate
                                {
                                    Value = port,
                                    Confidence = 0.8,
                                    Source = RunInfoSource.Scripts,
                                    Notes = $"来自 {envFile} 文件"
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // 忽略读取错误，保护隐私
                }
            }
        }
    }

    private async Task ExtractFromCodeFilesAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        var jsFiles = Directory.GetFiles(rootPath, "*.js", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(rootPath, "*.ts", SearchOption.AllDirectories))
            .Where(f => !f.Contains("node_modules") && !f.Contains("dist") && !f.Contains("build"))
            .Take(20)
            .ToArray();

        foreach (var jsFile in jsFiles)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var content = await File.ReadAllTextAsync(jsFile, cancellationToken);
                
                // 查找 listen 调用
                var listenMatches = Regex.Matches(content, @"\.listen\s*\(\s*(\d+)");
                foreach (Match match in listenMatches)
                {
                    if (int.TryParse(match.Groups[1].Value, out var port))
                    {
                        runInfo.Ports.Add(new PortCandidate
                        {
                            Value = port,
                            Confidence = 0.7,
                            Source = RunInfoSource.CodeInference,
                            Notes = $"来自 {Path.GetFileName(jsFile)} 的 listen 调用"
                        });
                    }
                }

                // 查找 process.env.PORT
                var envPortMatches = Regex.Matches(content, @"process\.env\.PORT\s*\|\|\s*(\d+)");
                foreach (Match match in envPortMatches)
                {
                    if (int.TryParse(match.Groups[1].Value, out var port))
                    {
                        runInfo.Ports.Add(new PortCandidate
                        {
                            Value = port,
                            Confidence = 0.6,
                            Source = RunInfoSource.CodeInference,
                            Notes = $"来自 {Path.GetFileName(jsFile)} 的环境变量默认值"
                        });
                    }
                }

                // 查找端口配置
                var portConfigMatches = Regex.Matches(content, @"(?:port|PORT)\s*[=:]\s*(\d+)");
                foreach (Match match in portConfigMatches)
                {
                    if (int.TryParse(match.Groups[1].Value, out var port) && port > 1000 && port < 65536)
                    {
                        runInfo.Ports.Add(new PortCandidate
                        {
                            Value = port,
                            Confidence = 0.5,
                            Source = RunInfoSource.CodeInference,
                            Notes = $"来自 {Path.GetFileName(jsFile)} 的端口配置"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析代码文件 {Path.GetFileName(jsFile)} 时发生错误: {ex.Message}");
            }
        }
    }

    private async Task ExtractFromFrameworkConfigAsync(string rootPath, RunInfo runInfo, CancellationToken cancellationToken)
    {
        // 检查 Next.js 配置
        var nextConfigPath = Path.Combine(rootPath, "next.config.js");
        if (File.Exists(nextConfigPath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(nextConfigPath, cancellationToken);
                // Next.js 通常使用 3000 端口
                runInfo.Ports.Add(new PortCandidate
                {
                    Value = 3000,
                    Confidence = 0.6,
                    Source = RunInfoSource.Default,
                    Notes = "Next.js 项目默认端口"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析 Next.js 配置时发生错误: {ex.Message}");
            }
        }

        // 检查 Vite 配置
        var viteConfigFiles = new[] { "vite.config.js", "vite.config.ts" };
        foreach (var configFile in viteConfigFiles)
        {
            var configPath = Path.Combine(rootPath, configFile);
            if (File.Exists(configPath))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(configPath, cancellationToken);
                    
                    // 查找端口配置
                    var portMatch = Regex.Match(content, @"port\s*:\s*(\d+)");
                    if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port))
                    {
                        runInfo.Ports.Add(new PortCandidate
                        {
                            Value = port,
                            Confidence = 0.8,
                            Source = RunInfoSource.CodeInference,
                            Notes = $"来自 {configFile} 配置"
                        });
                    }
                    else
                    {
                        // Vite 默认端口
                        runInfo.Ports.Add(new PortCandidate
                        {
                            Value = 5173,
                            Confidence = 0.5,
                            Source = RunInfoSource.Default,
                            Notes = "Vite 默认端口"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"分析 Vite 配置时发生错误: {ex.Message}");
                }
                break;
            }
        }
    }

    private async Task<(List<GraphNode>, List<GraphEdge>)> ExtractStructureAsync(string rootPath, CancellationToken cancellationToken)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        try
        {
            // 创建根项目节点
            var projectName = Path.GetFileName(rootPath);
            var packageJsonPath = Path.Combine(rootPath, "package.json");
            
            if (File.Exists(packageJsonPath))
            {
                var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
                var packageJson = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (packageJson.TryGetProperty("name", out var nameElement))
                {
                    projectName = nameElement.GetString() ?? projectName;
                }
            }

            var projectNode = new GraphNode
            {
                Id = "project",
                Type = GraphNodeType.Project,
                Name = projectName,
                FileRefs = new List<string> { packageJsonPath }
            };
            nodes.Add(projectNode);

            // 分析主要目录结构
            await AnalyzeDirectoryStructureAsync(rootPath, "project", nodes, edges, cancellationToken);

            // 分析依赖关系
            await AnalyzeDependencyStructureAsync(rootPath, "project", nodes, edges, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取 Node.js 结构信息时发生错误: {ex.Message}");
        }

        return (nodes, edges);
    }

    private async Task AnalyzeDirectoryStructureAsync(string rootPath, string parentId, List<GraphNode> nodes, List<GraphEdge> edges, CancellationToken cancellationToken)
    {
        try
        {
            var directories = Directory.GetDirectories(rootPath)
                .Where(d => !Path.GetFileName(d).StartsWith(".") &&
                           !Path.GetFileName(d).Equals("node_modules", StringComparison.OrdinalIgnoreCase) &&
                           !Path.GetFileName(d).Equals("dist", StringComparison.OrdinalIgnoreCase) &&
                           !Path.GetFileName(d).Equals("build", StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToArray();

            foreach (var dir in directories)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var dirName = Path.GetFileName(dir);
                var jsFiles = Directory.GetFiles(dir, "*.js", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(dir, "*.ts", SearchOption.AllDirectories))
                    .Where(f => !f.Contains("node_modules"))
                    .Count();

                if (jsFiles > 0)
                {
                    var moduleNode = new GraphNode
                    {
                        Id = $"module_{dirName}",
                        Type = GraphNodeType.Module,
                        Name = $"{dirName} ({jsFiles} files)",
                        Tags = new List<string> { "directory" },
                        FileRefs = new List<string> { dir }
                    };
                    nodes.Add(moduleNode);

                    edges.Add(new GraphEdge
                    {
                        SourceId = parentId,
                        TargetId = moduleNode.Id,
                        RelationType = GraphRelationType.Contains
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"分析目录结构时发生错误: {ex.Message}");
        }
    }

    private async Task AnalyzeDependencyStructureAsync(string rootPath, string parentId, List<GraphNode> nodes, List<GraphEdge> edges, CancellationToken cancellationToken)
    {
        try
        {
            var deps = await ExtractDependenciesAsync(rootPath, cancellationToken);
            
            // 只显示主要依赖（非开发依赖且风险评分较高的）
            var majorDeps = deps.Node
                .Where(p => !p.DevDependency && p.RiskScore > 20)
                .OrderByDescending(p => p.RiskScore)
                .Take(8)
                .ToList();

            foreach (var dep in majorDeps)
            {
                var depNode = new GraphNode
                {
                    Id = $"package_{dep.Name}",
                    Type = GraphNodeType.Package,
                    Name = $"{dep.Name} ({dep.Version})",
                    Tags = new List<string> { "dependency" },
                    RiskScore = dep.RiskScore
                };
                nodes.Add(depNode);

                edges.Add(new GraphEdge
                {
                    SourceId = parentId,
                    TargetId = depNode.Id,
                    RelationType = GraphRelationType.DependsOn,
                    Weight = dep.RiskScore / 100.0
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"分析依赖结构时发生错误: {ex.Message}");
        }
    }

    private async Task<List<CodeSnippet>> ExtractKeySnippetsAsync(string rootPath, CancellationToken cancellationToken)
    {
        var snippets = new List<CodeSnippet>();

        try
        {
            // 提取 package.json 关键片段
            var packageJsonPath = Path.Combine(rootPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
                snippets.Add(new CodeSnippet
                {
                    FilePath = packageJsonPath,
                    StartLine = 1,
                    EndLine = Math.Min(content.Split('\n').Length, 50),
                    Content = content.Length > 2000 ? content.Substring(0, 2000) + "..." : content,
                    Type = "Configuration",
                    Importance = 0.9,
                    Context = "项目配置文件"
                });
            }

            // 提取主要入口文件
            var entryFiles = new[] { "index.js", "index.ts", "app.js", "app.ts", "server.js", "server.ts" };
            foreach (var entryFile in entryFiles)
            {
                var entryPath = Path.Combine(rootPath, entryFile);
                if (File.Exists(entryPath))
                {
                    var content = await File.ReadAllTextAsync(entryPath, cancellationToken);
                    var lines = content.Split('\n');
                    
                    snippets.Add(new CodeSnippet
                    {
                        FilePath = entryPath,
                        StartLine = 1,
                        EndLine = Math.Min(lines.Length, 30),
                        Content = string.Join("\n", lines.Take(30)),
                        Type = "Entry Point",
                        Importance = 0.8,
                        Context = "应用入口文件"
                    });
                    break; // 只需要一个入口文件
                }
            }

            // 提取配置文件片段
            var configFiles = Directory.GetFiles(rootPath, "*.config.js", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(rootPath, "*.config.ts", SearchOption.TopDirectoryOnly))
                .Take(3)
                .ToArray();

            foreach (var configFile in configFiles)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var content = await File.ReadAllTextAsync(configFile, cancellationToken);
                var lines = content.Split('\n');
                
                snippets.Add(new CodeSnippet
                {
                    FilePath = configFile,
                    StartLine = 1,
                    EndLine = Math.Min(lines.Length, 25),
                    Content = string.Join("\n", lines.Take(25)),
                    Type = "Configuration",
                    Importance = 0.7,
                    Context = "框架配置文件"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取 Node.js 关键代码片段时发生错误: {ex.Message}");
        }

        return snippets.Take(15).ToList(); // 限制数量
    }

    private async Task<string> DetectFrameworkAsync(JsonElement packageJson, string rootPath, CancellationToken cancellationToken)
    {
        try
        {
            // 检查依赖中的框架
            var allDeps = new List<string>();
            
            if (packageJson.TryGetProperty("dependencies", out var deps))
            {
                allDeps.AddRange(deps.EnumerateObject().Select(p => p.Name));
            }
            
            if (packageJson.TryGetProperty("devDependencies", out var devDeps))
            {
                allDeps.AddRange(devDeps.EnumerateObject().Select(p => p.Name));
            }

            // 按优先级检测框架
            var frameworks = new Dictionary<string, string[]>
            {
                { "Next.js", new[] { "next" } },
                { "Nuxt.js", new[] { "nuxt" } },
                { "Express", new[] { "express" } },
                { "Koa", new[] { "koa" } },
                { "Fastify", new[] { "fastify" } },
                { "NestJS", new[] { "@nestjs/core" } },
                { "React", new[] { "react" } },
                { "Vue", new[] { "vue" } },
                { "Angular", new[] { "@angular/core" } },
                { "Vite", new[] { "vite" } },
                { "Webpack", new[] { "webpack" } }
            };

            foreach (var framework in frameworks)
            {
                if (framework.Value.Any(dep => allDeps.Contains(dep)))
                {
                    return framework.Key;
                }
            }

            // 检查配置文件
            if (File.Exists(Path.Combine(rootPath, "next.config.js")))
                return "Next.js";
            if (File.Exists(Path.Combine(rootPath, "nuxt.config.js")))
                return "Nuxt.js";
            if (File.Exists(Path.Combine(rootPath, "vite.config.js")) || File.Exists(Path.Combine(rootPath, "vite.config.ts")))
                return "Vite";
            if (File.Exists(Path.Combine(rootPath, "webpack.config.js")))
                return "Webpack";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检测框架类型时发生错误: {ex.Message}");
        }

        return "Node.js";
    }

    private async Task<List<string>> DetectMainModulesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var modules = new List<string>();

        try
        {
            var directories = Directory.GetDirectories(rootPath)
                .Where(d => !Path.GetFileName(d).StartsWith(".") &&
                           !Path.GetFileName(d).Equals("node_modules", StringComparison.OrdinalIgnoreCase) &&
                           !Path.GetFileName(d).Equals("dist", StringComparison.OrdinalIgnoreCase) &&
                           !Path.GetFileName(d).Equals("build", StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToArray();

            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                var jsFiles = Directory.GetFiles(dir, "*.js", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(dir, "*.ts", SearchOption.AllDirectories))
                    .Where(f => !f.Contains("node_modules"))
                    .Count();

                if (jsFiles > 0)
                {
                    modules.Add($"{dirName} ({jsFiles} files)");
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
            // 添加配置文件
            var configFiles = new[] 
            {
                "package.json", "tsconfig.json", "webpack.config.js", "vite.config.js", 
                "next.config.js", "nuxt.config.js", ".env", ".env.local"
            };

            foreach (var configFile in configFiles)
            {
                var filePath = Path.Combine(rootPath, configFile);
                if (File.Exists(filePath))
                {
                    keyFiles.Add(filePath);
                }
            }

            // 添加入口文件
            var entryFiles = new[] { "index.js", "index.ts", "app.js", "app.ts", "server.js", "server.ts" };
            foreach (var entryFile in entryFiles)
            {
                var filePath = Path.Combine(rootPath, entryFile);
                if (File.Exists(filePath))
                {
                    keyFiles.Add(filePath);
                }
            }

            // 添加锁文件
            var lockFiles = new[] { "package-lock.json", "yarn.lock", "pnpm-lock.yaml" };
            foreach (var lockFile in lockFiles)
            {
                var filePath = Path.Combine(rootPath, lockFile);
                if (File.Exists(filePath))
                {
                    keyFiles.Add(filePath);
                    break; // 只需要一个锁文件
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检测关键文件时发生错误: {ex.Message}");
        }

        return keyFiles.Take(15).ToList();
    }

    private async Task<Dictionary<string, object>> CalculateComplexityMetricsAsync(string rootPath, CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            var jsFiles = Directory.GetFiles(rootPath, "*.js", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(rootPath, "*.ts", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(rootPath, "*.jsx", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(rootPath, "*.tsx", SearchOption.AllDirectories))
                .Where(f => !f.Contains("node_modules") && !f.Contains("dist") && !f.Contains("build"))
                .ToArray();

            metrics["TotalFiles"] = jsFiles.Length;
            
            var totalLines = 0;
            var totalFunctions = 0;
            
            foreach (var file in jsFiles.Take(100)) // 限制分析数量
            {
                if (cancellationToken.IsCancellationRequested) break;

                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l.Trim())).Count();
                var functions = Regex.Matches(content, @"\bfunction\s+\w+|\w+\s*=>|\w+\s*:\s*function").Count;
                
                totalLines += lines;
                totalFunctions += functions;
            }

            metrics["TotalLinesOfCode"] = totalLines;
            metrics["TotalFunctions"] = totalFunctions;
            metrics["AverageFunctionsPerFile"] = jsFiles.Length > 0 ? (double)totalFunctions / jsFiles.Length : 0;
            metrics["AverageLinesPerFile"] = jsFiles.Length > 0 ? (double)totalLines / jsFiles.Length : 0;

            // 检查是否使用 TypeScript
            var tsFiles = jsFiles.Where(f => f.EndsWith(".ts") || f.EndsWith(".tsx")).Count();
            metrics["TypeScriptUsage"] = jsFiles.Length > 0 ? (double)tsFiles / jsFiles.Length : 0;
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
        var riskScore = 0.0;

        // 检查是否为预览版本
        if (version.Contains("alpha") || version.Contains("beta") || version.Contains("rc"))
        {
            riskScore += 40;
        }

        // 检查版本是否过旧
        if (version.StartsWith("0."))
        {
            riskScore += 30;
        }
        else if (version.StartsWith("1."))
        {
            riskScore += 15;
        }

        // 检查是否为已知的高维护成本包
        var highMaintenancePackages = new[] 
        {
            "webpack", "babel", "eslint", "typescript", "react", "vue", "angular"
        };
        
        if (highMaintenancePackages.Any(pkg => packageName.Contains(pkg, StringComparison.OrdinalIgnoreCase)))
        {
            riskScore += 20;
        }

        // 检查是否为核心依赖
        var corePackages = new[] 
        {
            "express", "react", "vue", "next", "nuxt", "koa", "fastify"
        };
        
        if (corePackages.Contains(packageName, StringComparer.OrdinalIgnoreCase))
        {
            riskScore += 25; // 核心依赖风险较高
        }

        return Math.Min(riskScore, 100);
    }

    #endregion
}