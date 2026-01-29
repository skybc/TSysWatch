# 硬件监控与定时记录功能

## 功能概述

硬件监控模块提供实时的系统硬件信息监控和定时自动采集功能，支持以下硬件信息采集：

- **CPU信息**: 温度、时钟频率、负载、功率等
- **GPU信息**: 显卡温度、负载、内存占用等
- **内存信息**: 已用/总容量、利用率等
- **存储设备**: 硬盘容量、读写速度、温度等
- **系统信息**: 主板传感器、电源、电池状态等
- **网络信息**: 网络设备状态

## 架构设计

### 核心服务

#### 1. HardwareMonitorConfigManager
**配置管理服务** - 负责硬件监控配置的读写

```csharp
// 配置文件路径: ini_config/HardwareMonitor.json
{
  "enableTimedRecording": false,          // 是否启用定时记录
  "recordingIntervalSeconds": 10,         // 采集间隔（秒，最小2秒）
  "csvStoragePath": "HardwareData",       // CSV存储目录
  "recordedSensorTypes": []               // 要记录的传感器类型(空表示全部)
}
```

**主要方法**:
- `LoadConfig()` - 加载配置文件
- `SaveConfig(config)` - 保存配置到文件
- `GetConfig()` - 获取当前配置

#### 2. HardwareDataCollectionService
**数据采集服务** - 负责硬件数据的采集和存储

**主要功能**:
- `CollectHardwareData()` - 采集当前硬件信息
- `SaveToCsv(data)` - 保存数据到CSV文件
- `GetCsvFiles(startDate, endDate)` - 查询指定日期范围的CSV文件

**CSV数据格式**:

带有动态列头，支持重复传感器的硬件区分：

```
Timestamp,CPU-Load,CPU-Temperature,GPU-Temperature(NVIDIA GeForce RTX 3080),Memory-Data
2026-01-28 10:30:00,45.2,65.5,72.1,8192
2026-01-28 10:30:10,48.1,66.2,73.5,8240
```

**列头生成规则**:
- 格式: `SensorName-SensorType[（HardwareName）]`
- 若同一传感器有多个硬件，在后面注上硬件名称进行区分
- 若只有一个硬件，不添加硬件名称

#### 3. HardwareDataRecordingService
**后台定时采集服务** - BackgroundService，定时执行采集任务

**工作流程**:
1. 定期检查配置中的EnableTimedRecording标志
2. 若启用，按RecordingIntervalSeconds间隔采集数据
3. 采集的数据保存到CSV文件（按日期分类）
4. 若特定时间内未启用，则进入低频检查模式（10秒检查一次）

### 前端页面

#### 硬件监控主页面 (Index.cshtml)
- **实时监控**: 每5秒刷新一次硬件数据
- **数据展示**: 将硬件信息按CPU、GPU、内存等分类显示
- **状态指示**: 根据传感器值显示正常/警告/严重三种状态

#### 配置弹框
**⚙️ 配置按钮** - 打开配置面板

配置项目:
1. **启用定时记录** (复选框)
   - 启用后显示采集间隔设置
   - 当启用时，后台服务开始定时采集

2. **定时记录间隔** (数字输入, 最小2秒)
   - 允许用户自定义采集频率
   - 不能低于2秒

3. **CSV存储目录** (文本输入)
   - 相对于应用程序目录的路径
   - 默认值: `HardwareData`

4. **传感器类型选择** (复选框列表)
   - **默认全选**: 首次打开时所有传感器类型均被选中
   - **快捷操作**: "全选" 和 "全不选" 按钮
   - 空列表表示记录所有类型的传感器

#### 下载弹框
**📥 下载记录按钮** - 打开历史记录下载面板

功能:
1. **日期范围选择**
   - **默认起始日期**: 当天
   - **默认结束日期**: 当天
   - 用户可修改日期范围

2. **文件查询**: "查询文件" 按钮
   - 显示所有符合条件的CSV文件列表
   - 显示文件大小和创建时间

3. **批量下载**: "下载选中范围" 按钮
   - 将选中日期范围内的所有CSV文件打包为ZIP
   - 下载文件名格式: `HardwareData_YYYY-MM-DD_to_YYYY-MM-DD.zip`

## 控制器API

### HardwareMonitorController

