# TSysWatch 项目文档

## 概述

TSysWatch 是一个 ASP.NET Core Web 应用，提供系统监控、CPU 核心绑定、文件自动管理等功能。

## 🏠 首页导航

首页采用**美观的卡片式导航设计**，提供所有工具的快速访问：
- **自动化工具**：自动删除、自动拷贝、自动移动、数据库修复
- **系统管理**：CPU 核心管理、硬件监控、文件管理

📄 [首页设计详细说明](HOME_PAGE_REDESIGN.md) | 📊 [完成总结](HOMEPAGE_COMPLETION.md)

## 模块导航

本项目采用模块化架构，各模块独立文档如下：

### 文件管理模块
- [自动删除文件功能](modules/01-autodeletefile.md)
  - ✨ 支持下载删除历史记录
- [自动移动文件功能](modules/02-automovefile.md)
  - ✨ 支持下载移动历史记录
- [自动拷贝文件功能](modules/03-autocopyfile.md)
  - ✨ 支持下载拷贝历史记录
- **📎 [下载功能实现指南](modules/FILE_OPERATIONS_UI_COMPLETION.md)** - 三个模块统一的下载历史记录 UI 实现
- **📊 [完成总结报告](FILE_OPERATIONS_COMPLETION_SUMMARY.md)** - 项目功能完成情况总结

### 系统监控模块
- [CPU 核心绑定功能](modules/10-cpucore.md)
- [硬件监控与定时记录功能](modules/11-hardwaremonitor.md)

### 基础设施模块
- [数据库维修功能](modules/20-dbrepair.md)
- [内存优化与内存泄漏修复](modules/30-memory-optimization.md)
- [端口检测与自适应启动](modules/32-port-detection.md) - 自动检测端口占用并切换可用端口
- [**Web 自自动更新系统**](modules/self-update.md) - Web 程序主程式化更新，独立 Updater.exe 架构
  - 🎯 [功能说明及使用指南](modules/self-update.md)

## 架构设计

### 分层架构
- **Controllers**: 处理 HTTP 请求/响应，调用业务逻辑
- **Services**: 业务逻辑实现，数据持久化
- **Models/ViewModels**: 数据模型和视图模型
- **Views**: Razor 视图，HTML 结构由 Razor 生成
- **JavaScript**: 前端交互逻辑，通过 Fetch API 与后端通信

### Razor 视图规范
遵循企业级 Razor 标准（razor.md）：
- 每个页面使用专属的 ViewModel，明确定义视图数据模型
- HTML 结构由 Razor 引擎生成（服务端渲染），JavaScript 不得包含 HTML 拼接
- 使用 Partial View 组件化视图，提高代码复用性
- JavaScript 仅负责事件处理和 API 调用，不负责 HTML 生成

### 配置管理
- 配置文件位置: `ini_config/{ModuleName}.json`
- 配置格式: JSON（System.Text.Json）
- 各模块独立管理自己的配置文件

## 开发规范

### 代码质量
- 遵循 SOLID 原则
- 所有公共 API 使用 XML 注释（中文）
- 启用可空引用类型检查
- 使用 async/await 处理 I/O 操作

### 提交规范
- 编程完成后必须进行 Code Review
- 所有功能变更需要更新对应模块文档
- 文档与代码必须保持同步

## 快速开始

### 环境要求
- .NET 8.0+
- Windows 系统（部分功能依赖 Windows API）

### 构建与运行
```bash
dotnet build
dotnet run
```

### 项目结构
```
TSysWatch/
├── Controllers/          # 控制器
├── Services/            # 业务逻辑服务
├── Models/              # 数据模型
├── Views/               # Razor 视图
├── wwwroot/             # 静态资源
├── ini_config/          # JSON 配置文件
├── doc/                 # 文档目录
│   ├── modules/         # 模块文档
│   └── readme.md        # 本文档
└── TSysWatch.csproj     # 项目文件
```
