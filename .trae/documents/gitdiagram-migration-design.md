# GitDiagram 功能迁移到 ProjectIgnite 设计方案

## 1. 项目概述

本文档详细设计了将 GitDiagram 项目的核心功能迁移到 ProjectIgnite Avalonia 应用程序的完整方案。GitDiagram 是一个将 GitHub 仓库转换为交互式架构图表的工具，我们将其核心能力集成到 ProjectIgnite 中，为用户提供项目可视化分析功能。

## 2. GitDiagram 核心工作流程分析

### 2.1 现有工作流程

**阶段一：GitHub 仓库数据获取**

* GitHub API 认证（PAT/GitHub App）

* 获取仓库元数据（分支、README）

* 递归遍历文件树结构

* 智能过滤无关文件（node\_modules、缓存、编译文件等）

**阶段二：AI 驱动的三阶段分析**

1. **架构解释生成**：基于文件树和 README 生成项目架构说明
2. **组件映射**：将抽象组件映射到具体文件/目录
3. **图表代码生成**：生成 Mermaid.js 格式的图表代码

**阶段三：交互式图表展示**

* Mermaid.js 图表渲染

* 节点点击跳转到 GitHub 源码

* 缩放、平移交互功能

* 图表修改和导出功能

### 2.2 迁移可行性评估

| 功能模块          | 迁移难度 | 可行性  | 备注                  |
| ------------- | ---- | ---- | ------------------- |
| GitHub API 集成 | 低    | ✅ 高  | 使用 Octokit.NET      |
| AI 服务调用       | 中    | ✅ 高  | 使用 OpenAI .NET SDK  |
| 图表渲染          | 高    | ⚠️ 中 | 需要 WebView 或自定义渲染   |
| 文件树分析         | 低    | ✅ 高  | 纯 .NET 实现           |
| 流式响应处理        | 中    | ✅ 高  | 使用 HttpClient + SSE |
| 交互功能          | 中    | ⚠️ 中 | 依赖图表渲染方案            |

## 3. 第三方包评估

### 3.1 核心依赖包

**GitHub API 客户端**

* **推荐**：`Octokit` (官方 GitHub .NET SDK)

* **活跃度**：⭐⭐⭐⭐⭐ (GitHub 官方维护)

* **兼容性**：✅ 完全兼容 .NET 9

* **功能**：完整的 GitHub API 支持

**AI 服务客户端**

* **推荐**：`Microsoft.Extensions.AI` (微软官方 AI 抽象层)

* **活跃度**：⭐⭐⭐⭐⭐ (微软官方维护)

* **兼容性**：✅ 完全兼容 .NET 9

* **功能**：原生支持流式响应、多 AI 提供商抽象、统一接口

**HTTP 客户端增强**

* **推荐**：`System.Net.Http` (内置) + `Microsoft.Extensions.Http`

* **活跃度**：⭐⭐⭐⭐⭐ (微软官方)

* **兼容性**：✅ 原生支持

* **功能**：HTTP 客户端工厂、重试策略

### 3.2 图表渲染方案评估

**方案一：Avalonia WebView + Mermaid.js**

* **包**：`Avalonia.WebView2` (Windows) / `Avalonia.WebView` (跨平台)

* **优势**：保持原有 Mermaid.js 功能，交互性强

* **劣势**：依赖 WebView，增加复杂性

* **兼容性**：⚠️ 需要系统 WebView 支持

<br />

### 3.3 辅助功能包

**JSON 处理**

* **推荐**：`System.Text.Json` (内置)

* **备选**：`Newtonsoft.Json`

**配置管理**

* **推荐**：`Microsoft.Extensions.Configuration`

**日志记录**

* **推荐**：`Microsoft.Extensions.Logging`

## 4. MVVM 架构设计

### 4.1 整体架构

**注意：GitDiagram 功能将集成到现有的 ProjectStructureView 页面中，而不是创建新的独立页面。**

```
┌─────────────────────────┐    ┌─────────────────────────┐    ┌─────────────────┐
│         View            │    │       ViewModel         │    │    Service      │
│                         │    │                         │    │                 │
│ ProjectStructureView    │◄──►│ ProjectStructureViewModel│◄──►│ GitHubService   │
│ (集成 GitDiagram 功能)    │    │ (扩展图表生成功能)          │    │ OpenAIService   │
│ - 项目结构树              │    │ - 原有项目管理功能          │    │ DiagramService  │
│ - 图表显示区域             │    │ - 新增图表生成功能          │    │                 │
│ - 图表控制面板             │    │ - 进度状态管理             │    │                 │
└─────────────────────────┘    └─────────────────────────┘    └─────────────────┘
                                         ▲                            ▲
                                         │                            │
                                ┌─────────────────┐         ┌─────────────────┐
                                │     Model       │         │   Repository    │
                                │                 │         │                 │
                                │ DiagramModel    │         │ ConfigRepo      │
                                │ RepositoryInfo  │         │ CacheRepo       │
                                │ GenerationState │         │                 │
                                │ ProjectSource   │         │                 │
                                └─────────────────┘         └─────────────────┘
```

