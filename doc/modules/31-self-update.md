# Web 自更新系统

## 概述

本模块实现了 ASP.NET Core Web 应用的**自动更新**功能，采用严格的"分进程"架构：

- **Web 程序**：负责上传、触发更新，不直接修改自身文件
- **Updater.exe**：独立进程，负责停止、备份、解压、覆盖、启动等所有文件操作

## 🏗️ 架构设计

### 工作流程

```
用户操作
    ↓
Web 上传 update.zip
    ↓
Web 触发 Updater.exe
    ↓
Updater.exe 执行以下步骤:
    ├─ 停止 Web 宿主 (IIS / WindowsService / Kestrel)
    ├─ 备份 Web 目录 (web_时间戳)
    ├─ 解压 update.zip
    ├─ 覆盖 Web 文件
    ├─ 验证更新
    ├─ 启动 Web 宿主
    └─ 若失败 → 自动回滚
    ↓
Web 恢复正常运行
```

### 职责分离

| 组件 | 职责 | 权限 |
|------|------|------|
| **Web** | 接收上传、触发更新、返回状态 | 普通用户权限 |
| **Updater.exe** | 停止、备份、更新、启动、回滚 | **管理员权限** |

**核心原则**：Web 程序【绝对禁止】修改自身文件

## 📦 更新包结构

update.zip 必须遵循以下结构：

```
update.zip
├─ web/                     # Web 程序文件目录（必须）
│  ├─ WebApp.exe           # Web 可执行文件（若为 self-contained）
│  ├─ dll 文件
│  ├─ appsettings.json
│  ├─ wwwroot/
│  └─ ...其他文件
├─ version.json            # 版本信息（可选）
└─ ...其他辅助文件
```

### version.json 示例

```json
{
  "version": "2.1.0",
  "buildTime": "2026-01-29T10:30:00Z",
  "appType": "aspnetcore",
  "releaseNotes": "修复 Bug、性能优化"
}
```

## 🔧 配置

### 配置文件位置

`ini_config/SelfUpdate.json` - Web 程序启动时自动加载

### 配置参数说明

| 参数 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `enabled` | bool | 是否启用自更新功能 | `true` |
| `packageDirectory` | string | 更新包存储目录 | `C:\WebUpdater\packages` |
| `backupDirectory` | string | 备份存储目录 | `C:\WebUpdater\backup` |
| `updaterExePath` | string | Updater.exe 路径 | `C:\WebUpdater\Updater.exe` |
| `hostingType` | string | Web 宿主类型 | `Kestrel` / `IIS` / `WindowsService` |
| `iisAppPoolName` | string | IIS 应用池名（仅 IIS 需要） | `DefaultAppPool` |
| `iisSiteName` | string | IIS 网站名（仅 IIS 需要） | `Default Web Site` |
| `windowsServiceName` | string | Windows Service 名（仅 WindowsService 需要） | `MyWebService` |
| `kestrelProcessName` | string | Kestrel 进程名 | `dotnet` |
| `maxPackageSize` | long | 最大上传包大小（字节） | `524288000` (500MB) |
| `updateTimeoutMs` | int | 更新超时时间（毫秒） | `300000` (5分钟) |

### 配置示例

#### Kestrel 配置

```json
{
  "enabled": true,
  "packageDirectory": "C:\\WebUpdater\\packages",
  "backupDirectory": "C:\\WebUpdater\\backup",
  "updaterExePath": "C:\\WebUpdater\\Updater\\Updater.exe",
  "hostingType": "Kestrel",
  "kestrelProcessName": "dotnet",
  "maxPackageSize": 524288000,
  "updateTimeoutMs": 300000
}
```

#### IIS 配置

```json
{
  "enabled": true,
  "packageDirectory": "C:\\WebUpdater\\packages",
  "backupDirectory": "C:\\WebUpdater\\backup",
  "updaterExePath": "C:\\WebUpdater\\Updater\\Updater.exe",
  "hostingType": "IIS",
  "iisAppPoolName": "DefaultAppPool",
  "iisSiteName": "Default Web Site",
  "maxPackageSize": 524288000,
  "updateTimeoutMs": 300000
}
```

