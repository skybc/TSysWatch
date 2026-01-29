# Web 自更新系统 - 架构与设计

## 核心架构

### 系统分层

```
┌─────────────────────────────────────────────────────┐
│  用户 (调用 API 上传、触发)                          │
└─────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────┐
│  Web 程序 (ASP.NET Core)                            │
│  - SelfUpdateController (API 端点)                  │
│  - SelfUpdateService (业务逻辑)                     │
│  - SelfUpdateConfigManager (配置管理)               │
│                                                     │
│  职责: 接收上传 → 保存文件 → 启动 Updater → 返回结果 │
└─────────────────────────────────────────────────────┘
                         ↓ (Process.Start)
┌─────────────────────────────────────────────────────┐
│  Updater.exe (独立 Console 程序)                    │
│  - HostManager (宿主管理)                           │
│  - UpdateExecutor (更新执行)                        │
│                                                     │
│  职责: 停止 → 备份 → 解压 → 覆盖 → 验证 → 启动     │
└─────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────┐
│  Web 程序 (重新启动)                               │
└─────────────────────────────────────────────────────┘
```

## 职责分离矩阵

| 操作 | Web | Updater | 说明 |
|------|-----|---------|------|
| **接收上传** | ✅ | ❌ | Web 公开 API，接收客户端上传 |
| **保存文件** | ✅ | ❌ | Web 保存 update.zip 到配置目录 |
| **启动进程** | ✅ | ❌ | Web 通过 Process.Start 启动 Updater |
| **停止宿主** | ❌ | ✅ | Updater 以管理员权限停止 IIS/Service/Kestrel |
| **备份文件** | ❌ | ✅ | Updater 备份当前版本 |
| **解压 ZIP** | ❌ | ✅ | Updater 解压新版本到临时目录 |
| **覆盖文件** | ❌ | ✅ | Updater 将新文件覆盖到 Web 目录 |
| **验证更新** | ❌ | ✅ | Updater 检查关键文件是否存在 |
| **启动宿主** | ❌ | ✅ | Updater 启动已停止的 IIS/Service/Kestrel |
| **回滚操作** | ❌ | ✅ | Updater 出错时恢复备份 |

## 关键设计决策

### 1. 为什么分离为两个进程？

**不分离的后果**：
- Web 在运行中修改自身 DLL → 文件被锁定
- Web Kill 自己的进程 → 请求中断
- Web 解压替换自身 → 权限不足或占用

**分离的好处**：
- Updater 以管理员权限独立运行
- Web 和 Updater 进程互不影响
- 更新失败时 Updater 可自动恢复
- 完整的日志记录和错误处理

### 2. 为什么需要备份？

- 更新过程中随时可能失败
- 硬盘空间不足、权限问题、文件损坏等
- 备份允许快速恢复到上一个可用版本
- 多个备份保留可用于版本回滚

### 3. 为什么需要超时控制？

- 某步操作可能永久卡住（如停止 IIS）
- 防止 Updater 僵尸进程
- 5 分钟超时是合理的平衡

### 4. 为什么支持多种宿主？

| 宿主 | 停止方式 | 启动方式 | 特点 |
|------|---------|---------|------|
| **Kestrel** | Kill 进程 | 启动 exe | 最简单，部署灵活 |
| **IIS** | appcmd.exe | appcmd.exe | 企业环境，需要 AppPool 名称 |
| **Windows Service** | ServiceController | ServiceController | 产品环境，按名称停启 |

## 通信流程

### 更新流程时序图

```
用户                Web                  Updater             文件系统
  │                 │                      │                    │
  ├─ 上传 update.zip─→                      │                    │
  │                 │                      │                    │
  │            ┌────┴─ 验证文件类型         │                    │
  │            │       限制文件大小         │                    │
  │            │                      │       保存到    │
  │            │                      │    ←─ packages/│
  │            │                      │
  │            │    返回上传成功       │                    │
  │            ←─────────────────     │                    │
  │                 │                      │                    │
  │  ─ 触发更新 ──→  │                      │                    │
  │                 │                      │                    │
  │            ┌────┴─ 启动 Updater  ──────→ 解析参数          │
  │            │                      │                    │
  │            │                      ├─── 停止 Web 宿主
  │            │                      │    ← ServiceController
  │            │                      │
  │            │                      ├─── 备份当前版本 ──→ backup/
  │            │                      │
  │            │                      ├─── 解压 update.zip
  │            │                      │    ← 临时目录
  │            │                      │
  │            │                      ├─── 覆盖 Web 文件  ──→ Web根目录
  │            │                      │
  │            │                      ├─── 验证新版本
  │            │                      │
  │            │                      ├─── 启动 Web 宿主
  │            │                      │    ← ServiceController
  │            │                      │
  │    返回"更新已开始"             │                    │
  │     ←───────────────            │                    │
  │                 │                      │                    │
  ├─等待 Web 重启 ──→ ← Web 恢复运行 ←─── Updater 退出
  │                 │                      │                    │
```