#### 1. GetHardwareContent (GET)
端点: `/HardwareMonitor/GetHardwareContent`

返回当前硬件监控数据的HTML片段（Partial View）

```json
{
  "CpuInfos": [...],
  "GpuInfos": [...],
  "MemoryInfo": {...},
  "StorageInfos": [...],
  ...
}
```

#### 2. GetRecordingConfig (GET)
端点: `/HardwareMonitor/GetRecordingConfig`

获取当前的定时记录配置

```json
{
  "success": true,
  "enableTimedRecording": false,
  "recordingIntervalSeconds": 10,
  "csvStoragePath": "HardwareData",
  "recordedSensorTypes": []
}
```

#### 3. SaveRecordingConfig (POST)
端点: `/HardwareMonitor/SaveRecordingConfig`

保存用户配置

```json
{
  "enableTimedRecording": true,
  "recordingIntervalSeconds": 10,
  "csvStoragePath": "HardwareData",
  "recordedSensorTypes": ["Temperature", "Load", "Voltage"]
}
```

#### 4. GetCsvFiles (GET)
端点: `/HardwareMonitor/GetCsvFiles`

查询指定日期范围内的CSV文件

参数:
- `startDate` (可选): 开始日期 (格式: YYYY-MM-DD)
- `endDate` (可选): 结束日期 (格式: YYYY-MM-DD)

```json
{
  "success": true,
  "files": [
    {
      "name": "HardwareData_2026-01-28.csv",
      "size": 102400,
      "created": "2026-01-28T10:30:00"
    }
  ]
}
```

#### 5. DownloadCsvZip (GET)
端点: `/HardwareMonitor/DownloadCsvZip`

下载指定日期范围内的CSV文件压缩包

参数:
- `startDate` (必需): 开始日期 (格式: YYYY-MM-DD)
- `endDate` (必需): 结束日期 (格式: YYYY-MM-DD)

返回: ZIP压缩文件下载

## 传感器类型

系统支持的传感器类型及其单位：

| 传感器类型 | 英文 | 单位 | 说明 |
|---------|------|------|------|
| 温度 | Temperature | °C | CPU/GPU/主板温度 |
| 电压 | Voltage | V | 电源电压 |
| 风扇 | Fan | RPM | 冷却风扇转速 |
| 功率 | Power | W | 功耗 |
| 负载 | Load | % | CPU/GPU利用率 |
| 时钟 | Clock | MHz | 处理器频率 |
| 数据 | Data | GB | 存储容量 |
| 流量 | Flow | L/h | 液体流量 |
| 液位 | Level | % | 液位百分比 |
| 频率 | Frequency | Hz | 工作频率 |
| 电流 | Current | A | 电流量 |
| 能量 | Energy | J | 能量量 |
| 小数据 | SmallData | MB | 小容量 |
| 吞吐量 | Throughput | Mbps | 网络速度 |
| 时长 | TimeSpan | h | 时间段 |
| 噪声 | Noise | dB | 声音等级 |
| 湿度 | Humidity | % | 相对湿度 |

## 状态等级

硬件监控根据传感器值显示三种状态：

| 状态 | 中文 | 样式 | 判断条件 |
|------|------|------|---------|
| normal | 正常 | 绿色 | 默认状态 |
| warning | 警告 | 黄色 | 如CPU温度>80°C、负载>90%等 |
| critical | 严重 | 红色 | 如CPU温度>95°C等 |

## CSV数据存储

### 目录结构
```
HardwareData/                          # CSV根目录
├── HardwareData_2026-01-28.csv        # 按日期命名
├── HardwareData_2026-01-29.csv
└── ...
```

### 文件特征
- **命名规则**: `HardwareData_YYYY-MM-DD.csv`
- **一天一个文件**: 同一天的数据写入同一文件
- **追加模式**: 新数据追加到文件末尾
- **编码**: UTF-8

## 关键改进

### 相比之前的改进
1. **删除了MonitorWindow** - 移除了不必要的后台监控线程
2. **支持配置化采集** - 用户可在Web界面配置和控制采集
3. **灵活的传感器选择** - 用户可选择需要记录的传感器类型
4. **优化的CSV格式** - 支持多硬件时的列头自动区分
5. **一日一文件** - 便于按日期管理和分析数据
6. **压缩包下载** - 支持批量下载多天的历史数据

