# 自动拷贝文件功能（AutoCopy）

## 概述

自动拷贝文件功能允许用户配置源目录和目标驱动器，系统会将指定源目录中的文件自动复制到目标位置，保留源文件不动。

## 功能特性

- **智能路径处理**: 自动在目标驱动器创建相同的目录结构
- **覆盖策略**: 支持跳过、覆盖、重命名等多种冲突处理方式
- **增量备份**: 支持增量拷贝标记，避免重复拷贝
- **日志记录**: 记录拷贝操作的历史记录
- **JSON 配置**: 基于 JSON 格式的配置管理
- **跨磁盘支持**: 支持跨不同磁盘备份文件

## 技术架构

### 后端设计

#### 控制器层 (Controllers/AutoCopyFileController.cs)
- `Index()`: 返回 ViewModel，包含可用驱动器和配置列表
- `GetConfigsHtml()`: 返回配置列表的 Partial View HTML
- `GetConfigs()`: API 端点，返回 JSON 配置数据
- `SaveConfig()`: 保存或更新配置
- `DeleteConfig()`: 删除配置
- `GetDrives()`: 获取可用驱动器列表

#### 业务逻辑层 (Services/AutoCopyFile.cs)
- 配置读写管理
- 文件拷贝执行
- 目录结构创建
- 冲突处理
- 增量标记管理

#### 数据模型
```csharp
// Models/AutoCopyFilePageViewModel.cs
public class AutoCopyFilePageViewModel
{
    public List<DriveOption> AvailableDrives { get; set; }  // 可用驱动器
    public List<AutoCopyConfigDisplay> Configs { get; set; }  // 配置列表
}

public class DriveOption
{
    public string Name { get; set; }      // 驱动器名称
    public string Label { get; set; }     // 驱动器标签
}

public class AutoCopyConfigDisplay
{
    public string SourceDirectory { get; set; }    // 源目录
    public string TargetDrive { get; set; }        // 目标驱动器
    public string? CopiedDirectory { get; set; }   // 已拷贝的目录标记
    public string? OverflowStrategy { get; set; }  // 溢出策略
}
```

### 前端设计

#### 视图结构 (Views/AutoCopyFile/)
- **Index.cshtml**: 主页面
  - 配置表单区域
  - 驱动器选择下拉框
  - 配置列表区域（使用 Partial View）
  
- **_ConfigList.cshtml**: 配置列表 Partial View
  - 卡片形式展示配置
  - 编辑/删除操作按钮

#### JavaScript 交互 (Views/AutoCopyFile/Index.cshtml)
- **事件委托**: 使用事件委托监听编辑/删除按钮
- **表单验证**: 客户端验证源目录和目标驱动器
- **动态刷新**: 操作后刷新配置列表
- **下拉框管理**: 动态加载可用驱动器

## 配置文件

### 位置
`ini_config/AutoCopy.json`

### 格式
```json
[
  {
    "SourceDirectory": "C:\\Projects",
    "TargetDrive": "E:",
    "CopiedDirectory": "",
    "OverflowStrategy": "Rename"
  }
]
```

### 字段说明
| 字段 | 类型 | 说明 |
|-----|-----|-----|
| SourceDirectory | string | 源目录路径 |
| TargetDrive | string | 目标驱动器（如 E:） |
| CopiedDirectory | string | 标记已拷贝的目录（用于增量处理） |
| OverflowStrategy | string | 文件冲突处理策略 |

## 功能流程

### 配置保存流程
1. 用户填写源目录和选择目标驱动器
2. 客户端验证表单数据
3. 发送 POST 请求到 `/AutoCopyFile/SaveConfig`
4. 服务器保存配置到 JSON 文件
5. 返回成功/失败消息
6. 客户端刷新配置列表
7. 显示操作结果通知

### 配置编辑流程
1. 用户点击"编辑"按钮
2. JavaScript 获取该配置的源目录
3. 发送 GET 请求到 `/AutoCopyFile/GetConfigs` 获取配置数据
4. 将配置数据填入表单
5. 禁用源目录输入框
6. 按钮文本变更为"更新配置"
7. 用户修改后点击"更新"按钮保存

### 增量备份流程
1. 首次拷贝：CopiedDirectory 为空
2. 系统拷贝源目录中的所有文件
3. 完成后在配置中记录 CopiedDirectory 的值
4. 下次拷贝时：
   - 系统检查 CopiedDirectory 标记
   - 仅拷贝该目录之后新增的文件
   - 更新标记值

## 架构要点

### Razor 视图规范遵循
✅ 不在 JavaScript 中拼接 HTML
✅ 使用 Partial View 组件化视图
✅ 使用专属 ViewModel 定义视图数据
✅ HTML 结构由服务端 Razor 引擎生成
✅ JavaScript 仅处理交互逻辑

### 配置管理
- 配置以 JSON 格式存储在 `ini_config/AutoCopy.json`
- 使用 System.Text.Json 进行序列化/反序列化
- 支持增量标记（CopiedDirectory）进行续传

### 与 AutoMove 的差异
| 特性 | AutoCopy | AutoMove |
|-----|---------|---------|
| 操作 | 复制文件 | 移动文件 |
| 源文件 | 保留 | 删除 |
| 场景 | 备份、同步 | 清理、整理 |
| 配置字段 | CopiedDirectory | MovedDirectory |

## 最近更新

### 2025-01-08
- ✅ 完成 Razor 视图重构，符合企业级架构标准
- ✅ 创建 AutoCopyFilePageViewModel，明确定义视图数据
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
| Controllers/AutoCopyFileController.cs | 控制器逻辑 |
| Services/AutoCopyFile.cs | 业务逻辑 |
| Models/AutoCopyFilePageViewModel.cs | 视图模型 |
| Views/AutoCopyFile/Index.cshtml | 主视图 |
| Views/AutoCopyFile/_ConfigList.cshtml | 配置列表 Partial View |
| ini_config/AutoCopy.json | 配置文件 |

## 后续改进

- [ ] 支持拷贝日志记录
- [ ] 支持拷贝预览功能
- [ ] 支持增量同步优化（基于文件哈希值）
- [ ] 支持拷贝前验证（空间检查）
- [ ] 添加拷贝规则编辑器（正则表达式支持）
- [ ] 支持压缩备份