### 4.2 核心 ViewModel 设计

**ProjectStructureViewModel（扩展版）**

````csharp
public class ProjectStructureViewModel : ViewModelBase
{
    // 原有项目结构功能
    public ObservableCollection<ProjectSource> Projects { get; set; }
    public ProjectSource? SelectedProject { get; set; }
    
    // 新增图表生成功能
    public string RepositoryUrl { get; set; }
    public string DiagramContent { get; set; }
    public GenerationState DiagramState { get; set; }
    public string ErrorMessage { get; set; }
    public double Progress { get; set; }
    public bool IsDiagramVisible { get; set; }
    
    // 原有命令
    public ICommand RefreshProjectsCommand { get; }
    public ICommand SelectProjectCommand { get; }
    
    // 新增图表相关命令
    public ICommand GenerateDiagramCommand { get; }
    public ICommand ModifyDiagramCommand { get; }
    public ICommand ExportDiagramCommand { get; }
    public ICommand CopyDiagramCommand { get; }
    public ICommand ToggleDiagramViewCommand { get; }
    
    // 服务依赖
    private readonly IProjectRepository _projectRepository;
    private readonly IDiagramService _diagramService;
    private readonly IConfigurationService _configService;
    private readonly IAIService _aiService;
}```

**ProgressViewModel**

```csharp
public class ProgressViewModel : ViewModelBase
{
    public string CurrentPhase { get; set; }
    public string PhaseDescription { get; set; }
    public double PhaseProgress { get; set; }
    public ObservableCollection<string> LogMessages { get; set; }
    public bool IsStreaming { get; set; }
}
````

### 4.3 Service 层设计

**IDiagramService**

```csharp
public interface IDiagramService
{
    Task<DiagramResult> GenerateDiagramAsync(
        string repositoryUrl, 
        string? customInstructions = null,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);
        
    Task<string> ModifyDiagramAsync(
        string currentDiagram, 
        string instructions,
        CancellationToken cancellationToken = default);
        
    Task<byte[]> ExportDiagramAsync(
        string diagramContent, 
        ExportFormat format,
        CancellationToken cancellationToken = default);
}
```

**IGitHubService**

```csharp
public interface IGitHubService
{
    Task<RepositoryInfo> GetRepositoryInfoAsync(string repositoryUrl);
    Task<FileTreeNode> GetFileTreeAsync(string owner, string repo, string? branch = null);
    Task<string> GetReadmeContentAsync(string owner, string repo);
}
```

**IAIService**

```csharp
public interface IAIService
{
    IAsyncEnumerable<StreamingChatCompletionUpdate> GenerateStreamingAsync(
        string systemPrompt,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default);
        
    Task<ChatCompletion> GenerateAsync(
        string systemPrompt,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default);
}
```

### 4.4 Model 设计

**核心数据模型**

```csharp
public class DiagramModel
{
    public string Id { get; set; }
    public string RepositoryUrl { get; set; }
    public string MermaidCode { get; set; }
    public string Explanation { get; set; }
    public Dictionary<string, string> ComponentMapping { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CustomInstructions { get; set; }
}

public class RepositoryInfo
{
    public string Owner { get; set; }
    public string Name { get; set; }
    public string DefaultBranch { get; set; }
    public string Description { get; set; }
    public string ReadmeContent { get; set; }
    public FileTreeNode FileTree { get; set; }
}

public class GenerationState
{
    public GenerationPhase Phase { get; set; }
    public string CurrentMessage { get; set; }
    public double Progress { get; set; }
    public bool IsCompleted { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum GenerationPhase
{
    Idle,
    FetchingRepository,
    GeneratingExplanation,
    MappingComponents,
    GeneratingDiagram,
    Completed,
    Error
}
```

## 5. 用户界面设计方案

### 5.1 ProjectStructureView 集成布局