#### Windows Service 配置

```json
{
  "enabled": true,
  "packageDirectory": "C:\\WebUpdater\\packages",
  "backupDirectory": "C:\\WebUpdater\\backup",
  "updaterExePath": "C:\\WebUpdater\\Updater\\Updater.exe",
  "hostingType": "WindowsService",
  "windowsServiceName": "MyWebService",
  "maxPackageSize": 524288000,
  "updateTimeoutMs": 300000
}
```

## 🌐 API 接口

### 1. 上传更新包

**请求**

```http
POST /api/self-update/upload
Content-Type: multipart/form-data

file: [二进制 ZIP 数据]
```

**cURL 示例**

```bash
curl -X POST http://localhost:5000/api/self-update/upload \
  -F "file=@update.zip"
```

**C# HttpClient 示例**

```csharp
using (var form = new MultipartFormDataContent())
{
    using (var fileStream = new FileStream("update.zip", FileMode.Open))
    {
        form.Add(new StreamContent(fileStream), "file", "update.zip");
        var response = await client.PostAsync("http://localhost:5000/api/self-update/upload", form);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine(result);
    }
}
```

**PowerShell 示例**

```powershell
$FilePath = "C:\update.zip"
$Uri = "http://localhost:5000/api/self-update/upload"
$FileStream = [IO.File]::OpenRead($FilePath)
$Form = @{file=$FileStream}

# 需要使用 Invoke-WebRequest
Invoke-WebRequest -Uri $Uri -Method Post -Form $Form
```

**响应成功示例**

```json
{
  "success": true,
  "message": "更新包上传成功",
  "packageInfo": {
    "version": {
      "version": "2.1.0",
      "buildTime": "2026-01-29",
      "appType": "aspnetcore"
    },
    "packagePath": "C:\\WebUpdater\\packages\\update.zip",
    "packageSize": 15728640,
    "uploadTime": "2026-01-29T10:30:00"
  }
}
```

**响应失败示例**

```json
{
  "success": false,
  "message": "只允许上传 .zip 文件",
  "error": "不支持的文件类型"
}
```

### 2. 触发自更新

**请求**

```http
POST /api/self-update/apply
```

**cURL 示例**

```bash
curl -X POST http://localhost:5000/api/self-update/apply
```

**PowerShell 示例**

```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/self-update/apply" -Method Post
```

**响应成功示例**

```json
{
  "success": true,
  "message": "更新已开始，系统将在更新完成后重启",
  "packageInfo": {
    "packagePath": "C:\\WebUpdater\\packages\\update.zip",
    "packageSize": 15728640
  }
}
```

**响应失败示例**

```json
{
  "success": false,
  "message": "启动更新程序失败，可能需要管理员权限",
  "error": "Win32Exception"
}
```

### 3. 获取更新包信息

**请求**

```http
GET /api/self-update/package-info
```

**响应成功示例**

```json
{
  "success": true,
  "message": "获取更新包信息成功",
  "packageInfo": {
    "version": {
      "version": "2.1.0",
      "buildTime": "2026-01-29",
      "appType": "aspnetcore"
    },
    "packagePath": "C:\\WebUpdater\\packages\\update.zip",
    "packageSize": 15728640,
    "uploadTime": "2026-01-29T10:30:00"
  }
}
```

### 4. 清理过期更新包

**请求**

```http
POST /api/self-update/cleanup
```

**响应**

```json
{
  "success": true,
  "message": "清理完成，删除了 2 个过期包"
}
```

### 5. 健康检查

**请求**

```http
GET /api/self-update/health
```

**响应**

```json
{
  "status": "healthy",
  "timestamp": "2026-01-29T10:30:00Z",
  "message": "自更新系统正常运行"
}
```

## 🚀 使用流程

### 步骤 1：发布更新包

准备更新内容，使用以下结构打包：

