# Git克隆功能UX设计

## 交互流程设计

### 1. 智能化的克隆对话框

```xml
<!-- GitCloneDialog.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        Title="克隆Git仓库" 
        Width="600" Height="500"
        WindowStartupLocation="CenterOwner"
        Classes="modern-dialog">
  
  <Grid Margin="24">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>   <!-- 仓库URL输入 -->
      <RowDefinition Height="Auto"/>   <!-- 智能建议区域 -->
      <RowDefinition Height="Auto"/>   <!-- 目标配置 -->
      <RowDefinition Height="Auto"/>   <!-- 高级选项 -->
      <RowDefinition Height="*"/>      <!-- 进度区域 -->
      <RowDefinition Height="Auto"/>   <!-- 按钮区域 -->
    </Grid.RowDefinitions>

    <!-- 1. 仓库URL输入区 -->
    <StackPanel Grid.Row="0" Margin="0,0,0,20">
      <TextBlock Text="Git仓库地址" 
                 Classes="section-header"/>
      <Border Classes="url-input-container" Margin="0,8,0,0">
        <Grid>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
          </Grid.ColumnDefinitions>
          
          <TextBox Grid.Column="0"
                   Text="{Binding RepositoryUrl, UpdateSourceTrigger=PropertyChanged}"
                   Watermark="https://github.com/user/repo.git"
                   Classes="url-input"/>
          
          <!-- URL验证指示器 -->
          <Border Grid.Column="1" 
                  Classes="validation-indicator"
                  Background="{Binding UrlValidationStatus, Converter={StaticResource StatusToColorConverter}}"
                  IsVisible="{Binding ShowValidationStatus}">
            <PathIcon Data="{Binding UrlValidationStatus, Converter={StaticResource StatusToIconConverter}}" 
                      Width="12"/>
          </Border>
        </Grid>
      </Border>
      
      <!-- URL解析信息显示 -->
      <StackPanel IsVisible="{Binding HasUrlInfo}" 
                  Margin="0,8,0,0">
        <Border Classes="url-info-panel">
          <Grid>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- 仓库信息 -->
            <StackPanel Grid.Column="0" Margin="0,0,16,0">
              <TextBlock Text="仓库信息" Classes="info-label"/>
              <TextBlock Text="{Binding ParsedRepoInfo.Owner}" FontWeight="SemiBold"/>
              <TextBlock Text="{Binding ParsedRepoInfo.RepoName}" FontSize="14"/>
            </StackPanel>
            
            <!-- 检测到的项目类型 -->
            <StackPanel Grid.Column="1" IsVisible="{Binding HasDetectedType}">
              <TextBlock Text="检测到的项目类型" Classes="info-label"/>
              <Border Classes="type-badge"
                      Background="{Binding DetectedProjectType, Converter={StaticResource TypeToColorConverter}}">
                <StackPanel Orientation="Horizontal">
                  <PathIcon Data="{Binding DetectedProjectType, Converter={StaticResource TypeToIconConverter}}" 
                            Width="14"/>
                  <TextBlock Text="{Binding DetectedProjectType}" 
                             Margin="6,0,0,0" Foreground="White"/>
                </StackPanel>
              </Border>
            </StackPanel>
          </Grid>
        </Border>
      </StackPanel>
    </StackPanel>

    <!-- 2. 智能建议区域 -->
    <StackPanel Grid.Row="1" 
                Margin="0,0,0,20"
                IsVisible="{Binding HasSuggestions}">
      <TextBlock Text="智能建议" Classes="section-header"/>
      <Border Classes="suggestions-panel" Margin="0,8,0,0">
        <ItemsControl ItemsSource="{Binding Suggestions}">
          <ItemsControl.ItemTemplate>
            <DataTemplate>
              <Border Classes="suggestion-item">
                <Grid>
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                  </Grid.ColumnDefinitions>
                  
                  <PathIcon Grid.Column="0" 
                            Data="{Binding Type, Converter={StaticResource SuggestionTypeToIconConverter}}" 
                            Width="16"
                            Foreground="{DynamicResource SystemAccentColor}"/>
                  
                  <StackPanel Grid.Column="1" Margin="12,0">
                    <TextBlock Text="{Binding Title}" FontWeight="Medium"/>
                    <TextBlock Text="{Binding Description}" 
                               Classes="suggestion-description"/>
                  </StackPanel>
                  
                  <Button Grid.Column="2" 
                          Classes="suggestion-action"
                          Content="{Binding ActionText}"
                          Command="{Binding $parent[Window].DataContext.ApplySuggestionCommand}"
                          CommandParameter="{Binding}"/>
                </Grid>
              </Border>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </Border>
    </StackPanel>

    <!-- 3. 目标配置区域 -->
    <StackPanel Grid.Row="2" Margin="0,0,0,20">
      <TextBlock Text="目标配置" Classes="section-header"/>
      
      <Grid Margin="0,8,0,0">
        <Grid.RowDefinitions>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
          <ColumnDefinition Width="100"/>
          <ColumnDefinition Width="*"/>
          <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- 工作区选择 -->
        <TextBlock Grid.Row="0" Grid.Column="0" 
                   Text="工作区:" 
                   VerticalAlignment="Center"/>
        <ComboBox Grid.Row="0" Grid.Column="1"
                  ItemsSource="{Binding AvailableWorkspaces}"
                  SelectedItem="{Binding SelectedWorkspace}"
                  DisplayMemberPath="Name"
                  Classes="modern-combo"/>
        <Button Grid.Row="0" Grid.Column="2"
                Classes="icon-button"
                Command="{Binding CreateWorkspaceCommand}"
                ToolTip.Tip="新建工作区"
                Margin="8,0,0,0">
          <PathIcon Data="{StaticResource AddIcon}" Width="14"/>
        </Button>

        <!-- 项目名称 -->
        <TextBlock Grid.Row="1" Grid.Column="0" 
                   Text="项目名:" 
                   VerticalAlignment="Center"
                   Margin="0,8,0,0"/>
        <TextBox Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="2"
                 Text="{Binding ProjectName}"
                 Classes="modern-textbox"
                 Margin="0,8,0,0"/>

        <!-- 完整路径预览 -->
        <TextBlock Grid.Row="2" Grid.Column="0" 
                   Text="路径:" 
                   VerticalAlignment="Center"
                   Margin="0,8,0,0"/>
        <Border Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2"
                Classes="path-preview"
                Margin="0,8,0,0">
          <TextBlock Text="{Binding FullTargetPath}" 
                     Classes="path-text"
                     TextTrimming="CharacterEllipsis"
                     ToolTip.Tip="{Binding FullTargetPath}"/>
        </Border>
      </Grid>
    </StackPanel>

    <!-- 4. 高级选项（可折叠） -->
    <Expander Grid.Row="3" 
              Header="高级选项" 
              Margin="0,0,0,20"
              Classes="modern-expander">
      <StackPanel Margin="0,12,0,0">
        
        <!-- 分支选择 -->
        <Grid Margin="0,0,0,12">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="100"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
          </Grid.ColumnDefinitions>
          
          <TextBlock Grid.Column="0" 
                     Text="分支:" 
                     VerticalAlignment="Center"/>
          <ComboBox Grid.Column="1"
                    ItemsSource="{Binding AvailableBranches}"
                    SelectedItem="{Binding SelectedBranch}"
                    IsEnabled="{Binding CanSelectBranch}"
                    Classes="modern-combo"/>
          <Button Grid.Column="2"
                  Classes="icon-button"
                  Command="{Binding RefreshBranchesCommand}"
                  ToolTip.Tip="刷新分支列表"
                  Margin="8,0,0,0">
            <PathIcon Data="{StaticResource RefreshIcon}" Width="12"/>
          </Button>
        </Grid>

        <!-- 克隆选项 -->
        <StackPanel>
          <CheckBox Content="浅克隆（仅最新提交）" 
                    IsChecked="{Binding UseShallowClone}"/>
          <CheckBox Content="克隆后自动安装依赖" 
                    IsChecked="{Binding AutoInstallDependencies}"
                    Margin="0,8,0,0"/>
          <CheckBox Content="克隆后立即启动项目" 
                    IsChecked="{Binding AutoStartAfterClone}"
                    Margin="0,8,0,0"/>
        </StackPanel>

        <!-- 认证信息 -->
        <StackPanel Margin="0,16,0,0">
          <TextBlock Text="认证信息（如需要）" Classes="subsection-header"/>
          <Grid Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <TextBox Grid.Column="0"
                     Text="{Binding Username}"
                     Watermark="用户名"
                     Margin="0,0,8,0"/>
            <TextBox Grid.Column="1"
                     Text="{Binding Password}"
                     PasswordChar="●"
                     Watermark="密码/Token"/>
          </Grid>
        </StackPanel>
      </StackPanel>
    </Expander>

    <!-- 5. 进度显示区域 -->
    <Border Grid.Row="4" 
            Classes="progress-panel"
            IsVisible="{Binding IsCloning}">
      <StackPanel>
        <!-- 总体进度 -->
        <ProgressBar Value="{Binding CloneProgress.ProgressPercentage}" 
                     Classes="clone-progress"
                     Height="8"/>
        
        <!-- 详细进度信息 -->
        <Grid Margin="0,12,0,0">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
          </Grid.ColumnDefinitions>
          
          <StackPanel Grid.Column="0">
            <TextBlock Text="{Binding CloneProgress.CurrentOperation}" 
                       Classes="progress-operation"/>
            <TextBlock Classes="progress-details">
              <Run Text="对象:"/>
              <Run Text="{Binding CloneProgress.ReceivedObjects}" FontWeight="Medium"/>
              <Run Text="/"/>
              <Run Text="{Binding CloneProgress.TotalObjects}" FontWeight="Medium"/>
              <Run Text="  大小:"/>
              <Run Text="{Binding CloneProgress.ReceivedBytes, Converter={StaticResource BytesToSizeConverter}}" FontWeight="Medium"/>
            </TextBlock>
          </StackPanel>
          
          <TextBlock Grid.Column="1" 
                     Text="{Binding CloneProgress.ProgressPercentage, StringFormat='{}{0:F1}%'}"
                     Classes="progress-percentage"
                     VerticalAlignment="Center"/>
        </Grid>

        <!-- 实时日志 -->
        <ScrollViewer Height="80" 
                      Margin="0,12,0,0"
                      Classes="log-viewer">
          <TextBlock Text="{Binding CloneLog}" 
                     Classes="log-text"
                     TextWrapping="Wrap"/>
        </ScrollViewer>
      </StackPanel>
    </Border>

    <!-- 6. 操作按钮 -->
    <StackPanel Grid.Row="5" 
                Orientation="Horizontal" 
                HorizontalAlignment="Right"
                Margin="0,20,0,0">
      
      <!-- 取消按钮 -->
      <Button Content="取消" 
              Command="{Binding CancelCommand}"
              Classes="secondary-button"
              Margin="0,0,12,0"
              IsVisible="{Binding CanCancel}"/>
      
      <!-- 主要操作按钮 -->
      <Button Classes="primary-button"
              Command="{Binding CloneCommand}"
              IsEnabled="{Binding CanClone}">
        <StackPanel Orientation="Horizontal">
          <PathIcon Data="{Binding IsCloning, Converter={StaticResource BoolToCloneIconConverter}}" 
                    Width="16"/>
          <TextBlock Text="{Binding IsCloning, Converter={StaticResource BoolToCloneTextConverter}}" 
                     Margin="8,0,0,0"/>
        </StackPanel>
      </Button>
    </StackPanel>
  </Grid>
</Window>
```