## 配置管理

### SelfUpdate.json 结构

```json
{
  "enabled": true,
  "packageDirectory": "string",
  "backupDirectory": "string",
  "updaterExePath": "string",
  "hostingType": "Kestrel|IIS|WindowsService",
  "iisAppPoolName": "string|null",
  "iisSiteName": "string|null",
  "windowsServiceName": "string|null",
  "kestrelProcessName": "string",
  "maxPackageSize": 524288000,
  "updateTimeoutMs": 300000
}
```

### 配置加载流程

1. 程序启动时 `SelfUpdateConfigManager` 被实例化
2. 自动加载 `ini_config/SelfUpdate.json`
3. 若文件不存在，创建默认配置
4. 验证必要参数，创建必要的目录

```csharp
public class SelfUpdateConfigManager
{
    private void LoadConfig()
    {
        // 1. 检查文件存在
        if (File.Exists(_configPath))
        {
            // 2. 反序列化 JSON
            _config = JsonSerializer.Deserialize<SelfUpdateConfig>(json);
        }
        else
        {
            // 3. 初始化默认配置
            InitializeDefaultConfig();
        }
        
        // 4. 验证配置
        ValidateConfig();
        
        // 5. 创建必要的目录
    }
}
```

## 错误处理与恢复

### 更新过程中的错误处理

```
ExecuteAsync()
  ├─ try
  │  ├─ 停止宿主
  │  ├─ 备份目录 ← 保存备份路径用于回滚
  │  ├─ 解压包
  │  ├─ 覆盖文件
  │  ├─ 验证更新
  │  └─ 启动宿主
  │
  └─ catch (Exception)
     ├─ 记录详细错误日志
     └─ RollbackAsync()  ← 使用保存的备份路径恢复
        ├─ 停止宿主
        ├─ 清空当前目录
        ├─ 复制备份文件
        └─ 启动宿主（旧版本）
```

### 故障场景

| 故障 | 原因 | 处理 | 结果 |
|------|------|------|------|
| 停止宿主失败 | 权限不足/宿主不存在 | 抛出异常 | 回滚 |
| 备份失败 | 磁盘空间不足/权限不足 | 抛出异常 | 回滚 |
| 解压失败 | ZIP 损坏/路径非法 | 抛出异常 | 回滚 |
| 覆盖失败 | 文件被占用/权限不足 | 记录警告，继续 | 可能失败 |
| 启动宿主失败 | 配置错误/宿主损坏 | 抛出异常 | 回滚 |
| 超时 | 某步卡住 | 取消操作 | 回滚 |

## 宿主管理

### IisHostManager 工作流程

```csharp
public class IisHostManager : IHostManager
{
    // 停止: appcmd.exe stop site + stop apppool
    public async Task StopAsync()
    {
        await RunCommand(appcmd.exe, "stop site {SiteName}");
        await RunCommand(appcmd.exe, "stop apppool {IisAppPoolName}");
    }
    
    // 启动: appcmd.exe start apppool + start site
    public async Task StartAsync()
    {
        await RunCommand(appcmd.exe, "start apppool {IisAppPoolName}");
        await RunCommand(appcmd.exe, "start site {SiteName}");
    }
}
```

### WindowsServiceHostManager 工作流程

```csharp
public class WindowsServiceHostManager : IHostManager
{
    // 停止: ServiceController.Stop()
    public async Task StopAsync()
    {
        var service = new ServiceController(_args.ServiceName);
        service.Stop();
        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
    }
    
    // 启动: ServiceController.Start()
    public async Task StartAsync()
    {
        var service = new ServiceController(_args.ServiceName);
        service.Start();
        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
    }
}
```

### KestrelHostManager 工作流程

```csharp
public class KestrelHostManager : IHostManager
{
    // 停止: Kill dotnet.exe 进程
    public async Task StopAsync()
    {
        var processes = Process.GetProcessesByName("dotnet");
        foreach (var p in processes)
        {
            p.Kill();
            p.WaitForExit(10000);
        }
    }
    
    // 启动: 启动 Web 程序 exe
    public async Task StartAsync()
    {
        var exe = FindWebAppExecutable();
        Process.Start(exe);
        await Task.Delay(3000); // 等待启动
    }
}
```

## 版本管理

### 版本信息结构

