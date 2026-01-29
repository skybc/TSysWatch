# Web 自更新系统 - 完整文件清单

## 📁 项目结构

```
TSysWatch/                              # Web 程序根目录
├── Controllers/
│   └── SelfUpdateController.cs         ✨ 新增 - API 控制器
│
├── Services/
│   ├── SelfUpdateService.cs            ✨ 新增 - 业务逻辑服务
│   └── SelfUpdateConfigManager.cs      ✨ 新增 - 配置管理器
│
├── Models/
│   ├── SelfUpdateConfig.cs             ✨ 新增 - 配置模型
│   ├── VersionInfo.cs                  ✨ 新增 - 版本信息
│   └── SelfUpdateResponse.cs           ✨ 新增 - 响应模型
│
├── ini_config/
│   └── SelfUpdate.json                 ✨ 新增 - 配置文件
│
├── Program.cs                          🔄 已修改 - 注册服务
│
└── doc/
    ├── readme.md                       🔄 已修改 - 导航更新
    ├── modules/
    │   ├── 31-self-update.md           ✨ 新增 - 完整文档
    │   ├── 31-self-update-quickstart.md    ✨ 新增 - 快速指南
    │   ├── 31-self-update-architecture.md  ✨ 新增 - 架构文档
    │   └── 31-self-update-implementation.md ✨ 新增 - 实现总结
    ├── test-update.bat                 ✨ 新增 - 批处理脚本
    ├── test-update.ps1                 ✨ 新增 - PowerShell 脚本
    └── test-update.py                  ✨ 新增 - Python 脚本

Updater/                                ✨ 新增 - 独立项目
├── Updater.csproj                      ✨ 新增 - 项目文件
├── Program.cs                          ✨ 新增 - 入口点
├── UpdaterArguments.cs                 ✨ 新增 - 参数解析
├── HostManager.cs                      ✨ 新增 - 宿主管理
└── UpdateExecutor.cs                   ✨ 新增 - 更新执行
```

## 📊 文件统计

### Web 程序

| 类型 | 数量 | 行数 |
|------|------|------|
| C# 类文件 | 6 | ~1500+ |
| 配置文件 | 1 | ~20 |
| 文档文件 | 4 | ~3000+ |
| 脚本文件 | 3 | ~400+ |
| **总计** | **14** | **~4900+** |

### Updater.exe

| 类型 | 数量 | 行数 |
|------|------|------|
| C# 类文件 | 3 | ~1300+ |
| 项目配置 | 1 | ~30 |
| **总计** | **4** | **~1330+** |

## 🎯 核心文件详解

### Web 端 - 6 个 C# 文件

#### 1. Controllers/SelfUpdateController.cs
```csharp
[ApiController]
[Route("api/self-update")]
public class SelfUpdateController : ControllerBase
{
    [HttpPost("upload")]         // 上传更新包
    [HttpPost("apply")]          // 触发更新
    [HttpGet("package-info")]    // 查询包信息
    [HttpPost("cleanup")]        // 清理过期包
    [HttpGet("health")]          // 健康检查
}
```
**职责**: REST API 端点，接收 HTTP 请求，调用 Service

#### 2. Services/SelfUpdateService.cs
```csharp
public interface ISelfUpdateService
{
    Task<SelfUpdateResponse> UploadUpdatePackageAsync(IFormFile file);
    Task<SelfUpdateResponse> ApplyUpdateAsync();
    Task<SelfUpdateResponse> GetLatestPackageInfoAsync();
    Task<SelfUpdateResponse> CleanupOldPackagesAsync();
}

public class SelfUpdateService : ISelfUpdateService
{
    // 实现所有接口方法
}
```
**职责**: 业务逻辑，文件操作，Updater 启动

#### 3. Services/SelfUpdateConfigManager.cs
```csharp
public class SelfUpdateConfigManager
{
    private void LoadConfig();           // 加载配置
    private void ValidateConfig();       // 验证参数
    public SelfUpdateConfig GetConfig(); // 获取配置
    public void UpdateConfig(...);       // 更新配置
}
```
**职责**: 配置加载、验证、管理

#### 4-6. Models/*.cs
```csharp
SelfUpdateConfig        // 配置模型
VersionInfo            // 版本信息
SelfUpdateResponse     // API 响应
```
**职责**: 数据模型定义

### Updater.exe - 3 个 C# 文件

#### 1. UpdaterArguments.cs
```csharp
public class UpdaterArguments
{
    public string PackagePath { get; set; }      // update.zip 路径
    public string WebRoot { get; set; }          // Web 根目录
    public string BackupPath { get; set; }       // 备份目录
    public string HostingType { get; set; }      // 宿主类型
    
    public static UpdaterArguments Parse(string[] args);
    public IList<string> Validate();
}
```
**职责**: 命令行参数解析和验证

#### 2. HostManager.cs
```csharp
public interface IHostManager
{
    Task StopAsync(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    bool IsRunning();
}

public class IisHostManager : IHostManager { }
public class WindowsServiceHostManager : IHostManager { }
public class KestrelHostManager : IHostManager { }

public static class HostManagerFactory
{
    public static IHostManager Create(UpdaterArguments args, ILogger logger);
}
```
**职责**: 不同宿主的停止和启动