## 性能考虑

1. **采集频率**: 最小间隔2秒，避免过频采集导致系统负担
2. **CSV写入**: 批量写入数据，而非逐条提交
3. **内存管理**: 后台服务定期清理临时对象
4. **文件大小**: CSV文件按天分割，单个文件不会过大

## 配置持久化机制

### 配置存储流程

1. **应用启动时**:
   - HardwareMonitorConfigManager 在依赖注入时被创建
   - 构造函数自动调用 `LoadConfig()` 加载配置文件
   - 若文件存在，加载保存的配置；若不存在，使用默认配置

2. **用户修改配置时**:
   - 前端提交配置变更到 `HardwareMonitor/SaveRecordingConfig` API
   - 控制器调用 `_configManager.SaveConfig(config)`
   - 配置被序列化为JSON并写入 `ini_config/HardwareMonitor.json`
   - 文件写入后进行验证，确保数据被持久化

3. **应用重启后**:
   - 应用启动时再次调用 `LoadConfig()`
   - 从文件读取之前保存的配置
   - 配置恢复到重启前的状态

### 配置持久化验证

配置管理器实现了以下验证机制：

- **加载验证**: 检查文件是否存在，JSON格式是否有效
- **保存验证**: 写入文件后再次读取以确认数据被成功写入
- **错误处理**: 若保存失败，详细的错误日志便于诊断问题

### 日志记录

配置操作会产生详细的日志，位于应用的日志输出中：

```
信息: 尝试加载配置文件: E:\app\ini_config\HardwareMonitor.json
信息: 配置文件内容: {...json内容...}
信息: 配置加载成功: EnableTimedRecording=True, Interval=10s

信息: 保存配置到: E:\app\ini_config\HardwareMonitor.json
信息: 配置内容: {...json内容...}
信息: 配置已成功保存, 文件大小: 245 字节
```

## 故障排查

### 常见问题

#### 1. "配置保存失败"
**原因**: ini_config目录不存在或无写入权限
**解决**: 
- 确保应用程序有该目录的写入权限
- 检查应用日志中的错误信息
- 手动创建 `ini_config` 目录并检查权限

#### 2. 配置重启后丢失
**原因**: 配置文件未被正确写入或应用未能正确加载
**解决**:
- 检查日志输出中是否有 "配置加载成功" 信息
- 验证 `ini_config/HardwareMonitor.json` 文件是否存在
- 检查文件内容是否为有效的JSON格式
- 确保文件权限允许读取

#### 3. CSV文件为空
**原因**: 定时采集未启用或采集间隔未到期
**解决**: 检查配置中的EnableTimedRecording是否为true

#### 4. 下载ZIP失败
**原因**: 指定日期范围内没有CSV文件
**解决**: 检查文件是否存在或日期范围是否正确

## 配置示例

### 完整配置文件 $(ini_config/HardwareMonitor.json)

```json
{
  "enableTimedRecording": true,
  "recordingIntervalSeconds": 60,
  "csvStoragePath": "HardwareData",
  "recordedSensorTypes": [
    "Temperature",
    "Load",
    "Voltage",
    "Fan",
    "Power"
  ]
}
```

此配置将：
- 每60秒采集一次数据
- 只记录温度、负载、电压、风扇和功率五种传感器
- 将数据存储在应用目录下的HardwareData文件夹

## 相关文件

- **配置管理**: `Services/HardwareMonitorConfigManager.cs`
- **数据采集**: `Services/HardwareDataCollectionService.cs`
- **后台服务**: `Services/HardwareDataRecordingService.cs`
- **控制器**: `Controllers/HardwareMonitorController.cs`
- **视图模型**: `Models/HardwareMonitorViewModel.cs`
- **主视图**: `Views/HardwareMonitor/Index.cshtml`
- **内容片段**: `Views/HardwareMonitor/_HardwareContent.cshtml`
- **配置文件**: `ini_config/HardwareMonitor.json`

## 最后更新

**日期**: 2026-01-28  
**更新内容**: 
- 新增硬件定时采集功能
- 实现配置弹框UI
- 实现历史记录下载功能
- 支持CSV格式的数据存储和压缩包下载
- 删除了MonitorWindow类及相关后台监控逻辑
