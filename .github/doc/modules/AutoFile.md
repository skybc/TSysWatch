# 自动文件处理模块文档

## 模块概述

自动文件处理模块包含三个功能模块：
- **AutoMoveFile**: 自动移动文件
- **AutoCopyFile**: 自动拷贝文件
- **AutoDeleteFile**: 自动删除文件

## 技术更新记录

### 配置文件目录统一管理（2026-01-07）

统一所有自动文件处理模块的配置文件目录结构：
- 创建独立的 `config` 配置目录，用于集中管理所有INI配置文件
- 自动目录创建：应用启动时如果 `config` 目录不存在，会自动创建
- 配置文件路径：
  - AutoMoveFile.ini → `config/AutoMoveFile.ini`
  - AutoCopyFile.ini → `config/AutoCopyFile.ini`
  - AutoDeleteFile.ini → `config/AutoDeleteFile.ini`

**修改的文件**：
- AutoMoveFile.cs、AutoCopyFile.cs、AutoDeleteFile.cs：添加 `ConfigDirPath` 和 `EnsureConfigDirectory()` 方法
- AutoMoveFileManager.cs、AutoCopyFileManager.cs、AutoDeleteFileManager.cs：统一使用新路径，保存配置时确保目录存在

### AutoDeleteFile 管理页面完善（2026-01-07）

升级了 AutoDeleteFile 的管理界面，实现完整的 CRUD 功能：
- AutoDeleteFileManager 更新为使用 INIParser 库而不是手动字符串解析
- AutoDeleteController 添加 SaveConfig 和 DeleteConfig API 接口
- Index.cshtml 优化为功能完整的配置管理界面

### INIParser 库升级（2026-01-07）

原有的三个自动文件处理模块统一使用 `ini-parser 2.5.2` NuGet包替换手动字符串解析方式，优点包括：
- 减少代码重复，提高可维护性
- 支持INI注释和高级特性
- 更好的error handling

相关代码位置：
- **AutoMoveFile.cs**: Services/AutoMoveFile.cs
- **AutoCopyFile.cs**: Services/AutoCopyFile.cs
- **AutoDeleteFile.cs**: Services/AutoDeleteFile.cs

---

## Web管理页面（2026-01-07）

### AutoMoveFile 管理页面

**访问URL**: `/AutoMoveFile/Index`

#### 功能特性

1. **配置管理**
   - 添加/编辑移动任务
   - 删除移动任务
   - 实时显示配置列表

2. **目录信息展示**
   - 源目录和目标目录存在性检查（绿色/红色标记）
   - 源目录文件数统计
   - 源目录大小显示

3. **移动时间限制**
   - 支持设置时间限制（分钟）
   - 值为0表示无限制，所有文件都会被移动
   - 值>0表示只移动超过N分钟的文件

#### 相关代码文件

- Controller: [AutoMoveFileController.cs](../../Controllers/AutoMoveFileController.cs)
- Manager: [AutoMoveFileManager.cs](../../Services/AutoMoveFileManager.cs)
- View: [Index.cshtml](../../Views/AutoMoveFile/Index.cshtml)

#### API接口

| 方法 | URL | 说明 |
|------|-----|------|
| GET | /AutoMoveFile/Index | 管理页面 |
| GET | /AutoMoveFile/GetConfigs | 获取配置列表 |
| POST | /AutoMoveFile/SaveConfig | 保存/更新配置 |
| POST | /AutoMoveFile/DeleteConfig | 删除配置 |
| POST | /AutoMoveFile/CheckDirectory | 检查目录信息 |

### AutoCopyFile 管理页面

**访问URL**: `/AutoCopyFile/Index`

#### 功能特性

1. **配置管理**
   - 添加/编辑拷贝任务
   - 删除拷贝任务
   - 实时显示配置列表

2. **图片文件统计**
   - 支持的图片格式：.jpg, .jpeg, .png, .bmp, .gif, .tiff, .tif, .webp, .ico
   - 自动统计源目录中的图片文件数
   - 显示源目录总大小

3. **已拷贝文件处理**
   - 配置了移动目录：拷贝完成后将源文件移动到指定目录
   - 未配置移动目录：拷贝完成后删除源文件

#### 相关代码文件

- Controller: [AutoCopyFileController.cs](../../Controllers/AutoCopyFileController.cs)
- Manager: [AutoCopyFileManager.cs](../../Services/AutoCopyFileManager.cs)
- View: [Index.cshtml](../../Views/AutoCopyFile/Index.cshtml)

#### API接口

