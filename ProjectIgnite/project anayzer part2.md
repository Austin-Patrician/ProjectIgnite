# 阶段二：结构化数据提取 - 产品设计文档

## 1. 功能概述

### 1.1 功能描述
结构化数据提取是在项目发现分类基础上，深度解析项目文件内容，提取关键配置信息、依赖关系、架构模式等结构化数据，为AI分析提供高质量的输入数据。

### 1.2 核心价值
- **精准提取**：智能识别关键信息，过滤噪音数据
- **结构化输出**：统一的数据格式便于后续AI分析
- **多维度解析**：从配置、依赖、架构、业务逻辑等维度全面分析
- **增量更新**：支持文件变更时的增量数据提取

## 2. 用户故事与需求分析

### 2.1 用户故事
```
作为一个开发者
我想要系统能自动提取项目中的关键配置和依赖信息
以便快速了解项目的技术栈、运行环境和架构设计
而不需要手动查看每个配置文件
```

### 2.2 核心需求
| 需求ID | 需求描述 | 优先级 | 验收标准 |
|--------|----------|--------|----------|
| SE-001 | 提取C#项目依赖信息 | P0 | 准确解析.csproj、packages.config |
| SE-002 | 提取Node项目依赖信息 | P0 | 准确解析package.json、yarn.lock |
| SE-003 | 提取配置文件信息 | P0 | 支持appsettings.json、.env等 |
| SE-004 | 识别项目架构模式 | P1 | 识别MVC、Clean、Layered等模式 |
| SE-005 | 提取启动配置信息 | P1 | 端口、URL、启动参数等 |
| SE-006 | 代码结构分析 | P1 | Controller、Service、Model分层分析 |

## 3. 技术架构设计

### 3.1 组件架构
```
StructuredDataExtractor
├── FileContentParser          (文件内容解析器)
├── LanguageSpecificExtractors (语言特定提取器)
│   ├── CSharpDataExtractor
│   └── NodeDataExtractor
├── ConfigurationAnalyzer      (配置分析器)
├── DependencyGraphBuilder     (依赖图构建器)
├── ArchitecturePatternDetector (架构模式检测器)
├── CodeStructureAnalyzer      (代码结构分析器)
└── DataNormalizer            (数据标准化器)
```

### 3.2 数据流设计
```
项目文件列表 → 文件优先级排序 → 内容解析 → 语言特定提取 → 配置分析 → 
依赖构建 → 架构检测 → 数据标准化 → 结构化输出
```

## 4. 核心数据模型

### 4.1 统一数据模型
```csharp
public class StructuredProjectData
{
    public ProjectMetadata Metadata { get; set; }
    public ProjectConfiguration Configuration { get; set; }
    public DependencyGraph Dependencies { get; set; }
    public ArchitectureInfo Architecture { get; set; }
    public CodeStructure Structure { get; set; }
    public List<ExtractedFile> SourceFiles { get; set; }
    public ExtractionMetrics Metrics { get; set; }
}

public class ProjectMetadata
{
    public string ProjectName { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string TargetFramework { get; set; }
    public List<string> Authors { get; set; }
    public string License { get; set; }
    public string Repository { get; set; }
}

public class ProjectConfiguration
{
    public ServerConfiguration Server { get; set; }
    public DatabaseConfiguration Database { get; set; }
    public Dictionary<string, EnvironmentConfig> Environments { get; set; }
    public List<ConfigurationFile> ConfigFiles { get; set; }
}

public class ServerConfiguration
{
    public List<int> Ports { get; set; }
    public List<string> Urls { get; set; }
    public string Protocol { get; set; }
    public Dictionary<string, object> Settings { get; set; }
}
```

## 5. 详细功能设计

### 5.1 文件内容解析器 (FileContentParser)

#### 5.1.1 文件优先级策略
```csharp
public class FileParsingPriority
{
    public static Dictionary<string, int> Priorities = new()
    {
        // 配置文件 (最高优先级)
        {".csproj", 10}, {"package.json", 10}, {"appsettings.json", 9},
        {".env", 9}, {"web.config", 9},
        
        // 启动文件
        {"Program.cs", 8}, {"Startup.cs", 8}, {"server.js", 8}, {"app.js", 8},
        
        // 核心业务文件
        {"Controller.cs", 7}, {"Service.cs", 7}, {"Repository.cs", 7},
        
        // 模型和实体
        {"Model.cs", 6}, {"Entity.cs", 6}, {"DTO.cs", 6},
        
        // 其他代码文件
        {".cs", 5}, {".js", 5}, {".ts", 5}
    };
}
```

