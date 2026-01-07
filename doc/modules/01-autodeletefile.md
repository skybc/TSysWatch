# 自动删除文件功能（AutoDelete）

## 概述

自动删除文件功能允许用户配置自动删除规则，当系统磁盘剩余空间低于阈值或文件修改时间超过指定天数时，自动清理指定目录中的文件。

## 功能特性

- **多磁盘支持**: 为不同磁盘配置不同的删除规则
- **灵活的删除条件**: 支持容量阈值和时间阈值的组合
- **逻辑模式**: 支持 OR（满足任一条件）和 AND（同时满足）两种逻辑
- **JSON 配置**: 基于 JSON 格式的配置管理，可视化编辑
- **实时监控**: 显示磁盘空间使用状态

## 技术架构

### 后端设计

#### 控制器层 (Controllers/AutoDeleteController.cs)
- `Index()`: 返回 ViewModel，包含磁盘列表和配置列表
- `GetConfigsHtml()`: 返回配置列表的 Partial View HTML
- `GetDrivesHtml()`: 返回磁盘信息的 Partial View HTML
- `GetConfigs()`: API 端点，返回 JSON 配置数据
- `SaveConfig()`: 保存或更新配置
- `DeleteConfig()`: 删除配置

#### 业务逻辑层 (Services/AutoDeleteFileManager.cs)
- `GetCurrentConfigs()`: 读取配置文件
- `AddOrUpdateConfig()`: 保存配置
- `RemoveConfig()`: 删除配置
- `GetDriveInfos()`: 获取磁盘信息
- `FormatBytes()`: 字节格式化

#### 数据模型
```csharp
// Models/AutoDeleteFilePageViewModel.cs
public class AutoDeleteFilePageViewModel
{
    public List<DriveInfoDisplay> Drives { get; set; }    // 磁盘列表
    public List<DiskCleanupConfigDisplay> Configs { get; set; }  // 配置列表
}

public class DriveInfoDisplay
{
    public string DriveLetter { get; set; }       // 驱动器字母
    public string TotalSize { get; set; }         // 总容量
    public string FreeSpace { get; set; }         // 剩余空间
    public double FreeSpaceGB { get; set; }       // 剩余空间（GB）
}

public class DiskCleanupConfigDisplay
{
    public string DriveLetter { get; set; }           // 驱动器字母
    public List<string> DeleteDirectories { get; set; }  // 删除目录列表
    public double StartDeleteSizeGB { get; set; }    // 开始删除大小阈值
    public double StopDeleteSizeGB { get; set; }     // 停止删除大小阈值
    public int StartDeleteFileDays { get; set; }     // 文件时间阈值
    public string LogicMode { get; set; }            // 逻辑模式 (OR/AND)
}
```

### 前端设计

#### 视图结构 (Views/AutoDelete/)
- **Index.cshtml**: 主页面
  - 配置表单区域
  - 配置列表区域（使用 Partial View）
  - 磁盘状态区域（使用 Partial View）
  
- **_ConfigList.cshtml**: 配置列表 Partial View
  - 表格形式展示配置
  - 编辑/删除操作按钮
  
- **_DrivesList.cshtml**: 磁盘信息 Partial View
  - 磁盘卡片展示
  - 进度条显示使用量

#### JavaScript 交互 (Views/AutoDelete/Index.cshtml)
- **事件委托**: 使用事件委托监听编辑/删除按钮点击
- **动态刷新**: 操作后动态获取 HTML 并更新 DOM
- **表单验证**: 客户端表单验证
- **通知系统**: 操作结果提示

关键函数：
- `saveConfig()`: 保存配置
- `editConfig(event)`: 编辑配置
- `deleteConfigWithConfirm(event)`: 删除配置
- `refreshConfigs()`: 刷新配置和磁盘列表
- `refreshConfigsList()`: 刷新配置列表
- `refreshDrivesList()`: 刷新磁盘列表

## 配置文件

### 位置
`ini_config/AutoDelete.json`