```json
{
  "version": "2.1.0",
  "buildTime": "2026-01-29T10:30:00Z",
  "appType": "aspnetcore",
  "releaseNotes": "修复 Bug、性能优化"
}
```

### 版本读取流程

```csharp
private async Task<VersionInfo> TryReadVersionInfoAsync(string zipPath)
{
    using (var archive = ZipFile.OpenRead(zipPath))
    {
        var versionEntry = archive.GetEntry("version.json");
        if (versionEntry != null)
        {
            using (var stream = versionEntry.Open())
            {
                return JsonSerializer.Deserialize<VersionInfo>(stream);
            }
        }
    }
    return null;
}
```

## 日志记录

### 日志级别

| 级别 | 用途 | 示例 |
|------|------|------|
| **Information** | 正常流程 | "【步骤 1/6】停止 Web 宿主..." |
| **Warning** | 非关键问题 | "删除过期更新包失败" |
| **Error** | 关键错误 | "停止 Web 宿主失败" |
| **Fatal** | 致命错误 | "Updater.exe 发生未处理异常" |

### 日志输出

```
[2026-01-29 10:30:00] [INF] Updater.exe 启动
[2026-01-29 10:30:00] [INF] 命令行参数: --package-path ...
[2026-01-29 10:30:00] [INF] 【步骤 1/6】停止 Web 宿主...
[2026-01-29 10:30:02] [INF] ✓ Web 宿主已停止
[2026-01-29 10:30:02] [INF] 【步骤 2/6】备份当前 Web 目录...
[2026-01-29 10:30:05] [INF] ✓ Web 目录备份成功: backup/web_20260129_103002
...
[2026-01-29 10:30:15] [INF] === Web 程序更新完成 ===
```

## 安全考虑

### 权限模型

```
Web (普通权限)
  ├─ √ 读取配置
  ├─ √ 接收 HTTP 请求
  ├─ √ 保存 update.zip
  ├─ × 停止 Windows Service
  ├─ × 修改 Web 目录
  └─ √ 启动 Updater.exe (以管理员身份)

Updater (管理员权限)
  ├─ √ 停止 IIS/Service/进程
  ├─ √ 创建备份
  ├─ √ 修改 Web 目录
  ├─ √ 解压 ZIP 文件
  └─ √ 启动宿主
```

### 建议的安全加强

1. **API 认证**
   ```csharp
   [Authorize]
   public IActionResult Upload(IFormFile file) { ... }
   ```

2. **包签名验证**
   ```csharp
   // 在上传时验证签名
   if (!VerifyPackageSignature(package))
       return Unauthorized("Invalid package signature");
   ```

3. **审计日志**
   ```csharp
   _auditLogger.LogUpdate(userId, packageVersion, result);
   ```

## 性能优化

### 文件复制优化

```csharp
// 使用 File.Copy 而非流操作
File.Copy(sourceFile, targetFile, true);

// 批量操作时显示进度
if (copiedCount % 100 == 0)
    _logger.Information("已复制 {Count} 个文件", copiedCount);
```

### 目录操作优化

```csharp
// 递归复制时保留目录结构
Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
```

### 超时设置优化

```csharp
// 根据 Web 程序大小调整超时
// 小程序: 3 分钟
// 中等程序: 5 分钟 (默认)
// 大程序: 10 分钟
```

## 扩展点

### 如何支持新的宿主类型？

1. 实现 `IHostManager` 接口
   ```csharp
   public class CustomHostManager : IHostManager
   {
       public async Task StopAsync(CancellationToken cancellationToken) { }
       public async Task StartAsync(CancellationToken cancellationToken) { }
       public bool IsRunning() { }
   }
   ```

2. 在工厂中注册
   ```csharp
   public static IHostManager Create(UpdaterArguments args, ILogger logger)
   {
       return args.HostingType.ToLower() switch
       {
           "custom" => new CustomHostManager(args, logger),
           ...
       };
   }
   ```

### 如何添加包验证？

```csharp
private bool VerifyPackageIntegrity(string zipPath)
{
    // 方法 1: 检查 version.json 存在
    using (var archive = ZipFile.OpenRead(zipPath))
    {
        if (archive.GetEntry("version.json") == null)
            return false;
        
        if (archive.GetEntry("web") == null)
            return false;
    }
    return true;
}
```

## 总结

该架构设计实现了：
- ✅ **完全分离**：Web 不修改自身
- ✅ **安全可靠**：完整备份和回滚
- ✅ **灵活部署**：支持多种宿主
- ✅ **易于维护**：清晰的职责分离
- ✅ **生产就绪**：完整的日志和错误处理