**在现有 ProjectStructureView\.axaml 中集成图表功能：**

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300" />    <!-- 项目列表区域 -->
        <ColumnDefinition Width="5" />      <!-- 分隔符 -->
        <ColumnDefinition Width="*" />      <!-- 主内容区域 -->
    </Grid.ColumnDefinitions>
    
    <!-- 左侧：项目列表（原有功能） -->
    <Border Grid.Column="0" BorderBrush="Gray" BorderThickness="0,0,1,0">
        <StackPanel Margin="16">
            <TextBlock Text="项目列表" FontWeight="Bold" Margin="0,0,0,16" />
            <ListBox ItemsSource="{Binding Projects}" 
                     SelectedItem="{Binding SelectedProject}" />
            <Button Command="{Binding RefreshProjectsCommand}" 
                    Content="刷新项目" Margin="0,16,0,0" />
        </StackPanel>
    </Border>
    
    <!-- 分隔符 -->
    <GridSplitter Grid.Column="1" Background="LightGray" />
    
    <!-- 右侧：主内容区域（集成图表功能） -->
    <Grid Grid.Column="2">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 工具栏 -->
            <RowDefinition Height="Auto" />  <!-- 图表输入区域 -->
            <RowDefinition Height="*" />     <!-- 内容显示区域 -->
            <RowDefinition Height="Auto" />  <!-- 控制区域 -->
        </Grid.RowDefinitions>
        
        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="16,16,16,8">
            <Button Content="项目结构" 
                    Command="{Binding ShowProjectStructureCommand}" />
            <Button Content="架构图表" 
                    Command="{Binding ShowDiagramViewCommand}" 
                    Margin="8,0,0,0" />
        </StackPanel>
        
        <!-- 图表输入区域（仅在图表模式显示） -->
        <StackPanel Grid.Row="1" Margin="16,8" 
                    IsVisible="{Binding IsDiagramMode}">
            <TextBox Text="{Binding RepositoryUrl}" 
                     Watermark="输入 GitHub 仓库 URL 或选择左侧项目" />
            <TextBox Text="{Binding CustomInstructions}" 
                     Watermark="自定义指令（可选）" 
                     Margin="0,8,0,0" />
            <Button Command="{Binding GenerateDiagramCommand}" 
                    Content="生成架构图表" 
                    Margin="0,8,0,0" />
        </StackPanel>
        
        <!-- 内容显示区域 -->
        <Border Grid.Row="2" BorderBrush="Gray" BorderThickness="1" Margin="16,8">
            <!-- 项目结构视图（原有功能） -->
            <ScrollViewer IsVisible="{Binding IsProjectStructureMode}">
                <TextBlock Text="{Binding SelectedProject.Description}" 
                           Margin="16" TextWrapping="Wrap" />
            </ScrollViewer>
            
            <!-- 图表显示区域（新增功能） -->
            <Grid IsVisible="{Binding IsDiagramMode}">
                <WebView Name="DiagramWebView" 
                         IsVisible="{Binding HasDiagram}" />
                <StackPanel IsVisible="{Binding IsGenerating}" 
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center">
                    <ProgressBar Value="{Binding Progress}" 
                                Width="300" Height="20" />
                    <TextBlock Text="{Binding DiagramState.CurrentMessage}" 
                              Margin="0,8,0,0" 
                              HorizontalAlignment="Center" />
                </StackPanel>
                <TextBlock Text="选择项目或输入仓库 URL 开始生成架构图表" 
                          IsVisible="{Binding !HasDiagram}" 
                          HorizontalAlignment="Center" 
                          VerticalAlignment="Center" 
                          Foreground="Gray" />
            </Grid>
        </Border>
        
        <!-- 控制区域（仅在图表模式显示） -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="16,8,16,16" 
                    IsVisible="{Binding IsDiagramMode}">
            <Button Command="{Binding ModifyDiagramCommand}" 
                    Content="修改图表" 
                    IsEnabled="{Binding HasDiagram}" />
            <Button Command="{Binding ExportDiagramCommand}" 
                    Content="导出图表" 
                    Margin="8,0,0,0" 
                    IsEnabled="{Binding HasDiagram}" />
            <Button Command="{Binding CopyDiagramCommand}" 
                    Content="复制代码" 
                    Margin="8,0,0,0" 
                    IsEnabled="{Binding HasDiagram}" />
            <ToggleButton IsChecked="{Binding ZoomEnabled}" 
                          Content="缩放模式" 
                          Margin="8,0,0,0" 
                          IsEnabled="{Binding HasDiagram}" />
        </StackPanel>
    </Grid>