#### 5.1.2 解析策略
1. **分批解析**：按优先级分批处理，避免内存溢出
2. **智能编码检测**：自动检测文件编码格式
3. **大文件处理**：超过1MB的文件进行流式解析
4. **错误容忍**：单个文件解析失败不影响整体流程

#### 5.1.3 内容预处理
```csharp
public class FileContentPreprocessor
{
    public ProcessedContent Process(RawFileContent content)
    {
        return new ProcessedContent
        {
            CleanContent = RemoveComments(content.Raw),
            CodeBlocks = ExtractCodeBlocks(content.Raw),
            ConfigSections = ExtractConfigSections(content.Raw),
            ImportStatements = ExtractImports(content.Raw),
            Metadata = ExtractMetadata(content.Raw)
        };
    }
}
```

### 5.2 C#项目数据提取器 (CSharpDataExtractor)

#### 5.2.1 项目文件解析
```csharp
public class CSharpProjectFileAnalyzer
{
    public CSharpProjectInfo AnalyzeProject(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        return new CSharpProjectInfo
        {
            TargetFramework = ExtractTargetFramework(doc),
            PackageReferences = ExtractPackageReferences(doc),
            ProjectReferences = ExtractProjectReferences(doc),
            Properties = ExtractProperties(doc),
            BuildConfiguration = ExtractBuildConfig(doc)
        };
    }
    
    private List<PackageReference> ExtractPackageReferences(XDocument doc)
    {
        return doc.Descendants("PackageReference")
            .Select(pr => new PackageReference
            {
                Name = pr.Attribute("Include")?.Value,
                Version = pr.Attribute("Version")?.Value,
                PrivateAssets = pr.Attribute("PrivateAssets")?.Value
            }).ToList();
    }
}
```

#### 5.2.2 配置文件解析
```csharp
public class AppSettingsAnalyzer
{
    public ConfigurationData AnalyzeAppSettings(string jsonPath)
    {
        var json = JObject.Parse(File.ReadAllText(jsonPath));
        
        return new ConfigurationData
        {
            ConnectionStrings = ExtractConnectionStrings(json),
            ServerUrls = ExtractServerConfiguration(json),
            LoggingConfig = ExtractLoggingConfig(json),
            CustomSettings = ExtractCustomSettings(json),
            Environments = DetectEnvironments(json)
        };
    }
    
    private ServerConfiguration ExtractServerConfiguration(JObject json)
    {
        var urls = json["Urls"]?.ToString();
        var kestrel = json["Kestrel"];
        
        return new ServerConfiguration
        {
            Urls = urls?.Split(';').ToList() ?? new List<string>(),
            Ports = ExtractPortsFromKestrel(kestrel),
            HttpsRedirection = json["HttpsRedirection"] != null,
            Cors = ExtractCorsSettings(json["Cors"])
        };
    }
}
```

#### 5.2.3 代码结构分析
```csharp
public class CSharpCodeStructureAnalyzer
{
    public CodeStructure AnalyzeStructure(List<string> csFiles)
    {
        var structure = new CodeStructure();
        
        foreach (var file in csFiles)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = syntaxTree.GetCompilationUnitRoot();
            
            structure.Controllers.AddRange(ExtractControllers(root, file));
            structure.Services.AddRange(ExtractServices(root, file));
            structure.Models.AddRange(ExtractModels(root, file));
            structure.Repositories.AddRange(ExtractRepositories(root, file));
        }
        
        return structure;
    }
    
    private List<ControllerInfo> ExtractControllers(SyntaxNode root, string filePath)
    {
        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.BaseList?.Types.Any(t => 
                t.Type.ToString().Contains("Controller")) == true)
            .Select(c => new ControllerInfo
            {
                Name = c.Identifier.ValueText,
                FilePath = filePath,
                Actions = ExtractActions(c),
                Routes = ExtractRoutes(c),
                Dependencies = ExtractConstructorDependencies(c)
            }).ToList();
    }
}
```

### 5.3 Node.js项目数据提取器 (NodeDataExtractor)