## 智能交互逻辑

### 1. URL输入时的实时反馈
```csharp
public class GitCloneViewModel : ViewModelBase
{
    private string _repositoryUrl = "";
    private ParsedRepositoryInfo _parsedRepoInfo;
    private List<CloneSuggestion> _suggestions = new();
    
    public string RepositoryUrl
    {
        get => _repositoryUrl;
        set
        {
            if (SetProperty(ref _repositoryUrl, value))
            {
                _ = ProcessUrlChangeAsync(value);
            }
        }
    }

    private async Task ProcessUrlChangeAsync(string url)
    {
        try
        {
            // 1. 实时URL验证
            UrlValidationStatus = await ValidateUrlAsync(url);
            
            if (UrlValidationStatus == ValidationStatus.Valid)
            {
                // 2. 解析仓库信息
                ParsedRepoInfo = ParseRepositoryUrl(url);
                
                // 3. 自动生成项目名称
                ProjectName = ParsedRepoInfo.RepoName;
                
                // 4. 构建目标路径
                UpdateTargetPath();
                
                // 5. 生成智能建议
                await GenerateSuggestionsAsync();
                
                // 6. 预检查仓库信息（可选）
                _ = PreCheckRepositoryAsync(url);
            }
        }
        catch (Exception ex)
        {
            // 处理错误
        }
    }

    private void UpdateTargetPath()
    {
        // 智能路径生成
        var workspace = SelectedWorkspace ?? GetDefaultWorkspace();
        FullTargetPath = Path.Combine(workspace.Path, ProjectName);
        
        // 检查路径冲突
        CheckPathConflict();
    }

    private async Task GenerateSuggestionsAsync()
    {
        var suggestions = new List<CloneSuggestion>();

        // 建议1: 路径优化
        if (Directory.Exists(FullTargetPath))
        {
            suggestions.Add(new CloneSuggestion
            {
                Type = SuggestionType.PathConflict,
                Title = "目标文件夹已存在",
                Description = $"建议使用: {ProjectName}-{DateTime.Now:MMdd}",
                ActionText = "应用",
                Action = () => ProjectName = $"{ProjectName}-{DateTime.Now:MMdd}"
            });
        }

        // 建议2: 工作区推荐
        var recommendedWorkspace = await GetRecommendedWorkspaceAsync();
        if (recommendedWorkspace != SelectedWorkspace)
        {
            suggestions.Add(new CloneSuggestion
            {
                Type = SuggestionType.WorkspaceRecommendation,
                Title = "推荐使用其他工作区",
                Description = $"基于项目类型，推荐使用'{recommendedWorkspace.Name}'工作区",
                ActionText = "切换",
                Action = () => SelectedWorkspace = recommendedWorkspace
            });
        }

        // 建议3: 分支选择
        if (ParsedRepoInfo.DefaultBranch != "main" && ParsedRepoInfo.DefaultBranch != "master")
        {
            suggestions.Add(new CloneSuggestion
            {
                Type = SuggestionType.BranchRecommendation,
                Title = "检测到非标准主分支",
                Description = $"该仓库使用 '{ParsedRepoInfo.DefaultBranch}' 作为主分支",
                ActionText = "确认",
                Action = () => SelectedBranch = ParsedRepoInfo.DefaultBranch
            });
        }

        Suggestions = suggestions;
    }
}
```

