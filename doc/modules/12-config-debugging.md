# 硬件监控配置保存问题诊断指南

## 问题症状
配置在保存后重启应用，配置信息丢失，恢复到默认状态。

## 根本原因
配置文件未被正确保存到磁盘，或在应用重启时未被正确加载。

## 解决方案

### 1. 配置文件位置
配置文件应该保存在：`{应用运行目录}/ini_config/HardwareMonitor.json`

例如：`E:\project\2025\TSysWatch\TSysWatch\bin\Debug\net8.0\ini_config\HardwareMonitor.json`

### 2. 检查日志
应用会输出详细的日志信息。查看应用的日志文件或控制台输出：

**正常流程日志：**
```
信息: 尝试加载配置文件: C:\app\ini_config\HardwareMonitor.json
信息: 配置加载成功: EnableTimedRecording=true, Interval=10s

信息: 保存配置到: C:\app\ini_config\HardwareMonitor.json
信息: 配置内容: {...json...}
信息: 配置已成功保存, 文件大小: 245 字节
```

**异常日志：**
```
警告: 配置文件不存在: C:\app\ini_config\HardwareMonitor.json
错误: 保存硬件监控配置失败: 访问被拒绝
```

### 3. 改进的初始化流程

应用启动时：
1. `HardwareMonitorConfigManager` 被创建
2. 自动在构造函数中调用 `LoadConfig()`
3. 如果文件存在，加载保存的配置
4. 如果文件不存在，使用默认配置并保存新文件

### 4. 配置保存流程

用户在Web界面保存配置时：
1. 前端发送配置到 `/HardwareMonitor/SaveRecordingConfig`
2. `SaveRecordingConfig()` API 方法接收配置
3. 调用 `_configManager.SaveConfig()`
4. 配置被写入JSON文件
5. 文件被验证成功写入（检查文件大小）
6. 内存中的配置也被更新

### 5. 常见问题排查

#### 问题：目录未创建
**症状**：无法保存配置，报错 "路径不存在"
**解决**：
- 确保应用具有创建目录的权限
- 手动创建 `ini_config` 目录

#### 问题：文件权限不足
**症状**：保存时报错 "访问被拒绝"
**解决**：
- 检查应用运行用户是否有该目录的写入权限
- Windows：在目录上右键 → 属性 → 安全 → 编辑权限

#### 问题：JSON 序列化失败
**症状**：保存失败，配置无法序列化
**解决**：
- 检查 `RecordedSensorTypes` 列表是否为空或包含无效值
- 确保所有属性都有默认值

#### 问题：配置文件被占用
**症状**：保存失败，文件被其他进程占用
**解决**：
- 确保没有其他程序打开该JSON文件
- 关闭可能编辑该文件的编辑器

### 6. 手动验证

可以直接查看配置文件内容：

```bash
# Windows PowerShell
Get-Content ".\ini_config\HardwareMonitor.json" | ConvertFrom-Json

# 应该看到类似输出：
# PSCustomObject
# @{EnableTimedRecording=True; RecordingIntervalSeconds=10; CsvStoragePath=HardwareData; RecordedSensorTypes=System.Object[]}
```

### 7. 默认配置示例

如果没有配置文件，应用会创建以下默认配置：

```json
{
  "EnableTimedRecording": false,
  "RecordingIntervalSeconds": 10,
  "CsvStoragePath": "HardwareData",
  "RecordedSensorTypes": []
}
```

## 核心改进

已实现以下改进确保配置正确保存：

1. **初始化加载** - 应用启动时自动加载配置
2. **详细日志** - 记录所有配置操作，便于调试
3. **验证写入** - 保存后验证文件确实被写入
4. **错误处理** - 完善的异常日志记录

## 测试步骤

1. 启动应用
2. 打开硬件监控页面
3. 点击 "⚙️ 配置" 按钮
4. 启用定时记录，设置间隔为 60 秒
5. 选择几个传感器类型
6. 点击 "保存配置"
7. 确认弹框显示 "配置保存成功"
8. 完全关闭应用
9. 重新启动应用
10. 再次打开 "⚙️ 配置"
11. **验证**：刚才的配置应该被恢复

如果步骤 11 中配置未恢复，请检查日志输出中是否有错误信息。