#### 5.3.1 Package.json解析
```csharp
public class PackageJsonAnalyzer
{
    public NodeProjectInfo AnalyzePackageJson(string packageJsonPath)
    {
        var json = JObject.Parse(File.ReadAllText(packageJsonPath));
        
        return new NodeProjectInfo
        {
            Name = json["name"]?.ToString(),
            Version = json["version"]?.ToString(),
            Description = json["description"]?.ToString(),
            Main = json["main"]?.ToString(),
            Scripts = ExtractScripts(json["scripts"] as JObject),
            Dependencies = ExtractDependencies(json["dependencies"] as JObject),
            DevDependencies = ExtractDependencies(json["devDependencies"] as JObject),
            Engines = ExtractEngines(json["engines"] as JObject),
            Repository = ExtractRepository(json["repository"])
        };
    }
    
    private Dictionary<string, string> ExtractScripts(JObject scripts)
    {
        if (scripts == null) return new Dictionary<string, string>();
        
        return scripts.Properties()
            .ToDictionary(p => p.Name, p => p.Value.ToString());
    }
}
```

#### 5.3.2 Express应用分析
```csharp
public class ExpressAppAnalyzer
{
    public ExpressAppInfo AnalyzeExpressApp(string appFilePath)
    {
        var content = File.ReadAllText(appFilePath);
        
        return new ExpressAppInfo
        {
            Port = ExtractPort(content),
            Routes = ExtractRoutes(content),
            Middleware = ExtractMiddleware(content),
            StaticPaths = ExtractStaticPaths(content),
            ViewEngine = ExtractViewEngine(content),
            DatabaseConnections = ExtractDatabaseConnections(content)
        };
    }
    
    private int? ExtractPort(string content)
    {
        // 使用正则表达式匹配端口配置
        var portPattern = @"\.listen\s*\(\s*(?:process\.env\.PORT\s*\|\|\s*)?(\d+)";
        var envPortPattern = @"PORT\s*=\s*(\d+)";
        
        var match = Regex.Match(content, portPattern);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int port))
        {
            return port;
        }
        
        // 检查环境变量文件
        return ExtractPortFromEnvFile();
    }
}
```

### 5.4 配置分析器 (ConfigurationAnalyzer)

#### 5.4.1 多环境配置检测
```csharp
public class EnvironmentConfigurationDetector
{
    public List<EnvironmentConfig> DetectEnvironments(string projectPath)
    {
        var environments = new List<EnvironmentConfig>();
        
        // C# 项目环境检测
        environments.AddRange(DetectDotNetEnvironments(projectPath));
        
        // Node.js 项目环境检测
        environments.AddRange(DetectNodeEnvironments(projectPath));
        
        return environments;
    }
    
    private List<EnvironmentConfig> DetectDotNetEnvironments(string projectPath)
    {
        var configs = new List<EnvironmentConfig>();
        
        // 检测 appsettings.{env}.json 文件
        var appSettingsFiles = Directory.GetFiles(projectPath, "appsettings.*.json");
        
        foreach (var file in appSettingsFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var envName = fileName.Split('.').LastOrDefault();
            
            if (!string.IsNullOrEmpty(envName) && envName != "appsettings")
            {
                configs.Add(new EnvironmentConfig
                {
                    Name = envName,
                    ConfigFile = file,
                    Variables = ExtractEnvironmentVariables(file)
                });
            }
        }
        
        return configs;
    }
}
```

#### 5.4.2 配置值提取
```csharp
public class ConfigurationValueExtractor
{
    public ConfigurationData ExtractConfiguration(List<string> configFiles)
    {
        var mergedConfig = new ConfigurationData();
        
        foreach (var file in configFiles.OrderBy(GetConfigPriority))
        {
            var fileConfig = ExtractFromFile(file);
            MergeConfiguration(mergedConfig, fileConfig);
        }
        
        return mergedConfig;
    }
    
    private ConfigurationData ExtractFromFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        
        return extension switch
        {
            ".json" => ExtractFromJson(filePath),
            ".xml" => ExtractFromXml(filePath),
            ".env" => ExtractFromEnv(filePath),
            ".ini" => ExtractFromIni(filePath),
            _ => new ConfigurationData()
        };
    }
}
```

### 5.5 架构模式检测器 (ArchitecturePatternDetector)