### 2. 工作区管理策略
```csharp
public class WorkspaceStrategy
{
    // 默认工作区策略
    public WorkspaceConfig GetDefaultWorkspaceStrategy()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        return new WorkspaceConfig
        {
            DefaultBasePath = Path.Combine(userProfile, "Projects"),
            WorkspaceByType = new Dictionary<ProjectType, string>
            {
                { ProjectType.DotNetWebApi, "dotnet-projects" },
                { ProjectType.ViteVue, "frontend-projects" },
                { ProjectType.ViteReact, "frontend-projects" },
                { ProjectType.Unknown, "misc-projects" }
            },
            CreateWorkspaceIfNotExists = true,
            AutoOrganizeByType = true
        };
    }

    public async Task<Workspace> GetRecommendedWorkspaceAsync(ProjectType projectType)
    {
        var config = GetDefaultWorkspaceStrategy();
        var workspaceName = config.WorkspaceByType.GetValueOrDefault(projectType, "default");
        
        var workspace = await _workspaceService.GetWorkspaceByNameAsync(workspaceName);
        if (workspace == null && config.CreateWorkspaceIfNotExists)
        {
            workspace = await _workspaceService.CreateWorkspaceAsync(workspaceName, config.DefaultBasePath);
        }
        
        return workspace ?? await _workspaceService.GetDefaultWorkspaceAsync();
    }
}
```