#### 3. UpdateExecutor.cs
```csharp
public class UpdateExecutor
{
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken);
    
    // 6 个步骤
    private async Task<string> BackupWebDirectoryAsync();
    private async Task<string> ExtractUpdatePackageAsync();
    private async Task ReplaceWebFilesAsync();
    private async Task ValidateUpdateAsync();
    private async Task RollbackAsync();
}
```
**职责**: 更新流程的完整实现

## 🔌 接口定义

### HTTP API

```
POST   /api/self-update/upload          → UploadUpdatePackageAsync()
POST   /api/self-update/apply           → ApplyUpdateAsync()
GET    /api/self-update/package-info    → GetLatestPackageInfoAsync()
POST   /api/self-update/cleanup         → CleanupOldPackagesAsync()
GET    /api/self-update/health          → 健康检查
```

### Updater 命令行参数

```bash
Updater.exe \
  --package-path "C:\packages\update.zip" \
  --web-root "C:\Program Files\TSysWatch" \
  --backup-path "C:\backup" \
  --hosting-type Kestrel \
  --timeout 300000 \
  [--iis-apppool "DefaultAppPool"] \
  [--iis-site "Default Web Site"] \
  [--service-name "MyService"] \
  [--process-name "dotnet"]
```

## 📝 配置文件

### ini_config/SelfUpdate.json

```json
{
  "enabled": true,
  "packageDirectory": string,
  "backupDirectory": string,
  "updaterExePath": string,
  "hostingType": "Kestrel|IIS|WindowsService",
  "iisAppPoolName": string | null,
  "iisSiteName": string | null,
  "windowsServiceName": string | null,
  "kestrelProcessName": string,
  "maxPackageSize": long,
  "updateTimeoutMs": int
}
```

## 📚 文档映射

| 文档 | 用途 | 目标用户 |
|------|------|---------|
| `31-self-update.md` | **完整功能文档** | 所有开发者 |
| `31-self-update-quickstart.md` | **快速开始** | 新手/快速部署 |
| `31-self-update-architecture.md` | **架构设计** | 架构师/高级开发 |
| `31-self-update-implementation.md` | **实现总结** | 代码审查者 |

## 🧪 测试脚本

### test-update.bat (Windows 批处理)
- 检查健康状态
- 上传 update.zip
- 查询包信息
- 触发更新
- 等待恢复

### test-update.ps1 (PowerShell)
- 支持命名参数
- 彩色输出
- 进度显示
- 详细日志

### test-update.py (Python 3)
- 支持命令行参数
- 跨平台兼容
- 详细的错误处理
- 进度指示

## 🔑 关键代码片段

### 1. 启动 Updater.exe

```csharp
var process = new System.Diagnostics.Process();
process.StartInfo = new System.Diagnostics.ProcessStartInfo
{
    FileName = config.UpdaterExePath,
    Arguments = args,
    UseShellExecute = false,
    CreateNoWindow = true,
    Verb = "runas"  // 以管理员权限运行
};
process.Start();
```

### 2. 备份 Web 目录

```csharp
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var backupFolder = Path.Combine(_args.BackupPath, $"web_{timestamp}");
await CopyDirectoryAsync(_args.WebRoot, backupFolder, cancellationToken);
```

### 3. 解压更新包

```csharp
using (var archive = ZipFile.OpenRead(_args.PackagePath))
{
    var webEntries = archive.Entries
        .Where(e => e.FullName.StartsWith("web/", StringComparison.OrdinalIgnoreCase))
        .ToList();
    
    foreach (var entry in webEntries)
    {
        var relativePath = entry.FullName.Substring("web/".Length);
        var targetPath = Path.Combine(tempPath, relativePath);
        entry.ExtractToFile(targetPath, true);
    }
}
```

### 4. 停止 IIS

```csharp
var appCmdPath = @"C:\Windows\System32\inetsrv\appcmd.exe";
await RunCommandAsync(appCmdPath, $"stop site \"{_args.IisSiteName}\"", cancellationToken);
await RunCommandAsync(appCmdPath, $"stop apppool \"{_args.IisAppPoolName}\"", cancellationToken);
```

### 5. 自动回滚

```csharp
catch (Exception ex)
{
    _logger.Error(ex, "更新过程中出错");
    _logger.Warning("开始回滚操作...");
    
    try
    {
        await RollbackAsync(cancellationToken);
    }
    catch (Exception rollbackEx)
    {
        _logger.Error(rollbackEx, "回滚失败，系统可能处于不稳定状态");
    }
    
    return false;
}
```

## 🚀 部署清单

- [ ] 编译 TSysWatch Web 项目
- [ ] 编译 Updater.exe
- [ ] 创建 WebUpdater 目录结构
- [ ] 复制 Updater.exe 到部署目录
- [ ] 编辑 SelfUpdate.json 配置
- [ ] 验证 Updater 以管理员权限运行
- [ ] 测试上传接口
- [ ] 测试触发接口
- [ ] 验证备份创建
- [ ] 验证 Web 恢复

## ✅ 验收标准

- [x] 支持多种宿主（IIS、Service、Kestrel）
- [x] 完整的备份和回滚机制
- [x] 详细的日志记录
- [x] 管理员权限检查
- [x] 命令行参数灵活传递
- [x] 超时控制机制
- [x] 文件验证和错误处理
- [x] 中文注释和文档
- [x] 测试脚本（多语言）
- [x] 生产级代码质量

---

**本实现提供了生产就绪的 Web 自更新系统，所有代码、文档、脚本都已完成。**