```bash
# 构建 Web 程序
dotnet publish TSysWatch.csproj -c Release -o bin/Release/net8.0/publish

# 创建 update.zip
mkdir update_temp
mkdir update_temp\web
xcopy bin\Release\net8.0\publish\* update_temp\web\ /E

# 创建 version.json
echo { "version": "2.1.0", "buildTime": "2026-01-29" } > update_temp\version.json

# 压缩为 ZIP
powershell -Command "Add-Type -A System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory('update_temp', 'update.zip')"
```

### 步骤 2：上传更新包

```csharp
using var fileStream = new FileStream("update.zip", FileMode.Open);
using var form = new MultipartFormDataContent();
form.Add(new StreamContent(fileStream), "file", "update.zip");

var httpClient = new HttpClient();
var response = await httpClient.PostAsync("http://localhost:5000/api/self-update/upload", form);
var result = await response.Content.ReadAsAsync<SelfUpdateResponse>();

if (result.Success)
{
    Console.WriteLine("更新包上传成功");
}
```

### 步骤 3：触发更新

```csharp
var httpClient = new HttpClient();
var response = await httpClient.PostAsync("http://localhost:5000/api/self-update/apply", null);
var result = await response.Content.ReadAsAsync<SelfUpdateResponse>();

if (result.Success)
{
    Console.WriteLine("更新已启动，请等待系统重启...");
}
```

### 步骤 4：等待完成

Updater.exe 会：
1. 停止 Web 宿主（1-3 秒）
2. 备份当前版本（取决于文件大小）
3. 解压新版本（取决于包大小）
4. 覆盖文件（取决于文件数量）
5. 启动 Web 宿主（1-3 秒）

总体时间通常为 **10-30 秒**（取决于程序大小）

## 🔧 Updater.exe 部署

### 部署位置

建议部署到与 Web 程序相同的机器上，但在不同目录：

```
C:\WebUpdater\
├─ Updater\
│  ├─ Updater.exe
│  ├─ Updater.dll
│  └─ 其他 .NET 运行时文件
├─ packages\         # 更新包存储目录
└─ backup\          # 备份存储目录
```

### 运行权限

**重要**：Updater.exe 必须以**管理员身份**运行

可通过以下方式确保：
1. 在 Web 程序的启动脚本中使用 `Process.Start` 并指定 `Verb = "runas"`
2. 为 Updater.exe 配置以管理员身份运行
3. 或将其集成为 Windows Service

### 编译 Updater

```bash
cd Updater
dotnet publish -c Release -r win-x64 -o bin/Release/publish /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

### 日志位置

Updater.exe 的日志输出到：

```
[Updater.exe 所在目录]\logs\updater_YYYYMMDD.txt
```

查看日志示例：

```
[2026-01-29 10:30:00 +08:00] [INF] ================================
[2026-01-29 10:30:00 +08:00] [INF] Updater.exe 启动
[2026-01-29 10:30:00 +08:00] [INF] 命令行参数: --package-path "C:\WebUpdater\packages\update.zip" ...
[2026-01-29 10:30:01 +08:00] [INF] 【步骤 1/6】停止 Web 宿主...
[2026-01-29 10:30:02 +08:00] [INF] ✓ Web 宿主已停止
...
```

## ⚠️ 故障排查

### 问题 1：更新失败，提示"需要管理员权限"

**原因**：Updater.exe 未以管理员身份运行

**解决**：
- 确保 Web 程序的 `Process.Start` 包含 `Verb = "runas"`
- 或手动以管理员身份运行 Updater.exe 进行测试

### 问题 2：更新超时

**原因**：更新包过大，网络慢，或宿主停止/启动时间长

**解决**：
- 增加配置中的 `updateTimeoutMs`
- 减小更新包大小
- 检查 Updater.exe 日志，查看哪一步花费时间最长

### 问题 3：更新后 Web 无法启动

**原因**：
- 配置文件损坏
- 关键 DLL 缺失
- 权限不足

**解决**：
- 检查 Updater.exe 日志，查看备份位置
- 手动从备份恢复：`xcopy backup\web_时间戳\* [Web根目录]\ /Y`
- 验证 update.zip 内容是否正确

### 问题 4：Web 程序无法启动 Updater.exe

**可能原因和解决方案**：

1. **Updater.exe 路径错误**
   - 检查 `SelfUpdate.json` 中的 `updaterExePath` 是否正确

2. **没有管理员权限**
   - 检查 `Process.Start` 是否设置了 `Verb = "runas"`

3. **Updater.exe 被防火墙/杀毒软件阻止**
   - 添加 Updater.exe 到白名单

## 🔄 回滚策略

如果更新过程中出错，Updater.exe 会**自动回滚**到备份版本：

1. **备份自动保留**：最新 3 个备份在 `backupDirectory` 中
2. **手动回滚**：可通过 API 或直接复制备份恢复
3. **日志记录**：所有回滚操作都记录在日志中

### 手动回滚示例

```bash
# 查看备份列表
dir C:\WebUpdater\backup\