### 3. 智能路径生成
```csharp
public class SmartPathGenerator
{
    public string GenerateProjectPath(ParsedRepositoryInfo repoInfo, Workspace workspace)
    {
        var baseName = repoInfo.RepoName;
        var targetDir = workspace.Path;
        
        // 1. 基础路径
        var projectPath = Path.Combine(targetDir, baseName);
        
        // 2. 处理路径冲突
        if (Directory.Exists(projectPath))
        {
            // 策略A: 添加序号后缀
            var counter = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(targetDir, $"{baseName}-{counter}");
                counter++;
            } 
            while (Directory.Exists(newPath) && counter < 100);
            
            return newPath;
        }
        
        return projectPath;
    }

    public List<string> GenerateAlternativePaths(ParsedRepositoryInfo repoInfo, Workspace workspace)
    {
        var alternatives = new List<string>();
        var baseName = repoInfo.RepoName;
        var targetDir = workspace.Path;
        
        // 不同的命名策略
        alternatives.Add(Path.Combine(targetDir, baseName));
        alternatives.Add(Path.Combine(targetDir, $"{repoInfo.Owner}-{baseName}"));
        alternatives.Add(Path.Combine(targetDir, $"{baseName}-{DateTime.Now:yyyy-MM}"));
        
        return alternatives.Where(p => !Directory.Exists(p)).ToList();
    }
}
```

