# TSysWatch 项目文档导航

## 模块文档

本项目包含以下主要模块，详细文档请参考对应链接：

### 自动文件处理
- [AutoFile 模块文档](./modules/AutoFile.md)
  - AutoMoveFile: 自动移动文件功能
  - AutoCopyFile: 自动拷贝文件功能
  - AutoDeleteFile: 自动删除文件功能
- [AutoFile 使用指南](./modules/AutoFile使用指南.md) - 快速开始和常见问题

## 技术栈

- **框架**: ASP.NET Core 8.0
- **语言**: C# 12.0+
- **配置管理**: ini-parser 2.5.2
- **日志**: Serilog
- **前端**: Bootstrap 5.x + Razor

## 更新日志

### 2026-01-07
- 升级自动文件处理模块配置读写方式，使用INIParser库替换手动解析
- 创建AutoMoveFile管理页面 (`/AutoMoveFile/Index`)
- 创建AutoCopyFile管理页面 (`/AutoCopyFile/Index`)
- 创建对应的Manager类和API接口
- 详见: [AutoFile 模块文档](./modules/AutoFile.md)

## 快速导航

### 管理页面

| 功能 | URL | 说明 |
|------|-----|------|
| 自动移动 | `/AutoMoveFile/Index` | 配置和管理自动移动任务 |
| 自动拷贝 | `/AutoCopyFile/Index` | 配置和管理自动拷贝任务 |
| 自动删除 | `/AutoDelete/Index` | 配置和管理自动删除任务 |

### 文档

- 详细的模块文档: [AutoFile.md](./modules/AutoFile.md)
- 使用指南和FAQ: [AutoFile使用指南.md](./modules/AutoFile使用指南.md)
- cshtml文件，需要阅读: [razor.md](./razor.md)