| 方法 | URL | 说明 |
|------|-----|------|
| GET | /AutoCopyFile/Index | 管理页面 |
| GET | /AutoCopyFile/GetConfigs | 获取配置列表 |
| POST | /AutoCopyFile/SaveConfig | 保存/更新配置 |
| POST | /AutoCopyFile/DeleteConfig | 删除配置 |

### AutoDeleteFile 管理页面

**访问URL**: `/AutoDelete/Index`

#### 功能特性

1. **完整的配置CRUD管理**
   - 添加新的磁盘清理配置
   - 编辑现有配置（驱动器字母除外）
   - 删除磁盘配置
   - 实时显示配置列表

2. **配置表单**
   - 驱动器字母：标识删除任务的目标磁盘（C:, D:, E:等）
   - 删除目录：支持多个目录，用逗号分隔（例：C:\temp,C:\logs）
   - 开始删除：磁盘剩余空间低于此值时开始删除（GB）
   - 停止删除：磁盘剩余空间达到此值时停止删除（GB）
   - 时间阈值：只删除超过N天的文件，0表示不限制时间（天）
   - 逻辑模式：OR（满足任一条件删除）或 AND（同时满足两个条件删除）

3. **磁盘状态监控**
   - 实时显示每个磁盘的总容量
   - 剩余空间显示（GB和格式化字符串）
   - 进度条显示磁盘使用情况
   - 剩余空间低于10GB时红色警示，10-25GB为黄色警告

4. **表格显示**
   - 配置列表以表格形式展示
   - 显示驱动器、删除目录、容量阈值、时间阈值、逻辑模式
   - 每行提供编辑和删除操作按钮

5. **用户交互**
   - 实时表单验证
   - 成功/错误消息提示（5秒自动隐藏）
   - 编辑时禁用驱动器字母修改
   - 删除前确认提示

#### 相关代码文件

- Controller: [AutoDeleteController.cs](../../Controllers/AutoDeleteController.cs)
- Manager: [AutoDeleteFileManager.cs](../../Services/AutoDeleteFileManager.cs)
- View: [Index.cshtml](../../Views/AutoDelete/Index.cshtml)

#### API接口

| 方法 | URL | 说明 |
|------|-----|------|
| GET | /AutoDelete/Index | 管理页面 |
| GET | /AutoDelete/GetConfigs | 获取配置和磁盘信息列表 |
| POST | /AutoDelete/SaveConfig | 保存/更新配置 |
| POST | /AutoDelete/DeleteConfig | 删除配置 |

#### 使用场景

- **OR + 容量阈值**：磁盘空间不足时立即清理文件
- **OR + 时间阈值**：定期清理超过N天的旧文件
- **AND**：只有在磁盘空间不足且文件过旧的情况下才删除（更保守的策略）

### 项目文件结构

```
config/                           # 配置目录（自动创建）
├── AutoMoveFile.ini                  # 移动任务配置
├── AutoCopyFile.ini                  # 拷贝任务配置
└── AutoDeleteFile.ini                # 删除任务配置

Controllers/
├── AutoMoveFileController.cs          # AutoMoveFile管理控制器
├── AutoCopyFileController.cs          # AutoCopyFile管理控制器
└── AutoDeleteController.cs            # AutoDeleteFile管理控制器

Services/
├── AutoMoveFile.cs                    # AutoMoveFile后台服务
├── AutoCopyFile.cs                    # AutoCopyFile后台服务
├── AutoDeleteFile.cs                  # AutoDeleteFile后台服务
├── AutoMoveFileManager.cs             # AutoMoveFile配置管理器
├── AutoCopyFileManager.cs             # AutoCopyFile配置管理器
└── AutoDeleteFileManager.cs           # AutoDeleteFile配置管理器

Views/
├── AutoMoveFile/
│   └── Index.cshtml                   # AutoMoveFile管理页面
├── AutoCopyFile/
│   └── Index.cshtml                   # AutoCopyFile管理页面
└── AutoDelete/
    └── Index.cshtml                   # AutoDeleteFile管理页面
```

### 导航菜单集成

所有管理页面已集成到主导航菜单（`Views/Shared/_TopNav.cshtml`）的"自动化工具"下拉菜单中：
- 自动删除: `/AutoDelete/Index`
- 自动拷贝: `/AutoCopyFile/Index`
- 自动移动: `/AutoMoveFile/Index`

### 技术栈

所有管理页面使用以下技术：
- **UI框架**: Bootstrap 5.x
- **模板引擎**: Razor (ASP.NET Core MVC)
- **脚本语言**: JavaScript (原生Fetch API)
- **通信**: REST API + JSON

---

**最后更新时间**: 2026-01-07  
**维护者**: GitHub Copilot  
**状态**: 已完成（包括管理页面）
