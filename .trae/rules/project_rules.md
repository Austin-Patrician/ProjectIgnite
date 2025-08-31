project:
  name: "ProjectIgnite"
  framework: ".NET 9"
  architecture: "MVVM · Clean Architecture"

rules:
 - id: 001
    name: "推荐使用官方 Avalonia API或者社区活跃度高的第三方库"
    description: |
      • 使用官方公开的 Avalonia 控件与类库。  
      • 禁止引用未认证的第三方 Avalonia *扩展包*，除非你已手动添加到 `.csproj`。
    enforcement: "onError: reject, errorMessage: '请不要使用未授权的第三方 Avalonia 扩展。'"

- id: 002
    name: "遵循 MVVM 架构"
    description: |
      • View：仅负责 XAML 布局 + 绑定声明。  
      • ViewModel：处理业务逻辑、命令、状态。  
      • Model：只为业务层提供数据结构/接口。  
      • 禁止在 View 中出现大量业务代码。
    enforcement: "onError: transform, action: replace, newValue: '请将业务逻辑迁移到 ViewModel。'"

- id: 003
    name: "保持低耦合，高内聚"
    description: |
      • 模块之间不直接访问私有字段。  
      • 通过 `IXXXXService` 接口来传递业务数据。  
      • 说明：如需在 `View` 中使用数据，应通过 `Binding` + `INotifyPropertyChanged`。
    enforcement: "onError: transform, action: replace, newValue: '请使用接口注入的方式减少耦合。'"