#### 5.5.1 架构模式规则
```csharp
public class ArchitecturePatternRules
{
    public static List<PatternRule> Rules = new()
    {
        new PatternRule
        {
            Pattern = "Clean Architecture",
            Indicators = new[]
            {
                "存在Application、Domain、Infrastructure层",
                "依赖方向从外向内",
                "Domain层无外部依赖"
            },
            DirectoryPatterns = new[] { "*Application*", "*Domain*", "*Infrastructure*" },
            FilePatterns = new[] { "*UseCase*", "*Entity*", "*Repository*" },
            Confidence = 0.9
        },
        
        new PatternRule
        {
            Pattern = "MVC",
            Indicators = new[]
            {
                "存在Controllers、Models、Views目录",
                "Controller继承自ControllerBase",
                "存在Action方法"
            },
            DirectoryPatterns = new[] { "Controllers", "Models", "Views" },
            FilePatterns = new[] { "*Controller.cs", "*Model.cs" },
            Confidence = 0.8
        }
    };
}
```

#### 5.5.2 检测算法
```csharp
public class ArchitecturePatternDetector
{
    public ArchitectureDetectionResult DetectPattern(StructuredProjectData projectData)
    {
        var results = new List<PatternMatch>();
        
        foreach (var rule in ArchitecturePatternRules.Rules)
        {
            var match = EvaluatePattern(rule, projectData);
            if (match.Confidence > 0.5)
            {
                results.Add(match);
            }
        }
        
        return new ArchitectureDetectionResult
        {
            PrimaryPattern = results.OrderByDescending(r => r.Confidence).FirstOrDefault(),
            AllMatches = results,
            Evidence = CollectEvidence(projectData)
        };
    }
    
    private PatternMatch EvaluatePattern(PatternRule rule, StructuredProjectData data)
    {
        double score = 0;
        var evidence = new List<string>();
        
        // 评估目录结构
        score += EvaluateDirectoryStructure(rule.DirectoryPatterns, data.Structure);
        
        // 评估文件模式
        score += EvaluateFilePatterns(rule.FilePatterns, data.SourceFiles);
        
        // 评估依赖关系
        score += EvaluateDependencies(rule, data.Dependencies);
        
        return new PatternMatch
        {
            PatternName = rule.Pattern,
            Confidence = Math.Min(score / rule.MaxScore, 1.0),
            Evidence = evidence
        };
    }
}
```

### 5.6 依赖图构建器 (DependencyGraphBuilder)

#### 5.6.1 依赖关系建模
```csharp
public class DependencyGraph
{
    public List<DependencyNode> Nodes { get; set; } = new();
    public List<DependencyEdge> Edges { get; set; } = new();
    public DependencyMetrics Metrics { get; set; }
}

public class DependencyNode
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public DependencyType Type { get; set; }
    public string Source { get; set; } // NuGet, NPM, etc.
    public List<string> Licenses { get; set; }
    public SecurityInfo Security { get; set; }
}

public class DependencyEdge
{
    public string From { get; set; }
    public string To { get; set; }
    public DependencyRelation Relation { get; set; }
    public string VersionConstraint { get; set; }
}
```

#### 5.6.2 构建算法
```csharp
public class DependencyGraphBuilder
{
    public DependencyGraph BuildGraph(StructuredProjectData projectData)
    {
        var graph = new DependencyGraph();
        
        // 添加直接依赖
        AddDirectDependencies(graph, projectData);
        
        // 解析传递依赖
        ResolveTransitiveDependencies(graph);
        
        // 检测循环依赖
        DetectCircularDependencies(graph);
        
        // 计算依赖度量
        CalculateMetrics(graph);
        
        return graph;
    }
    
    private void AddDirectDependencies(DependencyGraph graph, StructuredProjectData data)
    {
        // C# NuGet packages
        foreach (var package in data.GetNuGetPackages())
        {
            graph.Nodes.Add(new DependencyNode
            {
                Id = $"nuget:{package.Name}",
                Name = package.Name,
                Version = package.Version,
                Type = DependencyType.NuGet,
                Source = "NuGet Gallery"
            });
        }
        
        // Node.js NPM packages
        foreach (var package in data.GetNpmPackages())
        {
            graph.Nodes.Add(new DependencyNode
            {
                Id = $"npm:{package.Name}",
                Name = package.Name,
                Version = package.Version,
                Type = DependencyType.NPM,
                Source = "NPM Registry"
            });
        }
    }
}
```

## 6. 用户界面设计