### 格式
```json
[
  {
    "DriveLetter": "C:",
    "DeleteDirectories": [
      "C:\\Temp",
      "C:\\Logs"
    ],
    "StartDeleteSizeGB": 5.0,
    "StopDeleteSizeGB": 10.0,
    "StartDeleteFileDays": 30,
    "LogicMode": "OR"
  }
]
```

### 字段说明
| 字段 | 类型 | 说明 |
|-----|-----|-----|
| DriveLetter | string | 驱动器字母（如 C:） |
| DeleteDirectories | array | 要删除的目录列表 |
| StartDeleteSizeGB | number | 触发删除的磁盘剩余空间阈值 |
| StopDeleteSizeGB | number | 停止删除的磁盘剩余空间阈值 |
| StartDeleteFileDays | int | 删除文件的最小修改天数（0 表示不限制） |
| LogicMode | string | 逻辑模式：OR（或）或 AND（且） |

## 功能流程

### 配置保存流程
1. 用户填写表单
2. 客户端验证表单数据
3. 发送 POST 请求到 `/AutoDelete/SaveConfig`
4. 服务器保存配置到 JSON 文件
5. 返回成功/失败消息
6. 客户端刷新配置列表和磁盘列表
7. 显示操作结果通知

### 配置编辑流程
1. 用户点击"编辑"按钮
2. JavaScript 获取该配置的驱动器字母
3. 发送 GET 请求到 `/AutoDelete/GetConfigs` 获取配置数据
4. 将配置数据填入表单
5. 禁用驱动器字母输入框
6. 按钮文本变更为"更新配置"
7. 用户修改后点击"更新"按钮保存

### 配置删除流程
1. 用户点击"删除"按钮
2. 显示删除确认对话框
3. 用户确认后发送 POST 请求到 `/AutoDelete/DeleteConfig`
4. 服务器删除配置
5. 客户端刷新配置列表

## 架构要点

### Razor 视图规范遵循
✅ 不在 JavaScript 中拼接 HTML
✅ 使用 Partial View 组件化视图
✅ 使用专属 ViewModel 定义视图数据
✅ HTML 结构由服务端 Razor 引擎生成
✅ JavaScript 仅处理交互逻辑

### 动态更新模式
- 编辑/删除操作后，通过 AJAX 获取更新的 Partial View HTML
- 使用事件委托模式监听按钮点击，避免重复绑定
- DOM 更新后自动重新绑定事件监听器

### 性能考虑
- 磁盘信息在页面加载时获取，避免频繁系统调用
- 配置列表支持异步加载
- 使用 JSON 格式配置，减少 I/O 操作

## 最近更新

### 2025-01-08
- ✅ 完成 Razor 视图重构，符合企业级架构标准
- ✅ 创建 AutoDeleteFilePageViewModel，明确定义视图数据模型
- ✅ 创建 Partial View (_ConfigList.cshtml, _DrivesList.cshtml)
- ✅ 添加 GetConfigsHtml() 和 GetDrivesHtml() API 端点
- ✅ 简化 JavaScript，仅负责事件和 API 调用
- ✅ 实现事件委托模式，避免 onclick 属性
- ✅ 编译验证通过（0 errors）

### 历史更新
- JSON 配置格式迁移（之前使用 INI 格式）
- 功能实现完成

## 相关文件

| 文件 | 用途 |
|-----|-----|
| Controllers/AutoDeleteController.cs | 控制器逻辑 |
| Services/AutoDeleteFileManager.cs | 业务逻辑 |
| Models/AutoDeleteFilePageViewModel.cs | 视图模型 |
| Views/AutoDelete/Index.cshtml | 主视图 |
| Views/AutoDelete/_ConfigList.cshtml | 配置列表 Partial View |
| Views/AutoDelete/_DrivesList.cshtml | 磁盘列表 Partial View |
| ini_config/AutoDelete.json | 配置文件 |

## 后续改进

- [ ] 添加定时删除功能（Windows 任务计划集成）
- [ ] 支持删除日志记录
- [ ] 支持删除前备份重要文件
- [ ] 添加高级过滤器（按文件类型、大小等）
- [ ] 支持删除模拟运行（预览）
