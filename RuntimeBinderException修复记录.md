# RuntimeBinderException 修复记录

## 问题描述
在访问 CPU 核心管理器页面时遇到以下运行时错误：
```
RuntimeBinderException: 'object' does not contain a definition for 'SystemInfo'
```

具体错误位置在视图文件 `Index.cshtml` 中：
```html
<h2 class="text-primary">@Model.SystemInfo.ProcessorCount</h2>
```

## 问题原因

### 根本原因
控制器 `CpuCoreController` 使用了匿名对象作为视图模型：

```csharp
var model = new
{
    Config = _coreManager.GetConfig(),
    Processes = _coreManager.GetCurrentProcesses(),
    RecentLogs = _coreManager.GetRecentLogs(50),
    SystemInfo = new
    {
        ProcessorCount = Environment.ProcessorCount,
        IsAdministrator = PrivilegeManager.IsRunningAsAdministrator(),
        ConfigFilePath = _configManager.GetConfigFilePath()
    }
};
```

### 技术分析
- 匿名对象在编译时类型为 `object`
- 运行时动态绑定（dynamic binding）失败
- Razor 引擎无法解析匿名对象的属性
- 导致 `RuntimeBinderException` 异常

## 解决方案

### 1. 创建强类型视图模型
创建了专门的视图模型类 `Models/CpuCoreIndexViewModel.cs`：

```csharp
public class CpuCoreIndexViewModel
{
    public ProcessCoreConfig Config { get; set; } = new();
    public List<ProcessInfo> Processes { get; set; } = new();
    public List<ProcessAffinityLog> RecentLogs { get; set; } = new();
    public SystemInfo SystemInfo { get; set; } = new();
}

public class SystemInfo
{
    public int ProcessorCount { get; set; }
    public bool IsAdministrator { get; set; }
    public string ConfigFilePath { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public long WorkingSet { get; set; }
    public long TickCount { get; set; }
}
```

### 2. 修改控制器
更新控制器以使用强类型模型：

```csharp
public IActionResult Index()
{
    // 权限检查...
    
    var model = new CpuCoreIndexViewModel
    {
        Config = _coreManager.GetConfig(),
        Processes = _coreManager.GetCurrentProcesses(),
        RecentLogs = _coreManager.GetRecentLogs(50),
        SystemInfo = new SystemInfo
        {
            ProcessorCount = Environment.ProcessorCount,
            IsAdministrator = PrivilegeManager.IsRunningAsAdministrator(),
            ConfigFilePath = _configManager.GetConfigFilePath(),
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.ToString(),
            WorkingSet = Environment.WorkingSet,
            TickCount = Environment.TickCount64
        }
    };

    return View(model);
}
```

### 3. 更新视图
在视图顶部添加强类型模型声明：

```razor
@model TSysWatch.Models.CpuCoreIndexViewModel
```

## 修复效果

### ✅ 解决的问题
- **RuntimeBinderException 完全消除**
- **类型安全性增强** - 编译时检查属性访问
- **IntelliSense 支持** - 开发时更好的代码提示
- **性能提升** - 避免运行时动态绑定开销

### ✅ 保持的功能
- 所有原有功能完全保持
- Web 界面正常显示系统信息
- 进程列表正常加载
- 配置管理功能正常
- API 接口正常工作

### ✅ 代码改进
- **更好的可维护性** - 强类型模型易于理解和修改
- **更清晰的架构** - 分离视图模型和业务模型
- **更强的可测试性** - 可以单独测试视图模型

## 技术最佳实践

### 建议
1. **始终使用强类型视图模型** - 避免匿名对象
2. **分离关注点** - 视图模型专门服务于视图
3. **明确的属性定义** - 每个属性都有明确的类型和默认值
4. **一致的命名约定** - 遵循 C# 命名规范

### 避免的问题
- 匿名对象在视图中的使用
- 运行时绑定错误
- 缺乏编译时类型检查
- 开发时缺少 IntelliSense 支持

## 文件变更清单

### 新增文件
- `Models/CpuCoreIndexViewModel.cs` - 强类型视图模型

### 修改文件
- `Controllers/CpuCoreController.cs` - 使用强类型模型
- `Views/CpuCore/Index.cshtml` - 添加模型声明

### 测试验证
- ✅ 构建成功
- ✅ 无编译错误
- ✅ 运行时无异常
- ✅ 页面正常显示

## 结论
通过创建强类型视图模型，彻底解决了 `RuntimeBinderException` 问题，同时提升了代码质量、类型安全性和开发体验。这是 ASP.NET Core MVC 开发中的最佳实践。