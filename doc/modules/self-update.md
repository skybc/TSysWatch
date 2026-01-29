# Web 自动更新功能模块文档

## 概述

本模块实现了 Web 程序自身通过上传 ZIP 包进行自动更新的完整功能。采用 Web + 独立 Updater.exe 的架构，严格遵循职责分离原则。

## 核心架构

### 职责分离

```
Web 程序（ASP.NET Core）
  ├─ 提供上传接口 (/api/self-update/upload)
  ├─ 提供管理 UI (/self-update)
  ├─ 启动 Updater.exe 进程（仅此而已）
  └─ 返回响应（绝不修改自身文件）

Updater.exe（独立 Console 程序）
  ├─ 停止 Web 宿主（IIS/Service/Kestrel）
  ├─ 备份现有程序目录
  ├─ 解压更新包
  ├─ 覆盖 Web 程序文件
  ├─ 启动 Web 宿主
  └─ 失败时回滚处理
```

**核心原则：** ❌ Web 绝不能覆盖自己的 DLL/EXE；✔ 只能"启动更新程序"，让独立进程完成更新

---

## 功能模块

### 1. Web 用户界面（UI 页面）

#### 文件位置
- **视图：** `Views/SelfUpdate/Index.cshtml`
- **模型：** `Models/SelfUpdatePageViewModel.cs`
- **控制器：** `Controllers/SelfUpdateController.cs` (Index 方法)

#### URL 路由
```
GET /self-update              → 显示更新管理页面
POST /api/self-update/upload  → 上传 ZIP 文件
POST /api/self-update/apply   → 触发更新
```

#### 页面功能

**步骤 1：上传更新包**
- 接受 .zip 文件选择
- 实时上传进度条显示（XHR 原生 ProgressEvent）
- 文件验证：
  - ✓ 仅允许 .zip 格式
  - ✓ 最大 500 MB
  - ✓ 上传至 `C:\WebUpdater\packages\update.zip`

**步骤 2：应用更新**
- 显示警告信息：网站将停止、无法访问
- 点击"应用更新"按钮触发 Updater.exe
- 自动轮询重试机制（30 秒后刷新页面）

**页面特点：**
- 响应式布局（Bootstrap 4）
- 实时进度反馈
- 错误提示和成功提示
- 管理员友好的警告说明

---

### 2. Web API 接口实现

#### SelfUpdateController 的所有接口

**2.1 上传接口**
```csharp
[HttpPost("upload")]
[Route("api/self-update/upload")]
public async Task<IActionResult> Upload(IFormFile file)
```

**职责：**
- 验证文件为 .zip 和大小限制
- 保存到固定路径：`appSettings 配置的目录/update.zip`
- 返回 JSON 响应

**2.2 应用更新接口**
```csharp
[HttpPost("apply")]
[Route("api/self-update/apply")]
public async Task<IActionResult> Apply()
```

**职责：**
- 调用 `ISelfUpdateService.ApplyUpdateAsync()`
- 该方法启动 Updater.exe（并传递参数）
- 立即返回"更新已开始"响应
- ⚠️ Web 不做任何文件替换，仅启动进程

**2.3 其他管理接口**
- `[HttpGet("package-info")]` - 获取待更新的包信息
- `[HttpPost("cleanup")]` - 清理历史更新包
- `[HttpGet("health")]` - 健康检查

---

### 3. 更新包结构规范

#### 标准 ZIP 内部结构

```
update.zip
├─ web/
│  ├─ TSysWatch.dll
│  ├─ TSysWatch.runtimeconfig.json
│  ├─ appsettings.json
│  ├─ wwwroot/
│  │  ├─ css/
│  │  ├─ js/
│  │  └─ index.html
│  └─ [其他程序文件]
└─ version.json
```

#### version.json 格式

```json
{
  "version": "2.1.0",
  "buildTime": "2026-01-29T10:30:00Z",
  "appType": "aspnetcore",
  "changelog": "BUG 修复和性能优化"
}
```

**Updater.exe 必须验证此文件存在和格式正确**

---

### 4. ISelfUpdateService 接口

#### 核心职责

