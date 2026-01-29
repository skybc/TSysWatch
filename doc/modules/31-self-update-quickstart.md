# Web 自更新系统 - 快速开始指南

## 📋 实现总结

本实现提供了一个**生产级别**的 Web 自更新系统，严格遵循"分进程架构"原则：

```
Web (只负责上传/触发)
  └─→ Updater.exe (负责停止/备份/更新/启动)
```

## ⚡ 快速部署

### 1. Web 程序端

#### 已实现的组件

| 文件 | 说明 |
|------|------|
| `Controllers/SelfUpdateController.cs` | API 接口 (上传、触发、查询) |
| `Services/SelfUpdateService.cs` | 业务逻辑 |
| `Services/SelfUpdateConfigManager.cs` | 配置管理 |
| `Models/SelfUpdateConfig.cs` | 配置模型 |
| `Models/VersionInfo.cs` | 版本信息 |
| `Models/SelfUpdateResponse.cs` | API 响应 |
| `ini_config/SelfUpdate.json` | 配置文件 |

#### 修改点

- **Program.cs**：已注册服务
  ```csharp
  builder.Services.AddSingleton<SelfUpdateConfigManager>();
  builder.Services.AddScoped<ISelfUpdateService, SelfUpdateService>();
  ```

#### API 端点

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | `/api/self-update/upload` | 上传 update.zip |
| POST | `/api/self-update/apply` | 触发更新 |
| GET | `/api/self-update/package-info` | 查询更新包信息 |
| POST | `/api/self-update/cleanup` | 清理过期包 |
| GET | `/api/self-update/health` | 健康检查 |

### 2. Updater.exe 程序

#### 已实现的组件

| 文件 | 说明 |
|------|------|
| `Updater.csproj` | 项目配置 |
| `Program.cs` | 入口点 |
| `UpdaterArguments.cs` | 命令行参数解析 |
| `HostManager.cs` | 宿主管理 (IIS/Service/Kestrel) |
| `UpdateExecutor.cs` | 更新执行逻辑 |

#### 独立部署

在 Web 程序同级目录创建 Updater 目录：

```
C:\Program Files\
├─ TSysWatch\           # Web 程序所在
│  ├─ bin\
│  ├─ Controllers\
│  └─ ...
├─ Updater\            # Updater.exe 所在
│  ├─ Updater.exe
│  ├─ Updater.dll
│  └─ ...运行时文件
```

#### 编译 Updater

```bash
cd Updater
dotnet publish -c Release -r win-x64 --self-contained
```

### 3. 配置 SelfUpdate.json

根据宿主类型修改 `ini_config/SelfUpdate.json`：

**Kestrel（默认）**
```json
{
  "enabled": true,
  "packageDirectory": "C:\\WebUpdater\\packages",
  "backupDirectory": "C:\\WebUpdater\\backup",
  "updaterExePath": "C:\\Program Files\\Updater\\Updater.exe",
  "hostingType": "Kestrel",
  "kestrelProcessName": "dotnet",
  "maxPackageSize": 524288000,
  "updateTimeoutMs": 300000
}
```

**IIS**
```json
{
  "hostingType": "IIS",
  "iisAppPoolName": "DefaultAppPool",
  "iisSiteName": "Default Web Site"
}
```

**Windows Service**
```json
{
  "hostingType": "WindowsService",
  "windowsServiceName": "MyWebService"
}
```

## 🎯 使用流程

### 步骤 1：准备更新包

```bash
# 发布 Web 程序
dotnet publish TSysWatch.csproj -c Release -o publish

# 创建 update.zip
mkdir update_temp\web
xcopy publish\* update_temp\web\ /E

# 创建版本信息
echo { "version":"2.1.0", "buildTime":"2026-01-29" } > update_temp\version.json

# 打包为 ZIP
PowerShell -Command "Add-Type -A System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory('update_temp', 'update.zip')"
```

### 步骤 2：上传更新包

```bash
curl -X POST http://localhost:5000/api/self-update/upload \
  -F "file=@update.zip"
```

响应示例：
```json
{
  "success": true,
  "message": "更新包上传成功",
  "packageInfo": {
    "version": {"version": "2.1.0", ...},
    "packageSize": 15728640
  }
}
```

### 步骤 3：触发更新

```bash
curl -X POST http://localhost:5000/api/self-update/apply
```

Updater.exe 会自动：
1. 停止 Web (1-3 秒)
2. 备份当前版本
3. 解压新版本
4. 覆盖文件
5. 启动 Web (1-3 秒)

总耗时：**10-30 秒**（取决于程序大小）

### 步骤 4：验证更新

```bash
curl http://localhost:5000/api/self-update/health
```