### 6.1 进度展示界面
```
┌─────────────────────────────────────────────────┐
│ 结构化数据提取                                    │
├─────────────────────────────────────────────────┤
│ 当前阶段: 配置文件分析                           │
│ 总体进度: ████████░░ 75%                        │
├─────────────────────────────────────────────────┤
│ 提取详情:                                        │
│ ✓ 项目元数据        (2/2 项目)                   │
│ ✓ 依赖关系分析      (45个依赖包)                 │
│ ⚠ 配置文件解析      (3/4 完成)                   │
│ ○ 架构模式检测      (等待中...)                  │
│ ○ 代码结构分析      (等待中...)                  │
├─────────────────────────────────────────────────┤
│ 发现的关键信息:                                  │
│ • 服务端口: 5000, 5001                          │
│ • 数据库: SQL Server, Redis                     │
│ • 框架: ASP.NET Core 6.0                        │
│ • 主要依赖: Entity Framework, Serilog          │
└─────────────────────────────────────────────────┘
```

### 6.2 结果预览界面
```csharp
public class ExtractionResultViewModel : ViewModelBase
{
    public ObservableCollection<ProjectSummary> Projects { get; set; }
    public DependencyGraphViewModel DependencyGraph { get; set; }
    public ConfigurationSummary Configuration { get; set; }
    public ArchitectureInfo Architecture { get; set; }
    
    // 交互命令
    public ICommand ViewDetailsCommand { get; set; }
    public ICommand ExportDataCommand { get; set; }
    public ICommand RefreshCommand { get; set; }
}
```

## 7. 性能优化策略

### 7.1 并行处理设计
```csharp
public class ParallelExtractionEngine
{
    public async Task<StructuredProjectData> ExtractAsync(
        List<ProjectNode> projects, 
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task<ProjectExtraction>>();
        
        // 并行处理不同项目
        foreach (var project in projects)
        {
            tasks.Add(ExtractProjectDataAsync(project, cancellationToken));
        }
        
        var results = await Task.WhenAll(tasks);
        
        return MergeResults(results);
    }
    
    private async Task<ProjectExtraction> ExtractProjectDataAsync(
        ProjectNode project, 
        CancellationToken cancellationToken)
    {
        var extractionTasks = new[]
        {
            ExtractMetadataAsync(project),
            ExtractConfigurationAsync(project),
            ExtractDependenciesAsync(project),
            ExtractCodeStructureAsync(project)
        };
        
        await Task.WhenAll(extractionTasks);
        
        return new ProjectExtraction
        {
            Project = project,
            Metadata = extractionTasks[0].Result,
            Configuration = extractionTasks[1].Result,
            Dependencies = extractionTasks[2].Result,
            CodeStructure = extractionTasks[3].Result
        };
    }
}
```

### 7.2 内存优化
```csharp
public class MemoryEfficientExtractor
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConcurrency;
    
    public MemoryEfficientExtractor(int maxConcurrency = 4)
    {
        _maxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency);
    }
    
    public async Task<T> ProcessFileAsync<T>(string filePath, Func<Stream, T> processor)
    {
        await _semaphore.WaitAsync();
        
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return processor(stream);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 7.3 缓存策略
```csharp
public class ExtractionCache
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
    
    public T GetOrAdd<T>(string key, Func<T> factory)
    {
        if (_cache.TryGetValue(key, out var entry) && 
            entry.Timestamp > DateTime.UtcNow - _cacheExpiry)
        {
            return (T)entry.Value;
        }
        
        var value = factory();
        _cache[key] = new CacheEntry
        {
            Value = value,
            Timestamp = DateTime.UtcNow
        };
        
        return value;
    }
}
```

## 8. 错误处理与监控

### 8.1 异常处理策略
```csharp
public class ExtractionErrorHandler
{
    public async Task<ExtractionResult<T>> SafeExtractAsync<T>(
        Func<Task<T>> extraction, 
        string context)
    {
        try
        {
            var result = await extraction();
            return ExtractionResult<T>.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ExtractionResult<T>.Failed($"权限不足: {context}", ex);
        }
        catch (FileNotFoundException ex)
        {
            return ExtractionResult<T>.Failed($"文件未找到: {context}", ex);
        }
        catch (JsonException ex)
        {
            return ExtractionResult<T>.Failed($"JSON格式错误: {context}", ex);
        }
        catch (XmlException ex)
        {
            return ExtractionResult<T>.Failed($"XML格式错误: {context}", ex);
        }
        catch (Exception ex)
        {
            return ExtractionResult<T>.Failed($"未知错误: {context}", ex);
        }
    }
}
```