```csharp
public interface ISelfUpdateService
{
    /// 上传更新包：验证→保存→返回结果
    Task<SelfUpdateResponse> UploadUpdatePackageAsync(IFormFile file);
    
    /// 触发更新：启动 Updater.exe 进程
    Task<SelfUpdateResponse> ApplyUpdateAsync();
    
    /// 获取待更新的包信息
    Task<SelfUpdateResponse> GetLatestPackageInfoAsync();
    
    /// 清理旧的更新包文件
    Task<SelfUpdateResponse> CleanupOldPackagesAsync();
}
```

#### SelfUpdateService 实现

**UploadUpdatePackageAsync：**
1. 校验文件存在、类型为 .zip
2. 校验文件大小 ≤ 500 MB
3. 创建保存目录（如果不存在）
4. 保存文件到 `config/update-packages/update.zip`
5. 校验 ZIP 内部包含 `version.json` 和 `web/` 目录
6. 返回上传结果

**ApplyUpdateAsync：**
1. 验证 `update.zip` 存在
2. 从 `web.config` 或 `appsettings.json` 读取配置：
   - Web 运行模式（IIS / WindowsService / Kestrel）
   - Web 根目录
   - Updater.exe 路径
3. 构建命令行参数
4. 启动 Updater.exe：`Process.Start()`
5. **立即返回，不等待更新完成**

**命令行参数示例：**
```
Updater.exe --zip-path "C:/WebUpdater/packages/update.zip" 
            --web-root "C:/Program Files/TSysWatch"
            --host-type "iis"
            --log-path "C:/WebUpdater/logs/update.log"
```

---

### 5. Updater.exe 核心流程

#### 完整执行步骤

```
1️⃣ 解析命令行参数
   ├─ --zip-path: 更新包路径
   ├─ --web-root: Web 程序根目录
   ├─ --host-type: 宿主类型 (iis/service/kestrel)
   └─ --log-path: 日志输出位置

2️⃣ 验证环境
   ├─ ZIP 文件是否存在
   ├─ Web 目录是否存在
   └─ 是否拥有管理员权限

3️⃣ 停止 Web 宿主
   ├─ IIS: StopAppPool() → StopWebSite()
   ├─ WindowsService: Stop-Service
   └─ Kestrel: Kill 进程

4️⃣ 备份当前版本
   └─ 复制整个 Web 目录到 backup/web_时间戳

5️⃣ 解压更新包
   └─ 提取 ZIP 到临时目录

6️⃣ 覆盖程序文件
   ├─ 删除旧目录中的 DLL、EXE
   └─ 复制新文件到 Web 目录

7️⃣ 启动 Web 宿主
   ├─ IIS: StartAppPool() → StartWebSite()
   ├─ WindowsService: Start-Service
   └─ Kestrel: 执行启动脚本

8️⃣ 验证成功
   └─ 检查 Web 进程是否正常运行

❌ 失败回滚：
   ├─ 删除已替换的新文件
   ├─ 从备份恢复原版本
   └─ 重新启动 Web
```

#### 关键实现细节

**进程占用处理：**
```csharp
// 重试机制：等待文件释放
for (int retry = 0; retry < 3; retry++)
{
    try
    {
        File.Delete(filePath);
        break;
    }
    catch (IOException) when (retry < 2)
    {
        Thread.Sleep(1000); // 等待 1 秒重试
    }
}
```

**管理员权限检查：**
```csharp
if (!IsRunAsAdministrator())
{
    throw new InvalidOperationException("Updater 必须以管理员身份运行");
}
```

**回滚策略：**
```csharp
try
{
    // 更新步骤...
}
catch (Exception ex)
{
    _logger.LogError("更新失败，执行回滚: {Message}", ex.Message);
    RestoreFromBackup(backupPath, webRootPath);
    RestartWebHost();
    Environment.Exit(1);
}
```

---

## 配置文件说明

### appsettings.json

```json
{
  "SelfUpdate": {
    "Enabled": true,
    "PackagePath": "C:/WebUpdater/packages",
    "BackupPath": "C:/WebUpdater/backup",
    "UpdaterExePath": "C:/WebUpdater/Updater.exe",
    "MaxPackageSizeMB": 500,
    "HostType": "kestrel",
    "RetentionCount": 3
  }
}
```