## 📊 调用示例

### C# HttpClient

```csharp
// 上传
using var form = new MultipartFormDataContent();
using var fileStream = new FileStream("update.zip", FileMode.Open);
form.Add(new StreamContent(fileStream), "file", "update.zip");
var response = await client.PostAsync("http://localhost/api/self-update/upload", form);

// 触发
await client.PostAsync("http://localhost/api/self-update/apply", null);
```

### PowerShell

```powershell
# 上传
$FilePath = "C:\update.zip"
$Uri = "http://localhost/api/self-update/upload"
$FileStream = [IO.File]::OpenRead($FilePath)
$Form = @{file=$FileStream}
Invoke-WebRequest -Uri $Uri -Method Post -Form $Form

# 触发
Invoke-WebRequest -Uri "http://localhost/api/self-update/apply" -Method Post
```

### Python

```python
import requests

# 上传
with open('update.zip', 'rb') as f:
    files = {'file': f}
    response = requests.post('http://localhost/api/self-update/upload', files=files)
    print(response.json())

# 触发
response = requests.post('http://localhost/api/self-update/apply')
print(response.json())
```

## 🔍 故障排查

### 问题：更新失败，提示"需要管理员权限"

**检查项**：
1. Updater.exe 是否以管理员身份运行
2. 检查日志：`[Updater目录]\logs\updater_YYYYMMDD.txt`
3. 检查配置中的 `updaterExePath` 是否正确

### 问题：更新包上传成功但无法触发

**检查项**：
1. 确保 `updaterExePath` 指向正确的 Updater.exe
2. 检查该文件是否存在且可访问
3. Web 程序是否有权限启动进程

### 问题：更新超时

**检查项**：
1. 增加 `updateTimeoutMs`（默认 300000ms = 5分钟）
2. 查看 Updater.exe 日志，确定在哪一步超时
3. 检查硬盘空间是否足够

### 问题：Web 更新后无法启动

**手动恢复**：
```bash
# 查看备份
dir C:\WebUpdater\backup\

# 恢复最新备份
xcopy C:\WebUpdater\backup\web_时间戳\* [WebRoot]\ /Y /E

# 重启 Web 程序
```

## 🔐 安全建议

1. **API 认证**：建议在生产环境添加身份验证（JWT/OAuth2）
2. **HTTPS**：生产环境必须使用 HTTPS
3. **包验证**：可添加包签名验证（MD5/SHA256）
4. **访问控制**：限制 API 访问 IP
5. **日志审计**：定期审查 Updater 日志

## 📝 关键设计原则

### ✅ 已遵循

1. **分进程架构**：Web 和 Updater 完全分离
2. **无自修改**：Web 程序不直接修改自身文件
3. **自动备份**：更新前必须备份
4. **自动回滚**：出错自动恢复备份
5. **完整日志**：所有操作都有详细日志
6. **超时保护**：防止更新卡死
7. **多宿主支持**：IIS、Service、Kestrel
8. **管理员权限**：Updater 运行需要提升权限

### ❌ 禁止事项

- ❌ Web 直接修改自身 exe/dll
- ❌ Web 在运行中解压替换
- ❌ Web Kill 自己的进程
- ❌ 在 Web 进程内执行更新

## 📚 详细文档

完整的使用指南、配置说明、API 文档见：

[Web 自更新系统完整文档](./modules/31-self-update.md)

## 🎯 架构图

```
用户界面
    ↓
Web API (SelfUpdateController)
    ├─ POST /api/self-update/upload → SelfUpdateService
    └─ POST /api/self-update/apply → Process.Start(Updater.exe)
    ↓
Updater.exe (独立进程)
    ├─ HostManager.StopAsync()        → IIS/Service/Kestrel
    ├─ BackupWebDirectoryAsync()      → backup/web_timestamp
    ├─ ExtractUpdatePackageAsync()    → 临时目录
    ├─ ReplaceWebFilesAsync()         → Web 根目录
    ├─ ValidateUpdateAsync()          → 验证新版本
    ├─ HostManager.StartAsync()       → IIS/Service/Kestrel
    └─ RollbackAsync()                → 失败时恢复备份
    ↓
Web 程序恢复运行
```

## ✨ 完成清单

- [x] Web Controller (上传、触发、查询)
- [x] Web Service (业务逻辑)
- [x] Updater 程序 (独立 Console 项目)
- [x] 宿主管理器 (支持 IIS、Service、Kestrel)
- [x] 备份回滚机制
- [x] 日志记录
- [x] 配置管理
- [x] 多宿主支持
- [x] 命令行参数解析
- [x] 超时控制
- [x] 完整文档

---

**本实现遵循企业级规范，中文注释，生产可用。**