</Grid>
```

### 5.2 进度反馈设计

**流式进度显示**

* 实时显示当前处理阶段

* 显示详细的处理日志

* 进度条显示整体完成度

* 支持取消操作

**状态指示器**

* 不同阶段使用不同颜色

* 动画效果增强用户体验

* 错误状态明确提示

### 5.3 交互体验设计

**图表交互**

* 节点点击：在新窗口打开 GitHub 文件

* 缩放控制：鼠标滚轮 + 工具栏按钮

* 平移操作：鼠标拖拽

* 全屏模式：F11 或工具栏按钮

**快捷操作**

* Ctrl+C：复制图表代码

* Ctrl+S：保存图表

* Ctrl+E：导出图片

* Esc：退出全屏

## 6. 技术实现路径

### 6.1 实现阶段

**阶段一：基础架构搭建（1-2天）**

1. 创建 Service 接口和基础实现
2. 设计 Model 类和数据结构
3. 扩展现有 ProjectStructureViewModel 以支持图表功能
4. 配置依赖注入容器

**阶段二：GitHub 集成（2-3天）**

1. 集成 Octokit.NET
2. 实现仓库信息获取
3. 实现文件树遍历和过滤
4. 添加认证和错误处理

**阶段三：AI 服务集成（2-3天）**

1. 集成 Microsoft.Extensions.AI
2. 配置 AI 服务提供商（OpenAI/Azure OpenAI）
3. 实现流式响应处理（原生支持）
4. 移植提示词模板
5. 实现三阶段生成流程

**阶段四：图表渲染（3-4天）**

1. 集成 WebView 组件
2. 实现 Mermaid.js 渲染
3. 添加交互功能
4. 实现缩放和导出

**阶段五：UI 集成完善（2-3天）**

1. 修改现有 ProjectStructureView\.axaml 集成图表功能
2. 更新 ProjectStructureView\.axaml.cs 支持新功能
3. 添加模式切换和进度反馈
4. 实现配置管理和错误处理

**阶段六：测试和优化（2-3天）**

1. 单元测试
2. 集成测试
3. 性能优化
4. 用户体验优化

### 6.2 关键技术点

**WebView 集成**

```csharp
// WebView 初始化
public void InitializeWebView()
{
    var html = GenerateMermaidHtml(diagramCode);
    DiagramWebView.NavigateToString(html);
}

// JavaScript 交互
public async Task<string> ExecuteScriptAsync(string script)
{
    return await DiagramWebView.ExecuteScriptAsync(script);
}
```

**流式响应处理（Microsoft.Extensions.AI 原生支持）**

```csharp
public async IAsyncEnumerable<string> ProcessStreamAsync(
    IChatClient chatClient,
    ChatMessage[] messages,
    CancellationToken cancellationToken = default)
{
    await foreach (var update in chatClient.CompleteStreamingAsync(
        messages, cancellationToken: cancellationToken))
    {
        if (update.Text != null)
        {
            yield return update.Text;
        }
    }
}
```

## 7. 风险评估与缓解策略

### 7.1 技术风险

| 风险            | 影响 | 概率 | 缓解策略              |
| ------------- | -- | -- | ----------------- |
| WebView 兼容性问题 | 高  | 中  | 提供 SkiaSharp 备选方案 |
| AI API 限流     | 中  | 高  | 实现重试机制和用户提示       |
| 大仓库性能问题       | 中  | 中  | 文件数量限制和分页处理       |
| 网络连接问题        | 低  | 高  | 离线缓存和错误恢复         |

### 7.2 用户体验风险

| 风险     | 影响 | 概率 | 缓解策略        |
| ------ | -- | -- | ----------- |
| 生成时间过长 | 中  | 中  | 流式反馈和进度显示   |
| 图表质量不佳 | 高  | 低  | 提供修改和重新生成功能 |
| 配置复杂   | 低  | 低  | 提供默认配置和向导   |

### 7.3 成本风险

| 风险            | 影响 | 概率 | 缓解策略      |
| ------------- | -- | -- | --------- |
| AI API 成本过高   | 中  | 中  | 成本预估和用户配额 |
| GitHub API 限制 | 低  | 中  | 认证和缓存策略   |

## 8. 配置和部署

### 8.1 配置项

```json
{
  "GitHub": {
    "PersonalAccessToken": "",
    "ApiBaseUrl": "https://api.github.com"
  },
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "",
      "BaseUrl": "https://api.openai.com/v1",
      "Model": "gpt-4o-mini",
      "MaxTokens": 4000
    },
    "AzureOpenAI": {
      "Endpoint": "",
      "ApiKey": "",
      "DeploymentName": ""
    }
  },
  "Diagram": {
    "MaxFileCount": 1000,
    "CacheEnabled": true,
    "CacheDurationHours": 24
  }
}
```

### 8.2 依赖包清单

```xml
<PackageReference Include="Octokit" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.AI" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.0.0" />
<PackageReference Include="Avalonia.WebView2" Version="11.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
```

## 9. 总结

本迁移方案将 GitDiagram 的核心功能完整地集成到 ProjectIgnite 中，通过 MVVM 架构确保代码的可维护性和可测试性。主要优势：

1. **功能完整性**：保留原有的所有核心功能
2. **架构清晰**：严格遵循 MVVM 和 Clean Architecture 原则
3. **技术可靠**：使用成熟稳定的第三方库
4. **用户体验**：提供流畅的交互和及时的反馈
5. **可扩展性**：为未来功能扩展预留接口

通过分阶段实施，可以有效控制开发风险，确保项目按时交付。