## 完整的UX流程

### 🎬 用户交互场景

#### 场景1: 简单快速克隆
```
1. 用户粘贴URL: https://github.com/microsoft/vscode.git
2. 自动解析显示: microsoft/vscode (TypeScript项目)
3. 智能建议工作区: frontend-projects
4. 自动生成路径: ~/Projects/frontend-projects/vscode
5. 用户点击"克隆并导入" - 一步完成
```

#### 场景2: 需要自定义的克隆
```
1. 用户粘贴URL: https://github.com/dotnet/aspnetcore.git  
2. 系统检测: 大型.NET项目 (500MB+)
3. 智能建议:
   - 使用浅克隆减少下载
   - 推荐放在SSD工作区
   - 建议克隆后不自动启动（大项目）
4. 用户调整配置后克隆
```

### 📱 移动端友好的紧凑版本
```csharp
// 可以考虑为小屏幕设计紧凑版对话框
public class CompactCloneDialog : UserControl
{
    // 垂直布局，更适合小屏幕
    // 使用抽屉式展开高级选项
    // 手势友好的操作方式
}
```

## 🚀 推荐的实现方案

### 最佳UX流程
```
URL输入 → 实时验证 → 智能解析 → 生成建议 → 用户确认 → 执行克隆 → 自动导入
     ↓
   自动填充工作区、项目名、路径等信息
     ↓  
   显示智能建议（路径冲突、工作区推荐等）
     ↓
   用户可以快速接受默认值，也可以自定义
```

### 用户配置保存
```csharp
public class ClonePreferences
{
    public string DefaultWorkspace { get; set; }
    public bool AlwaysUseShallowClone { get; set; } = false;
    public bool AutoInstallDependencies { get; set; } = true;
    public bool AutoStartAfterClone { get; set; } = false;
    public Dictionary<string, string> RecentRepositories { get; set; } = new();
    public string PreferredOrganizationPattern { get; set; } // 如 "~/Projects/{organization}/{repo}"
}
```

这样设计的好处是：
- **零思考**: 大部分情况下用户粘贴URL直接点克隆即可
- **高度定制**: 高级用户可以精确控制每个细节
- **智能建议**: 帮助用户做出更好的决策
- **一致体验**: 无论简单还是复杂项目都有流畅体验

你觉得这个交互设计如何？还有什么需要优化的地方吗？