# 自动移动文件功能（AutoMove）

## 概述

自动移动文件功能允许用户配置源目录和目标驱动器，系统会将指定源目录中的文件自动移动到目标位置。

## 功能特性

- **智能路径处理**: 自动在目标驱动器创建相同的目录结构
- **覆盖策略**: 支持跳过、覆盖、重命名等多种冲突处理方式
- **日志记录**: 记录移动操作的历史记录
- **JSON 配置**: 基于 JSON 格式的配置管理
- **跨磁盘支持**: 支持在不同磁盘间移动文件

## 技术架构

### 后端设计

#### 控制器层 (Controllers/AutoMoveFileController.cs)
- `Index()`: 返回 ViewModel，包含可用驱动器和配置列表
- `GetConfigsHtml()`: 返回配置列表的 Partial View HTML
- `GetConfigs()`: API 端点，返回 JSON 配置数据
- `SaveConfig()`: 保存或更新配置
- `DeleteConfig()`: 删除配置
- `GetDrives()`: 获取可用驱动器列表

#### 业务逻辑层 (Services/AutoMoveFile.cs)
- 配置读写管理
- 文件移动执行
- 目录结构创建
- 冲突处理

#### 数据模型
```csharp
// Models/AutoMoveFilePageViewModel.cs
public class AutoMoveFilePageViewModel
{
    public List<DriveOption> AvailableDrives { get; set; }  // 可用驱动器
    public List<AutoMoveConfigDisplay> Configs { get; set; }  // 配置列表
}

public class DriveOption
{
    public string Name { get; set; }      // 驱动器名称
    public string Label { get; set; }     // 驱动器标签
}

public class AutoMoveConfigDisplay
{
    public string SourceDirectory { get; set; }    // 源目录
    public string TargetDrive { get; set; }        // 目标驱动器
    public string? MovedDirectory { get; set; }    // 已移动的目录标记
    public string? OverflowStrategy { get; set; }  // 溢出策略
}
```

### 前端设计

#### 视图结构 (Views/AutoMoveFile/)
- **Index.cshtml**: 主页面
  - 配置表单区域
  - 驱动器选择下拉框
  - 配置列表区域（使用 Partial View）
  
- **_ConfigList.cshtml**: 配置列表 Partial View
  - 卡片形式展示配置
  - 编辑/删除操作按钮

#### JavaScript 交互 (Views/AutoMoveFile/Index.cshtml)
- **事件委托**: 使用事件委托监听编辑/删除按钮
- **表单验证**: 客户端验证源目录和目标驱动器
- **动态刷新**: 操作后刷新配置列表

## 配置文件

### 位置
`ini_config/AutoMove.json`

### 格式
```json
[
  {
    "SourceDirectory": "C:\\Users\\Downloads",
    "TargetDrive": "D:",
    "MovedDirectory": "",
    "OverflowStrategy": "Rename"
  }
]
```

### 字段说明
| 字段 | 类型 | 说明 |
|-----|-----|-----|
| SourceDirectory | string | 源目录路径 |
| TargetDrive | string | 目标驱动器（如 D:） |
| MovedDirectory | string | 标记已移动的目录（用于增量处理） |
| OverflowStrategy | string | 文件冲突处理策略 |

## 架构要点

### Razor 视图规范遵循
✅ 不在 JavaScript 中拼接 HTML
✅ 使用 Partial View 组件化视图
✅ 使用专属 ViewModel 定义视图数据
✅ HTML 结构由服务端 Razor 引擎生成
✅ JavaScript 仅处理交互逻辑

### 配置管理
- 配置以 JSON 格式存储在 `ini_config/AutoMove.json`
- 使用 System.Text.Json 进行序列化/反序列化
- 支持增量标记（MovedDirectory）进行续传

## 最近更新

### 2025-01-08
- ✅ 完成 Razor 视图重构，符合企业级架构标准
- ✅ 创建 AutoMoveFilePageViewModel，明确定义视图数据
- ✅ 创建 Partial View (_ConfigList.cshtml)
- ✅ 添加 GetConfigsHtml() API 端点
- ✅ 简化 JavaScript，仅负责事件和 API 调用
- ✅ 实现事件委托模式，避免 onclick 属性
- ✅ 编译验证通过（0 errors）

### 历史更新
- JSON 配置格式迁移（之前使用 INI 格式）
- 功能实现完成

## 相关文件

| 文件 | 用途 |
|-----|-----|
| Controllers/AutoMoveFileController.cs | 控制器逻辑 |
| Services/AutoMoveFile.cs | 业务逻辑 |
| Models/AutoMoveFilePageViewModel.cs | 视图模型 |
| Views/AutoMoveFile/Index.cshtml | 主视图 |
| Views/AutoMoveFile/_ConfigList.cshtml | 配置列表 Partial View |
| ini_config/AutoMove.json | 配置文件 |

## 后续改进

- [ ] 支持移动日志记录
- [ ] 支持移动预览功能
- [ ] 支持增量同步（仅移动新增文件）
- [ ] 支持移动前备份
- [ ] 添加移动规则编辑器（正则表达式支持）