# 从最新备份恢复
xcopy C:\WebUpdater\backup\web_20260129_103000\* C:\TSysWatch\ /Y /E

# 重启 Web 程序
```

## 📋 安全注意事项

1. **访问控制**：建议对 API 端点添加身份验证，防止未授权更新
2. **上传验证**：系统已验证 ZIP 文件有效性和大小限制
3. **管理员权限**：确保 Updater.exe 运行环境安全，限制其访问权限
4. **备份保留**：定期清理过期备份，使用 `/api/self-update/cleanup` 接口
5. **签名验证**（可选）：可在 SelfUpdateService 中添加包签名验证

## 📚 代码示例

### 完整的更新流程示例

```csharp
public class UpdateManager
{
    private readonly HttpClient _httpClient;
    private readonly string _webUrl = "http://localhost:5000";

    public async Task PerformUpdateAsync(string packagePath)
    {
        try
        {
            // 1. 上传更新包
            Console.WriteLine("正在上传更新包...");
            var uploadResponse = await UploadPackageAsync(packagePath);
            if (!uploadResponse.Success)
            {
                Console.WriteLine($"上传失败: {uploadResponse.Message}");
                return;
            }

            Console.WriteLine($"上传成功, 版本: {uploadResponse.PackageInfo?.Version?.Version}");

            // 2. 触发更新
            Console.WriteLine("正在触发更新...");
            var applyResponse = await ApplyUpdateAsync();
            if (!applyResponse.Success)
            {
                Console.WriteLine($"更新触发失败: {applyResponse.Message}");
                return;
            }

            Console.WriteLine("更新已启动，请等待系统重启...");

            // 3. 等待 Web 重启
            await WaitForWebRestartAsync();
            Console.WriteLine("Web 程序已重启，更新完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新过程出错: {ex.Message}");
        }
    }

    private async Task<SelfUpdateResponse> UploadPackageAsync(string packagePath)
    {
        using var fileStream = new FileStream(packagePath, FileMode.Open);
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(fileStream), "file", Path.GetFileName(packagePath));

        var response = await _httpClient.PostAsync(
            $"{_webUrl}/api/self-update/upload",
            form);

        return await response.Content.ReadAsAsync<SelfUpdateResponse>();
    }

    private async Task<SelfUpdateResponse> ApplyUpdateAsync()
    {
        var response = await _httpClient.PostAsync(
            $"{_webUrl}/api/self-update/apply",
            null);

        return await response.Content.ReadAsAsync<SelfUpdateResponse>();
    }

    private async Task WaitForWebRestartAsync()
    {
        for (int i = 0; i < 60; i++) // 最多等待 60 秒
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_webUrl}/api/self-update/health");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Web 尚未启动，继续等待
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("Web 程序在规定时间内未重启");
    }
}
```

## 🎯 总结

自更新系统提供了一个生产级别的、安全的 Web 程序自动更新解决方案：

- ✅ **安全分离**：Web 和 Updater 完全分离，避免自我修改
- ✅ **自动备份**：每次更新前自动备份，支持回滚
- ✅ **多宿主支持**：支持 IIS、Windows Service、Kestrel
- ✅ **完整日志**：记录每一步操作，便于故障排查
- ✅ **超时保护**：防止更新卡死
- ✅ **异常回滚**：出现错误自动回滚到备份版本

遵循本文档的规范部署和使用，可确保 Web 程序的安全、稳定更新。