**配置项说明：**
- `PackagePath` - 上传 ZIP 的保存目录
- `BackupPath` - Updater 执行备份的目录
- `UpdaterExePath` - Updater.exe 的完整路径
- `HostType` - 当前 Web 的宿主类型
- `RetentionCount` - 保留多少个历史更新包

---

## 使用流程

### 为最终用户

1. **访问管理页面**
   ```
   https://yourapp.com/self-update
   ```

2. **上传更新包**
   - 选择已打包好的 `update.zip`
   - 点击"上传更新包"
   - 等待进度条完成

3. **应用更新**
   - 页面提示"准备就绪"
   - 点击"应用更新"
   - **注意：网站将停止访问**
   - 等待约 1-5 分钟
   - 页面自动刷新，验证新版本

### 为开发者

1. **准备更新包**
   ```powershell
   # 构建新版本
   dotnet publish -c Release
   
   # 打包成 ZIP
   # update.zip 内必须包含：
   #  - web/ (所有发布文件)
   #  - version.json (版本信息)
   ```

2. **监控更新过程**
   - 查看 Updater 日志：`C:\WebUpdater\logs\update.log`
   - 检查备份目录：`C:\WebUpdater\backup\`
   - 如需回滚：manually 复制备份回 Web 根目录

---

## 安全考虑

1. **权限控制**
   - 建议给 `/self-update` 页面配置身份验证
   - 仅管理员可访问上传接口

2. **文件验证**
   - 验证 ZIP 内部结构和 version.json 格式
   - 检查更新包签名（可选加密）

3. **备份策略**
   - Updater 自动备份当前版本
   - 设置合理的备份保留时间

4. **日志记录**
   - 完整记录每个更新步骤
   - 包括失败原因和回滚过程

---

## 常见问题

**Q: 更新中网站不可用，这是设计问题吗？**
A: 不是。这是架构必然。Web 无法更新自身，必须停止才能被 Updater 替换。这是安全、可靠的做法。

**Q: 如果 Updater 失败了怎么办？**
A: Updater 会自动回滚备份，重启旧版本 Web。如果备份也失败，需要手工恢复。

**Q: 如何保证 Updater.exe 本身的安全？**
A: 建议把 Updater.exe 放在专属目录（如 C:\WebUpdater），启用 Windows 文件访问权限限制。

**Q: Web 必须运行在 IIS / Service 吗？**
A: 不必。支持 Kestrel、IIS、Windows Service 三种模式。通过 `HostType` 配置选择。

---

## 文件清单

### Web 程序文件

| 文件 | 说明 |
|------|------|
| `Controllers/SelfUpdateController.cs` | 控制器（API + UI） |
| `Services/SelfUpdateService.cs` | 业务逻辑实现 |
| `Models/SelfUpdatePageViewModel.cs` | 页面模型 |
| `Models/SelfUpdateConfig.cs` | 配置模型 |
| `Models/SelfUpdateResponse.cs` | 响应模型 |
| `Models/VersionInfo.cs` | 版本信息模型 |
| `Views/SelfUpdate/Index.cshtml` | 管理 UI |
| `config/SelfUpdate.json` | 配置文件 |

### Updater 程序文件

| 文件 | 说明 |
|------|------|
| `Program.cs` | 入口点 |
| `UpdaterArguments.cs` | 命令行参数解析 |
| `UpdateExecutor.cs` | 更新执行逻辑 |
| `HostManager.cs` | 宿主启停管理 |

---

## 扩展和定制

### 自定义 UI

编辑 `Views/SelfUpdate/Index.cshtml`：
- 修改页面样式和布局
- 添加自定义 JavaScript 逻辑
- 集成企业品牌和主题

### 自定义验证

在 `SelfUpdateService.UploadUpdatePackageAsync()` 中添加：
```csharp
// 例如：验证 ZIP 签名
await ValidatePackageSignatureAsync(zipPath);

// 例如：检查版本号递增
await ValidateVersionIncrementAsync(newVersion);
```

### 自定义宿主支持

在 `HostManager.cs` 中扩展：
```csharp
case "mycustom-host":
    await StopCustomHostAsync();
    // 自定义逻辑
    break;
```

