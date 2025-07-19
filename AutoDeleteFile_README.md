# 自动删除文件功能使用说明

## 概述
本功能实现了基于磁盘空间监控的自动文件删除功能。当磁盘剩余空间低于设定阈值时，自动删除配置目录中的旧文件，直到磁盘空间恢复到安全水平。

## 功能特点
1. **多磁盘支持**：支持同时监控多个磁盘驱动器
2. **灵活配置**：通过INI配置文件设置删除目录、空间阈值等参数
3. **智能删除**：按文件修改时间排序，优先删除最旧的文件
4. **安全机制**：达到停止删除阈值后自动停止清理
5. **详细日志**：记录详细的删除过程和结果

## 配置文件格式
配置文件路径：`程序目录/AutoDeleteFile.ini`

```ini
# 自动删除文件配置
# 格式：[磁盘驱动器]
# DeleteDirectories=目录1,目录2,目录3
# StartDeleteSizeGB=开始删除时的磁盘剩余空间(GB)
# StopDeleteSizeGB=停止删除时的磁盘剩余空间(GB)

[C:]
DeleteDirectories=C:\temp,C:\Windows\temp,C:\Users\Public\temp
StartDeleteSizeGB=5.0
StopDeleteSizeGB=10.0

[D:]
DeleteDirectories=D:\temp,D:\logs,D:\cache
StartDeleteSizeGB=10.0
StopDeleteSizeGB=20.0
```

## 配置参数说明
- **[驱动器]**：磁盘驱动器标识，如 `[C:]`、`[D:]`
- **DeleteDirectories**：要清理的目录列表，用逗号分隔，支持子目录递归删除
- **StartDeleteSizeGB**：开始删除的磁盘剩余空间阈值（GB）
- **StopDeleteSizeGB**：停止删除的磁盘剩余空间阈值（GB）

## 使用方法

### 1. 启动自动删除功能
```csharp
// 在程序启动时调用
AutoDeleteFile.Start();
```

### 2. 管理配置
```csharp
// 获取当前配置
var configs = AutoDeleteFileManager.GetCurrentConfigs();

// 添加或更新配置
AutoDeleteFileManager.AddOrUpdateConfig("C:", 
    new List<string> { @"C:\temp", @"C:\logs" },
    5.0, 10.0);

// 删除配置
AutoDeleteFileManager.RemoveConfig("C:");
```

### 3. 测试功能
```csharp
// 测试配置
AutoDeleteFileTest.TestConfiguration();

// 添加测试配置
AutoDeleteFileTest.AddTestConfiguration();

// 模拟清理过程
AutoDeleteFileTest.SimulateCleanup();
```

## 工作流程
1. **配置读取**：程序启动时读取INI配置文件
2. **空间监控**：每60秒检查一次磁盘剩余空间
3. **触发清理**：当剩余空间≤开始删除阈值时，开始清理
4. **文件扫描**：扫描配置目录中的所有文件（包括子目录）
5. **排序删除**：按文件修改时间排序，优先删除最旧的文件
6. **监控停止**：当剩余空间≥停止删除阈值时，停止清理

## 安全注意事项
1. **谨慎配置删除目录**：避免配置系统关键目录
2. **合理设置阈值**：确保停止删除阈值大于开始删除阈值
3. **定期检查日志**：监控删除过程是否正常
4. **备份重要文件**：删除前确保重要文件已备份

## 日志记录
程序会记录以下信息：
- 配置文件读取结果
- 磁盘空间检查结果
- 文件删除过程
- 错误和异常信息

日志文件位置：`程序目录/Logs/`

## 示例配置
```ini
# 开发环境配置
[C:]
DeleteDirectories=C:\temp,C:\Windows\temp,C:\Users\%USERNAME%\AppData\Local\Temp
StartDeleteSizeGB=2.0
StopDeleteSizeGB=5.0

# 生产环境配置
[D:]
DeleteDirectories=D:\logs,D:\temp,D:\cache
StartDeleteSizeGB=20.0
StopDeleteSizeGB=50.0
```

## 故障排除
1. **配置文件不存在**：程序会自动创建默认配置文件
2. **目录不存在**：会在日志中记录警告，不影响其他目录的清理
3. **文件删除失败**：会记录错误日志，继续处理其他文件
4. **磁盘不可用**：会跳过该磁盘的清理任务

## API参考
- `AutoDeleteFile.Start()`：启动自动删除功能
- `AutoDeleteFile.Stop()`：停止自动删除功能
- `AutoDeleteFileManager.GetCurrentConfigs()`：获取当前配置
- `AutoDeleteFileManager.SaveConfigs()`：保存配置
- `AutoDeleteFileManager.GetDriveInfos()`：获取磁盘信息